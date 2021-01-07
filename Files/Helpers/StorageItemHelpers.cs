using Files.Filesystem;
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
            return (await item.ToStorageItemResult(associatedInstance)).Result;
        }

        public static async Task<IStorageItem> ToStorageItem(string path, IShellPage associatedInstance = null)
        {
            FilesystemResult<StorageFile> file;
            FilesystemResult<StorageFolder> folder;

            if (associatedInstance == null)
            {
                file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
                folder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
            }
            else
            {
                file = await associatedInstance?.FilesystemViewModel?.GetFileFromPathAsync(path);
                folder = await associatedInstance?.FilesystemViewModel?.GetFolderFromPathAsync(path);
            }

            if (file != null)
                return (IStorageItem)file;
            else
                return (IStorageItem)folder;
        }

        public static async Task<StorageFile> ToStorageFile(string path, IShellPage associatedInstance = null)
        {
            FilesystemResult<StorageFile> file;

            if (associatedInstance == null)
            {
                file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
            }
            else
            {
                file = await associatedInstance?.FilesystemViewModel?.GetFileFromPathAsync(path);
            }

            return file;
        }

        public static async Task<StorageFolder> ToStorageFolder(string path, IShellPage associatedInstance = null)
        {
            FilesystemResult<StorageFolder> folder;

            if (associatedInstance == null)
            {
                folder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
            }
            else
            {
                folder = await associatedInstance?.FilesystemViewModel?.GetFolderFromPathAsync(path);
            }

            return folder;
        }

        public static async Task<FilesystemResult<IStorageItem>> ToStorageItemResult(this IStorageItemWithPath item, IShellPage associatedInstance = null)
        {
            var returnedItem = new FilesystemResult<IStorageItem>(null, FilesystemErrorCode.ERROR_GENERIC);
            if (!string.IsNullOrEmpty(item.Path))
            {
                returnedItem = (item.ItemType == FilesystemItemType.File) ?
                    ToType<IStorageItem, StorageFile>(associatedInstance != null ?
                        await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(item.Path) :
                        await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path))) :
                    ToType<IStorageItem, StorageFolder>(associatedInstance != null ?
                        await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(item.Path) :
                        await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(item.Path)));
            }
            if (returnedItem.Result == null && item.Item != null)
            {
                returnedItem = new FilesystemResult<IStorageItem>(item.Item, FilesystemErrorCode.ERROR_SUCCESS);
            }
            return returnedItem;
        }

        public static IStorageItemWithPath FromPathAndType(string customPath, FilesystemItemType? itemType)
        {
            return (itemType == FilesystemItemType.File) ?
                    (IStorageItemWithPath)new StorageFileWithPath(null, customPath) :
                    (IStorageItemWithPath)new StorageFolderWithPath(null, customPath);
        }

        public static async Task<FilesystemItemType> GetTypeFromPath(string path, IShellPage associatedInstance = null)
        {
            FilesystemResult<StorageFolder> folder;

            if (associatedInstance == null)
            {
                folder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
            }
            else
            {
                folder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(path);
            }

            if (folder)
                return FilesystemItemType.Directory;
            else
                return FilesystemItemType.File;
        }

        public static async Task<bool> Exists(string path, IShellPage associatedInstance = null)
        {
            FilesystemResult<StorageFile> file;
            FilesystemResult<StorageFolder> folder;

            if (associatedInstance == null)
            {
                file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
                folder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
            }
            else
            {
                file = await associatedInstance?.FilesystemViewModel?.GetFileFromPathAsync(path);
                folder = await associatedInstance?.FilesystemViewModel?.GetFolderFromPathAsync(path);
            }

            return file || folder;
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

        public static FilesystemResult<T> ToType<T, V>(FilesystemResult<V> result) where T : class
        {
            return new FilesystemResult<T>(result.Result as T, result.ErrorCode);
        }
    }
}