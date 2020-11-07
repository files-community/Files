using Files.Filesystem;
using Files.Filesystem.FilesystemHistory;
using Files.Filesystem.FilesystemOperations;
using Files.UserControls;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.Helpers
{
    public class FilesystemHelpers
    {
        #region Private Members

        private readonly IShellPage _associatedInstance;

        private readonly IFilesystemOperations _filesystemOperations;

        private readonly CancellationToken _cancellationToken;

        #endregion

        #region Constructor

        public FilesystemHelpers(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this._associatedInstance = associatedInstance;
            this._cancellationToken = cancellationToken;
            this._filesystemOperations = new FilesystemOperations(this._associatedInstance);
        }

        #endregion

        #region CreateNew

        public async Task CreateAsync(string fullPath, FilesystemItemType itemType, IProgress<Status> status, bool registerHistory)
        {
            IStorageHistory storageHistory = await this._filesystemOperations.CreateAsync(fullPath, itemType, status, _cancellationToken);

            if (registerHistory)
                App.AddHistory(storageHistory);
        }

        #endregion

        #region Delete, Restore

        #region Delete

        public async Task<Status> DeleteAsync(IEnumerable<IStorageItem> source, bool showDialog, bool permanently, bool registerHistory)
        {
            try
            {
                if (_associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // Permanently delete if deleting from recycle bin
                    permanently = true;
                }

                PostedStatusBanner banner;
                if (permanently)
                {
                    banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        string.Empty,
                        this._associatedInstance.FilesystemViewModel.WorkingDirectory,
                        0,
                        Status.InProgress,
                        FileOperationType.Delete);
                }
                else
                {
                    banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        string.Empty,
                        this._associatedInstance.FilesystemViewModel.WorkingDirectory,
                        0,
                        Status.InProgress,
                        FileOperationType.Recycle);
                }

                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    IStorageHistory storageHistory;
                    List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

                    foreach (IStorageItem item in source)
                    {
                        // TODO: I've managed to break permanently checkbox - when it's selected the Files crash
                        IStorageHistory history = await this._filesystemOperations.DeleteAsync(item, banner.Progress, banner.Status, showDialog, permanently, this._cancellationToken);
                        rawStorageHistory.Add(history);
                    }

                    storageHistory = new StorageHistory(rawStorageHistory[0].OperationType, source, null);
                    if (!permanently && registerHistory)
                        App.AddHistory(storageHistory);

                    banner.Remove();
                    sw.Stop();

                    if (sw.Elapsed.TotalSeconds >= 10)
                    {
                        if (permanently)
                        {
                            _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                            "Deletion Complete",
                            "The operation has completed.",
                            0,
                            Status.Success,
                            FileOperationType.Delete);
                        }
                        else
                        {
                            _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                            "Recycle Complete",
                            "The operation has completed.",
                            0,
                            Status.Success,
                            FileOperationType.Recycle);
                        }
                    }

                    _associatedInstance.NavigationToolbar.CanGoForward = false;
                }
                catch (UnauthorizedAccessException)
                {
                    banner.Remove();
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        Status.Failed,
                        FileOperationType.Delete);
                }
                catch (FileNotFoundException)
                {
                    banner.Remove();
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        Status.Failed,
                        FileOperationType.Delete);
                }
                catch (IOException)
                {
                    banner.Remove();
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostActionBanner(
                        "FileInUseDeleteDialog/Title".GetLocalized(),
                        "FileInUseDeleteDialog/Text".GetLocalized(),
                        "FileInUseDeleteDialog/PrimaryButtonText".GetLocalized(),
                        "FileInUseDeleteDialog/SecondaryButtonText".GetLocalized(), async () => { await DeleteAsync(source, showDialog, permanently, registerHistory); });
                }

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
        }

        public async Task<Status> DeleteAsync(IStorageItem source, bool showDialog, bool permanently, bool registerHistory)
        {
            try
            {
                if (_associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // Permanently delete if deleting from recycle bin
                    permanently = true;
                }

                PostedStatusBanner banner;
                if (permanently)
                {
                    banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        string.Empty,
                        this._associatedInstance.FilesystemViewModel.WorkingDirectory,
                        0,
                        Status.InProgress,
                        FileOperationType.Delete);
                }
                else
                {
                    banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        string.Empty,
                        this._associatedInstance.FilesystemViewModel.WorkingDirectory,
                        0,
                        Status.InProgress,
                        FileOperationType.Recycle);
                }

                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    // TODO: I've managed to break permanently checkbox - when it's selected the Files crash
                    IStorageHistory storageHistory = await this._filesystemOperations.DeleteAsync(source, banner.Progress, banner.Status, showDialog, permanently, this._cancellationToken);

                    if (!permanently && registerHistory)
                        App.AddHistory(storageHistory);

                    banner.Remove();
                    sw.Stop();

                    if (sw.Elapsed.TotalSeconds >= 10)
                    {
                        if (permanently)
                        {
                            _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                            "Deletion Complete",
                            "The operation has completed.",
                            0,
                            Status.Success,
                            FileOperationType.Delete);
                        }
                        else
                        {
                            _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                            "Recycle Complete",
                            "The operation has completed.",
                            0,
                            Status.Success,
                            FileOperationType.Recycle);
                        }
                    }

                    _associatedInstance.NavigationToolbar.CanGoForward = false;
                }
                catch (UnauthorizedAccessException)
                {
                    banner.Remove();
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        Status.Failed,
                        FileOperationType.Delete);
                }
                catch (FileNotFoundException)
                {
                    banner.Remove();
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        Status.Failed,
                        FileOperationType.Delete);
                }
                catch (IOException)
                {
                    banner.Remove();
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostActionBanner(
                        "FileInUseDeleteDialog/Title".GetLocalized(),
                        "FileInUseDeleteDialog/Text".GetLocalized(),
                        "FileInUseDeleteDialog/PrimaryButtonText".GetLocalized(),
                        "FileInUseDeleteDialog/SecondaryButtonText".GetLocalized(), async () => { await DeleteAsync(source, showDialog, permanently, registerHistory); });
                }

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
        }

        #endregion

        #region Restore

        public async Task<Status> RestoreFromTrashAsync(RecycleBinItem source, IStorageItem destination, bool registerHistory)
        {
            IStorageHistory history = await this._filesystemOperations.RestoreFromTrashAsync(source, destination, null, null, this._cancellationToken);

            if (registerHistory)
                App.AddHistory(history);

            return Status.Success;
        }

        #endregion

        #endregion

        #region Clone Directory

        public async static Task<StorageFolder> CloneDirectoryAsync(IStorageFolder SourceFolder, IStorageFolder DestinationFolder, string sourceRootName)
        {
            StorageFolder createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            DestinationFolder = createdRoot;

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }

            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                await CloneDirectoryAsync(folderinSourceDir, DestinationFolder, folderinSourceDir.Name);
            }

            return createdRoot;
        }

        #endregion

        #region Copy, Move

        public async Task<Status> PerformPasteTypeAsync(DataPackageOperation operation, DataPackageView packageView, IStorageItem destination)
        {
            switch (operation)
            {
                case DataPackageOperation.Copy:
                    return await CopyItemsFromClipboard(packageView, destination, true);

                case DataPackageOperation.Move:
                    return await MoveItemsFromClipboard(packageView, destination, true);

                default:
                    return Status.InProgress;
            }
        }

        #region Copy

        public async Task<Status> CopyItemsAsync(IEnumerable<IStorageItem> source, IStorageItem destination, bool registerHistory)
        {
            try
            {
                PostedStatusBanner banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    string.Empty,
                    _associatedInstance.FilesystemViewModel.WorkingDirectory,
                    0,
                    Status.InProgress,
                    FileOperationType.Copy);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                IStorageHistory storageHistory;
                List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

                foreach (IStorageItem item in source)
                {
                    IStorageHistory history = await this._filesystemOperations.CopyAsync(item, destination, banner.Progress, banner.Status, this._cancellationToken);
                    rawStorageHistory.Add(history);
                }

                storageHistory = new StorageHistory(rawStorageHistory[0].OperationType, source, new List<IStorageItem>() { destination });
                if (registerHistory)
                    App.AddHistory(storageHistory);

                banner.Remove();

                sw.Stop();

                if (sw.Elapsed.TotalSeconds >= 10)
                {
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "Paste Complete",
                        "The operation has completed.",
                        0,
                        Status.Success,
                        FileOperationType.Copy);
                }

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
        }

        public async Task<Status> CopyItemAsync(IStorageItem source, IStorageItem destination, bool registerHistory)
        {
            try
            {
                PostedStatusBanner banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    string.Empty,
                    _associatedInstance.FilesystemViewModel.WorkingDirectory,
                    0,
                    Status.InProgress,
                    FileOperationType.Copy);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                IStorageHistory storageHistory = await this._filesystemOperations.CopyAsync(source, destination, banner.Progress, banner.Status, this._cancellationToken);

                if (registerHistory)
                    App.AddHistory(storageHistory);

                banner.Remove();

                sw.Stop();

                if (sw.Elapsed.TotalSeconds >= 10)
                {
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "Paste Complete",
                        "The operation has completed.",
                        0,
                        Status.Success,
                        FileOperationType.Copy);
                }

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
        }

        public async Task<Status> CopyItemsFromClipboard(DataPackageView packageView, IStorageItem destination, bool registerHistory)
        {
            try
            {
                IReadOnlyList<IStorageItem> source = await packageView.GetStorageItemsAsync();
                await CopyItemsAsync(source, destination, registerHistory);

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
            finally
            {
                packageView.ReportOperationCompleted(DataPackageOperation.Copy);
            }
        }

        #endregion

        #region Move

        public async Task<Status> MoveItemsAsync(IEnumerable<IStorageItem> source, IStorageItem destination, bool registerHistory)
        {
            try
            {
                PostedStatusBanner banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    string.Empty,
                    _associatedInstance.FilesystemViewModel.WorkingDirectory,
                    0,
                    Status.InProgress,
                    FileOperationType.Copy);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                IStorageHistory storageHistory;
                List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();

                foreach (IStorageItem item in source)
                {
                    IStorageHistory history = await this._filesystemOperations.MoveAsync(item, destination, banner.Progress, banner.Status, this._cancellationToken);
                    rawStorageHistory.Add(history);
                }

                storageHistory = new StorageHistory(rawStorageHistory[0].OperationType, source, new List<IStorageItem>() { destination });
                if (registerHistory)
                    App.AddHistory(storageHistory);

                banner.Remove();

                sw.Stop();

                if (sw.Elapsed.TotalSeconds >= 10)
                {
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "Paste Complete",
                        "The operation has completed.",
                        0,
                        Status.Success,
                        FileOperationType.Copy);
                }

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
        }

        public async Task<Status> MoveItemAsync(IStorageItem source, IStorageItem destination, bool registerHistory)
        {
            try
            {
                PostedStatusBanner banner = _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    string.Empty,
                    _associatedInstance.FilesystemViewModel.WorkingDirectory,
                    0,
                    Status.InProgress,
                    FileOperationType.Copy);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                IStorageHistory storageHistory = await this._filesystemOperations.MoveAsync(source, destination, banner.Progress, banner.Status, this._cancellationToken);

                if (registerHistory)
                    App.AddHistory(storageHistory);

                banner.Remove();

                sw.Stop();

                if (sw.Elapsed.TotalSeconds >= 10)
                {
                    _associatedInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "Paste Complete",
                        "The operation has completed.",
                        0,
                        Status.Success,
                        FileOperationType.Copy);
                }

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
        }

        public async Task<Status> MoveItemsFromClipboard(DataPackageView packageView, IStorageItem destination, bool registerHistory)
        {
            try
            {
                IReadOnlyList<IStorageItem> source = await packageView.GetStorageItemsAsync();
                await MoveItemsAsync(source, destination, registerHistory);

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
            finally
            {
                packageView.ReportOperationCompleted(DataPackageOperation.Move);
            }
        }

        #endregion

        #endregion

        #region Rename

        public async Task RenameAsync(IStorageItem source, string newName, bool replace, bool registerHistory)
        {
            IStorageHistory storageHistory = await this._filesystemOperations.RenameAsync(source, newName, replace, null, this._cancellationToken);

            if (registerHistory)
                App.AddHistory(storageHistory);
        }

        #endregion
    }
}
