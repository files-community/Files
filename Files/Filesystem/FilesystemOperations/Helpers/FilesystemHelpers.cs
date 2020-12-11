using Files.Dialogs;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Files.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Text.RegularExpressions;
using FileAttributes = System.IO.FileAttributes;
using static Files.Helpers.NativeFindStorageItemHelper;
using Files.Enums;

namespace Files.Filesystem
{
    public class FilesystemHelpers : IFilesystemHelpers
    {
        #region Private Members

        private IShellPage associatedInstance;

        private IFilesystemOperations filesystemOperations;

        private RecycleBinHelpers recycleBinHelpers;

        private readonly CancellationToken cancellationToken;

        #region Helpers Members

        private static readonly List<string> RestrictedFileNames = new List<string>()
        {
                "CON", "PRN", "AUX",
                "NUL", "COM1", "COM2",
                "COM3", "COM4", "COM5",
                "COM6", "COM7", "COM8",
                "COM9", "LPT1", "LPT2",
                "LPT3", "LPT4", "LPT5",
                "LPT6", "LPT7", "LPT8", "LPT9"
        };

        #endregion

        #endregion

        #region Constructor

        public FilesystemHelpers(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this.associatedInstance = associatedInstance;
            this.cancellationToken = cancellationToken;
            filesystemOperations = new FilesystemOperations(this.associatedInstance);
            recycleBinHelpers = new RecycleBinHelpers(this.associatedInstance);
        }

        #endregion

        #region IFilesystemHelpers

        #region Create

        public async Task<ReturnResult> CreateAsync(PathWithType source, bool registerHistory)
        {
            FilesystemErrorCode returnCode = FilesystemErrorCode.ERROR_INPROGRESS;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await filesystemOperations.CreateAsync(source, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            return returnCode.ToStatus();
        }

        #endregion

        #region Delete

        public async Task<ReturnResult> DeleteItemsAsync(IEnumerable<PathWithType> source, bool showDialog, bool permanently, bool registerHistory)
        {
            bool deleteFromRecycleBin = false;
            foreach (PathWithType item in source)
            {
                if (await recycleBinHelpers.IsRecycleBinItem(item.Path))
                {
                    deleteFromRecycleBin = true;
                    break;
                }
            }

            PostedStatusBanner banner;
            if (permanently)
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Delete);
            }
            else
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Recycle);
            }

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            if (App.AppSettings.ShowConfirmDeleteDialog && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                ConfirmDeleteDialog dialog = new ConfirmDeleteDialog(
                    deleteFromRecycleBin,
                    !deleteFromRecycleBin ? permanently : deleteFromRecycleBin,
                    associatedInstance.ContentPage.SelectedItemsPropertiesViewModel);

                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected  items if the result is Yes
                {
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history;
            var rawStorageHistory = new List<IStorageHistory>();

            bool originalPermanently = permanently;
            foreach (PathWithType item in source)
            {
                if (await recycleBinHelpers.IsRecycleBinItem(item.Path))
                {
                    permanently = true;
                }
                else
                {
                    permanently = originalPermanently;
                }

                rawStorageHistory.Add(await filesystemOperations.DeleteAsync(item, banner.Progress, banner.ErrorCode, permanently, cancellationToken));
            }

            if (rawStorageHistory.TrueForAll((item) => item != null))
            {
                history = new StorageHistory(
                    rawStorageHistory[0].OperationType,
                    rawStorageHistory.SelectMany((item) => item.Source).ToList(),
                    rawStorageHistory.SelectMany((item) => item.Destination).ToList());

                if (!permanently && registerHistory)
                {
                    App.HistoryWrapper.AddHistory(history);
                }
            }

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(returnStatus, permanently ? FileOperationType.Delete : FileOperationType.Recycle, sw, associatedInstance);

            return returnStatus;
        }

        public async Task<ReturnResult> DeleteItemAsync(PathWithType source, bool showDialog, bool permanently, bool registerHistory)
        {
            PostedStatusBanner banner;
            bool deleteFromRecycleBin = await recycleBinHelpers.IsRecycleBinItem(source.Path);

            if (deleteFromRecycleBin)
            {
                permanently = true;
            }

            if (permanently)
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Delete);
            }
            else
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Recycle);
            }

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            if (App.AppSettings.ShowConfirmDeleteDialog && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                ConfirmDeleteDialog dialog = new ConfirmDeleteDialog(
                    deleteFromRecycleBin,
                    permanently,
                    associatedInstance.ContentPage.SelectedItemsPropertiesViewModel);

                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected item if the result is Yes
                {
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history = await filesystemOperations.DeleteAsync(source, banner.Progress, banner.ErrorCode, permanently, cancellationToken);

            if (!permanently && registerHistory)
            {
                App.HistoryWrapper.AddHistory(history);
            }

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(returnStatus, permanently ? FileOperationType.Delete : FileOperationType.Recycle, sw, associatedInstance);

            return returnStatus;
        }

        public async Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItem> source, bool showDialog, bool permanently, bool registerHistory)
        {
            bool deleteFromRecycleBin = false;
            foreach (IStorageItem item in source)
            {
                if (await recycleBinHelpers.IsRecycleBinItem(item))
                {
                    deleteFromRecycleBin = true;
                    break;
                }
            }

            PostedStatusBanner banner;
            if (permanently)
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Delete);
            }
            else
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Recycle);
            }

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            if (App.AppSettings.ShowConfirmDeleteDialog && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                ConfirmDeleteDialog dialog = new ConfirmDeleteDialog(
                    deleteFromRecycleBin,
                    !deleteFromRecycleBin ? permanently : deleteFromRecycleBin,
                    associatedInstance.ContentPage.SelectedItemsPropertiesViewModel);

                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected  items if the result is Yes
                {
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history;
            List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

            bool originalPermanently = permanently;
            foreach (IStorageItem item in source)
            {
                if (await recycleBinHelpers.IsRecycleBinItem(item))
                {
                    permanently = true;
                }
                else
                {
                    permanently = originalPermanently;
                }

                rawStorageHistory.Add(await filesystemOperations.DeleteAsync(item, banner.Progress, banner.ErrorCode, permanently, cancellationToken));
            }

            if (rawStorageHistory.TrueForAll((item) => item != null))
            {
                history = new StorageHistory(
                    rawStorageHistory[0].OperationType,
                    rawStorageHistory.SelectMany((item) => item.Source).ToList(),
                    rawStorageHistory.SelectMany((item) => item.Destination).ToList());

                if (!permanently && registerHistory)
                {
                    App.HistoryWrapper.AddHistory(history);
                }
            }

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(returnStatus, permanently ? FileOperationType.Delete : FileOperationType.Recycle, sw, associatedInstance);

            return returnStatus;
        }

        public async Task<ReturnResult> DeleteItemAsync(IStorageItem source, bool showDialog, bool permanently, bool registerHistory)
        {
            PostedStatusBanner banner;
            bool deleteFromRecycleBin = await recycleBinHelpers.IsRecycleBinItem(source);

            if (deleteFromRecycleBin)
            {
                permanently = true;
            }

            if (permanently)
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Delete);
            }
            else
            {
                banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Recycle);
            }

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            if (App.AppSettings.ShowConfirmDeleteDialog && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                ConfirmDeleteDialog dialog = new ConfirmDeleteDialog(
                    deleteFromRecycleBin,
                    permanently,
                    associatedInstance.ContentPage.SelectedItemsPropertiesViewModel);

                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected item if the result is Yes
                {
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history = await filesystemOperations.DeleteAsync(source, banner.Progress, banner.ErrorCode, permanently, cancellationToken);

            if (!permanently && registerHistory)
            {
                App.HistoryWrapper.AddHistory(history);
            }

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(returnStatus, permanently ? FileOperationType.Delete : FileOperationType.Recycle, sw, associatedInstance);

            return returnStatus;
        }

        #endregion

        public async Task<ReturnResult> RestoreFromTrashAsync(PathWithType source, string destination, bool registerHistory)
        {
            FilesystemErrorCode returnCode = FilesystemErrorCode.ERROR_INPROGRESS;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await filesystemOperations.RestoreFromTrashAsync(source, destination, null, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            return returnCode.ToStatus();
        }

        public async Task<ReturnResult> PerformOperationTypeAsync(DataPackageOperation operation,
                                                                  DataPackageView packageView,
                                                                  string destination,
                                                                  bool registerHistory)
        {
            try
            {
                switch (operation)
                {
                    case DataPackageOperation.Copy:
                        return await CopyItemsFromClipboard(packageView, destination, registerHistory);

                    case DataPackageOperation.Move:
                        return await MoveItemsFromClipboard(packageView, destination, registerHistory);

                    case DataPackageOperation.None: // Other
                        return await CopyItemsFromClipboard(packageView, destination, registerHistory);

                    default: return default;
                }
            }
            finally
            {
                packageView.ReportOperationCompleted(operation);
            }
        }

        #region Copy

        public async Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory)
        {
            return await CopyItemsAsync(source.Select((item) => new PathWithType(item.Path, item.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList(), destination, registerHistory);
        }

        public async Task<ReturnResult> CopyItemAsync(IStorageItem source, string destination, bool registerHistory)
        {
            return await CopyItemAsync(new PathWithType(source.Path, source.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory), destination, registerHistory);
        }

        public async Task<ReturnResult> CopyItemsAsync(IEnumerable<PathWithType> source, IEnumerable<string> destination, bool registerHistory)
        {
            PostedStatusBanner banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Copy);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history;
            List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

            associatedInstance.ContentPage.ClearSelection();
            for (int i = 0; i < source.Count(); i++)
            {
                rawStorageHistory.Add(await filesystemOperations.CopyAsync(
                    source.ElementAt(i),
                    destination.ElementAt(i),
                    banner.Progress,
                    banner.ErrorCode,
                    cancellationToken));
            }

            if (rawStorageHistory.TrueForAll((item) => item != null))
            {
                history = new StorageHistory(
                    rawStorageHistory[0].OperationType,
                    rawStorageHistory.SelectMany((item) => item.Source).ToList(),
                    rawStorageHistory.SelectMany((item) => item.Destination).ToList());

                if (registerHistory && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
                {
                    App.HistoryWrapper.AddHistory(history);
                }
            }

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    "Copy Complete",
                    "The operation has completed.",
                    0,
                    ReturnResult.Success,
                    FileOperationType.Copy);
            }

            return returnStatus;
        }

        public async Task<ReturnResult> CopyItemAsync(PathWithType source, string destination, bool registerHistory)
        {
            PostedStatusBanner banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Copy);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            associatedInstance.ContentPage.ClearSelection();
            IStorageHistory history = await filesystemOperations.CopyAsync(source, destination, banner.Progress, banner.ErrorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    "Copy Complete",
                    "The operation has completed.",
                    0,
                    ReturnResult.Success,
                    FileOperationType.Copy);
            }

            return returnStatus;
        }

        public async Task<ReturnResult> CopyItemsFromClipboard(DataPackageView packageView, string destination, bool registerHistory)
        {
            if (!packageView.Contains(StandardDataFormats.StorageItems))
            {
                // Happens if you copy some text and then you Ctrl+V in Files
                // Should this be done in ModernShellPage?
                return ReturnResult.BadArgumentException;
            }

            IReadOnlyList<IStorageItem> source = await packageView.GetStorageItemsAsync();
            ReturnResult returnStatus = ReturnResult.InProgress;

            List<string> destinations = new List<string>();
            foreach (IStorageItem item in source)
            {
                destinations.Add(Path.Combine(destination, item.Name));
            }

            returnStatus = await CopyItemsAsync(source, destinations, true);

            return returnStatus;
        }

        #endregion

        #region Move

        public async Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory)
        {
            return await MoveItemsAsync(source.Select((item) => new PathWithType(item.Path, item.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList(), destination, registerHistory);
        }

        public async Task<ReturnResult> MoveItemAsync(IStorageItem source, string destination, bool registerHistory)
        {
            return await MoveItemAsync(new PathWithType(source.Path, source.IsOfType(StorageItemTypes.File) ? FilesystemItemType.File : FilesystemItemType.Directory), destination, registerHistory);
        }

        public async Task<ReturnResult> MoveItemsAsync(IEnumerable<PathWithType> source, IEnumerable<string> destination, bool registerHistory)
        {
            PostedStatusBanner banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Move);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history;
            List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

            associatedInstance.ContentPage.ClearSelection();
            for (int i = 0; i < source.Count(); i++)
            {
                rawStorageHistory.Add(await filesystemOperations.MoveAsync(
                    source.ElementAt(i),
                    destination.ElementAt(i),
                    banner.Progress,
                    banner.ErrorCode,
                    cancellationToken));
            }

            if (rawStorageHistory.TrueForAll((item) => item != null))
            {
                history = new StorageHistory(
                    rawStorageHistory[0].OperationType,
                    rawStorageHistory.SelectMany((item) => item.Source).ToList(),
                    rawStorageHistory.SelectMany((item) => item.Destination).ToList());

                if (registerHistory && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
                {
                    App.HistoryWrapper.AddHistory(history);
                }
            }

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    "Move Complete",
                    "The operation has completed.",
                    0,
                    ReturnResult.Success,
                    FileOperationType.Move);
            }

            return returnStatus;
        }

        public async Task<ReturnResult> MoveItemAsync(PathWithType source, string destination, bool registerHistory)
        {
            PostedStatusBanner banner = associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                string.Empty,
                associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Move);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            associatedInstance.ContentPage.ClearSelection();
            IStorageHistory history = await filesystemOperations.MoveAsync(source, destination, banner.Progress, banner.ErrorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    "Move Complete",
                    "The operation has completed.",
                    0,
                    ReturnResult.Success,
                    FileOperationType.Move);
            }

            return returnStatus;
        }

        public async Task<ReturnResult> MoveItemsFromClipboard(DataPackageView packageView, string destination, bool registerHistory)
        {
            if (!packageView.Contains(StandardDataFormats.StorageItems))
            {
                // Happens if you copy some text and then you Ctrl+V in Files
                // Should this be done in ModernShellPage?
                return ReturnResult.BadArgumentException;
            }

            IReadOnlyList<IStorageItem> source = await packageView.GetStorageItemsAsync();
            ReturnResult returnStatus = ReturnResult.InProgress;

            List<string> destinations = new List<string>();
            foreach (IStorageItem item in source)
            {
                destinations.Add(Path.Combine(destination, item.Name));
            }

            returnStatus = await MoveItemsAsync(source, destinations, true);

            return returnStatus;
        }

        #endregion

        #region Rename

        public async Task<ReturnResult> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, bool registerHistory)
        {
            FilesystemErrorCode returnCode = FilesystemErrorCode.ERROR_INPROGRESS;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            return returnCode.ToStatus();
        }

        public async Task<ReturnResult> RenameAsync(PathWithType source, string newName, NameCollisionOption collision, bool registerHistory)
        {
            FilesystemErrorCode returnCode = FilesystemErrorCode.ERROR_INPROGRESS;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            return returnCode.ToStatus();
        }

        #endregion

        #endregion

        #region Public Helpers

        public async static Task<StorageFolder> CloneDirectoryAsync(IStorageFolder sourceFolder, IStorageFolder destinationFolder, string sourceRootName)
        {
            StorageFolder createdRoot = await destinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            destinationFolder = createdRoot;

            foreach (IStorageFile fileInSourceDir in await sourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.CopyAsync(destinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }

            foreach (IStorageFolder folderinSourceDir in await sourceFolder.GetFoldersAsync())
            {
                await CloneDirectoryAsync(folderinSourceDir, destinationFolder, folderinSourceDir.Name);
            }

            return createdRoot;
        }

        public static async Task<StorageFolder> MoveDirectoryAsync(IStorageFolder sourceFolder, IStorageFolder destinationDirectory, string sourceRootName, CreationCollisionOption collision = CreationCollisionOption.FailIfExists)
        {
            StorageFolder createdRoot = await destinationDirectory.CreateFolderAsync(sourceRootName, collision);
            destinationDirectory = createdRoot;

            foreach (StorageFile fileInSourceDir in await sourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.MoveAsync(destinationDirectory, fileInSourceDir.Name, (NameCollisionOption)((int)collision));
            }

            foreach (StorageFolder folderinSourceDir in await sourceFolder.GetFoldersAsync())
            {
                await MoveDirectoryAsync(folderinSourceDir, destinationDirectory, folderinSourceDir.Name);
            }

            App.JumpList.RemoveFolder(sourceFolder.Path);

            return createdRoot;
        }

        public static async Task<long> GetItemSize(IStorageItem item)
        {
            if (item == null)
            {
                return 0L;
            }

            if (item.IsOfType(StorageItemTypes.Folder))
            {
                return await CalculateFolderSizeAsync(item.Path);
            }
            else
            {
                return CalculateFileSize(item.Path);
            }
        }

        public static async Task<long> CalculateFolderSizeAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return 0;
            }

            long size = 0;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(
                path + "\\*.*",
                findInfoLevel,
                out WIN32_FIND_DATA findData,
                FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                IntPtr.Zero,
                additionalFlags);

            int count = 0;
            if (hFile.ToInt64() != -1)
            {
                do
                {
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        size += findData.GetSize();
                        ++count;
                    }
                    else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            string itemPath = Path.Combine(path, findData.cFileName);

                            size += await CalculateFolderSizeAsync(itemPath);
                            ++count;
                        }
                    }
                } while (FindNextFile(hFile, out findData));
                FindClose(hFile);
                return size;
            }
            else
            {
                return 0;
            }
        }

        public static long CalculateFileSize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                // In MTP devices calculating folder size would be too slow
                // Also should use StorageFolder methods instead of FindFirstFileExFromApp
                return 0;
            }

            long size = 0;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(path, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);

            if (hFile.ToInt64() != -1)
            {
                if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                {
                    size += findData.GetSize();
                }
                FindClose(hFile);
                Debug.WriteLine("Individual file size for Progress UI will be reported as: " + size.ToString() + " bytes");
                return size;
            }
            else
            {
                return 0;
            }
        }

        public static bool ContainsRestrictedCharacters(string input)
        {
            Regex regex = new Regex("\\\\|\\/|\\:|\\*|\\?|\\\"|\\<|\\>|\\|"); // Restricted symbols for file names
            MatchCollection matches = regex.Matches(input);

            if (matches.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static bool ContainsRestrictedFileName(string input)
        {
            foreach (string name in RestrictedFileNames)
            {
                Regex regex = new Regex($"^{name}($|\\.)(.+)?");
                MatchCollection matches = regex.Matches(input.ToUpper());

                if (matches.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            associatedInstance?.Dispose();
            filesystemOperations?.Dispose();
            recycleBinHelpers?.Dispose();

            associatedInstance = null;
            filesystemOperations = null;
            recycleBinHelpers = null;
        }

        #endregion
    }
}
