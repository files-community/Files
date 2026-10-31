// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using WinRT.Interop;

namespace Files.App.Services
{
	internal sealed partial class StoreUpdateService : ObservableObject, IUpdateService
	{
		private StoreContext? _storeContext;
		private List<StorePackageUpdate>? _updatePackages;

		private bool IsMandatory => _updatePackages?.Where(e => e.Mandatory).ToList().Count >= 1;

		private bool _isUpdateAvailable;
		public bool IsUpdateAvailable
		{
			get => _isUpdateAvailable;
			set => SetProperty(ref _isUpdateAvailable, value);
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

		public StoreUpdateService()
		{
			_updatePackages = [];
			App.ServerChanged += OnServerChanged;
		}

		public async Task DownloadUpdatesAsync()
		{
			if (!HasUpdates())
				return;

			OnUpdateInProgress();

			try
			{
				// double check for Mandatory
				if (IsMandatory)
				{
					// Show dialog
					var dialog = await ShowDialogAsync();
					if (!dialog)
					{
						// User rejected mandatory update.
						OnUpdateCancelled();
						return;
					}
				}

				await DownloadAndInstallAsync();
				OnUpdateCompleted();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "STORE: Update flow failed.");
				OnUpdateCancelled();
			}
		}

		public async Task DownloadMandatoryUpdatesAsync()
		{
			// Prompt the user to download if the package list
			// contains mandatory updates.
			if (!IsMandatory || !HasUpdates())
				return;

			// CheckAppUpdate runs inside Task.Run, so the consent dialog and the Store
			// download API must be marshalled to the UI thread to avoid hard crashes.
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				try
				{
					if (!await ShowDialogAsync())
						return;

					App.Logger.LogInformation("STORE: Downloading updates...");
					OnUpdateInProgress();
					await DownloadAndInstallAsync();
					OnUpdateCompleted();
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, "STORE: Mandatory update flow failed.");
					OnUpdateCancelled();
				}
			});
		}

		public async Task CheckForUpdatesAsync()
		{
			IsUpdateAvailable = false;
			App.Logger.LogInformation("STORE: Checking for updates...");

			await GetUpdatePackagesAsync();

			if (_updatePackages is not null && _updatePackages.Count > 0)
			{
				App.Logger.LogInformation("STORE: Update found.");

				MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
				{
					IsUpdateAvailable = true;
				});
			}
		}

		public async Task CheckForReleaseNotesAsync()
		{
			AreReleaseNotesAvailable = await SafetyExtensions.IgnoreExceptions(async () => await Server.UpdateService.CheckReleaseNotesAsync(), App.Logger);
		}

		private async Task DownloadAndInstallAsync()
		{
			// Save the updated tab list before installing the update
			AppLifecycleHelper.SaveSessionTabs();
			if (!Server.SettingsService.FlushAllSettings())
				throw new System.IO.IOException("Could not save settings before installing the update.");

			var serverUpdateStarted = SafetyExtensions.IgnoreExceptions(Server.UpdateService.BeginUpdate, App.Logger);

			try
			{
				_storeContext ??= StoreContext.GetDefault();
				InitializeWithWindow.Initialize(_storeContext, MainWindow.Instance.WindowHandle);
				var downloadOperation = _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);

				if (downloadOperation is null)
					return;

				downloadOperation.Progress = (op, status) =>
				{
					MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
					{
						UpdateProgress = (int)(status.TotalDownloadProgress * 100);
					});
				};

				await downloadOperation.AsTask();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "STORE: Update download failed.");
			}
			finally
			{
				if (serverUpdateStarted)
					SafetyExtensions.IgnoreExceptions(Server.UpdateService.EndUpdate, App.Logger);
			}
		}

		private async Task GetUpdatePackagesAsync()
		{
			try
			{
				_updatePackages = (await Server.UpdateService.GetStoreUpdatesAsync()).ToList();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Could not check Store updates.");
			}
		}

		private async void OnServerChanged(object? sender, string name)
		{
			if (name == ServerChangeNames.Updates && !IsUpdating)
				await SafetyExtensions.IgnoreExceptions(CheckForUpdatesAsync, App.Logger);
		}

		private static async Task<bool> ShowDialogAsync()
		{
			//TODO: Use IDialogService in future.
			ContentDialog dialog = new()
			{
				Title = Strings.ConsentDialogTitle.GetLocalizedResource(),
				Content = Strings.ConsentDialogContent.GetLocalizedResource(),
				CloseButtonText = Strings.Close.GetLocalizedResource(),
				PrimaryButtonText = Strings.ConsentDialogPrimaryButtonText.GetLocalizedResource()
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			ContentDialogResult result = await dialog.TryShowAsync();

			return result == ContentDialogResult.Primary;
		}

		public async Task CheckAndUpdateFilesLauncherAsync()
		{
			await SafetyExtensions.IgnoreExceptions(async () => await Server.UpdateService.UpdateLauncherAsync(), App.Logger);
		}

		private bool HasUpdates()
		{
			return _updatePackages is not null && _updatePackages.Count >= 1;
		}

		private void OnUpdateInProgress()
		{
			IsUpdating = true;
		}

		private void OnUpdateCompleted()
		{
			IsUpdating = false;
			IsUpdateAvailable = false;
			UpdateProgress = 0;

			_updatePackages?.Clear();
		}

		private void OnUpdateCancelled()
		{
			IsUpdating = false;
			UpdateProgress = 0;
		}
	}
}
