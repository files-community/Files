using Files.Common;
using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Helpers;
using Files.Services;
using Files.UserControls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
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
    public class NetworkDrivesManager
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

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
                Path = CommonPaths.NetworkFolderPath,
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
            if (!UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection)
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
                if (status == AppServiceResponseStatus.Success && response.ContainsKey("NetworkLocations"))
                {
                    var items = JsonConvert.DeserializeObject<List<ShellLinkItem>>((string)response["NetworkLocations"]);
                    foreach (var item in items ?? new())
                    {
                        var networkItem = new DriveItem()
                        {
                            Text = System.IO.Path.GetFileNameWithoutExtension(item.FileName),
                            Path = item.TargetPath,
                            DeviceID = item.FilePath,
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
                    if (UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection && section == null)
                    {
                        section = new LocationItem()
                        {
                            Text = "SidebarNetworkDrives".GetLocalized(),
                            Section = SectionType.Network,
                            SelectsOnInvoked = false,
                            Icon = await UIHelpers.GetIconResource(Constants.ImageRes.NetworkDrives),
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
                            if (!string.IsNullOrEmpty(drive.DeviceID))
                            {
                                drive.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(drive.DeviceID, 24);
                            }
                            if (drive.IconData == null)
                            {
                                var resource = await UIHelpers.GetIconResourceInfo(Constants.ImageRes.Folder);
                                drive.IconData = resource?.IconDataBytes;
                            }
                            drive.Icon = await drive.IconData.ToBitmapAsync();
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
                if (!UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        public async void UpdateNetworkDrivesSectionVisibility()
        {
            if (UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection)
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