using Files.DataModels.NavigationControlItems;
using Files.Extensions;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading.Tasks;
using Windows.Storage;
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
            StorageFolder diskRoot = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(Drive.Path);
            if (diskRoot == null)
            {
                return;
            }

            string freeSpace = "System.FreeSpace";
            string capacity = "System.Capacity";
            string fileSystem = "System.Volume.FileSystem";

            try
            {
                if (ViewModel.LoadFileIcon)
                {
                    var thumbnail = await diskRoot.GetThumbnailAsync(ThumbnailMode.SingleItem, 80, ThumbnailOptions.UseCurrentScale);
                    ViewModel.IconData = await thumbnail.ToByteArrayAsync();
                }

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