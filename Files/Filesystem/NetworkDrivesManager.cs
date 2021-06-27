using Files.Common;
using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Helpers;
using Files.UserControls;
using Files.UserControls.Widgets;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class NetworkDrivesManager : ObservableObject
    {
        private static readonly Logger Logger = App.Logger;
        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        private LocationItem networkDrivesSection;
        public BulkConcurrentObservableCollection<NetworkDrivesLocationItem> NetworkDriveItems { get; } = new BulkConcurrentObservableCollection<NetworkDrivesLocationItem>();

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

        public NetworkDrivesManager()
        {
            var networkItem = new DriveItem()
            {
                DeviceID = "network-folder",
                Text = "Network".GetLocalized(),
                Path = App.AppSettings.NetworkFolderPath,
                Type = DriveType.Network,
                ItemType = NavigationControlItemType.Drive
            };
            lock (drivesList)
            {
                drivesList.Add(networkItem);
            }
        }

        public async Task EnumerateDrivesAsync()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "NetworkDriveOperation" },
                    { "netdriveop", "GetNetworkLocations" }
                });
                if (status == AppServiceResponseStatus.Success && response.ContainsKey("Count"))
                {
                    foreach (var key in response.Keys
                        .Where(k => k != "Count" && k != "RequestID"))
                    {
                        var networkItem = new DriveItem()
                        {
                            Text = key,
                            Path = (string)response[key],
                            Type = DriveType.Network,
                            ItemType = NavigationControlItemType.Drive
                        };
                        lock (drivesList)
                        {
                            if (!drivesList.Any(x => x.Path == networkItem.Path))
                            {
                                drivesList.Add(networkItem);
                            }
                        }
                    }
                }
            }

            await RefreshUI();
        }

        public async Task NetworkDrivesEnumeratorAsync()
        {
            try
            {
                await SyncNetworkDrivesSideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += EnumerateNetworkDrivesAsync;
            }
        }

        private async void EnumerateNetworkDrivesAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncNetworkDrivesSideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateNetworkDrivesAsync;
        }

        private async Task SyncNetworkDrivesSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (App.AppSettings.ShowFavoritesSection && !SidebarControl.SideBarItems.Contains(networkDrivesSection))
                {
                    await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                    try
                    {
                        SidebarControl.SideBarItems.BeginBulkOperation();

                        networkDrivesSection = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarNetworkDrives".GetLocalized()) as LocationItem;
                        if (networkDrivesSection == null)
                        {
                            networkDrivesSection = new LocationItem
                            {
                                Text = "SidebarNetworkDrives".GetLocalized(),
                                Section = SectionType.Network,
                                SelectsOnInvoked = false,
                                Icon = UIHelpers.GetImageForIconOrNull(SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.Folder).Image),
                                ChildItems = new ObservableCollection<INavigationControlItem>()
                            };
                            SidebarControl.SideBarItems.Insert(SidebarControl.SideBarItems.Count.Equals(0) ? 0 : 1, networkDrivesSection);
                        }

                        if (networkDrivesSection != null)
                        {
                            await EnumerateDrivesAsync();

                            foreach (DriveItem drive in Drives.ToList())
                            {
                                if (!networkDrivesSection.ChildItems.Contains(drive))
                                {
                                    networkDrivesSection.ChildItems.Add(drive);

                                    if (drive.Type != DriveType.VirtualDrive)
                                    {
                                        DrivesWidget.ItemsAdded.Add(drive);
                                    }
                                }
                            }

                            foreach (DriveItem drive in networkDrivesSection.ChildItems.ToList())
                            {
                                if (!Drives.Contains(drive))
                                {
                                    networkDrivesSection.ChildItems.Remove(drive);
                                    DrivesWidget.ItemsAdded.Remove(drive);
                                }
                            }
                        }

                        SidebarControl.SideBarItems.EndBulkOperation();
                    }
                    finally
                    {
                        SidebarControl.SideBarItemsSemaphore.Release();
                    }
                }
            });
        }

        public async void UpdateNetworkDrivesSectionVisibility()
        {
            if (App.AppSettings.ShowNetworkDrivesSection)
            {
                await NetworkDrivesEnumeratorAsync();
            }
            else
            {
                RemoveNetworkDrivesSideBarSection();
            }
        }

        public void RemoveNetworkDrivesSideBarSection()
        {
            try
            {
                RemoveNetworkDrivesSideBarItemsUI();
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RemoveNetworkDrivesItems;
            }
        }

        internal void UnpinNetworkDrivesSideBarSection()
        {
            var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarNetworkDrives".GetLocalized()) select n).FirstOrDefault();
            if (SidebarControl.SideBarItems.Contains(item) && item != null)
            {
                SidebarControl.SideBarItems.Remove(item);
                App.AppSettings.ShowNetworkDrivesSection = false;
            }
        }

        private void RemoveNetworkDrivesItems(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            RemoveNetworkDrivesSideBarItemsUI();
            CoreApplication.MainView.Activated -= RemoveNetworkDrivesItems;
        }

        public void RemoveNetworkDrivesSideBarItemsUI()
        {
            SidebarControl.SideBarItems.BeginBulkOperation();

            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarNetworkDrives".GetLocalized()) select n).FirstOrDefault();
                if (!App.AppSettings.ShowNetworkDrivesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }

            SidebarControl.SideBarItems.EndBulkOperation();
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
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (App.AppSettings.ShowNetworkDrivesSection)
                {
                    await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                    try
                    {
                        SidebarControl.SideBarItems.BeginBulkOperation();

                        var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarNetworkDrives".GetLocalized()) as LocationItem;
                        if (section == null)
                        {
                            section = new LocationItem()
                            {
                                Text = "SidebarNetworkDrives".GetLocalized(),
                                Section = SectionType.Network,
                                SelectsOnInvoked = false,
                                Icon = UIHelpers.GetImageForIconOrNull(SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.NetworkDrives).Image),
                                ChildItems = new ObservableCollection<INavigationControlItem>()
                            };
                            SidebarControl.SideBarItems.Add(section);
                        }

                        if (section != null)
                        {
                            foreach (var drive in Drives.ToList()
                            .OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                            .ThenBy(o => o.Text))
                            {
                                drive.Icon = UIHelpers.GetImageForIconOrNull(SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.Folder).Image);
                                if (!section.ChildItems.Contains(drive))
                                {
                                    section.ChildItems.Add(drive);
                                }
                            }
                        }

                        SidebarControl.SideBarItems.EndBulkOperation();
                    }
                    finally
                    {
                        SidebarControl.SideBarItemsSemaphore.Release();
                    }
                }
            });
        }
    }
}