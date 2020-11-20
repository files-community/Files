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
                        if (IsHistoryNull(history)) break;

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this.filesystemOperations.RenameAsync(await history.Source.ElementAt(i).ToStorageItem(), Path.GetFileName(history.Destination.ElementAt(i)), collision, errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        if (IsHistoryNull(history)) break;

                        return await this.filesystemHelpers.CopyItemsAsync(ExtractDictionaryFilesFromHistory(history.Destination, history.Source), history.Destination.Select((item) => item.Split('|')[0]), false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        if (IsHistoryNull(history)) break;

                        return await this.filesystemHelpers.MoveItemsAsync(ExtractDictionaryFilesFromHistory(history.Destination, history.Source), history.Destination.Select((item) => item.Split('|')[0]), false);
                    }

                case FileOperationType.Extract: // Extract PASS
                    {
                        returnStatus = ReturnResult.Success;
                        Debugger.Break();

                        break;
                    }

                case FileOperationType.Recycle: // Recycle PASS
                    {
                        if (IsHistoryNull(history.Source)) break;

                        List<IStorageHistory> rawHistory = new List<IStorageHistory>();

                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            IStorageHistory history1 = await this.filesystemOperations.DeleteAsync(await history.Source.ElementAt(i).ToStorageItem(), null, errorCode, false, this.cancellationToken);
                            rawHistory.Add(history1);
                        }

                        IStorageHistory newHistory = new StorageHistory(FileOperationType.Recycle, rawHistory.SelectMany((item) => item.Source).ToList(), rawHistory.SelectMany((item) => item.Destination).ToList());

                        // We need to change the recycled item paths (since IDs are different) - for Undo() to work
                        App.StorageHistory[App.StorageHistoryIndex - 1].Modify(newHistory);

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        if (IsHistoryNull(history)) break;

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

                        if (IsHistoryNull(history.Source)) break;

                        return await this.filesystemHelpers.DeleteItemsAsync(ExtractDictionaryFilesFromHistory(history.Destination), false, true, false);
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        // Opposite: Restore original item names

                        if (IsHistoryNull(history)) break;

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await this.filesystemOperations.RenameAsync(await history.Destination.ElementAt(i).ToStorageItem(), Path.GetFileName(history.Source.ElementAt(i)), collision, errorCode, this.cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        // Opposite: Delete copied items

                        if (IsHistoryNull(history.Destination)) break;

                        return await this.filesystemHelpers.DeleteItemsAsync(ExtractDictionaryFilesFromHistory(history.Destination), false, true, false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        // Opposite: Move the items to original directory

                        if (IsHistoryNull(history)) break;

                        return await this.filesystemHelpers.MoveItemsAsync(ExtractDictionaryFilesFromHistory(history.Destination), history.Source, false);
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
                            returnStatus = await this.filesystemHelpers.RestoreFromTrashAsync(history.Destination.ElementAt(i), history.Source.ElementAt(i), false);
                        }

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        // Opposite: Move restored items to Recycle Bin

                        if (IsHistoryNull(history.Destination)) break;

                        List<IStorageHistory> rawHistory = new List<IStorageHistory>();

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            IStorageHistory history1 = await this.filesystemOperations.DeleteAsync(await history.Destination.ElementAt(i).ToStorageItem(), null, errorCode, false, this.cancellationToken);
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

        #region Private Helpers

        private Dictionary<string, FilesystemItemType> ExtractDictionaryFilesFromHistory(IEnumerable<string> source)
        {
            Dictionary<string, FilesystemItemType> items = new Dictionary<string, FilesystemItemType>();

            foreach (string item in source)
            {
                items.Add(item.Split('|')[0], item.Split('|')[1].GetEnum<FilesystemItemType>());
            }

            return items;
        }

        private Dictionary<string, FilesystemItemType> ExtractDictionaryFilesFromHistory(IEnumerable<string> source, IEnumerable<string> replace)
        {
            Dictionary<string, FilesystemItemType> items = new Dictionary<string, FilesystemItemType>();

            for (int i = 0; i < source.Count(); i++)
            {
                items.Add(replace.ElementAt(i), source.ElementAt(i).Split('|')[1].GetEnum<FilesystemItemType>());
            }

            return items;
        }

        private bool IsHistoryNull(IStorageHistory history) =>
            !(history.Source.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item))
                && history.Destination.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item)));

        private bool IsHistoryNull(IEnumerable<string> source) =>
            !(source.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item)));

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
