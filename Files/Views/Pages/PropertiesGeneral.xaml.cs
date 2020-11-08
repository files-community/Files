using Files.Filesystem;
using Files.Helpers;
using Files.View_Models.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;

namespace Files
{
    public sealed partial class PropertiesGeneral : PropertiesTab
    {
        public PropertiesGeneral()
        {
            this.InitializeComponent();
            base.ItemMD5HashProgress = ItemMD5HashProgress;
        }

        public async Task SaveChanges(ListedItem item)
        {
            if (BaseProperties is DriveProperties)
            {
                var drive = (BaseProperties as DriveProperties).Drive;
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    if (App.Connection != null)
                    {
                        await App.Connection.SendMessageAsync(new ValueSet() {
                            { "Arguments", "SetVolumeLabel" },
                            { "drivename", drive.Path },
                            { "newlabel", ViewModel.ItemName }});
                        _ = CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                        {
                            await drive.Update();
                            await App.CurrentInstance.FilesystemViewModel.SetWorkingDirectory(drive.Path);
                        });
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => AppInstance.InteractionOperations.RenameFileItem(item,
                          ViewModel.OriginalItemName,
                          ViewModel.ItemName));
                }
            }
        }
    }
}