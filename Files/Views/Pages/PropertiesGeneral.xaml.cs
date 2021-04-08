using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;
using System.IO;
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
                        _ = CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
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
                var newName = ViewModel.ItemName;
                if (!string.IsNullOrWhiteSpace(newName) && ViewModel.OriginalItemName != newName)
                {
                    if (AppInstance.FilesystemViewModel != null && App.LibraryManager.CanCreateLibrary(newName).result)
                    {
                        var libraryPath = library.ItemPath;
                        var renamed = await AppInstance.FilesystemHelpers.RenameAsync(new StorageFileWithPath(null, libraryPath), newName, Windows.Storage.NameCollisionOption.FailIfExists, false);
                        if (renamed == Enums.ReturnResult.Success)
                        {
                            var newPath = Path.Combine(Path.GetDirectoryName(libraryPath), $"{newName}{ShellLibraryItem.EXTENSION}");
                            _ = CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                await AppInstance.FilesystemViewModel?.SetWorkingDirectoryAsync(newPath);
                            });
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(ViewModel.ItemName) && ViewModel.OriginalItemName != ViewModel.ItemName)
                {
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIFilesystemHelpers.RenameFileItemAsync(item,
                          ViewModel.OriginalItemName,
                          ViewModel.ItemName,
                          AppInstance));
                }

                // Handle the hidden attribute
                if (BaseProperties is CombinedProperties combinedProps)
                {
                    // Handle each file independently
                    foreach (var fileOrFolder in combinedProps.List)
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIFilesystemHelpers.SetHiddenAttributeItem(fileOrFolder, ViewModel.IsHidden, AppInstance.SlimContentPage.ItemManipulationModel));
                    }
                }
                else
                {
                    // Handle the visibility attribute for a single file
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIFilesystemHelpers.SetHiddenAttributeItem(item, ViewModel.IsHidden, AppInstance.SlimContentPage.ItemManipulationModel));
                }
            }
        }
    }
}