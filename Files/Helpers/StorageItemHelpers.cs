using Files.Filesystem;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Helpers
{
    /// <summary>
    /// <see cref="IStorageItem"/> related Helpers
    /// </summary>
    public static class StorageItemHelpers
    {
        public static async Task<IStorageItem> ToStorageItem(this IStorageItemWithPath item, IShellPage associatedInstance = null)
        {
            if (!string.IsNullOrEmpty(item.Path))
            {
                return (item.ItemType == FilesystemItemType.File) ?
                    (associatedInstance != null ?
                        (IStorageItem)(StorageFile)await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(item.Path) :
                        (IStorageItem)(StorageFile)await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path))) :
                    (associatedInstance != null ?
                        (IStorageItem)(StorageFolder)await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(item.Path) :
                        (IStorageItem)(StorageFolder)await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(item.Path)));
            }
            if (item.Item != null)
            {
                return item.Item;
            }
            return null;
        }

        public static IStorageItemWithPath FromPathAndType(string customPath, FilesystemItemType? itemType)
        {
            return (itemType == FilesystemItemType.File) ?
                    (IStorageItemWithPath)new StorageFileWithPath(null, customPath) :
                    (IStorageItemWithPath)new StorageFolderWithPath(null, customPath);
        }

        public static IStorageItemWithPath FromStorageItem(this IStorageItem item, string customPath = null, FilesystemItemType? itemType = null)
        {
            if (item == null)
            {
                return FromPathAndType(customPath, itemType);
            }
            else if (item.IsOfType(StorageItemTypes.File))
            {
                return new StorageFileWithPath(item as StorageFile, string.IsNullOrEmpty(item.Path) ? customPath : item.Path);
            }
            else if (item.IsOfType(StorageItemTypes.Folder))
            {
                return new StorageFolderWithPath(item as StorageFolder, string.IsNullOrEmpty(item.Path) ? customPath : item.Path);
            }
            return null;
        }
    }
}