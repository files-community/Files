// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Win32;
using Windows.Win32.System.Recovery;

namespace Files.App.Services
{
	public sealed partial class SideloadUpdateService : ObservableObject, IUpdateService, IDisposable
	{
		private const string SIDELOAD_STABLE = "https://cdn.files.community/files/stable/Files.Package.appinstaller";
		private const string SIDELOAD_PREVIEW = "https://cdn.files.community/files/preview/Files.Package.appinstaller";


		private readonly Dictionary<string, string> _sideloadVersion = new()
		{
			{ "Files", SIDELOAD_STABLE },
			{ "FilesPreview", SIDELOAD_PREVIEW }
		};

		private ILogger? Logger { get; } = Ioc.Default.GetRequiredService<ILogger<App>>();

		private string PackageName { get; } = Package.Current.Id.Name;

		private Version PackageVersion { get; } = new(
			Package.Current.Id.Version.Major,
			Package.Current.Id.Version.Minor,
			Package.Current.Id.Version.Build,
			Package.Current.Id.Version.Revision);

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

		private int _updateProgress;
		public int UpdateProgress
		{
			get => _updateProgress;
			private set => SetProperty(ref _updateProgress, value);
		}

		public bool IsAppUpdated
		{
			get => AppLifecycleHelper.IsAppUpdated;
		}

		private bool _areReleaseNotesAvailable = false;
		public bool AreReleaseNotesAvailable
		{
			get => _areReleaseNotesAvailable;
			private set => SetProperty(ref _areReleaseNotesAvailable, value);
		}

		public SideloadUpdateService() => App.ServerChanged += OnServerChanged;

		private async void OnServerChanged(object? sender, string name)
		{
			if (name == ServerChangeNames.Updates && !IsUpdating)
				await SafetyExtensions.IgnoreExceptions(CheckForUpdatesAsync, Logger);
		}

		public async Task DownloadUpdatesAsync()
		{
			await ApplyPackageUpdateAsync();
		}

		public Task DownloadMandatoryUpdatesAsync()
		{
			return Task.CompletedTask;
		}

		public async Task CheckForUpdatesAsync()
		{
			await SafetyExtensions.IgnoreExceptions(async () =>
			{
				var available = await Server.UpdateService.CheckSideloadUpdatesAsync();
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => IsUpdateAvailable = available);
			}, Logger);
		}

		public async Task CheckAndUpdateFilesLauncherAsync()
		{
			await SafetyExtensions.IgnoreExceptions(async () => await Server.UpdateService.UpdateLauncherAsync(), App.Logger);
		}

		public async Task CheckForReleaseNotesAsync()
		{
			AreReleaseNotesAvailable = await SafetyExtensions.IgnoreExceptions(async () => await Server.UpdateService.CheckReleaseNotesAsync(), App.Logger);
		}

		private async Task ApplyPackageUpdateAsync()
		{
			if (!IsUpdateAvailable)
				return;

			IsUpdating = true;

			DeploymentResult? result = null;
			var serverUpdateStarted = false;

			try
			{
				PackageManager packageManager = new PackageManager();
				if (!Server.SettingsService.FlushAllSettings())
					throw new System.IO.IOException("Could not save settings before installing the update.");

				var restartStatus = PInvoke.RegisterApplicationRestart(null, (REGISTER_APPLICATION_RESTART_FLAGS)0);
				serverUpdateStarted = SafetyExtensions.IgnoreExceptions(Server.UpdateService.BeginUpdate, Logger);

				Logger?.LogInformation($"Register for restart: {restartStatus}");

				await Task.Run(async () =>
				{
					var appInstallerUri = new Uri(_sideloadVersion[PackageName]);

					var deployment = packageManager.AddPackageByAppInstallerFileAsync(
						appInstallerUri,
						AddPackageByAppInstallerOptions.ForceTargetAppShutdown,
						packageManager.GetDefaultPackageVolume());

					deployment.Progress = (op, progress) =>
					{
						MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
						{
							UpdateProgress = (int)progress.percentage;
						});
					};

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
				if (serverUpdateStarted)
					SafetyExtensions.IgnoreExceptions(Server.UpdateService.EndUpdate, Logger);

				// Reset fields
				IsUpdating = false;
				IsUpdateAvailable = false;
				UpdateProgress = 0;
			}
		}

		public void Dispose()
		{
			App.ServerChanged -= OnServerChanged;
		}
	}
}
