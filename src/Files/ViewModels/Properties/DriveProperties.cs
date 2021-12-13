using Files.DataModels.NavigationControlItems;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;

namespace Files.ViewModels.Properties
{
    internal class DriveProperties : BaseProperties
    {
        public DriveItem Drive { get; }

        public DriveProperties(SelectedItemsPropertiesViewModel viewModel, DriveItem driveItem, IShellPage instance)
        {
            ViewModel = viewModel;
            Drive = driveItem;
            AppInstance = instance;
            GetBaseProperties();
        }

        public override void GetBaseProperties()
        {
            if (Drive != null)
            {
                ViewModel.CustomIconSource = Drive.IconSource;
                ViewModel.IconData = Drive.IconData;
                ViewModel.LoadCustomIcon = Drive.IconSource != null && Drive.IconData == null;
                ViewModel.LoadFileIcon = Drive.IconData != null;
                ViewModel.ItemName = Drive.Text;
                ViewModel.OriginalItemName = Drive.Text;
                // Note: if DriveType enum changes, the corresponding resource keys should change too
                ViewModel.ItemType = string.Format("DriveType{0}", Drive.Type).GetLocalized();
            }
        }

        public async override void GetSpecialProperties()
        {
            ViewModel.ItemAttributesVisibility = Visibility.Collapsed;
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(Drive.Path));
            BaseStorageFolder diskRoot = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(Drive.Path, item));

            if (ViewModel.LoadFileIcon)
            {
                if (diskRoot != null)
                {
                    ViewModel.IconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(diskRoot, 80, ThumbnailMode.SingleItem);
                }
                else
                {
                    ViewModel.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Drive.Path, 80);
                }
                ViewModel.IconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Drive.DeviceID, 80); // For network shortcuts
            }

            if (diskRoot == null || diskRoot.Properties == null)
            {
                ViewModel.LastSeparatorVisibility = Visibility.Collapsed;
                return;
            }

            try
            {
                string freeSpace = "System.FreeSpace";
                string capacity = "System.Capacity";
                string fileSystem = "System.Volume.FileSystem";

                var properties = await diskRoot.Properties.RetrievePropertiesAsync(new[] { freeSpace, capacity, fileSystem });

                ViewModel.DriveCapacityValue = (ulong)properties[capacity];
                ViewModel.DriveFreeSpaceValue = (ulong)properties[freeSpace];
                ViewModel.DriveUsedSpaceValue = ViewModel.DriveCapacityValue - ViewModel.DriveFreeSpaceValue;
                ViewModel.DriveFileSystem = (string)properties[fileSystem];
            }
            catch (Exception e)
            {
                ViewModel.LastSeparatorVisibility = Visibility.Collapsed;
                App.Logger.Warn(e, e.Message);
            }
        }
    }
}