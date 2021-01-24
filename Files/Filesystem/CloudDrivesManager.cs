using Files.Filesystem.Cloud;
using Files.UserControls;
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

        private async Task EnumerateDrivesAsync()
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

        public static async Task<CloudDrivesManager> CreateInstance()
        {
            var drives = new CloudDrivesManager();
            await drives.EnumerateDrivesAsync();
            return drives;
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
                lock (SidebarControl.Items)
                {
                    //var drivesSection = SidebarControl.Items.FirstOrDefault(x => x is HeaderItem && x.Text == "SidebarCloudDrives".GetLocalized());

                    //if (drivesSection != null && Drives.Count == 0)
                    //{
                    //    //No drives - remove the header
                    //    SidebarControl.Items.Remove(drivesSection);
                    //}

                    //if (Drives.Count > 0)
                    //{
                    //    //drivesSection = new HeaderItem()
                    //    //{
                    //    //    Text = "SidebarCloudDrives".GetLocalized()
                    //    //};

                    //    //Get the last location item in the sidebar
                    //    var lastLocationItem = SidebarControl.Items.LastOrDefault(x => x is LocationItem);

                    //    if (lastLocationItem != null)
                    //    {
                    //        //Get the index of the last location item
                    //        var lastLocationItemIndex = SidebarControl.Items.IndexOf(lastLocationItem);
                    //        //Insert the drives title beneath it
                    //        SidebarControl.Items.Insert(lastLocationItemIndex + 1, drivesSection);
                    //    }
                    //    else
                    //    {
                    //        SidebarControl.Items.Add(drivesSection);
                    //    }
                    //}

                    //var sectionStartIndex = SidebarControl.Items.IndexOf(drivesSection);

                    //Remove all existing cloud drives from the sidebar
                    var cloudDrivesSection = SidebarControl.GetFirstHeaderItemOfType(HeaderItem.HeaderItemType.Cloud);

                    foreach (var item in cloudDrivesSection.MenuItems
                        .Where(x => x.ItemType == NavigationControlItemType.CloudDrive)
                        .ToList())
                    {
                        cloudDrivesSection.MenuItems.Remove(item);
                    }

                    //Add all cloud drives to the sidebar
                    foreach (var drive in Drives.OrderBy(o => o.Text))
                    {
                        cloudDrivesSection.MenuItems.Add(drive);
                    }
                }
            });
        }
    }
}