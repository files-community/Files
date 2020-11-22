using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Helpers;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryOperations : IStorageHistoryOperations
    {
        #region Private Members

        private IFilesystemOperations filesystemOperations;

        private IFilesystemHelpers filesystemHelpers;

        private IShellPage associatedInstance;

        private readonly CancellationToken cancellationToken;

        #endregion

        #region Constructor

        public StorageHistoryOperations(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this.associatedInstance = associatedInstance;
            this.cancellationToken = cancellationToken;
            this.filesystemOperations = new FilesystemOperations(associatedInstance);
            this.filesystemHelpers = new FilesystemHelpers(associatedInstance, cancellationToken);
        }

        #endregion

        #region IStorageHistoryOperations

        public async Task<ReturnResult> Redo(IStorageHistory history)
        {
            ReturnResult returnStatus = ReturnResult.InProgress;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();

            errorCode.ProgressChanged += (s, e) => { returnStatus = e.ToStatus(); };

            switch (history.OperationType)
            {
                case FileOperationType.CreateNew: // CreateNew PASS
                    {
                        if (IsHistoryNull(history)) break;

                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this.filesystemOperations.CreateAsync(history.Source.ElementAt(i), errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        if (IsHistoryNull(history)) break;

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this.filesystemOperations.RenameAsync(history.Source.ElementAt(i), Path.GetFileName(history.Destination.ElementAt(i).Path), collision, errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        if (IsHistoryNull(history)) break;

                        return await this.filesystemHelpers.CopyItemsAsync(history.Source, history.Destination.Select((item) => item.Path), false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        if (IsHistoryNull(history)) break;

                        return await this.filesystemHelpers.MoveItemsAsync(history.Source, history.Destination.Select((item) => item.Path), false);
                    }

                case FileOperationType.Extract: // Extract PASS
                    {
                        returnStatus = ReturnResult.Success;
                        Debugger.Break();

                        break;
                    }

                case FileOperationType.Recycle: // Recycle PASS
                    {
                        if (IsHistoryNull(history.Destination)) break;

                        List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            rawStorageHistory.Add(await this.filesystemOperations.DeleteAsync(history.Source.ElementAt(i), null, errorCode, false, this.cancellationToken));
                        }

                        IStorageHistory newHistory = new StorageHistory(FileOperationType.Recycle, rawStorageHistory.SelectMany((item) => item?.Source).ToList(), rawStorageHistory.SelectMany((item) => item?.Destination).ToList());

                        // We need to change the recycled item paths (since IDs are different) - for Undo() to work
                        App.StorageHistory[ArrayHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count)].Modify(newHistory);

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        if (IsHistoryNull(history)) break;

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await filesystemHelpers.RestoreFromTrashAsync(history.Source.ElementAt(i), history.Destination.ElementAt(i).Path, false);
                        }

                        break;
                    }

                case FileOperationType.Delete: // Delete PASS
                    {
                        returnStatus = ReturnResult.Success;

                        break;
                    }
            }

            return returnStatus;
        }

        public async Task<ReturnResult> Undo(IStorageHistory history)
        {
            ReturnResult returnStatus = ReturnResult.InProgress;
            Progress<FilesystemErrorCode> errorCode = new Progress<FilesystemErrorCode>();

            errorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            switch (history.OperationType)
            {
                case FileOperationType.CreateNew: // CreateNew PASS
                    {
                        // Opposite: Delete created items

                        if (IsHistoryNull(history.Source)) break;

                        return await this.filesystemHelpers.DeleteItemsAsync(history.Source, false, true, false);
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        // Opposite: Restore original item names

                        if (IsHistoryNull(history)) break;

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await this.filesystemOperations.RenameAsync(history.Destination.ElementAt(i), Path.GetFileName(history.Source.ElementAt(i).Path), collision, errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        // Opposite: Delete copied items

                        if (IsHistoryNull(history.Destination)) break;

                        return await this.filesystemHelpers.DeleteItemsAsync(history.Destination, false, true, false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        // Opposite: Move the items to original directory

                        if (IsHistoryNull(history)) break;

                        return await this.filesystemHelpers.MoveItemsAsync(history.Destination, history.Source.Select((item) => item.Path), false);
                    }

                case FileOperationType.Extract: // Extract PASS
                    {
                        // Opposite: No opposite for archive extraction

                        returnStatus = ReturnResult.Success;
                        Debugger.Break();

                        break;
                    }

                case FileOperationType.Recycle: // Recycle PASS
                    {
                        // Opposite: Restore recycled items
                        if (IsHistoryNull(history)) break;

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            returnStatus = await this.filesystemHelpers.RestoreFromTrashAsync(history.Destination.ElementAt(i), history.Source.ElementAt(i).Path, false);
                        }

                        if (returnStatus == ReturnResult.IntegrityCheckFailed) // Not found, corrupted
                        {
                            App.StorageHistory.Remove(history);
                            App.StorageHistoryIndex--;
                        }

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        // Opposite: Move restored items to Recycle Bin

                        if (IsHistoryNull(history.Destination)) break;

                        List<IStorageHistory> rawStorageHistory = new List<IStorageHistory>();
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            rawStorageHistory.Add(await this.filesystemOperations.DeleteAsync(history.Destination.ElementAt(i), null, errorCode, false, this.cancellationToken));
                        }

                        IStorageHistory newHistory = new StorageHistory(FileOperationType.Restore, rawStorageHistory.SelectMany((item) => item?.Destination).ToList(), rawStorageHistory.SelectMany((item) => item?.Source).ToList());

                        // We need to change the recycled item paths (since IDs are different) - for Redo() to work
                        App.StorageHistory[ArrayHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count)].Modify(newHistory);

                        break;
                    }

                case FileOperationType.Delete: // Delete PASS
                    {
                        // Opposite: No opposite for pernament deletion

                        returnStatus = ReturnResult.Success;
                        break;
                    }
            }

            return returnStatus;
        }

        #endregion

        #region Private Helpers

        private bool IsHistoryNull(IStorageHistory history) =>
            !(history.Source.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item.Path))
                && history.Destination.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item.Path)));

        private bool IsHistoryNull(IEnumerable<PathWithType> source) =>
            !(source.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item.Path)));

        #endregion

        #region IDisposable

        public void Dispose()
        {
            this.associatedInstance?.Dispose();
            this.filesystemOperations?.Dispose();
            this.filesystemHelpers?.Dispose();

            this.associatedInstance = null;
            this.filesystemOperations = null;
            this.filesystemHelpers = null;
        }

        #endregion
    }
}
