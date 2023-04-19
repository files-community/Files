using CommunityToolkit.WinUI.Helpers;
using Files.Backend.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Services.Store;
using Windows.Storage;
using WinRT.Interop;

namespace Files.App.ServicesImplementation
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

		public async Task DownloadUpdates()
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

			await DownloadAndInstall();
			OnUpdateCompleted();
		}

		public async Task DownloadMandatoryUpdates()
		{
			// Prompt the user to download if the package list
			// contains mandatory updates.
			if (IsMandatory && HasUpdates())
			{
				if (await ShowDialogAsync())
				{
					App.Logger.LogInformation("STORE: Downloading updates...");
					OnUpdateInProgress();
					await DownloadAndInstall();
					OnUpdateCompleted();
				}
			}
		}

		public async Task CheckForUpdates()
		{
			App.Logger.LogInformation("STORE: Checking for updates...");

			await GetUpdatePackages();

			if (_updatePackages is not null && _updatePackages.Count > 0)
			{
				App.Logger.LogInformation("STORE: Update found.");
				IsUpdateAvailable = true;
			}
		}

		private async Task DownloadAndInstall()
		{
			App.SaveSessionTabs();
			var downloadOperation = _storeContext?.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);
			await downloadOperation.AsTask();
		}

		private async Task GetUpdatePackages()
		{
			try
			{
				_storeContext ??= await Task.Run(StoreContext.GetDefault);

				InitializeWithWindow.Initialize(_storeContext, App.WindowHandle);

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
