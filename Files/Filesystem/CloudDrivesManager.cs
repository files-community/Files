using Files.Filesystem.Cloud;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class CloudDrivesManager : ObservableObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private List<DriveItem> drivesList = new List<DriveItem>();

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
            await cloudProviderController.DetectInstalledCloudProvidersAsync();

            foreach (var provider in cloudProviderController.CloudProviders)
            {
                var cloudProviderItem = new DriveItem()
                {
                    Text = provider.Name,
                    Path = provider.SyncFolder,
                    Type = DriveType.CloudDrive,
                };
                lock (drivesList)
                {
                    drivesList.Add(cloudProviderItem);
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
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RefreshUI;
            }
        }

        private async void RefreshUI(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncSideBarItemsUI();
            CoreApplication.MainView.Activated -= RefreshUI;
        }

        private async Task SyncSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var drivesSection = MainPage.SideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarCloudDrives".GetLocalized());

                if (drivesSection != null && Drives.Count == 0)
                {
                    //No drives - remove the header
                    MainPage.SideBarItems.Remove(drivesSection);
                }

                if (drivesSection == null && Drives.Count > 0)
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
                foreach (var drive in Drives.OrderBy(o => o.Text))
                {
                    MainPage.SideBarItems.Insert(insertAt, drive);
                    insertAt++;
                }
            });
        }
    }
}