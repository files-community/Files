using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Uwp.Helpers.ListedItems
{
    public static class ListedItemHelpers
    {
        public static async Task<ListedItem> AddFolderAsync(
            BaseStorageFolder folder,
            StorageFolderWithPath currentStorageFolder = null,
            string dateReturnFormat = null,
            CancellationTokenSource cancellationTokenSource = null,
            bool getMinimalPropertySet = false)
        {
            if (cancellationTokenSource is CancellationTokenSource source && source.Token.IsCancellationRequested)
            {
                return null;
            }
            else
            {
                DateTimeOffset? itemModifiedDate = null;

                if (!getMinimalPropertySet)
                {
                    var basicProperties = await folder.GetBasicPropertiesAsync();
                    itemModifiedDate = basicProperties.DateModified;
                }

                return new ListedItem(folder.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemNameRaw = folder.DisplayName,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateCreatedReal = folder.DateCreated,
                    ItemType = folder.DisplayType,
                    IsHiddenItem = false,
                    Opacity = 1,
                    FileImage = null,
                    ItemPath = string.IsNullOrEmpty(folder.Path) && currentStorageFolder != null ? PathNormalization.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
                    FileSize = null,
                    FileSizeBytes = 0
                };
            }
        }

        public static async Task<ListedItem> AddFileAsync(
            BaseStorageFile file,
            StorageFolderWithPath currentStorageFolder = null,
            string dateReturnFormat = null,
            CancellationTokenSource cancellationTokenSource = null,
            bool getMinimalPropertySet = false
        )
        {
            if (cancellationTokenSource is CancellationTokenSource source && source.Token.IsCancellationRequested)
            {
                return null;
            }
            else
            {
                string itemSizeText = null;
                long? itemSizeBytes = null;
                DateTimeOffset? itemModifiedDate = null;

                if (!getMinimalPropertySet)
                {
                    var basicProperties = await file.GetBasicPropertiesAsync();
                    itemModifiedDate = basicProperties.DateModified;
                    itemSizeBytes = (long)basicProperties.Size;
                    itemSizeText = itemSizeBytes?.ToSizeString();
                }

                if (file.Name.EndsWith(".lnk", StringComparison.Ordinal) || file.Name.EndsWith(".url", StringComparison.Ordinal))
                {
                    // This shouldn't happen, StorageFile API does not support shortcuts
                    Debug.WriteLine("Something strange: StorageFile api returned a shortcut");
                    return null;
                }
                // TODO: is this needed to be handled here?
                else if (App.LibraryManager.TryGetLibrary(file.Path, out LibraryLocationItem library))
                {
                    return new LibraryItem(library)
                    {
                        ItemDateModifiedReal = itemModifiedDate,
                        ItemDateCreatedReal = file.DateCreated,
                    };
                }
                else
                {
                    return new ListedItem(file.FolderRelativeId, dateReturnFormat)
                    {
                        PrimaryItemAttribute = StorageItemTypes.File,
                        FileExtension = file.FileType,
                        IsHiddenItem = false,
                        Opacity = 1,
                        FileImage = null,
                        ItemNameRaw = file.Name,
                        ItemDateModifiedReal = itemModifiedDate,
                        ItemDateCreatedReal = file.DateCreated,
                        ItemType = file.DisplayType,
                        ItemPath = string.IsNullOrEmpty(file.Path) && currentStorageFolder != null ? PathNormalization.Combine(currentStorageFolder.Path, file.Name) : file.Path,
                        FileSize = itemSizeText,
                        FileSizeBytes = itemSizeBytes,
                    };
                }
            }
        }
    }
}
