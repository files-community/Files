using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Microsoft.Toolkit.Uwp.Extensions;
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

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var res = await DeleteItemAsync(deleteOption, AppInstance, bannerResult.Progress);
            bannerResult.Remove();
            sw.Stop();
            if (!res)
            {
                if (res.ErrorCode == FilesystemErrorCode.ERROR_UNAUTHORIZED)
                {
                    bannerResult.Remove();
                    AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        StatusBanner.StatusBannerSeverity.Error,
                        StatusBanner.StatusBannerOperation.Delete);
                }
                else if (res.ErrorCode == FilesystemErrorCode.ERROR_NOTFOUND)
                {
                    bannerResult.Remove();
                    AppInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        StatusBanner.StatusBannerSeverity.Error,
                        StatusBanner.StatusBannerOperation.Delete);
                }
                else if (res.ErrorCode == FilesystemErrorCode.ERROR_INUSE)
                {
                    bannerResult.Remove();
                    AppInstance.BottomStatusStripControl.OngoingTasksControl.PostActionBanner(
                        "FileInUseDeleteDialog/Title".GetLocalized(),
                        "FileInUseDeleteDialog/Text".GetLocalized(),
                        "FileInUseDeleteDialog/PrimaryButtonText".GetLocalized(),
                        "FileInUseDeleteDialog/SecondaryButtonText".GetLocalized(), () => { DeleteItemWithStatus(deleteOption); });
                }
            }
            else if (sw.Elapsed.TotalSeconds >= 10)
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

        private async Task<FilesystemResult> DeleteItemAsync(StorageDeleteOption deleteOption, IShellPage AppInstance, IProgress<uint> progress)
        {
            var deleted = (FilesystemResult)false;
            var deleteFromRecycleBin = AppInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);

            List<ListedItem> selectedItems = new List<ListedItem>();
            foreach (ListedItem selectedItem in AppInstance.ContentPage.SelectedItems)
            {
                selectedItems.Add(selectedItem);
            }

            if (App.AppSettings.ShowConfirmDeleteDialog == true) //check if the setting to show a confirmation dialog is on
            {
                var dialog = new ConfirmDeleteDialog(deleteFromRecycleBin, deleteOption, AppInstance.ContentPage.SelectedItemsPropertiesViewModel);
                await dialog.ShowAsync();

                if (dialog.Result != MyResult.Delete) //delete selected  item(s) if the result is yes
                {
                    return (FilesystemResult)true; //return if the result isn't delete
                }
                deleteOption = dialog.PermanentlyDelete;
            }

            int itemsDeleted = 0;
            foreach (ListedItem storItem in selectedItems)
            {
                uint progressValue = (uint)(itemsDeleted * 100.0 / selectedItems.Count);
                if (selectedItems.Count > 3) { progress.Report((uint)progressValue); }

                if (storItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    deleted = await AppInstance.FilesystemViewModel.GetFileFromPathAsync(storItem.ItemPath)
                        .OnSuccess(t => t.DeleteAsync(deleteOption).AsTask());
                }
                else
                {
                    deleted = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(storItem.ItemPath)
                        .OnSuccess(t => t.DeleteAsync(deleteOption).AsTask());
                }

                if (deleted.ErrorCode == FilesystemErrorCode.ERROR_UNAUTHORIZED)
                {
                    if (deleteOption == StorageDeleteOption.Default)
                    {
                        // Try again with fulltrust process
                        if (AppInstance.FilesystemViewModel.Connection != null)
                        {
                            var response = await AppInstance.FilesystemViewModel.Connection.SendMessageAsync(new ValueSet() {
                                { "Arguments", "FileOperation" },
                                { "fileop", "MoveToBin" },
                                { "filepath", storItem.ItemPath } });
                            deleted = (FilesystemResult)(response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success);
                        }
                    }
                    else
                    {
                        // Try again with DeleteFileFromApp
                        if (!NativeDirectoryChangesHelper.DeleteFileFromApp(storItem.ItemPath))
                        {
                            Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        }
                        else
                        {
                            deleted = (FilesystemResult)true;
                        }
                    }
                }
                else if (deleted.ErrorCode == FilesystemErrorCode.ERROR_INUSE)
                {
                    // TODO: retry or show dialog
                    await DialogDisplayHelper.ShowDialogAsync("FileInUseDeleteDialog/Title".GetLocalized(), "FileInUseDeleteDialog/Text".GetLocalized());
                }

                if (deleteFromRecycleBin)
                {
                    // Recycle bin also stores a file starting with $I for each item
                    var iFilePath = Path.Combine(Path.GetDirectoryName(storItem.ItemPath), Path.GetFileName(storItem.ItemPath).Replace("$R", "$I"));
                    await AppInstance.FilesystemViewModel.GetFileFromPathAsync(iFilePath)
                        .OnSuccess(t => t.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
                }

                if (deleted)
                {
                    await AppInstance.FilesystemViewModel.RemoveFileOrFolderAsync(storItem);
                    itemsDeleted++;
                }
                else
                {
                    // Stop at first error
                    return deleted;
                }
            }
            return (FilesystemResult)true;
        }
    }
}