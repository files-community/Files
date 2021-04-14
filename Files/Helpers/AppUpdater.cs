using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers
{
    internal class AppUpdater
    {
        private StoreContext context = null;
        private IReadOnlyList<StorePackageUpdate> UpdateList = null;

        public AppUpdater()
        {
        }

        public async void CheckForUpdatesAsync(bool mandantoryOnly = true)
        {
            try
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
            }
            catch (Exception)
            {
            }
        }

        private async Task<bool> DownloadUpdatesConsent()
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "ConsentDialogTitle".GetLocalized(),
                Content = "ConsentDialogContent".GetLocalized(),
                CloseButtonText = "ConsentDialogCloseButtonText".GetLocalized(),
                PrimaryButtonText = "ConsentDialogPrimaryButtonText".GetLocalized()
            };
            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return true;
            }
            return false;
        }

        private IAsyncResult DownloadUpdates()
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