using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Files.View_Models.Properties
{
    class DriveProperties : BaseProperties
    {
        public DriveProperties(SelectedItemsPropertiesViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public override void GetProperties()
        {
            ViewModel.ItemAttributesVisibility = Visibility.Collapsed;
            StorageFolder diskRoot = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(ViewModel.Drive.Path)).Result;

            try
            {
                var properties = Task.Run(async () =>
                {
                    return await diskRoot.Properties.RetrievePropertiesAsync(new[] {
                    "System.FreeSpace",
                    "System.Capacity",
                    "System.Volume.FileSystem" });
                }).Result;

                ViewModel.DriveCapacityValue = (ulong)properties["System.Capacity"];
                ViewModel.DriveFreeSpaceValue = (ulong)properties["System.FreeSpace"];
                ViewModel.DriveUsedSpaceValue = ViewModel.DriveCapacityValue - ViewModel.DriveFreeSpaceValue;
                ViewModel.DriveFileSystem = (string)properties["System.Volume.FileSystem"];
            }
            catch (Exception e)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(e, e.Message);
            }
        }
    }
}
