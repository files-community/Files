using Files.DataModels.NavigationControlItems;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading.Tasks;
using Windows.Storage;
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
                ViewModel.LoadCustomIcon = true;
                ViewModel.ItemName = Drive.Text;
                ViewModel.OriginalItemName = Drive.Text;
                // Note: if DriveType enum changes, the corresponding resource keys should change too
                ViewModel.ItemType = string.Format("DriveType{0}", Drive.Type).GetLocalized();
            }
        }

        public override void GetSpecialProperties()
        {
            ViewModel.ItemAttributesVisibility = Visibility.Collapsed;
            StorageFolder diskRoot = Task.Run(async () => await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(Drive.Path)).Result;

            string freeSpace = "System.FreeSpace";
            string capacity = "System.Capacity";
            string fileSystem = "System.Volume.FileSystem";

            try
            {
                var properties = Task.Run(async () =>
                {
                    return await diskRoot.Properties.RetrievePropertiesAsync(new[] { freeSpace, capacity, fileSystem });
                }).Result;

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