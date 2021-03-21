using System;
using Files.DataModels;
using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.Helpers
{
    public static class UIFilesystemHelpers
    {
        public static async Task PasteItemAsync(string destinationPath, IShellPage associatedInstance)
        {
            DataPackageView packageView = await FilesystemTasks.Wrap(() => Task.FromResult(Clipboard.GetContent()));
            if (packageView != null)
            {
                await associatedInstance.FilesystemHelpers.PerformOperationTypeAsync(packageView.RequestedOperation, packageView, destinationPath, true);
                associatedInstance.SlimContentPage.ResetItemOpacity();
            }
        }

        public static async Task<bool> RenameFileItemAsync(ListedItem item, string oldName, string newName, IShellPage associatedInstance)
        {
            if (oldName == newName)
            {
                return true;
            }

            ReturnResult renamed = ReturnResult.InProgress;
            if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                renamed = await associatedInstance.FilesystemHelpers.RenameAsync(StorageItemHelpers.FromPathAndType(item.ItemPath, FilesystemItemType.Directory),
                    newName, NameCollisionOption.FailIfExists, true);
            }
            else
            {
                if (item.IsShortcutItem || !App.AppSettings.ShowFileExtensions)
                {
                    newName += item.FileExtension;
                }

                renamed = await associatedInstance.FilesystemHelpers.RenameAsync(StorageItemHelpers.FromPathAndType(item.ItemPath, FilesystemItemType.File),
                    newName, NameCollisionOption.FailIfExists, true);
            }

            if (renamed == ReturnResult.Success)
            {
                associatedInstance.NavigationToolbar.CanGoForward = false;
                return true;
            }
            return false;
        }

        public static async void CreateFileFromDialogResultType(AddItemType itemType, ShellNewEntry itemInfo, IShellPage associatedInstance)
        {
            string currentPath = null;
            if (associatedInstance.SlimContentPage != null)
            {
                currentPath = associatedInstance.FilesystemViewModel.WorkingDirectory;
            }

            // Show rename dialog
            DynamicDialog dialog = DynamicDialogFactory.GetFor_RenameDialog();
            await dialog.ShowAsync();

            if (dialog.DynamicResult != DynamicDialogResult.Primary)
            {
                return;
            }

            // Create file based on dialog result
            string userInput = dialog.ViewModel.AdditionalData as string;
            var folderRes = await associatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(currentPath);
            FilesystemResult created = folderRes;
            if (folderRes)
            {
                switch (itemType)
                {
                    case AddItemType.Folder:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalized();
                        created = await FilesystemTasks.Wrap(async () =>
                        {
                            return await associatedInstance.FilesystemHelpers.CreateAsync(
                                StorageItemHelpers.FromPathAndType(System.IO.Path.Combine(folderRes.Result.Path, userInput), FilesystemItemType.Directory),
                                true);
                        });
                        break;

                    case AddItemType.File:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalized();
                        created = await FilesystemTasks.Wrap(async () =>
                        {
                            return await associatedInstance.FilesystemHelpers.CreateAsync(
                                StorageItemHelpers.FromPathAndType(System.IO.Path.Combine(folderRes.Result.Path, userInput + itemInfo?.Extension), FilesystemItemType.File),
                                true);
                        });
                        break;
                }
            }
            if (created == FileSystemStatusCode.Unauthorized)
            {
                await DialogDisplayHelper.ShowDialogAsync("AccessDeniedCreateDialog/Title".GetLocalized(), "AccessDeniedCreateDialog/Text".GetLocalized());
            }
        }

        /// <summary>
        /// Set a single file or folder to hidden or unhidden an refresh the
        /// view after setting the flag
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isHidden"></param>
        public static void SetHiddenAttributeItem(ListedItem item, bool isHidden, IBaseLayout slimContentPage)
        {
            item.IsHiddenItem = isHidden;
            slimContentPage.ResetItemOpacity();
        }
    }
}
