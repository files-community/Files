using Files.Filesystem.FilesystemOperations;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryOperations : IStorageHistoryOperations
    {
        #region Private Members

        private readonly IShellPage _appInstance;

        private readonly IFilesystemOperations _filesystemOperations;

        private readonly CancellationToken _cancellationToken;

        #endregion

        #region Constructor

        public StorageHistoryOperations(IShellPage appInstance, IFilesystemOperations filesystemOperations, CancellationToken cancellationToken)
        {
            this._appInstance = appInstance;
            this._filesystemOperations = filesystemOperations;
            this._cancellationToken = cancellationToken;
        }

        #endregion

        #region FilesystemHistory

        public async Task Redo(IStorageHistory history) // TODO: Return Task<Stauts>
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
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await this._filesystemOperations.RenameAsync(await (history.Source.ElementAt(i) as string).ToStorageItem(), Path.GetFileName(history.Destination.ElementAt(i) as string), true, null, this._cancellationToken);
                        }
                        break;
                    }

                case FileOperationType.Copy: // Copy
                    await new FilesystemHelpers(_appInstance, _filesystemOperations, _cancellationToken).CopyItems(history.Source as IEnumerable<IStorageItem>, history.Destination.ElementAt(0) as IStorageItem, false);
                    break;

                case FileOperationType.Move: // Move
                    await new FilesystemHelpers(_appInstance, _filesystemOperations, _cancellationToken).MoveItems(history.Source as IEnumerable<IStorageItem>, history.Destination.ElementAt(0) as IStorageItem, false);
                    break;

                case FileOperationType.Extract: // Extract
                    // TODO: Extract
                    throw new NotImplementedException();
                    break;

                case FileOperationType.Recycle: // Recycle
                    // TODO: Restore RestoreItem_Click
                    throw new NotImplementedException();
                    break;

                case FileOperationType.Delete: // Delete
                    // Items cannot be deleted if they havent been deleted-undo
                    break;
            }
        }

        public async Task Undo(IStorageHistory history) // TODO: Return Task<Stauts>
        {
            switch (history.OperationType)
            {
                case FileOperationType.CreateNew: // CreateNew
                    // Opposite: Delete created items
                    await new FilesystemHelpers(_appInstance, _filesystemOperations, _cancellationToken)
                            .DeleteAsync(history.Source as IEnumerable<IStorageItem>, false, true, false);

                    break;

                case FileOperationType.Rename: // Rename
                    {
                        // Opposite: Restore original item names
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            await this._filesystemOperations.RenameAsync(await (history.Destination.ElementAt(i) as string).ToStorageItem(), Path.GetFileName(history.Source.ElementAt(i) as string), true, null, this._cancellationToken);
                        }
                        break;
                    }

                case FileOperationType.Copy: // Copy
                    {
                        // Opposite: Delete the copied items

                        List<IStorageItem> itemsToDelete = new List<IStorageItem>();
                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            string item1 = (history.Destination.ElementAt(i) as IStorageItem).Path;
                            string item2 = Path.GetFileName((history.Source.ElementAt(i) as IStorageItem).Path);

                            itemsToDelete.Add(await Path.Combine(item1, item2).ToStorageItem());
                        }

                        await new FilesystemHelpers(_appInstance, _filesystemOperations, _cancellationToken)
                            .DeleteAsync(itemsToDelete, false, true, false);
                        break;
                    }

                case FileOperationType.Move: // Move
                    {
                        // Opposite: Move the items to original directory

                        List<IStorageItem> sourceItems = new List<IStorageItem>();
                        IStorageItem destinationItem = null;

                        for (int i = 0; i < history.Destination.Count(); i++)
                        {
                            string item1 = (history.Source.ElementAt(i) as IStorageItem).Path;
                            string item2 = (history.Destination.ElementAt(i) as IStorageItem).Path;

                            sourceItems.Add(await Path.Combine(item2, Path.GetFileName(item1)).ToStorageItem());
                            destinationItem = await Path.GetDirectoryName(item1).ToStorageItem();
                        }

                        await new FilesystemHelpers(_appInstance, _filesystemOperations, _cancellationToken)
                            .MoveItems(sourceItems, destinationItem, false);
                        break;
                    }

                case FileOperationType.Extract: // Extract
                    // Opposite: TODO Create opposite for Extraction
                    throw new NotImplementedException();
                    break;

                case FileOperationType.Recycle: // Recycle
                    // Opposite: Restore recycled items

                    break;

                case FileOperationType.Delete: // Delete
                    // Opposite: No opposite for pernament deletion

                    break;
            }
        }

        #endregion
    }
}
