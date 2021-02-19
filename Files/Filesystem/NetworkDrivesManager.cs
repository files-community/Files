using Files.Helpers;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using NLog;
using System;
using System.Collections.Generic;
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

        public NetworkDrivesManager()
        {
            var networkItem = new DriveItem()
            {
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
                var (status, response) = await connection.SendMessageWithRetryAsync(new ValueSet()
                {
                    { "Arguments", "NetworkDriveOperation" },
                    { "netdriveop", "GetNetworkLocations" }
                }, TimeSpan.FromSeconds(10));
                if (status == AppServiceResponseStatus.Success)
                {
                    foreach (var key in response.Message.Keys)
                    {
                        var networkItem = new DriveItem()
                        {
                            Text = key,
                            Path = (string)response.Message[key],
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
                await MainPage.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    MainPage.SideBarItems.BeginBulkOperation();

                    var drivesSnapshot = Drives.ToList();
                    var drivesSection = MainPage.SideBarItems.FirstOrDefault(x => x is HeaderTextItem && x.Text == "SidebarNetworkDrives".GetLocalized());

                    if (drivesSection != null && drivesSnapshot.Count == 0)
                    {
                        //No drives - remove the header
                        MainPage.SideBarItems.Remove(drivesSection);
                    }

                    if (drivesSection == null && drivesSnapshot.Count > 0)
                    {
                        drivesSection = new HeaderTextItem()
                        {
                            Text = "SidebarNetworkDrives".GetLocalized()
                        };

                        MainPage.SideBarItems.Add(drivesSection);
                    }

                    var sectionStartIndex = MainPage.SideBarItems.IndexOf(drivesSection);
                    var insertAt = sectionStartIndex + 1;

                    //Remove all existing network drives from the sidebar
                    while (insertAt < MainPage.SideBarItems.Count)
                    {
                        var item = MainPage.SideBarItems[insertAt];
                        if (item.ItemType != NavigationControlItemType.Drive)
                        {
                            break;
                        }
                        MainPage.SideBarItems.Remove(item);
                    }

                    //Add all network drives to the sidebar
                    foreach (var drive in drivesSnapshot
                        .OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Text))
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