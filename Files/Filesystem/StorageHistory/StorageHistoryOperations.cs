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

        private readonly IFilesystemOperations _filesystemOperations;

        private readonly FilesystemHelpers _filesystemHelpers;

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

        #region FilesystemHistory

        public async Task Redo(IStorageHistory history) 
        {
            switch (history.OperationType)
            {
                case FileOperationType.CreateNew: // CreateNew
                    foreach (IStorageItem item in history.Source)
                    {
                        FilesystemItemType itemType = FilesystemItemType.File;
                        if (item.IsOfType(StorageItemTypes.File))
                            itemType = FilesystemItemType.File;
                        else if (item.IsOfType(StorageItemTypes.Folder))
                            itemType = FilesystemItemType.Directory;

                        await this._filesystemOperations.CreateAsync(item.Path, itemType, null, this._cancellationToken);
                    }
                    break;

                case FileOperationType.Rename: // Rename
                    {
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this._filesystemOperations.RenameAsync(await (history.Source.ElementAt(i) as string).ToStorageItem(), Path.GetFileName(history.Destination.ElementAt(i) as string), true, null, this._cancellationToken);
                        }
                        break;
                    }

                case FileOperationType.Copy: // Copy
                    await this._filesystemHelpers.CopyItemsAsync(history.Source as IEnumerable<IStorageItem>, history.Destination.ElementAt(0) as IStorageItem, false);
                    break;

                case FileOperationType.Move: // Move
                    await this._filesystemHelpers.MoveItemsAsync(history.Source as IEnumerable<IStorageItem>, history.Destination.ElementAt(0) as IStorageItem, false);
                    break;

                case FileOperationType.Extract: // Extract

                    // Cannot compress items
                    Debugger.Break();
                    break;

                case FileOperationType.Recycle: // Recycle
                    {
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this._filesystemOperations.DeleteAsync(
                                await Path.Combine((history.Destination.ElementAt(0) as IStorageItem).Path,
                                    Path.GetFileName((history.Source.ElementAt(i) as RecycleBinItem).ItemOriginalPath)).ToStorageItem(),
                                null, null, false, false, this._cancellationToken);
                        }
                        break;
                    }

                case FileOperationType.Restore:
                    {
                        Debugger.Break();
                        throw new NotImplementedException(); 

                        //for (int i = 0; i < history.Destination.Count(); i++)
                        //{
                        //    await _filesystemOperations.RestoreFromTrashAsync(history.Source.ElementAt(i) as RecycleBinItem, history.Destination.ElementAt(i) as IStorageItem, null, null, this._cancellationToken);
                        //}
                        break;
                    }

                case FileOperationType.Delete: // Delete
                    // Items cannot be deleted if they havent been undo-deleted
                    break;
            }
        }

        public async Task Undo(IStorageHistory history)
        {
            switch (history.OperationType)
            {
                case FileOperationType.CreateNew: // CreateNew
                    {
                        // Opposite: Delete created items

                        await this._filesystemHelpers.DeleteAsync(history.Source as IEnumerable<IStorageItem>, false, true, false);
                        break;
                    }

                case FileOperationType.Rename: // Rename
                    {
                        // Opposite: Restore original item names

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await this._filesystemOperations.RenameAsync(await (history.Destination.ElementAt(0) as string).ToStorageItem(), Path.GetFileName(history.Source.ElementAt(i) as string), true, null, this._cancellationToken);
                        }
                        break;
                    }

                case FileOperationType.Copy: // Copy
                    {
                        // Opposite: Delete copied items

                        List<IStorageItem> itemsToDelete = new List<IStorageItem>();
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            itemsToDelete.Add(await Path.Combine((history.Destination.ElementAt(0) as IStorageItem).Path, 
                                Path.GetFileName((history.Source.ElementAt(i) as IStorageItem).Path)).ToStorageItem());
                        }

                        await this._filesystemHelpers.DeleteAsync(itemsToDelete, false, true, false);
                        break;
                    }
                    
                case FileOperationType.Move: // Move
                    {
                        // Opposite: Move the items to original directory

                        List<IStorageItem> sourceItems = new List<IStorageItem>();
                        IStorageItem destinationItem = null;

                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            sourceItems.Add(await Path.Combine((history.Destination.ElementAt(0) as IStorageItem).Path, Path.GetFileName((history.Source.ElementAt(i) as IStorageItem).Path)).ToStorageItem());
                            destinationItem = await Path.GetDirectoryName((history.Source.ElementAt(i) as IStorageItem).Path).ToStorageItem();
                        }

                        await this._filesystemHelpers.MoveItemsAsync(sourceItems, destinationItem, false);
                        break;
                    }

                case FileOperationType.Extract: // Extract
                    {
                        // No opposite for file archive extraction
                        Debugger.Break();
                        break;
                    }

                case FileOperationType.Recycle: // Recycle
                    {
                        // Opposite: Restore recycled items

                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            // history.Destination is null
                            await _filesystemOperations.RestoreFromTrashAsync(history.Source.ElementAt(i) as RecycleBinItem, history.Destination.ElementAt(0) as IStorageItem, null, null, this._cancellationToken);
                        }
                        break;
                    }

                case FileOperationType.Restore: // Restore
                    {
                        // Opposite: Delete restored items
                        for (int i = 0; i < history.Source.Count(); i++)
                        {
                            await this._filesystemOperations.DeleteAsync(
                            await Path.Combine((history.Destination.ElementAt(0) as IStorageItem).Path,
                                Path.GetFileName((history.Source.ElementAt(i) as RecycleBinItem).ItemOriginalPath)).ToStorageItem(),
                            null, null, false, false, this._cancellationToken);
                        }
                        break;
                    }

                case FileOperationType.Delete: // Delete
                    {
                        // Opposite: No opposite for pernament deletion
                        break;
                    }
            }
        }

        #endregion
    }
}
