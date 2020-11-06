using Files.Extensions;
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

        private readonly IShellPage _appInstance;

        private readonly IFilesystemOperations _filesystemOperations;

        private readonly CancellationToken _cancellationToken;

        #endregion

        #region Constructor

        public FilesystemHelpers(IShellPage appInstance, IFilesystemOperations filesystemOperations, CancellationToken cancellationToken) // IFilesystemOperations interface as a parameter?
        {
            this._appInstance = appInstance;
            this._filesystemOperations = filesystemOperations;
            this._cancellationToken = cancellationToken;
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

        #region Delete

        public async Task<Status> DeleteAsync(IEnumerable<IStorageItem> source, bool showDialog, bool pernamently, bool registerHistory)
        {
            try
            {
                if (App.CurrentInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // Permanently delete if deleting from recycle bin
                    pernamently = true;
                }

                PostedStatusBanner banner;
                if (pernamently)
                {
                    banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        string.Empty,
                        this._appInstance.FilesystemViewModel.WorkingDirectory,
                        0,
                        Status.InProgress,
                        FileOperationType.Delete);
                }
                else
                {
                    banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        string.Empty,
                        this._appInstance.FilesystemViewModel.WorkingDirectory,
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
                        // TODO: I managed to break Pernamently checkbox - when it's selected the Files crash
                        IStorageHistory history = await this._filesystemOperations.DeleteAsync(item, banner.Progress, banner.Status, showDialog, pernamently, this._cancellationToken);
                        rawStorageHistory.Add(history);
                    }
                    //source.ForEach<IStorageItem>(async (item) =>
                    //rawStorageHistory.Add(await this._filesystemOperations.DeleteAsync(item, banner.Progress, banner.Status, pernamently, this._cancellationToken)));

                    storageHistory = new StorageHistory(rawStorageHistory[0].OperationType, source, null); // Is rawStorageHistory[0] - [0] indexing smart here?
                    if (!pernamently && registerHistory) // TODO: Don't add pernamently, or skip it in Undo() ?
                        App.AddHistory(storageHistory);

                    banner.Remove();
                    sw.Stop();

                    if (sw.Elapsed.TotalSeconds >= 10)
                    {
                        if (pernamently)
                        {
                            App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                            "Deletion Complete",
                            "The operation has completed.",
                            0,
                            Status.Success,
                            FileOperationType.Delete);
                        }
                        else
                        {
                            App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                            "Recycle Complete",
                            "The operation has completed.",
                            0,
                            Status.Success,
                            FileOperationType.Recycle);
                        }
                    }

                    App.CurrentInstance.NavigationToolbar.CanGoForward = false;
                }
                catch (UnauthorizedAccessException)
                {
                    banner.Remove();
                    App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "AccessDeniedDeleteDialog/Text".GetLocalized(),
                        0,
                        Status.Failed,
                        FileOperationType.Delete);
                }
                catch (FileNotFoundException)
                {
                    banner.Remove();
                    App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                        "FileNotFoundDialog/Title".GetLocalized(),
                        "FileNotFoundDialog/Text".GetLocalized(),
                        0,
                        Status.Failed,
                        FileOperationType.Delete);
                }
                catch (IOException)
                {
                    banner.Remove();
                    App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostActionBanner(
                        "FileInUseDeleteDialog/Title".GetLocalized(),
                        "FileInUseDeleteDialog/Text".GetLocalized(),
                        "FileInUseDeleteDialog/PrimaryButtonText".GetLocalized(),
                        "FileInUseDeleteDialog/SecondaryButtonText".GetLocalized(), async () => { await DeleteAsync(source, showDialog, pernamently, registerHistory); });
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

        #region Clone

        public async static Task<StorageFolder> CloneDirectoryAsync(StorageFolder SourceFolder, StorageFolder DestinationFolder, string sourceRootName)
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

        #region Copy/Move

        public async Task<Status> PerformPasteType(DataPackageOperation operation, DataPackageView packageView, IStorageItem destination)
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

        public async Task<Status> CopyItems(IEnumerable<IStorageItem> source, IStorageItem destination, bool registerHistory)
        {
            try
            {
                PostedStatusBanner banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    string.Empty,
                    App.CurrentInstance.FilesystemViewModel.WorkingDirectory,
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
                //source.ForEach<IStorageItem>(async (item) =>
                //    rawStorageHistory.Add(await this._filesystemOperations.CopyAsync(item, destination, banner.Progress, banner.Status, this._cancellationToken)));

                storageHistory = new StorageHistory(rawStorageHistory[0].OperationType, source, new List<IStorageItem>() { destination });
                if (registerHistory)
                    App.AddHistory(storageHistory);

                banner.Remove();

                sw.Stop();

                if (sw.Elapsed.TotalSeconds >= 10)
                {
                    App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
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

        public async Task<Status> MoveItems(IEnumerable<IStorageItem> source, IStorageItem destination, bool registerHistory)
        {
            try
            {
                PostedStatusBanner banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    string.Empty,
                    App.CurrentInstance.FilesystemViewModel.WorkingDirectory,
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
                //source.ForEach<IStorageItem>(async (item) =>
                //    rawStorageHistory.Add(await this._filesystemOperations.MoveAsync(item, destination, banner.Progress, banner.Status, this._cancellationToken)));

                storageHistory = new StorageHistory(rawStorageHistory[0].OperationType, source, new List<IStorageItem>() { destination });
                if (registerHistory)
                    App.AddHistory(storageHistory);

                banner.Remove();

                sw.Stop();

                if (sw.Elapsed.TotalSeconds >= 10)
                {
                    App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
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
                await CopyItems(source, destination, registerHistory);

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
                await MoveItems(source, destination, registerHistory);

                return Status.Success;
            }
            catch (Exception e)
            {
                // TODO: Log the exception here
                return Status.UnknownException;
            }
        }

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
