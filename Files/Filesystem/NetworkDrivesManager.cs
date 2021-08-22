using Files.Common;
using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Helpers;
using Files.UserControls;
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
            if (!App.AppSettings.ShowNetworkDrivesSection)
            {
                return;
            }

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
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarNetworkDrives".GetLocalized()) as LocationItem;
                    if (App.AppSettings.ShowNetworkDrivesSection && section == null)
                    {
                        section = new LocationItem()
                        {
                            Text = "SidebarNetworkDrives".GetLocalized(),
                            Section = SectionType.Network,
                            SelectsOnInvoked = false,
                            Icon = UIHelpers.GetImageForIconOrNull(SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.NetworkDrives)?.Image),
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Library) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Drives) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.CloudDrives) ? 1 : 0); // After cloud section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }

                    if (section != null)
                    {
                        foreach (var drive in Drives.ToList()
                        .OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Text))
                        {
                            var resource = SidebarPinnedModel.IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.Folder);
                            drive.Icon = UIHelpers.GetImageForIconOrNull(resource?.Image);
                            drive.IconData = resource?.IconDataBytes;
                            if (!section.ChildItems.Contains(drive))
                            {
                                section.ChildItems.Add(drive);
                            }
                        }
                    }
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private void RemoveNetworkDrivesSideBarSection()
        {
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
        }

        public async void UpdateNetworkDrivesSectionVisibility()
        {
            if (App.AppSettings.ShowNetworkDrivesSection)
            {
                await EnumerateDrivesAsync();
            }
            else
            {
                RemoveNetworkDrivesSideBarSection();
            }
        }
    }
}