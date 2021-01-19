using Files.Filesystem;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;

namespace Files.Views
{
    public sealed partial class PropertiesGeneral : PropertiesTab
    {
        public PropertiesGeneral()
        {
            this.InitializeComponent();
            base.ItemMD5HashProgress = ItemMD5HashProgress;
        }

        public async Task SaveChangesAsync(ListedItem item)
        {
            if (BaseProperties is DriveProperties)
            {
                var drive = (BaseProperties as DriveProperties).Drive;
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    if (AppInstance.FilesystemViewModel != null)
                    {
                        await AppInstance.FilesystemViewModel.Connection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "SetVolumeLabel" },
                            { "drivename", drive.Path },
                            { "newlabel", ViewModel.ItemName }
                        });
                        _ = CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                        {
                            await drive.UpdateLabelAsync();
                            await AppInstance.FilesystemViewModel.SetWorkingDirectoryAsync(drive.Path);
                        });
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => AppInstance.InteractionOperations.RenameFileItemAsync(item,
                          ViewModel.OriginalItemName,
                          ViewModel.ItemName));
                }
            }
        }
    }
}