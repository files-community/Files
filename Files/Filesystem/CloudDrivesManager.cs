using Files.Filesystem.Cloud;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class CloudDrivesManager : ObservableObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        public IReadOnlyList<DriveItem> Drives
        {
            get
            {
                lock (drivesList)
                {
                    return drivesList.ToList().AsReadOnly();
                }
            }
        }

        public CloudDrivesManager()
        {
        }

        public async Task EnumerateDrivesAsync()
        {
            var cloudProviderController = new CloudProviderController();
            var cloudProviders = await cloudProviderController.DetectInstalledCloudProvidersAsync();

            foreach (var provider in cloudProviders)
            {
                Logger.Info($"Adding cloud provider \"{provider.Name}\" mapped to {provider.SyncFolder}");
                var cloudProviderItem = new DriveItem()
                {
                    Text = provider.Name,
                    Path = provider.SyncFolder,
                    Type = DriveType.CloudDrive,
                };
                lock (drivesList)
                {
                    if (!drivesList.Any(x => x.Path == cloudProviderItem.Path))
                    {
                        drivesList.Add(cloudProviderItem);
                    }
                }
            }

            await RefreshUI();
        }

        private async Task RefreshUI()
        {
            try
            {
                await SyncSideBarItemsUI();
            }
            catch (Exception ex) // UI Thread not ready yet, so we defer the previous operation until it is.
            {
                Logger.Error(ex, "UI thread not ready yet");
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RefreshUI;
            }
        }

        private async void RefreshUI(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            CoreApplication.MainView.Activated -= RefreshUI;
            await SyncSideBarItemsUI();
        }

        private async Task SyncSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await MainPage.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    MainPage.SideBarItems.BeginBulkOperation();

                    var drivesSnapshot = Drives.ToList();
                    var drivesSection = MainPage.SideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarCloudDrives".GetLocalized());

                    if (drivesSection != null && drivesSnapshot.Count == 0)
                    {
                        //No drives - remove the header
                        MainPage.SideBarItems.Remove(drivesSection);
                    }

                    if (drivesSection == null && drivesSnapshot.Count > 0)
                    {
                        drivesSection = new HeaderTextItem()
                        {
                            Text = "SidebarCloudDrives".GetLocalized()
                        };

                        //Get the last location item in the sidebar
                        var lastLocationItem = MainPage.SideBarItems.LastOrDefault(x => x is LocationItem);

                        if (lastLocationItem != null)
                        {
                            //Get the index of the last location item
                            var lastLocationItemIndex = MainPage.SideBarItems.IndexOf(lastLocationItem);
                            //Insert the drives title beneath it
                            MainPage.SideBarItems.Insert(lastLocationItemIndex + 1, drivesSection);
                        }
                        else
                        {
                            MainPage.SideBarItems.Add(drivesSection);
                        }
                    }

                    var sectionStartIndex = MainPage.SideBarItems.IndexOf(drivesSection);

                    //Remove all existing cloud drives from the sidebar
                    foreach (var item in MainPage.SideBarItems
                        .Where(x => x.ItemType == NavigationControlItemType.CloudDrive)
                        .ToList())
                    {
                        MainPage.SideBarItems.Remove(item);
                    }

                    //Add all cloud drives to the sidebar
                    var insertAt = sectionStartIndex + 1;
                    foreach (var drive in drivesSnapshot.OrderBy(o => o.Text))
                    {
                        MainPage.SideBarItems.Insert(insertAt, drive);
                        insertAt++;
                    }
                    MainPage.SideBarItems.EndBulkOperation();
                }
                finally
                {
                    MainPage.SideBarItemsSemaphore.Release();
                }
            });
        }
    }
}