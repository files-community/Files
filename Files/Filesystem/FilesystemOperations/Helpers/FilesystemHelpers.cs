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
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.Filesystem
{
    public class FilesystemHelpers : IFilesystemHelpers
    {
        #region Private Members

        private IShellPage _associatedInstance;

        private IFilesystemOperations _filesystemOperations;

        private RecycleBinHelpers _recycleBinHelpers;

        private readonly CancellationToken _cancellationToken;

        #endregion

        #region Constructor

        public FilesystemHelpers(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this._associatedInstance = associatedInstance;
            this._cancellationToken = cancellationToken;
            this._filesystemOperations = new FilesystemOperations(this._associatedInstance);
            this._recycleBinHelpers = new RecycleBinHelpers(this._associatedInstance);
        }

        #endregion

        #region IFilesystemHelpers

        #region Create

        public async Task<ReturnResult> CreateAsync(string fullPath, FilesystemItemType itemType, bool registerHistory)
        {
            FilesystemErrorCode returnCode = FilesystemErrorCode.ERROR_INPROGRESS;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await this._filesystemOperations.CreateAsync(fullPath, itemType, errorCode, this._cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(fullPath))
                App.AddHistory(history);

            return returnCode.ToStatus();
        }

        #endregion

        #region Delete

        public async Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItem> source, bool showDialog, bool permanently, bool registerHistory)
        {
            bool deleteFromRecycleBin = false;
            foreach (IStorageItem item in source) /*_associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);*/
                if (await this._recycleBinHelpers.IsRecycleBinItem(item)) deleteFromRecycleBin = true;

            PostedStatusBanner banner;
            if (permanently)
            {
                banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                _associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Delete);
            }
            else
            {
                banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                _associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Recycle);
            }

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            if (App.AppSettings.ShowConfirmDeleteDialog && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                ConfirmDeleteDialog dialog = new ConfirmDeleteDialog(deleteFromRecycleBin, !deleteFromRecycleBin ? permanently : deleteFromRecycleBin, _associatedInstance.ContentPage.SelectedItemsPropertiesViewModel);
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
                if (await this._recycleBinHelpers.IsRecycleBinItem(item))
                    permanently = true;
                else
                    permanently = originalPermanently;

                // TODO: Remove history1
                IStorageHistory history1 = await this._filesystemOperations.DeleteAsync(item, banner.Progress, banner.ErrorCode, permanently, this._cancellationToken);
                rawStorageHistory.Add(history1);
            }

            history = new StorageHistory(rawStorageHistory[0].OperationType, rawStorageHistory.SelectMany((item) => item.Source).ToList(), rawStorageHistory.SelectMany((item) => item.Destination).ToList());
            if (!permanently && registerHistory)
                App.AddHistory(history);

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(returnStatus, permanently ? FileOperationType.Delete : FileOperationType.Recycle, sw, _associatedInstance);

            return returnStatus;
        }

        public async Task<ReturnResult> DeleteItemAsync(IStorageItem source, bool showDialog, bool permanently, bool registerHistory)
        {
            PostedStatusBanner banner;
            bool deleteFromRecycleBin = await this._recycleBinHelpers.IsRecycleBinItem(source);

            if (deleteFromRecycleBin)
                permanently = true;

            if (permanently)
            {
                banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                _associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Delete);
            }
            else
            {
                banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(string.Empty,
                _associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Recycle);
            }

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            if (App.AppSettings.ShowConfirmDeleteDialog && showDialog) // Check if the setting to show a confirmation dialog is on
            {
                ConfirmDeleteDialog dialog = new ConfirmDeleteDialog(deleteFromRecycleBin, permanently, _associatedInstance.ContentPage.SelectedItemsPropertiesViewModel);
                await dialog.ShowAsync();

                if (dialog.Result != DialogResult.Delete) // Delete selected item if the result is Yes
                {
                    return ReturnResult.Cancelled; // Return if the result isn't delete
                }
                permanently = dialog.PermanentlyDelete;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history = await this._filesystemOperations.DeleteAsync(source, banner.Progress, banner.ErrorCode, permanently, this._cancellationToken);

            if (!permanently && registerHistory)
                App.AddHistory(history);

            banner.Remove();
            sw.Stop();

            PostBannerHelpers.PostBanner_Delete(returnStatus, permanently ? FileOperationType.Delete : FileOperationType.Recycle, sw, _associatedInstance);

            return returnStatus;
        }

        #endregion

        public async Task<ReturnResult> RestoreFromTrashAsync(string source, string destination, bool registerHistory)
        {
            FilesystemErrorCode returnCode = FilesystemErrorCode.ERROR_INPROGRESS;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await this._filesystemOperations.RestoreFromTrashAsync(source, destination, null, errorCode, this._cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source))
                App.AddHistory(history);

            return returnCode.ToStatus();
        }

        public async Task<ReturnResult> PerformOperationTypeAsync(DataPackageOperation operation, DataPackageView packageView, string destination, bool registerHistory)
        {
            switch (operation)
            {
                case DataPackageOperation.Copy:
                    return await CopyItemsFromClipboard(packageView, destination, registerHistory);

                case DataPackageOperation.Move:
                    return await MoveItemsFromClipboard(packageView, destination, registerHistory);

                default: return default;
            }
        }

        #region Copy

        public async Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory)
        {
            PostedStatusBanner banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                string.Empty,
                _associatedInstance.FilesystemViewModel.WorkingDirectory,
                0,
                ReturnResult.InProgress,
                FileOperationType.Copy);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history;
            List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

            _associatedInstance.ContentPage.ClearSelection();
            for (int i = 0; i < source.Count(); i++)
            {
                IStorageHistory history1 = await this._filesystemOperations.CopyAsync(source.ElementAt(i), destination.ElementAt(i), banner.Progress, banner.ErrorCode, this._cancellationToken);
                rawStorageHistory.Add(history1);
            }

            history = new StorageHistory(rawStorageHistory[0].OperationType, rawStorageHistory.SelectMany((item) => item.Source).ToList(), rawStorageHistory.SelectMany((item) => item.Destination).ToList());
            if (registerHistory && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
                App.AddHistory(history);

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    "Copy Complete",
                    "The operation has completed.",
                    0,
                    ReturnResult.Success,
                    FileOperationType.Copy);
            }

            return returnStatus;
        }

        public async Task<ReturnResult> CopyItemAsync(IStorageItem source, string destination, bool registerHistory)
        {
            PostedStatusBanner banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
               string.Empty,
               _associatedInstance.FilesystemViewModel.WorkingDirectory,
               0,
               ReturnResult.InProgress,
               FileOperationType.Copy);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            _associatedInstance.ContentPage.ClearSelection();
            IStorageHistory history = await this._filesystemOperations.CopyAsync(source, destination, banner.Progress, banner.ErrorCode, this._cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
                App.AddHistory(history);

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
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
            try
            {
                if (!packageView.Contains(StandardDataFormats.StorageItems))
                {
                    // Happens if you copy some text and then you Ctrl+V in Files
                    // Should this be done in ModernShellPage?
                    return ReturnResult.BadArgumentException;
                }

                IReadOnlyList<IStorageItem> source = await packageView.GetStorageItemsAsync();
                ReturnResult returnStatus = ReturnResult.InProgress;

                foreach (IStorageItem item in source)
                {
                    returnStatus = await CopyItemAsync(item, Path.Combine(destination, item.Name), registerHistory);
                }
                return returnStatus;
            }
            finally
            {
                packageView.ReportOperationCompleted(DataPackageOperation.Copy);
            }
        }

        #endregion

        #region Move

        public async Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory)
        {
            PostedStatusBanner banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    string.Empty,
                    _associatedInstance.FilesystemViewModel.WorkingDirectory,
                    0,
                    ReturnResult.InProgress,
                    FileOperationType.Move);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            IStorageHistory history;
            List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

            _associatedInstance.ContentPage.ClearSelection();
            for (int i = 0; i < source.Count(); i++)
            {
                IStorageHistory history1 = await this._filesystemOperations.MoveAsync(source.ElementAt(i), destination.ElementAt(i), banner.Progress, banner.ErrorCode, this._cancellationToken);
                rawStorageHistory.Add(history1);
            }

            history = new StorageHistory(rawStorageHistory[0].OperationType, rawStorageHistory.SelectMany((item) => item.Source).ToList(), rawStorageHistory.SelectMany((item) => item.Destination).ToList());
            if (registerHistory && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
                App.AddHistory(history);

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
                    "Move Complete",
                    "The operation has completed.",
                    0,
                    ReturnResult.Success,
                    FileOperationType.Move);
            }

            return returnStatus;
        }

        public async Task<ReturnResult> MoveItemAsync(IStorageItem source, string destination, bool registerHistory)
        {
            PostedStatusBanner banner = _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
               string.Empty,
               _associatedInstance.FilesystemViewModel.WorkingDirectory,
               0,
               ReturnResult.InProgress,
               FileOperationType.Move);

            ReturnResult returnStatus = ReturnResult.InProgress;
            banner.ErrorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            _associatedInstance.ContentPage.ClearSelection();
            IStorageHistory history = await this._filesystemOperations.MoveAsync(source, destination, banner.Progress, banner.ErrorCode, this._cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
                App.AddHistory(history);

            banner.Remove();
            sw.Stop();

            if (sw.Elapsed.TotalSeconds >= 10)
            {
                _associatedInstance.BottomStatusStripControl.OngoingTasksControl.PostBanner(
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
            try
            {
                if (!packageView.Contains(StandardDataFormats.StorageItems))
                {
                    // Happens if you copy some text and then you Ctrl+V in Files
                    // Should this be done in ModernShellPage?
                    return ReturnResult.BadArgumentException;
                }

                IReadOnlyList<IStorageItem> source = await packageView.GetStorageItemsAsync();
                ReturnResult returnStatus = ReturnResult.InProgress;

                foreach (IStorageItem item in source)
                {
                    returnStatus = await MoveItemAsync(item, Path.Combine(destination, item.Name), registerHistory);
                }
                return returnStatus;
            }
            finally
            {
                packageView.ReportOperationCompleted(DataPackageOperation.Move);
            }
        }

        #endregion

        public async Task<ReturnResult> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, bool registerHistory)
        {
            FilesystemErrorCode returnCode = FilesystemErrorCode.ERROR_INPROGRESS;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();
            errorCode.ProgressChanged += (s, e) => returnCode = e;

            IStorageHistory history = await this._filesystemOperations.RenameAsync(source, newName, collision, errorCode, this._cancellationToken);

            if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
                App.AddHistory(history);

            return returnCode.ToStatus();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            this._associatedInstance?.Dispose();
            this._filesystemOperations?.Dispose();
            this._recycleBinHelpers?.Dispose();

            this._associatedInstance = null;
            this._filesystemOperations = null;
            this._recycleBinHelpers = null;
        }

        #endregion
    }
}
