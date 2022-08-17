using Files.Shared;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Uwp.Filesystem
{
    public class NetworkDrivesManager
    {
        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

        private readonly List<DriveItem> drives = new();
        public IReadOnlyList<DriveItem> Drives
        {
            get
            {
                lock (drives)
                {
                    return drives.ToList().AsReadOnly();
                }
            }
        }

        public NetworkDrivesManager()
        {
            var networkItem = new DriveItem
            {
                DeviceID = "network-folder",
                Text = "Network".GetLocalized(),
                Path = CommonPaths.NetworkFolderPath,
                Type = DriveType.Network,
                ItemType = NavigationControlItemType.Drive,
            };
            networkItem.MenuOptions = new ContextMenuOptions
            {
                IsLocationItem = true,
                ShowShellItems = true,
                ShowEjectDevice = networkItem.IsRemovable,
                ShowProperties = true
            };

            lock (drives)
            {
                drives.Add(networkItem);
            }
            DataChanged?.Invoke(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, networkItem));
        }

        public async Task UpdateDrivesAsync()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection is not null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet
                {
                    { "Arguments", "NetworkDriveOperation" },
                    { "netdriveop", "GetNetworkLocations" },
                });
                if (status is AppServiceResponseStatus.Success && response.ContainsKey("NetworkLocations"))
                {
                    var items = JsonConvert.DeserializeObject<List<ShellLinkItem>>((string)response["NetworkLocations"]);
                    foreach (var item in items ?? new())
                    {
                        var networkItem = new DriveItem
                        {
                            Text = System.IO.Path.GetFileNameWithoutExtension(item.FileName),
                            Path = item.TargetPath,
                            DeviceID = item.FilePath,
                            Type = DriveType.Network,
                            ItemType = NavigationControlItemType.Drive,
                        };
                        networkItem.MenuOptions = new ContextMenuOptions
                        {
                            IsLocationItem = true,
                            ShowEjectDevice = networkItem.IsRemovable,
                            ShowShellItems = true,
                            ShowProperties = true,
                        };

                        lock (drives)
                        {
                            if (drives.Any(x => x.Path == networkItem.Path))
                            {
                                continue;
                            }
                            drives.Add(networkItem);
                        }
                    }

                    var orderedDrives = Drives
                        .OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Text);
                    foreach (var drive in orderedDrives)
                    {
                        DataChanged?.Invoke(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, drive));
                    }
                }
            }
        }
    }
}