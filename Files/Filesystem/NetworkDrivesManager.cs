using Files.Common;
using Files.DataModels.NavigationControlItems;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Filesystem
{
    public class NetworkDrivesManager
    {
        private static readonly Logger Logger = App.Logger;
        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        public event EventHandler<IReadOnlyList<DriveItem>> RefreshCompleted;
        public event EventHandler RemoveNetworkDrivesSidebarSection;

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

            RefreshCompleted?.Invoke(this, Drives);
        }

        public async void UpdateNetworkDrivesSectionVisibility()
        {
            if (App.AppSettings.ShowNetworkDrivesSection)
            {
                await EnumerateDrivesAsync();
            }
            else
            {
                RemoveNetworkDrivesSidebarSection?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}