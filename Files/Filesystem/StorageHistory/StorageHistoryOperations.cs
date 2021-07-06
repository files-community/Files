using Files.Enums;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryOperations : IStorageHistoryOperations
    {
        #region Private Members

        private IFilesystemOperations filesystemOperations;

        private IFilesystemHelpers filesystemHelpers;

        private IShellPage associatedInstance;

        private readonly CancellationToken cancellationToken;

        #endregion Private Members

        #region Constructor

        public StorageHistoryOperations(IShellPage associatedInstance, CancellationToken cancellationToken)
        {
            this.associatedInstance = associatedInstance;
            this.cancellationToken = cancellationToken;
            filesystemOperations = new ShellFilesystemOperations(associatedInstance);
            filesystemHelpers = this.associatedInstance.FilesystemHelpers;
        }

        #endregion Constructor

        #region IStorageHistoryOperations

        public async Task<ReturnResult> Redo(IStorageHistory history)
        {
            ReturnResult returnStatus = ReturnResult.InProgress;
            Progress<FileSystemStatusCode> errorCode = new Progress<FileSystemStatusCode>();

            errorCode.ProgressChanged += (s, e) => { returnStatus = e.ToStatus(); };

            switch (history.OperationType)
            {
                case FileOperationType.CreateNew: // CreateNew PASS
                    {
                        if (IsHistoryNull(history))
                        {
                            break;
                        }

                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await filesystemOperations.CreateAsync(history.Source.ElementAt(i), errorCode, cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        if (IsHistoryNull(history))
                        {
                            break;
                        }

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await filesystemOperations.RenameAsync(
                                history.Source.ElementAt(i),
                                Path.GetFileName(history.Destination.ElementAt(i).Path),
                                collision,
                                errorCode,
                                cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        if (IsHistoryNull(history))
                        {
                            break;
                        }

                        return await filesystemHelpers.CopyItemsAsync(history.Source, history.Destination.Select((item) => item.Path), false, false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        if (IsHistoryNull(history))
                        {
                            break;
                        }

                        return await filesystemHelpers.MoveItemsAsync(history.Source, history.Destination.Select((item) => item.Path), false, false);
                    }

                case FileOperationType.Extract: // Extract PASS
                    {
                        returnStatus = ReturnResult.Success;
                        Debugger.Break();

                        break;
                    }

                case FileOperationType.Recycle: // Recycle PASS
                    {
                        if (IsHistoryNull(history.Destination))
                        {
                            break;
                        }

                        var newHistory = await filesystemOperations.DeleteItemsAsync(history.Source, null, errorCode, false, cancellationToken);
                        if (newHistory != null)
                        {
                            // We need to change the recycled item paths (since IDs are different) - for Undo() to work
                            App.HistoryWrapper.ModifyCurrentHistory(newHistory);
                        }
                        else
                        {
                            App.HistoryWrapper.RemoveHistory(history, true);
                        }

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        if (IsHistoryNull(history))
                        {
                            break;
                        }

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
            Progress<FileSystemStatusCode> errorCode = new Progress<FileSystemStatusCode>();

            errorCode.ProgressChanged += (s, e) => returnStatus = e.ToStatus();

            switch (history.OperationType)
            {
                case FileOperationType.CreateNew: // CreateNew PASS
                    {
                        // Opposite: Delete created items

                        if (IsHistoryNull(history.Source))
                        {
                            break;
                        }

                        return await filesystemHelpers.DeleteItemsAsync(history.Source, false, true, false);
                    }

                case FileOperationType.Rename: // Rename PASS
                    {
                        // Opposite: Restore original item names

                        if (IsHistoryNull(history))
                        {
                            break;
                        }

                        NameCollisionOption collision = NameCollisionOption.GenerateUniqueName;
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await filesystemOperations.RenameAsync(
                                history.Destination.ElementAt(i),
                                Path.GetFileName(history.Source.ElementAt(i).Path),
                                collision,
                                errorCode,
                                cancellationToken);
                        }

                        break;
                    }

                case FileOperationType.Copy: // Copy PASS
                    {
                        // Opposite: Delete copied items

                        if (IsHistoryNull(history.Destination))
                        {
                            break;
                        }

                        return await filesystemHelpers.DeleteItemsAsync(history.Destination, false, true, false);
                    }

                case FileOperationType.Move: // Move PASS
                    {
                        // Opposite: Move the items to original directory

                        if (IsHistoryNull(history))
                        {
                            break;
                        }

                        return await filesystemHelpers.MoveItemsAsync(history.Destination, history.Source.Select((item) => item.Path), false, false);
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
                        if (IsHistoryNull(history))
                        {
                            break;
                        }

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            returnStatus = await filesystemHelpers.RestoreFromTrashAsync(
                                history.Destination.ElementAt(i),
                                history.Source.ElementAt(i).Path,
                                false);
                        }

                        if (returnStatus == ReturnResult.IntegrityCheckFailed) // Not found, corrupted
                        {
                            App.HistoryWrapper.RemoveHistory(history, false);
                        }

                        break;
                    }

                case FileOperationType.Restore: // Restore PASS
                    {
                        // Opposite: Move restored items to Recycle Bin

                        if (IsHistoryNull(history.Destination))
                        {
                            break;
                        }

                        var newHistory = await filesystemOperations.DeleteItemsAsync(history.Destination, null, errorCode, false, cancellationToken);
                        if (newHistory != null)
                        {
                            // We need to change the recycled item paths (since IDs are different) - for Redo() to work
                            App.HistoryWrapper.ModifyCurrentHistory(newHistory);
                        }
                        else
                        {
                            App.HistoryWrapper.RemoveHistory(history, false);
                        }

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

        #endregion IStorageHistoryOperations

        #region Private Helpers

        // history.Destination is null with CreateNew
        private bool IsHistoryNull(IStorageHistory history) =>
            !(history.Source.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item.Path))
                && (history.Destination == null || history.Destination.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item.Path))));

        private bool IsHistoryNull(IEnumerable<IStorageItemWithPath> source) =>
            !(source.ToList().TrueForAll((item) => item != null && !string.IsNullOrWhiteSpace(item.Path)));

        #endregion Private Helpers

        #region IDisposable

        public void Dispose()
        {
            filesystemOperations?.Dispose();
            filesystemHelpers?.Dispose();

            associatedInstance = null;
            filesystemOperations = null;
            filesystemHelpers = null;
        }

        #endregion IDisposable
    }
}