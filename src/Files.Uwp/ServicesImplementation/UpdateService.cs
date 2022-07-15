using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Services;
using Microsoft.Toolkit.Uwp;

namespace Files.Uwp.ServicesImplementation
{
    internal sealed class UpdateService : ObservableObject, IUpdateService
    {
        private StoreContext _storeContext;
        private IList<StorePackageUpdate> _updatePackages;

        private bool IsMandatory => _updatePackages?.Where(e => e.Mandatory).ToList().Count >= 1;

        private bool _isUpdateAvailable;

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set
            {
                _isUpdateAvailable = value;
                OnPropertyChanged(nameof(IsUpdateAvailable));
            }
        }

        private bool _isUpdating;

        public bool IsUpdating
        {
            get => _isUpdating;
            private set
            {
                _isUpdating = value;
                OnPropertyChanged(nameof(IsUpdating));
            }
        }

        // TODO: This needs to be implemented in this service.
        public int DownloadPercentage { get; }

        public UpdateService()
        {
            _updatePackages = new List<StorePackageUpdate>();
        }

        public void ReportToAppCenter() {}

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
            var downloadOperation = _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);
            await downloadOperation.AsTask();
        }

        private async Task GetUpdatePackages()
        {
            try
            {
                _storeContext ??= await Task.Run(StoreContext.GetDefault);
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
                Title             = "ConsentDialogTitle".GetLocalized(),
                Content           = "ConsentDialogContent".GetLocalized(),
                CloseButtonText   = "Close".GetLocalized(),
                PrimaryButtonText = "ConsentDialogPrimaryButtonText".GetLocalized()
            };
            ContentDialogResult result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary;
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
            _updatePackages.Clear();
        }

        private void OnUpdateCancelled()
        {
            IsUpdating = false;
        }
    }
}
