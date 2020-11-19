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

        private IFilesystemOperations filesystemOperations;

        private IFilesystemHelpers filesystemHelpers;

        private readonly CancellationToken cancellationToken;

        #endregion

        #region Constructor

        public StorageHistoryOperations(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
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
                        FilesystemItemType itemType;
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            itemType = EnumExtensions.GetEnum<FilesystemItemType>(history.Destination.ElementAt(i));
                            await this.filesystemOperations.CreateAsync(history.Source.ElementAt(i), itemType, errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this.filesystemOperations.RenameAsync(await history.Source.ElementAt(i).ToStorageItem() as IStorageItem, Path.GetFileName(history.Destination.ElementAt(i)), collision, errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            returnStatus = await this.filesystemHelpers.CopyItemAsync(await history.Source.ElementAt(i).ToStorageItem() as IStorageItem, history.Destination.ElementAt(i), false);
                        }

                        break;
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this.filesystemHelpers.MoveItemAsync(await history.Source.ElementAt(i).ToStorageItem() as IStorageItem, history.Destination.ElementAt(i), false);
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
                            IStorageHistory history1 = await this.filesystemOperations.DeleteAsync(await history.Source.ElementAt(i).ToStorageItem() as IStorageItem, null, errorCode, false, this.cancellationToken);
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
                            await filesystemHelpers.RestoreFromTrashAsync(history.Source.ElementAt(i), history.Destination.ElementAt(i), false);
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

                        return await this.filesystemHelpers.DeleteItemsAsync(await history.Source.ToStorageItemCollection(), false, true, false);
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        // Opposite: Restore original item names

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await this.filesystemOperations.RenameAsync(await history.Destination.ElementAt(i).ToStorageItem() as IStorageItem, Path.GetFileName(history.Source.ElementAt(i)), collision, errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        // Opposite: Delete copied items

                        return await this.filesystemHelpers.DeleteItemsAsync(await history.Destination.ToStorageItemCollection(), false, true, false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        // Opposite: Move the items to original directory

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            returnStatus = await this.filesystemHelpers.MoveItemAsync(await history.Destination.ElementAt(i).ToStorageItem() as IStorageItem, history.Source.ElementAt(i), false);
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
                            returnStatus = await this.filesystemHelpers.RestoreFromTrashAsync(history.Destination.ElementAt(i), history.Source.ElementAt(i), false);
                        }

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        // Opposite: Move restored items to Recycle Bin

                        List<IStorageHistory> rawHistory = new List<IStorageHistory>();

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            IStorageHistory history1 = await this.filesystemOperations.DeleteAsync(await history.Destination.ElementAt(i).ToStorageItem() as IStorageItem, null, errorCode, false, this.cancellationToken);
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
            this.filesystemOperations?.Dispose();
            this.filesystemHelpers?.Dispose();

            this.filesystemOperations = null;
            this.filesystemHelpers = null;
        }

        #endregion
    }
}
