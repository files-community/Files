// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Net.Http;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using Windows.Storage;
using WinRT.Interop;

namespace Files.App.Services
{
	internal sealed class UpdateService : ObservableObject, IUpdateService
	{
		private StoreContext? _storeContext;
		private IList<StorePackageUpdate>? _updatePackages;

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

		private bool _isReleaseNotesAvailable;
		public bool IsReleaseNotesAvailable
		{
			get => _isReleaseNotesAvailable;
			private set => SetProperty(ref _isReleaseNotesAvailable, value);
		}

		public bool IsAppUpdated
		{
			get => SystemInformation.Instance.IsAppUpdated;
		}

		public UpdateService()
		{
			_updatePackages = new List<StorePackageUpdate>();
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
				IsUpdateAvailable = true;
			}
		}

		private async Task DownloadAndInstallAsync()
		{
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
			catch (FileNotFoundException)
			{
				// Suppress the FileNotFoundException.
				// GetAppAndOptionalStorePackageUpdatesAsync throws for unknown reasons.
			}
		}

		private static async Task<bool> ShowDialogAsync()
		{
			//TODO: Use IDialogService in future.
			ContentDialog dialog = new()
			{
				Title = "ConsentDialogTitle".GetLocalizedResource(),
				Content = "ConsentDialogContent".GetLocalizedResource(),
				CloseButtonText = "Close".GetLocalizedResource(),
				PrimaryButtonText = "ConsentDialogPrimaryButtonText".GetLocalizedResource()
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			ContentDialogResult result = await dialog.TryShowAsync();

			return result == ContentDialogResult.Primary;
		}

		public async Task CheckLatestReleaseNotesAsync(CancellationToken cancellationToken = default)
		{
			if (!IsAppUpdated)
				return;

			var result = await GetLatestReleaseNotesAsync();

			if (result is not null)
				IsReleaseNotesAvailable = true;
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

		public async Task CheckAndUpdateFilesLauncherAsync()
		{
			var destFolderPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Files");
			var destExeFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe");

			if (Path.Exists(destExeFilePath))
			{
				var hashEqual = false;
				var srcHashFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/Files.App.Launcher.exe.sha256"));
				var destHashFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe.sha256");

				if (Path.Exists(destHashFilePath))
				{
					using var srcStream = (await srcHashFile.OpenReadAsync()).AsStream();
					using var destStream = File.OpenRead(destHashFilePath);

					hashEqual = HashEqual(srcStream, destStream);
				}

				if (!hashEqual)
				{
					var srcExeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/Files.App.Launcher.exe"));
					var destFolder = await StorageFolder.GetFolderFromPathAsync(destFolderPath);

					await srcExeFile.CopyAsync(destFolder, "Files.App.Launcher.exe", NameCollisionOption.ReplaceExisting);
					await srcHashFile.CopyAsync(destFolder, "Files.App.Launcher.exe.sha256", NameCollisionOption.ReplaceExisting);

					App.Logger.LogInformation("Files.App.Launcher updated.");
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
