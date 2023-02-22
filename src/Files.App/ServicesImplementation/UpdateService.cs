using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Extensions;
using Files.Core.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;
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
					App.Logger.Info("STORE: Downloading updates...");
					OnUpdateInProgress();
					await DownloadAndInstall();
					OnUpdateCompleted();
				}
			}
		}

		public async Task CheckForUpdates()
		{
			App.Logger.Info("STORE: Checking for updates...");

			await GetUpdatePackages();

			if (_updatePackages is not null && _updatePackages.Count > 0)
			{
				App.Logger.Info("STORE: Update found.");
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

			ContentDialogResult result = await SetContentDialogRoot(dialog).ShowAsync();

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

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
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
