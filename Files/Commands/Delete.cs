using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;
using static Files.Dialogs.ConfirmDeleteDialog;

namespace Files.Commands
{
    public partial class ItemOperations
    {
        public async void DeleteItemWithStatus(StorageDeleteOption deleteOption)
        {
            var deleteFromRecycleBin = AppInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);
            if (deleteFromRecycleBin)
            {
                // Permanently delete if deleting from recycle bin
                deleteOption = StorageDeleteOption.PermanentDelete;
            }

            PostedStatusBanner bannerResult = null;
            if (deleteOption == StorageDeleteOption.PermanentDelete)
            {
                bannerResult = AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(null,
                AppInstance.FilesystemViewModel.WorkingDirectory,
                0,
                StatusBanner.StatusBannerSeverity.Ongoing,
                StatusBanner.StatusBannerOperation.Delete);
            }
            else
            {
                bannerResult = AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(null,
                AppInstance.FilesystemViewModel.WorkingDirectory,
                0,
                StatusBanner.StatusBannerSeverity.Ongoing,
                StatusBanner.StatusBannerOperation.Recycle);
            }

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                await DeleteItem(deleteOption, AppInstance, bannerResult.Progress);
                bannerResult.Remove();

                sw.Stop();

                if (sw.Elapsed.TotalSeconds >= 10)
                {
                    if (deleteOption == StorageDeleteOption.PermanentDelete)
                    {
                        AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "Deletion Complete",
                        "The operation has completed.",
                        0,
                        StatusBanner.StatusBannerSeverity.Success,
                        StatusBanner.StatusBannerOperation.Delete);
                    }
                    else
                    {
                        AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "Recycle Complete",
                        "The operation has completed.",
                        0,
                        StatusBanner.StatusBannerSeverity.Success,
                        StatusBanner.StatusBannerOperation.Recycle);
                    }
                }

                AppInstance.NavigationToolbar.CanGoForward = false;
            }
            catch (UnauthorizedAccessException)
            {
                bannerResult.Remove();
                AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    ResourceController.GetTranslation("AccessDeniedDeleteDialog/Title"),
                    ResourceController.GetTranslation("AccessDeniedDeleteDialog/Text"),
                    0,
                    StatusBanner.StatusBannerSeverity.Error,
                    StatusBanner.StatusBannerOperation.Delete);
            }
            catch (FileNotFoundException)
            {
                bannerResult.Remove();
                AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    ResourceController.GetTranslation("FileNotFoundDialog/Title"),
                    ResourceController.GetTranslation("FileNotFoundDialog/Text"),
                    0,
                    StatusBanner.StatusBannerSeverity.Error,
                    StatusBanner.StatusBannerOperation.Delete);
            }
            catch (IOException)
            {
                bannerResult.Remove();
                AppInstance.BottomStatusStripControl.OngoingTasksControl.PostActionBanner(
                    ResourceController.GetTranslation("FileInUseDeleteDialog/Title"),
                    ResourceController.GetTranslation("FileInUseDeleteDialog/Text"),
                    ResourceController.GetTranslation("FileInUseDeleteDialog/PrimaryButtonText"),
                    ResourceController.GetTranslation("FileInUseDeleteDialog/SecondaryButtonText"), () => { DeleteItemWithStatus(deleteOption); });
            }
        }

        private async Task DeleteItem(StorageDeleteOption deleteOption, IShellPage AppInstance, IProgress<uint> progress)
        {
            var deleteFromRecycleBin = AppInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);

            List<ListedItem> selectedItems = new List<ListedItem>();
            foreach (ListedItem selectedItem in AppInstance.ContentPage.SelectedItems)
            {
                selectedItems.Add(selectedItem);
            }

            if (App.AppSettings.ShowConfirmDeleteDialog == true) //check if the setting to show a confirmation dialog is on
            {
                var dialog = new ConfirmDeleteDialog(deleteFromRecycleBin, deleteOption);
                dialog.SelectedItemsPropertiesViewModel = AppInstance.ContentPage.SelectedItemsPropertiesViewModel;
                await dialog.ShowAsync();

                if (dialog.Result != MyResult.Delete) //delete selected  item(s) if the result is yes
                {
                    return; //return if the result isn't delete
                }
                deleteOption = dialog.PermanentlyDelete;
            }

            int itemsDeleted = 0;
            foreach (ListedItem storItem in selectedItems)
            {
                uint progressValue = (uint)(itemsDeleted * 100.0 / selectedItems.Count);
                if (selectedItems.Count > 3) { progress.Report((uint)progressValue); }

                IStorageItem item;
                try
                {
                    if (storItem.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        item = await AppInstance.FilesystemViewModel.GetFileFromPathAsync(storItem.ItemPath);
                    }
                    else
                    {
                        item = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(storItem.ItemPath);
                    }

                    await item.DeleteAsync(deleteOption);
                }
                catch (UnauthorizedAccessException)
                {
                    if (deleteOption == StorageDeleteOption.Default)
                    {
                        // Try again with fulltrust process
                        if (AppInstance.FilesystemViewModel.Connection != null)
                        {
                            var result = await AppInstance.FilesystemViewModel.Connection.SendMessageAsync(new ValueSet() {
                            { "Arguments", "FileOperation" },
                            { "fileop", "MoveToBin" },
                            { "filepath", storItem.ItemPath } });
                        }
                    }
                    else
                    {
                        // Try again with DeleteFileFromApp
                        if (!NativeDirectoryChangesHelper.DeleteFileFromApp(storItem.ItemPath))
                        {
                            Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        }
                    }
                }
                catch (FileLoadException)
                {
                    // try again
                    if (storItem.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        item = await AppInstance.FilesystemViewModel.GetFileFromPathAsync(storItem.ItemPath);
                    }
                    else
                    {
                        item = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(storItem.ItemPath);
                    }

                    await item.DeleteAsync(deleteOption);
                }

                if (deleteFromRecycleBin)
                {
                    // Recycle bin also stores a file starting with $I for each item
                    var iFilePath = Path.Combine(Path.GetDirectoryName(storItem.ItemPath), Path.GetFileName(storItem.ItemPath).Replace("$R", "$I"));
                    await (await AppInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)).DeleteAsync(StorageDeleteOption.PermanentDelete);
                }

                AppInstance.FilesystemViewModel.RemoveFileOrFolder(storItem);
                itemsDeleted++;
            }
        }
    }
}