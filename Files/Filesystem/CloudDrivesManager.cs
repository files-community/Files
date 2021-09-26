using Files.Common;
using Files.DataModels.NavigationControlItems;
using Files.Filesystem.Cloud;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Filesystem
{
    public class CloudDrivesManager
    {
        private static readonly Logger Logger = App.Logger;
        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        public event EventHandler<IReadOnlyList<DriveItem>> RefreshCompleted;
        public event EventHandler<SectionType> RemoveCloudDrivesSidebarSection;

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

        public CloudDrivesManager()
        {
        }

        public async Task EnumerateDrivesAsync()
        {
            if (!App.AppSettings.ShowCloudDrivesSection)
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
                    Type = DriveType.CloudDrive,
                };

                var iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(provider.SyncFolder, 24);
                if (iconData != null)
                {
                    cloudProviderItem.ThumbnailData = iconData;
                }

                lock (drivesList)
                {
                    if (!drivesList.Any(x => x.Path == cloudProviderItem.Path))
                    {
                        drivesList.Add(cloudProviderItem);
                    }
                }
            }

            RefreshCompleted?.Invoke(this, Drives);
        }

        public async void UpdateCloudDrivesSectionVisibility()
        {
            if (App.AppSettings.ShowCloudDrivesSection)
            {
                await EnumerateDrivesAsync();
            }
            else
            {
                RemoveCloudDrivesSidebarSection?.Invoke(this, SectionType.CloudDrives);
            }
        }
    }
}