using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Helpers;
using Files.Extensions;
using Files.Filesystem.FilesystemHistory;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryOperations : IStorageHistoryOperations
    {
        #region Private Members

        private IFilesystemOperations _filesystemOperations;

        private IFilesystemHelpers _filesystemHelpers;

        private readonly CancellationToken _cancellationToken;

        #endregion

        #region Constructor

        public StorageHistoryOperations(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this._cancellationToken = cancellationToken;
            this._filesystemOperations = new FilesystemOperations(associatedInstance);
            this._filesystemHelpers = new FilesystemHelpers(associatedInstance, _cancellationToken);
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
                        FilesystemItemType itemType;
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            itemType = EnumExtensions.GetEnum<FilesystemItemType>(history.Destination.ElementAt(i));
                            await this._filesystemOperations.CreateAsync(history.Source.ElementAt(i), itemType, errorCode, this._cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this._filesystemOperations.RenameAsync(await history.Source.ElementAt(i).ToStorageItem(), Path.GetFileName(history.Destination.ElementAt(i)), collision, errorCode, this._cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            returnStatus = await this._filesystemHelpers.CopyItemAsync(await history.Source.ElementAt(i).ToStorageItem(), history.Destination.ElementAt(i), false);
                        }

                        break;
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this._filesystemHelpers.MoveItemAsync(await history.Source.ElementAt(i).ToStorageItem(), history.Destination.ElementAt(i), false);
                        }

                        break;
                    }

                case FileOperationType.Extract: // Extract PASS
                    {
                        returnStatus = ReturnResult.Success;
                        Debugger.Break();

                        break;
                    }

                case FileOperationType.Recycle: // Recycle PASS
                    {
                        List<IStorageHistory> rawHistory = new List<IStorageHistory>();

                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            IStorageHistory history1 = await this._filesystemOperations.DeleteAsync(await history.Source.ElementAt(i).ToStorageItem(), null, errorCode, false, this._cancellationToken);
                            rawHistory.Add(history1);
                        }

                        IStorageHistory newHistory = new StorageHistory(FileOperationType.Recycle, rawHistory.SelectMany((item) => item.Source).ToList(), rawHistory.SelectMany((item) => item.Destination).ToList());

                        // We need to change the recycled item paths (since IDs are different) - for Undo() to work
                        App.StorageHistory[App.StorageHistoryIndex - 1].Modify(newHistory);

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await _filesystemHelpers.RestoreFromTrashAsync(history.Source.ElementAt(i), history.Destination.ElementAt(i), false);
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

                        return await this._filesystemHelpers.DeleteItemsAsync(await history.Source.ToStorageItemCollection(), false, true, false);
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        // Opposite: Restore original item names

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await this._filesystemOperations.RenameAsync(await history.Destination.ElementAt(i).ToStorageItem(), Path.GetFileName(history.Source.ElementAt(i)), collision, errorCode, this._cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        // Opposite: Delete copied items

                        return await this._filesystemHelpers.DeleteItemsAsync(await history.Destination.ToStorageItemCollection(), false, true, false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        // Opposite: Move the items to original directory

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            returnStatus = await this._filesystemHelpers.MoveItemAsync(await history.Destination.ElementAt(i).ToStorageItem(), history.Source.ElementAt(i), false);
                        }

                        break;
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

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            returnStatus = await this._filesystemHelpers.RestoreFromTrashAsync(history.Destination.ElementAt(i), history.Source.ElementAt(i), false);
                        }

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        // Opposite: Move restored items to Recycle Bin

                        List<IStorageHistory> rawHistory = new List<IStorageHistory>();

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            IStorageHistory history1 = await this._filesystemOperations.DeleteAsync(await history.Destination.ElementAt(i).ToStorageItem(), null, errorCode, false, this._cancellationToken);
                            rawHistory.Add(history1);
                        }

                        IStorageHistory newHistory = new StorageHistory(FileOperationType.Restore, rawHistory.SelectMany((item) => item.Destination).ToList(), rawHistory.SelectMany((item) => item.Source).ToList());

                        // We need to change the recycled item paths (since IDs are different) - for Redo() to work
                        App.StorageHistory[App.StorageHistoryIndex].Modify(newHistory);

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

        #region IDisposable

        public void Dispose()
        {
            this._filesystemOperations?.Dispose();
            this._filesystemHelpers?.Dispose();

            this._filesystemOperations = null;
            this._filesystemHelpers = null;
        }

        #endregion
    }
}
