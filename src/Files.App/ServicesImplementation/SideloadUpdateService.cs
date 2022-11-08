using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services;
using Files.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage;

namespace Files.App.ServicesImplementation
{
	public sealed class SideloadUpdateService : ObservableObject, IUpdateService, IDisposable
	{
		private const string SIDELOAD_STABLE = "https://cdn.files.community/files/stable/Files.Package.appinstaller";
		private const string SIDELOAD_PREVIEW = "https://cdn.files.community/files/preview/Files.Package.appinstaller";

		private bool _isUpdateAvailable;
		private bool _isUpdating;

		private readonly HttpClient _client = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(1) });

		private readonly Dictionary<string, string> _sideloadVersion = new()
		{
			{ "Files", SIDELOAD_STABLE },
			{ "FilesPreview", SIDELOAD_PREVIEW }
		};

		private const string TEMPORARY_UPDATE_PACKAGE_NAME = "UpdatePackage.msix";

		private ILogger? Logger { get; } = Ioc.Default.GetService<ILogger>();

		private string PackageName { get; } = Package.Current.Id.Name;

		private Version PackageVersion { get; } = new(Package.Current.Id.Version.Major,
			Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

		private Uri? DownloadUri { get; set; }

		public bool IsUpdateAvailable
		{
			get => _isUpdateAvailable;
			private set => SetProperty(ref _isUpdateAvailable, value);
		}

		public bool IsUpdating
		{
			get => _isUpdating;
			private set => SetProperty(ref _isUpdating, value);
		}

		public async Task DownloadUpdates()
		{
			await ApplyPackageUpdate();
		}

		public Task DownloadMandatoryUpdates()
		{
			return Task.CompletedTask;
		}

		public async Task CheckForUpdates()
		{
			try
			{
				Logger?.Info($"SIDELOAD: Checking for updates...");

				await using var stream = await _client.GetStreamAsync(_sideloadVersion[PackageName]);

				// Deserialize AppInstaller.
				XmlSerializer xml = new XmlSerializer(typeof(AppInstaller));
				var appInstaller = (AppInstaller?)xml.Deserialize(stream);

				if (appInstaller is null)
					throw new ArgumentNullException(nameof(appInstaller));

				var remoteVersion = new Version(appInstaller.Version);

				Logger?.Info($"SIDELOAD: Current Package Name: {PackageName}");
				Logger?.Info($"SIDELOAD: Remote Package Name: {appInstaller.MainBundle.Name}");
				Logger?.Info($"SIDELOAD: Current Version: {PackageVersion}");
				Logger?.Info($"SIDELOAD: Remote Version: {remoteVersion}");

				// Check details and version number.
				if (appInstaller.MainBundle.Name.Equals(PackageName) && remoteVersion.CompareTo(PackageVersion) > 0)
				{
					Logger?.Info("SIDELOAD: Update found.");
					Logger?.Info("SIDELOAD: Starting background download.");
					DownloadUri = new Uri(appInstaller.MainBundle.Uri);
					await StartBackgroundDownload();
				}
				else
				{
					Logger?.Warn("SIDELOAD: Update not found.");
					IsUpdateAvailable = false;
				}
			}
			catch (Exception e)
			{
				Logger?.Error(e, e.Message);
			}
		}

		private async Task StartBackgroundDownload()
		{
			try
			{
				var tempDownloadPath = ApplicationData.Current.LocalFolder.Path + "\\" + TEMPORARY_UPDATE_PACKAGE_NAME;

				Stopwatch timer = Stopwatch.StartNew();

				await using (var stream = await _client.GetStreamAsync(DownloadUri))
				await using (var fileStream = new FileStream(tempDownloadPath, FileMode.OpenOrCreate))
					await stream.CopyToAsync(fileStream);

				timer.Stop();
				var timespan = timer.Elapsed;

				Logger?.Info($"Download time taken: {timespan.Hours:00}:{timespan.Minutes:00}:{timespan.Seconds:00}");

				IsUpdateAvailable = true;
			}
			catch (Exception e)
			{
				Logger?.Error(e, e.Message);
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
				await Task.Run(async () =>
				{
					var bundlePath = new Uri(ApplicationData.Current.LocalFolder.Path + "\\" + TEMPORARY_UPDATE_PACKAGE_NAME);

					var deployment = pm.RequestAddPackageAsync(
						bundlePath,
						null,
						DeploymentOptions.ForceTargetApplicationShutdown,
						pm.GetDefaultPackageVolume(),
						null,
						null);

					result = await deployment;
				});
			}
			catch (Exception e)
			{
				if (result?.ExtendedErrorCode is not null)
					Logger?.Info(result.ErrorText);

				Logger?.Error(e, e.Message);
			}
			finally
			{
				// Reset fields.
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
