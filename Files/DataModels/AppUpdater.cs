using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml.Controls;

namespace Files.DataModels
{
    class AppUpdater
    {
        private StoreContext context = null;
        private IReadOnlyList<StorePackageUpdate> UpdateList = null;

        public AppUpdater()
        {
            context = StoreContext.GetDefault();
        }

        public async Task CheckForUpdatesAsync(bool mandantoryOnly = false)
        {
            if (context == null)
            {
                context = StoreContext.GetDefault();
            }

            UpdateList = await context.GetAppAndOptionalStorePackageUpdatesAsync();

            if (mandantoryOnly)
            {
                UpdateList = (IReadOnlyList<StorePackageUpdate>)UpdateList.Where(e => e.Mandatory);
            }

            if (UpdateList.Count > 0)
            {
                if (await DownloadUpdatesConsent())
                {
                    DownloadUpdates();
                }
            }
            ShowNoUpdatesAvailableDialog();
        }

        public async Task<bool> DownloadUpdatesConsent()
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Updates available",
                Content = "Do you want to download and install the available updates?",
                CloseButtonText = "No",
                PrimaryButtonText = "Yes"
            };
            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return true;
            }
            return false;
        }

        private async void ShowNoUpdatesAvailableDialog()
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "No updates available",
                Content = "You have the latest version of Files installed!",
                CloseButtonText = "Ok"
            };
            await dialog.ShowAsync();
        }

        public IAsyncResult DownloadUpdates()
        {
            if (UpdateList == null || UpdateList.Count < 1)
            {
                return null;
            }

            if (context == null)
            {
                context = StoreContext.GetDefault();
            }

            IAsyncResult downloadOperation = (IAsyncResult)context.RequestDownloadAndInstallStorePackageUpdatesAsync(UpdateList);
            return downloadOperation;
        }
    }
}
