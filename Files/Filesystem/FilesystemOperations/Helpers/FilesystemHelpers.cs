using Files.Dialogs;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Files.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

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

        #endregion Helpers Members

        #endregion Private Members

        #region Constructor

        public FilesystemHelpers(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this.associatedInstance = associatedInstance;
            this.cancellationToken = cancellationToken;
            filesystemOperations = new FilesystemOperations(this.associatedInstance);
            recycleBinHelpers = new RecycleBinHelpers(this.associatedInstance);
        }

        #endregion Constructor

        #region IFilesystemHelpers

        #region Create

        public async Task<ReturnResult> CreateAsync(IStorageItemWithPath source, bool registerHistory)
        {
            FileSystemStatusCode returnCode = FileSystemStatusCode.InProgress;
            Progress<FileSystemStatusCode> errorCode = new Progress<FileSystemStatusCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await filesystemOperations.CreateAsync(source, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            return returnCode.ToStatus();
        }

        #endregion Create

        #region Delete

        public async Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItemWithPath> source, bool showDialog, bool permanently, bool registerHistory)
        {
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

            var pathsUnderRecycleBin = GetPathsUnderRecycleBin(source);

            if (App.AppSettings.ShowConfirmDeleteDialog && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                var deleteFromRecycleBin = pathsUnderRecycleBin.Count > 0;

                ConfirmDeleteDialog dialog = new ConfirmDeleteDialog(
                    deleteFromRecycleBin,
                    !deleteFromRecycleBin ? permanently : deleteFromRecycleBin,
                    associatedInstance.ContentPage.SelectedItemsPropertiesViewModel);

                if (Interacts.Interaction.IsAnyContentDialogOpen())
                {
                    // Can show only one dialog at a time
                    banner.Remove();
                    return ReturnResult.Cancelled;
                }
                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected items if the result is Yes
                {
                    banner.Remove();
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history;
            var rawStorageHistory = new List<IStorageHistory>();

            bool originalPermanently = permanently;
            float progress;
            for (int i = 0; i < source.Count(); i++)
            {
                if (pathsUnderRecycleBin.Contains(source.ElementAt(i).Path))
                {
                    permanently = true;
                }
                else
                {
                    permanently = originalPermanently;
                }

                rawStorageHistory.Add(await filesystemOperations.DeleteAsync(source.ElementAt(i), null, banner.ErrorCode, permanently, cancellationToken));
                progress = ((float)i / (float)source.Count()) * 100.0f;
                ((IProgress<float>)banner.Progress).Report(progress);
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

        private ISet<string> GetPathsUnderRecycleBin(IEnumerable<IStorageItemWithPath> source)
        {
            return source.Select(item => item.Path).Where(path => recycleBinHelpers.IsPathUnderRecycleBin(path)).ToHashSet();
        }

        public async Task<ReturnResult> DeleteItemAsync(IStorageItemWithPath source, bool showDialog, bool permanently, bool registerHistory)
        {
            PostedStatusBanner banner;
            bool deleteFromRecycleBin = recycleBinHelpers.IsPathUnderRecycleBin(source.Path);

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

                if (Interacts.Interaction.IsAnyContentDialogOpen())
                {
                    // Can show only one dialog at a time
                    banner.Remove();
                    return ReturnResult.Cancelled;
                }
                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected item if the result is Yes
                {
                    banner.Remove();
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history = await filesystemOperations.DeleteAsync(source, banner.Progress, banner.ErrorCode, permanently, cancellationToken);
            ((IProgress<float>)banner.Progress).Report(100.0f);

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
            return await DeleteItemsAsync(source.Select((item) => item.FromStorageItem()), showDialog, permanently, registerHistory);
        }

        public async Task<ReturnResult> DeleteItemAsync(IStorageItem source, bool showDialog, bool permanently, bool registerHistory)
        {
            PostedStatusBanner banner;
            bool deleteFromRecycleBin = recycleBinHelpers.IsPathUnderRecycleBin(source.Path);

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

                if (Interacts.Interaction.IsAnyContentDialogOpen())
                {
                    // Can show only one dialog at a time
                    banner.Remove();
                    return ReturnResult.Cancelled;
                }
                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected item if the result is Yes
                {
                    banner.Remove();
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }

                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history = await filesystemOperations.DeleteAsync(source, banner.Progress, banner.ErrorCode, permanently, cancellationToken);
            ((IProgress<float>)banner.Progress).Report(100.0f);

            if (!permanently && registerHistory)
            {
                App.HistoryWrapper.AddHistory(history);
            }

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(returnStatus, permanently ? FileOperationType.Delete : FileOperationType.Recycle, sw, associatedInstance);

            return returnStatus;
        }

        #endregion Delete

        public async Task<ReturnResult> RestoreFromTrashAsync(IStorageItemWithPath source, string destination, bool registerHistory)
        {
            FileSystemStatusCode returnCode = FileSystemStatusCode.InProgress;
            Progress<FileSystemStatusCode> errorCode = new Progress<FileSystemStatusCode>();
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
                if (operation.HasFlag(DataPackageOperation.Copy))
                {
                    return await CopyItemsFromClipboard(packageView, destination, registerHistory);
                }
                else if (operation.HasFlag(DataPackageOperation.Move))
                {
                    return await MoveItemsFromClipboard(packageView, destination, registerHistory);
                }
                else if (operation.HasFlag(DataPackageOperation.Link))
                {
                    // TODO: Support link creation
                    return default;
                }
                else if (operation.HasFlag(DataPackageOperation.None))
                {
                    return await CopyItemsFromClipboard(packageView, destination, registerHistory);
                }
                else
                {
                    return default;
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
            return await CopyItemsAsync(source.Select((item) => item.FromStorageItem()).ToList(), destination, registerHistory);
        }

        public async Task<ReturnResult> CopyItemAsync(IStorageItem source, string destination, bool registerHistory)
        {
            return await CopyItemAsync(source.FromStorageItem(), destination, registerHistory);
        }

        public async Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool registerHistory)
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
            float progress;
            for (int i = 0; i < source.Count(); i++)
            {
                rawStorageHistory.Add(await filesystemOperations.CopyAsync(
                    source.ElementAt(i),
                    destination.ElementAt(i),
                    null,
                    banner.ErrorCode,
                    cancellationToken));

                progress = ((float)i / (float)source.Count()) * 100.0f;
                ((IProgress<float>)banner.Progress).Report(progress);
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

        public async Task<ReturnResult> CopyItemAsync(IStorageItemWithPath source, string destination, bool registerHistory)
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
            ((IProgress<float>)banner.Progress).Report(100.0f);

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
            if (packageView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> source;
                try
                {
                    source = await packageView.GetStorageItemsAsync();
                }
                catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
                {
                    return ReturnResult.UnknownException;
                }
                ReturnResult returnStatus = ReturnResult.InProgress;

                List<string> destinations = new List<string>();
                foreach (IStorageItem item in source)
                {
                    destinations.Add(Path.Combine(destination, item.Name));
                }

                returnStatus = await CopyItemsAsync(source, destinations, registerHistory);

                return returnStatus;
            }

            if (packageView.Contains(StandardDataFormats.Bitmap))
            {
                try
                {
                    var imgSource = await packageView.GetBitmapAsync();
                    using var imageStream = await imgSource.OpenReadAsync();
                    var folder = await StorageFolder.GetFolderFromPathAsync(destination);
                    // Set the name of the file to be the current time and date
                    var file = await folder.CreateFileAsync($"{DateTime.Now:mm-dd-yy-HHmmss}.png", CreationCollisionOption.GenerateUniqueName);

                    using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                    await imageStream.AsStreamForRead().CopyToAsync(stream.AsStreamForWrite());
                    return ReturnResult.Success;
                }
                catch (Exception)
                {
                    return ReturnResult.UnknownException;
                }
            }

            // Happens if you copy some text and then you Ctrl+V in Files
            // Should this be done in ModernShellPage?
            return ReturnResult.BadArgumentException;
        }

        #endregion Copy

        #region Move

        public async Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory)
        {
            return await MoveItemsAsync(source.Select((item) => item.FromStorageItem()).ToList(), destination, registerHistory);
        }

        public async Task<ReturnResult> MoveItemAsync(IStorageItem source, string destination, bool registerHistory)
        {
            return await MoveItemAsync(source.FromStorageItem(), destination, registerHistory);
        }

        public async Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool registerHistory)
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
            float progress;
            for (int i = 0; i < source.Count(); i++)
            {
                rawStorageHistory.Add(await filesystemOperations.MoveAsync(
                    source.ElementAt(i),
                    destination.ElementAt(i),
                    null,
                    banner.ErrorCode,
                    cancellationToken));

                progress = ((float)i / (float)source.Count()) * 100.0f;
                ((IProgress<float>)banner.Progress).Report(progress);
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

        public async Task<ReturnResult> MoveItemAsync(IStorageItemWithPath source, string destination, bool registerHistory)
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
            ((IProgress<float>)banner.Progress).Report(100.0f);

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

            IReadOnlyList<IStorageItem> source;
            try
            {
                source = await packageView.GetStorageItemsAsync();
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80040064)
            {
                return ReturnResult.UnknownException;
            }
            ReturnResult returnStatus = ReturnResult.InProgress;

            List<string> destinations = new List<string>();
            foreach (IStorageItem item in source)
            {
                destinations.Add(Path.Combine(destination, item.Name));
            }

            returnStatus = await MoveItemsAsync(source, destinations, registerHistory);

            return returnStatus;
        }

        #endregion Move

        #region Rename

        public async Task<ReturnResult> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, bool registerHistory)
        {
            FileSystemStatusCode returnCode = FileSystemStatusCode.InProgress;
            Progress<FileSystemStatusCode> errorCode = new Progress<FileSystemStatusCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            return returnCode.ToStatus();
        }

        public async Task<ReturnResult> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, bool registerHistory)
        {
            FileSystemStatusCode returnCode = FileSystemStatusCode.InProgress;
            Progress<FileSystemStatusCode> errorCode = new Progress<FileSystemStatusCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await filesystemOperations.RenameAsync(source, newName, collision, errorCode, cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
            {
                App.HistoryWrapper.AddHistory(history);
            }

            return returnCode.ToStatus();
        }

        #endregion Rename

        #endregion IFilesystemHelpers

        #region Public Helpers

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
                Debug.WriteLine($"Individual file size for Progress UI will be reported as: {size} bytes");
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

        #endregion Public Helpers

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

        #endregion IDisposable
    }
}