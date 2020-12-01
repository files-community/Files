using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.UI.Popups;
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

        public async Task<object> CheckForUpdatesAsync(bool mandantoryOnly = false)
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
                    return true;
                }
                return false;
            }

            return true;
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

        public IAsyncResult DownloadUpdates()
        {
            //if (UpdateList == null || UpdateList.Count < 1)
            //{
            //    return null;
            //}

            if (context == null)
            {
                context = StoreContext.GetDefault();
            }

            IAsyncResult downloadOperation = (IAsyncResult)Task.Delay(5000);//context.RequestDownloadAndInstallStorePackageUpdatesAsync(UpdateList);
            return downloadOperation;
        }
    }
}
