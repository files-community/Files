using Files.Models.JsonSettings;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml.Controls;
using Files.Enums;

namespace Files.Services.Implementation
{
    public class UpdateSettingsService : BaseObservableJsonSettingsModel, IUpdateSettingsService
    {
        private StoreContext _storeContext;
        private IList<StorePackageUpdate> _updatePackages;

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

        public UpdateSettingsService()
        {
            _updatePackages = new List<StorePackageUpdate>();
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

        public void ReportToAppCenter() {}

        public async Task DownloadUpdates()
        {
            // Notify that we are starting updates.
            IsUpdating = true;

            await GetUpdatePackages();

            // Prompt the user to download if the package list
            // contains mandatory updates.
            if (IsMandatory)
            {
                var dialog = await ShowDialogAsync();

                if (!dialog)
                {
                    IsUpdating = false;
                    return;
                }
            }

            if (_updatePackages is not null && _updatePackages.Count > 1)
            {
                App.SaveSessionTabs();
                var downloadOperation = _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);
                await downloadOperation.AsTask();

                _updatePackages.Clear();
                IsUpdateAvailable = false;
            }

            // Notify that update is complete.
            IsUpdating = false;
        }

        public async void CheckForUpdates()
        {
            await GetUpdatePackages();

            if (_updatePackages is not null && _updatePackages.Count > 0)
            {
                IsUpdateAvailable = true;
            }
        }

        private async Task GetUpdatePackages()
        {
            try
            {
                _storeContext ??= await Task.Run(StoreContext.GetDefault);
                var updateList = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
                _updatePackages = updateList.ToList();
            }
            catch (Exception)
            {
            }
        }

        private bool IsMandatory => _updatePackages?.Where(e => e.Mandatory).ToList().Count >= 1;
    }
}
