using Files.Shared;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Helpers;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using System.Collections.Specialized;

namespace Files.Uwp.Filesystem
{
    public class NetworkDrivesManager
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

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
            networkItem.MenuOptions = new ContextMenuOptions
            {
                IsLocationItem = true,
                ShowShellItems = true,
                ShowEjectDevice = networkItem.IsRemovable,
                ShowProperties = true
            };
            lock (drivesList)
            {
                drivesList.Add(networkItem);
            }
            DataChanged?.Invoke(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, networkItem));
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
                        networkItem.MenuOptions = new ContextMenuOptions
                        {
                            IsLocationItem = true,
                            ShowEjectDevice = networkItem.IsRemovable,
                            ShowShellItems = true,
                            ShowProperties = true
                        };
                        lock (drivesList)
                        {
                            if (!drivesList.Any(x => x.Path == networkItem.Path))
                            {
                                drivesList.Add(networkItem);
                            }
                        }
                    }
                    foreach (var drive in Drives
                        .OrderByDescending(o => string.Equals(o.Text, "Network".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Text))
                    {
                        DataChanged?.Invoke(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, drive));
                    }
                }
            }
        }
    }
}