using Files.Filesystem;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
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
            if (BaseProperties is DriveProperties driveProps)
            {
                var drive = driveProps.Drive;
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    if (AppInstance.FilesystemViewModel != null)
                    {
                        await AppInstance.ServiceConnection?.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "SetVolumeLabel" },
                            { "drivename", drive.Path },
                            { "newlabel", ViewModel.ItemName }
                        });
                        _ = CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                        {
                            await drive.UpdateLabelAsync();
                            await AppInstance.FilesystemViewModel?.SetWorkingDirectoryAsync(drive.Path);
                        });
                    }
                }
            }
            else if (BaseProperties is LibraryProperties libProps)
            {
                var library = libProps.Library;
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    if (AppInstance.FilesystemViewModel != null)
                    {
                        var newLibrary = await App.LibraryManager.RenameLibrary(library.ItemPath, ViewModel.ItemName);
                        if (newLibrary != null)
                        {
                            _ = CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                            {
                                libProps.UpdateLibrary(new LibraryItem(newLibrary));
                                await AppInstance.FilesystemViewModel?.SetWorkingDirectoryAsync(library.ItemPath);
                            });
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => AppInstance.InteractionOperations?.RenameFileItemAsync(item,
                          ViewModel.OriginalItemName,
                          ViewModel.ItemName));
                }

                // Handle the hidden attribute
                if (BaseProperties is CombinedProperties combinedProps)
                {
                    // Handle each file independently
                    foreach (var fileOrFolder in combinedProps.List)
                    {
                        await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => AppInstance.InteractionOperations?.SetHiddenAttributeItem(fileOrFolder, ViewModel.IsHidden));
                    }
                }
                else
                {
                    // Handle the visibility attribute for a single file
                    await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => AppInstance.InteractionOperations?.SetHiddenAttributeItem(item, ViewModel.IsHidden));
                }
            }
        }
    }
}