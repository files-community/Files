using Files.Shared;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem.Cloud;
using Files.Uwp.Helpers;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace Files.Uwp.Filesystem
{
    public class CloudDrivesManager
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private static readonly ILogger Logger = App.Logger;
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

        public async Task EnumerateDrivesAsync()
        {
            if (!UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection)
            {
                return;
            }

            var cloudProviderController = new CloudProviderController();
            var cloudProviders = await cloudProviderController.DetectInstalledCloudProvidersAsync();

            foreach (var provider in cloudProviders)
            {
                Logger.Info($"Adding cloud provider \"{provider.Name}\" mapped to {provider.SyncFolder}");
                var cloudProviderItem = new DriveItem()
                {
                    Text = provider.Name,
                    Path = provider.SyncFolder,
                    Type = DriveType.CloudDrive
                };
                cloudProviderItem.MenuOptions = new ContextMenuOptions
                {
                    IsLocationItem = true,
                    ShowEjectDevice = cloudProviderItem.IsRemovable,
                    ShowShellItems = true,
                    ShowProperties = true
                };
                var iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(provider.SyncFolder, 24);
                if (iconData != null)
                {
                    cloudProviderItem.IconData = iconData;
                }

                lock (drivesList)
                {
                    if (!drivesList.Any(x => x.Path == cloudProviderItem.Path))
                    {
                        drivesList.Add(cloudProviderItem);
                    }
                }

                DataChanged?.Invoke(SectionType.CloudDrives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, cloudProviderItem));
            }
        }
    }
}