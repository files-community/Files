// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Windows.System;

namespace Files.App.Services
{
	/// <summary>
	/// Update service for the portable (unpackaged) distribution. Checks the release
	/// manifest published to the Files CDN and directs the user to the download for
	/// their architecture; a portable install cannot be updated in place.
	/// </summary>
	public sealed partial class PortableUpdateService : ObservableObject, IUpdateService, IDisposable
	{
		private const string ManifestUrl = "https://cdn.files.community/files/stable/Files.Portable.json";
		private const string DownloadPageUrl = "https://files.community/download";

		private readonly HttpClient _client = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(3) });

		private ILogger? Logger { get; } = Ioc.Default.GetRequiredService<ILogger<App>>();

		private static string Architecture => RuntimeInformation.ProcessArchitecture switch
		{
			System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
			System.Runtime.InteropServices.Architecture.X86 => "x86",
			_ => "x64",
		};

		private string? _downloadUrl;

		private bool _isUpdateAvailable;
		public bool IsUpdateAvailable
		{
			get => _isUpdateAvailable;
			private set => SetProperty(ref _isUpdateAvailable, value);
		}

		public bool IsUpdating
			=> false;

		public int UpdateProgress
			=> 0;

		public bool IsAppUpdated
			=> AppLifecycleHelper.IsAppUpdated;

		private bool _areReleaseNotesAvailable = false;
		public bool AreReleaseNotesAvailable
		{
			get => _areReleaseNotesAvailable;
			private set => SetProperty(ref _areReleaseNotesAvailable, value);
		}

		public PortableUpdateService()
		{
			_client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Files", AppLifecycleHelper.AppVersion.ToString()));
		}

		public async Task CheckForUpdatesAsync()
		{
			IsUpdateAvailable = false;

			try
			{
				Logger?.LogInformation("PORTABLE: Checking for updates...");

				using var document = JsonDocument.Parse(await _client.GetStringAsync(ManifestUrl));
				var root = document.RootElement;

				var manifestVersion = root.GetProperty("version").GetString();
				if (!Version.TryParse(manifestVersion, out var remoteVersion))
				{
					Logger?.LogWarning($"PORTABLE: Could not parse remote version '{manifestVersion}'.");
					return;
				}

				_downloadUrl = root.TryGetProperty("downloads", out var downloads) &&
					downloads.TryGetProperty(Architecture, out var download) &&
					download.TryGetProperty("url", out var url)
						? url.GetString()
						: null;

				var currentVersion = AppLifecycleHelper.AppVersion;

				Logger?.LogInformation($"PORTABLE: Current Version: {currentVersion}");
				Logger?.LogInformation($"PORTABLE: Remote Version: {remoteVersion}");

				if (Normalize(remoteVersion).CompareTo(Normalize(currentVersion)) > 0)
				{
					Logger?.LogInformation("PORTABLE: Update found.");
					MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
					{
						IsUpdateAvailable = true;
					});
				}
				else
				{
					Logger?.LogInformation("PORTABLE: Update not found.");
				}
			}
			catch (HttpRequestException ex) // The manifest is absent until the first portable release is published
			{
				Logger?.LogDebug(ex, ex.Message);
			}
			catch (Exception ex)
			{
				Logger?.LogError(ex, ex.Message);
			}

			static Version Normalize(Version version)
				=> new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
		}

		public async Task DownloadUpdatesAsync()
		{
			if (!IsUpdateAvailable)
				return;

			await Launcher.LaunchUriAsync(new Uri(_downloadUrl ?? DownloadPageUrl));
		}

		public Task DownloadMandatoryUpdatesAsync()
		{
			return Task.CompletedTask;
		}

		public Task CheckAndUpdateFilesLauncherAsync()
		{
			return Task.CompletedTask;
		}

		public async Task CheckForReleaseNotesAsync()
		{
			try
			{
				var response = await _client.GetAsync(Constants.ExternalUrl.ReleaseNotesUrl);
				AreReleaseNotesAvailable = response.IsSuccessStatusCode;
			}
			catch
			{
				AreReleaseNotesAvailable = false;
			}
		}

		public void Dispose()
		{
			_client?.Dispose();
		}
	}
}
