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
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
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
            catch (Exception)       // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                Debug.WriteLine($"RefreshUI Exception");
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
            Debug.WriteLine("SyncSideBarItemsUI");
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (MainPage.SideBarItems)
                {
                    var drivesSection = MainPage.SideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarCloudDrives".GetLocalized());

                    if (drivesSection != null && Drives.Count == 0)
                    {
                        //No drives - remove the header
                        MainPage.SideBarItems.Remove(drivesSection);
                    }

                    if (drivesSection == null)
                    {
                        drivesSection = new HeaderTextItem()
                        {
                            Text = "SidebarCloudDrives".GetLocalized()
                        };
                        MainPage.SideBarItems.Add(drivesSection);
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
                }
            });
        }

        public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
        {
            if (!Path.IsPathRooted(devicePath))
            {
                return null;
            }
            var rootPath = Path.GetPathRoot(devicePath);
            if (devicePath.StartsWith("\\\\?\\")) // USB device
            {
                // Check among already discovered drives
                StorageFolder matchingDrive = App.AppSettings.CloudDrivesManager.Drives.FirstOrDefault(x =>
                    Helpers.PathNormalization.NormalizePath(x.Path) == Helpers.PathNormalization.NormalizePath(rootPath))?.Root;
                if (matchingDrive == null)
                {
                    // Check on all removable drives
                    var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
                    foreach (var item in remDevices)
                    {
                        try
                        {
                            var root = StorageDevice.FromId(item.Id);
                            if (Helpers.PathNormalization.NormalizePath(rootPath).Replace("\\\\?\\", "") == root.Name.ToUpperInvariant())
                            {
                                matchingDrive = root;
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore this..
                        }
                    }
                }
                if (matchingDrive != null)
                {
                    return new StorageFolderWithPath(matchingDrive, rootPath);
                }
            }
            else if (devicePath.StartsWith("\\\\")) // Network share
            {
                rootPath = rootPath.LastIndexOf("\\") > 1 ? rootPath.Substring(0, rootPath.LastIndexOf("\\")) : rootPath; // Remove share name
                return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
            }
            // It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
            return null;
        }

    }
}