using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.Helpers
{
    internal class AppUpdater
    {
        private StoreContext context;

        public AppUpdater()
        {
        }

        public async void CheckForUpdatesAsync(bool mandatoryOnly = true)
        {
            try
            {
                if (context == null)
                {
                    context = await Task.Run(() => StoreContext.GetDefault());
                }

                var updateList = await context.GetAppAndOptionalStorePackageUpdatesAsync();

                if (mandatoryOnly)
                {
                    updateList = updateList.Where(e => e.Mandatory).ToList();
                }

                if (updateList.Count > 0)
                {
                    if (await DownloadUpdatesConsent())
                    {
                        await DownloadUpdates(updateList);
                    }
                }
            }
#if !DEBUG
            catch (Exception ex)
            {

                App.Logger.Warn(ex, "Could not fetch updates.");
            }
#else
            catch (Exception)
            {
            }
#endif
        }

        private async Task<bool> DownloadUpdatesConsent()
        {
            ContentDialog dialog = new()
            {
                Title = "ConsentDialogTitle".GetLocalized(),
                Content = "ConsentDialogContent".GetLocalized(),
                CloseButtonText = "Close".GetLocalized(),
                PrimaryButtonText = "ConsentDialogPrimaryButtonText".GetLocalized()
            };
            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return true;
            }
            return false;
        }

        private async Task<StorePackageUpdateResult> DownloadUpdates(IReadOnlyList<StorePackageUpdate> updateList)
        {
            if (updateList == null || updateList.Count < 1)
            {
                return null;
            }

            if (context == null)
            {
                context = await Task.Run(() => StoreContext.GetDefault());
            }

            App.SaveSessionTabs(); // save the tabs so they can be restored after the update completes
            var downloadOperation = context.RequestDownloadAndInstallStorePackageUpdatesAsync(updateList);
            return await downloadOperation.AsTask();
        }
    }
}