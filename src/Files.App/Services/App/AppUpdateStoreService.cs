// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Net.Http;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using Windows.Storage;
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
		}

		public async Task DownloadUpdatesAsync()
		{
			OnUpdateInProgress();

			if (!HasUpdates())
			{
				return;
			}

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

		public async Task DownloadMandatoryUpdatesAsync()
		{
			// Prompt the user to download if the package list
			// contains mandatory updates.
			if (IsMandatory && HasUpdates())
			{
				if (await ShowDialogAsync())
				{
					App.Logger.LogInformation("STORE: Downloading updates...");
					OnUpdateInProgress();
					await DownloadAndInstallAsync();
					OnUpdateCompleted();
				}
			}
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
			using var client = new HttpClient();

			try
			{
				var response = await client.GetAsync(Constants.ExternalUrl.ReleaseNotesUrl);
				AreReleaseNotesAvailable = response.IsSuccessStatusCode;
			}
			catch
			{
				AreReleaseNotesAvailable = false;
			}
		}

		private async Task DownloadAndInstallAsync()
		{
			// Save the updated tab list before installing the update
			AppLifecycleHelper.SaveSessionTabs();

			App.AppModel.ForceProcessTermination = true;

			var downloadOperation = _storeContext?.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);
			var result = await downloadOperation.AsTask();

			if (result.OverallState == StorePackageUpdateState.Canceled)
				App.AppModel.ForceProcessTermination = false;
		}

		private async Task GetUpdatePackagesAsync()
		{
			try
			{
				_storeContext ??= await Task.Run(StoreContext.GetDefault);

				InitializeWithWindow.Initialize(_storeContext, MainWindow.Instance.WindowHandle);

				var updateList = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
				_updatePackages = updateList?.ToList();
			}
			catch (Exception ex)
			{
				// GetAppAndOptionalStorePackageUpdatesAsync throws for unknown reasons.
				App.Logger.LogWarning(ex, ex.Message);
			}
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
			var destFolderPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Files");
			var destExeFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe");
			var branchFilePath = Path.Combine(destFolderPath, "Branch.txt");

			// If Files.App.Launcher.exe doesn't exist, no need to update it.
			if (!File.Exists(destExeFilePath))
				return;

			// Check if the launcher file is associated with the current branch of the app.
			if (File.Exists(branchFilePath))
			{
				try
				{
					var branch = await File.ReadAllTextAsync(branchFilePath, Encoding.UTF8);
					if (!string.Equals(branch.Trim(), "files-dev", StringComparison.OrdinalIgnoreCase))
						return;
				}
				catch { }
			}
			else
			{
				try
				{
					// Create branch file for users updating from versions earlier than v4.0.20.
					await File.WriteAllTextAsync(branchFilePath, "files-dev", Encoding.UTF8);
				}
				catch { }
			}

			var srcExeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/Files.App.Launcher.exe"));
			var destFolder = await StorageFolder.GetFolderFromPathAsync(destFolderPath);

			await srcExeFile.CopyAsync(destFolder, "Files.App.Launcher.exe", NameCollisionOption.ReplaceExisting);

			App.Logger.LogInformation("Files.App.Launcher updated.");
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

			_updatePackages?.Clear();
		}

		private void OnUpdateCancelled()
		{
			IsUpdating = false;
		}
	}
}
