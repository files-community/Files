using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using Files.Backend.Services;
using Files.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage;

namespace Files.App.ServicesImplementation
{
	public sealed class SideloadUpdateService : ObservableObject, IUpdateService, IDisposable
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint RegisterApplicationRestart(string pwzCommandLine, int dwFlags);

		private const string SIDELOAD_STABLE = "https://cdn.files.community/files/stable/Files.Package.appinstaller";
		private const string SIDELOAD_PREVIEW = "https://cdn.files.community/files/preview/Files.Package.appinstaller";

		private readonly HttpClient _client = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(1) });

		private readonly Dictionary<string, string> _sideloadVersion = new()
		{
			{ "Files", SIDELOAD_STABLE },
			{ "FilesPreview", SIDELOAD_PREVIEW }
		};

		private const string TEMPORARY_UPDATE_PACKAGE_NAME = "UpdatePackage.msix";

		private ILogger? Logger { get; } = Ioc.Default.GetRequiredService<ILogger<App>>();

		private string PackageName { get; } = Package.Current.Id.Name;

		private Version PackageVersion { get; } = new(
			Package.Current.Id.Version.Major,
			Package.Current.Id.Version.Minor,
			Package.Current.Id.Version.Build,
			Package.Current.Id.Version.Revision);

		private Uri? DownloadUri { get; set; }

		private bool _isUpdateAvailable;
		public bool IsUpdateAvailable
		{
			get => _isUpdateAvailable;
			private set => SetProperty(ref _isUpdateAvailable, value);
		}

		private bool _isUpdating;
		public bool IsUpdating
		{
			get => _isUpdating;
			private set => SetProperty(ref _isUpdating, value);
		}

		public bool IsAppUpdated
		{
			get => SystemInformation.Instance.IsAppUpdated;
		}

		private bool _isReleaseNotesAvailable;
		public bool IsReleaseNotesAvailable
		{
			get => _isReleaseNotesAvailable;
			private set => SetProperty(ref _isReleaseNotesAvailable, value);
		}
		public async Task DownloadUpdates()
		{
			await ApplyPackageUpdate();
		}

		public Task DownloadMandatoryUpdates()
		{
			return Task.CompletedTask;
		}

		public async Task<string?> GetLatestReleaseNotesAsync(CancellationToken cancellationToken = default)
		{
			var applicationVersion = $"{SystemInformation.Instance.ApplicationVersion.Major}.{SystemInformation.Instance.ApplicationVersion.Minor}.{SystemInformation.Instance.ApplicationVersion.Build}";
			var releaseNotesLocation = string.Concat("https://raw.githubusercontent.com/files-community/Release-Notes/main/", applicationVersion, ".md");

			using var client = new HttpClient();

			try
			{
				var result = await client.GetStringAsync(releaseNotesLocation, cancellationToken);
				return result == string.Empty ? null : result;
			}
			catch
			{
				return null;
			}
		}

		public async Task CheckLatestReleaseNotesAsync(CancellationToken cancellationToken = default)
		{
			if (!IsAppUpdated)
				return;

			var result = await GetLatestReleaseNotesAsync();
			if (result is not null)
				IsReleaseNotesAvailable = true;
		}

		public async Task CheckForUpdates()
		{
			try
			{
				Logger?.LogInformation($"SIDELOAD: Checking for updates...");

				await using var stream = await _client.GetStreamAsync(_sideloadVersion[PackageName]);

				// Deserialize AppInstaller.
				XmlSerializer xml = new XmlSerializer(typeof(AppInstaller));
				var appInstaller = (AppInstaller?)xml.Deserialize(stream);

				if (appInstaller is null)
					throw new ArgumentNullException(nameof(appInstaller));

				var remoteVersion = new Version(appInstaller.Version);

				Logger?.LogInformation($"SIDELOAD: Current Package Name: {PackageName}");
				Logger?.LogInformation($"SIDELOAD: Remote Package Name: {appInstaller.MainBundle.Name}");
				Logger?.LogInformation($"SIDELOAD: Current Version: {PackageVersion}");
				Logger?.LogInformation($"SIDELOAD: Remote Version: {remoteVersion}");

				// Check details and version number
				if (appInstaller.MainBundle.Name.Equals(PackageName) && remoteVersion.CompareTo(PackageVersion) > 0)
				{
					Logger?.LogInformation("SIDELOAD: Update found.");
					Logger?.LogInformation("SIDELOAD: Starting background download.");
					DownloadUri = new Uri(appInstaller.MainBundle.Uri);
					await StartBackgroundDownload();
				}
				else
				{
					Logger?.LogWarning("SIDELOAD: Update not found.");
					IsUpdateAvailable = false;
				}
			}
			catch (Exception e)
			{
				Logger?.LogError(e, e.Message);
			}
		}

		public async Task CheckAndUpdateFilesLauncherAsync()
		{
			var destFolderPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Files");
			var destExeFilePath = Path.Combine(destFolderPath, "FilesLauncher.exe");

			if (Path.Exists(destExeFilePath))
			{
				var hashEqual = false;
				var srcHashFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/FilesLauncher.exe.sha256"));
				var destHashFilePath = Path.Combine(destFolderPath, "FilesLauncher.exe.sha256");

				if (Path.Exists(destHashFilePath))
				{
					using var srcStream = (await srcHashFile.OpenReadAsync()).AsStream();
					using var destStream = File.OpenRead(destHashFilePath);

					hashEqual = HashEqual(srcStream, destStream);
				}

				if (!hashEqual)
				{
					var srcExeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/FilesLauncher.exe"));
					var destFolder = await StorageFolder.GetFolderFromPathAsync(destFolderPath);

					await srcExeFile.CopyAsync(destFolder, "FilesLauncher.exe", NameCollisionOption.ReplaceExisting);
					await srcHashFile.CopyAsync(destFolder, "FilesLauncher.exe.sha256", NameCollisionOption.ReplaceExisting);

					App.Logger.LogInformation("FilesLauncher updated.");
				}
			}

			bool HashEqual(Stream a, Stream b)
			{
				Span<byte> bufferA = stackalloc byte[64];
				Span<byte> bufferB = stackalloc byte[64];

				a.Read(bufferA);
				b.Read(bufferB);

				return bufferA.SequenceEqual(bufferB);
			}
		}

		private async Task StartBackgroundDownload()
		{
			try
			{
				var tempDownloadPath = ApplicationData.Current.LocalFolder.Path + "\\" + TEMPORARY_UPDATE_PACKAGE_NAME;

				Stopwatch timer = Stopwatch.StartNew();

				await using (var stream = await _client.GetStreamAsync(DownloadUri))
				await using (var fileStream = new FileStream(tempDownloadPath, FileMode.Create))
					await stream.CopyToAsync(fileStream);

				timer.Stop();
				var timespan = timer.Elapsed;

				Logger?.LogInformation($"Download time taken: {timespan.Hours:00}:{timespan.Minutes:00}:{timespan.Seconds:00}");

				IsUpdateAvailable = true;
			}
			catch (Exception e)
			{
				Logger?.LogError(e, e.Message);
			}
		}

		private async Task ApplyPackageUpdate()
		{
			if (!IsUpdateAvailable)
				return;

			IsUpdating = true;

			PackageManager pm = new PackageManager();
			DeploymentResult? result = null;

			try
			{
				var restartStatus = RegisterApplicationRestart(null, 0);

				Logger?.LogInformation($"Register for restart: {restartStatus}");

				await Task.Run(async () =>
				{
					var bundlePath = new Uri(ApplicationData.Current.LocalFolder.Path + "\\" + TEMPORARY_UPDATE_PACKAGE_NAME);

					var deployment = pm.RequestAddPackageAsync(
						bundlePath,
						null,
						DeploymentOptions.ForceApplicationShutdown,
						pm.GetDefaultPackageVolume(),
						null,
						null);

					result = await deployment;
				});
			}
			catch (Exception e)
			{
				if (result?.ExtendedErrorCode is not null)
					Logger?.LogInformation(result.ErrorText);

				Logger?.LogError(e, e.Message);
			}
			finally
			{
				// Reset fields
				IsUpdating = false;
				IsUpdateAvailable = false;
				DownloadUri = null;
			}
		}

		public void Dispose()
		{
			_client?.Dispose();
		}
	}

	/// <summary>
	/// AppInstaller class to hold information about remote updates.
	/// </summary>
	[XmlRoot(ElementName = "AppInstaller", Namespace = "http://schemas.microsoft.com/appx/appinstaller/2018")]
	public sealed class AppInstaller
	{
		[XmlElement("MainBundle")]
		public MainBundle MainBundle { get; set; }

		[XmlAttribute("Uri")]
		public string Uri { get; set; }

		[XmlAttribute("Version")]
		public string Version { get; set; }
	}

	public sealed class MainBundle
	{
		[XmlAttribute("Name")]
		public string Name { get; set; }

		[XmlAttribute("Version")]
		public string Version { get; set; }

		[XmlAttribute("Uri")]
		public string Uri { get; set; }
	}
}
