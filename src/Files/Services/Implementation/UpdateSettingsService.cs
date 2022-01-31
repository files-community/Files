using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml.Controls;
using Files.Models.JsonSettings;
using Microsoft.Toolkit.Uwp;

namespace Files.Services.Implementation
{
    public class UpdateSettingsService : BaseObservableJsonSettingsModel, IUpdateSettingsService
    {
        private StoreContext _storeContext;

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

        public bool MandatoryOnly
        {
            get => Get(true);
            set => Set(value);
        }

        private async Task SetStoreContext()
        {
            _storeContext ??= await Task.Run(StoreContext.GetDefault);
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

        public async void DownloadUpdates()
        {
            var dialog = await ShowDialogAsync();

            if (!dialog)
            {
                return;
            }

            var updateList = await GetUpdateList();

            if (updateList is not null && updateList.Count > 1)
            {
                App.SaveSessionTabs();
                var downloadOperation = _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(updateList);
                await downloadOperation.AsTask();

                IsUpdateAvailable = false;
            }
        }

        public async void CheckForUpdates()
        {
            // Uncomment to test button appearing on toolbar.
// #if DEBUG
//             IsUpdateAvailable = true;
//             return;
// #endif

            var updateList = await GetUpdateList();

            if (updateList is not null && updateList.Count > 0)
            {
                IsUpdateAvailable = true;
            }
        }

        private async Task<IReadOnlyList<StorePackageUpdate>> GetUpdateList()
        {
            IReadOnlyList<StorePackageUpdate> updateList = null;
            try
            {
                await SetStoreContext();

                updateList = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();

                if (MandatoryOnly)
                {
                    updateList = updateList.Where(e => e.Mandatory).ToList();
                }
            }
            catch (Exception)
            {
            }

            return updateList;
        }
    }
}
