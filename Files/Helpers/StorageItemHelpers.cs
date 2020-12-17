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

        /*
        // TODO: If the TODO of IStorageItemWithPath is implemented, change return type to IStorageItem
        public static async Task<IStorageItem> ToStorageItem(this string path, StorageFolderWithPath parentFolder = null)
        {
            FilesystemResult<StorageFolderWithPath> fsRootFolderResult = await FilesystemTasks.Wrap(async () =>
            {
                return (StorageFolderWithPath)await Path.GetPathRoot(path).ToStorageItemWithPath();
            });

            FilesystemResult<StorageFile> fsFileResult = await FilesystemTasks.Wrap(() =>
            {
                return StorageFileExtensions.DangerousGetFileFromPathAsync(path, fsRootFolderResult.Result, parentFolder);
            });

            if (fsFileResult)
            {
                if (!string.IsNullOrWhiteSpace(fsFileResult.Result.Path))
                {
                    return fsFileResult.Result;
                }
                else
                {
                    FilesystemResult<StorageFileWithPath> fsFileWithPathResult = await FilesystemTasks.Wrap(() =>
                    {
                        return StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(path, fsRootFolderResult);
                    });

                    if (fsFileWithPathResult)
                    {
                        return null; // fsFileWithPathResult.Result // Could be done if IStorageItemWithPath implemented IStorageItem
                    }
                }
            }

            FilesystemResult<StorageFolder> fsFolderResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

            if (fsFolderResult)
            {
                if (!string.IsNullOrWhiteSpace(fsFolderResult.Result.Path))
                {
                    return fsFolderResult.Result;
                }
                else
                {
                    FilesystemResult<StorageFolderWithPath> fsFolderWithPathResult = await FilesystemTasks.Wrap(() =>
                    {
                        return StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, fsRootFolderResult);
                    });

                    if (fsFolderWithPathResult)
                    {
                        return null; // fsFolderWithPathResult.Result; // Could be done if IStorageItemWithPath implemented IStorageItem
                    }
                }
            }

            return null;
        }

        public static async Task<IStorageItemWithPath> ToStorageItemWithPath(this string path, StorageFolderWithPath parentFolder = null)
        {
            StorageFolderWithPath rootFolder = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));

            FilesystemResult<StorageFileWithPath> fsFileWithPathResult = await FilesystemTasks.Wrap(() =>
            {
                return StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(path, rootFolder, parentFolder);
            });

            if (fsFileWithPathResult)
            {
                return fsFileWithPathResult.Result;
            }

            FilesystemResult<StorageFolderWithPath> fsFolderWithPathResult = await FilesystemTasks.Wrap(() =>
            {
                return StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, rootFolder);
            });

            if (fsFolderWithPathResult)
            {
                return fsFolderWithPathResult.Result;
            }

            return null;
        }

        public static async Task<bool?> IsOfType(this string path, StorageItemTypes type)
        {
            IStorageItem item = await path.ToStorageItem() is IStorageItem storageItem ? storageItem : null;
            return item?.IsOfType(type);
        }

        public static async Task<IEnumerable<IStorageItemWithPath>> ToStorageItemWithPathCollection(this IEnumerable<ListedItem> listedItems,
                                                                                                    StorageFolderWithPath parentFolder = null)
        {
            List<IStorageItemWithPath> output = new List<IStorageItemWithPath>();

            foreach (ListedItem item in listedItems)
            {
                output.Add(await item.ItemPath.ToStorageItemWithPath(parentFolder));
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItemWithPath>> ToStorageItemWithPathCollection(this IEnumerable<string> paths,
                                                                                                    StorageFolderWithPath parentFolder = null)
        {
            List<IStorageItemWithPath> output = new List<IStorageItemWithPath>();

            foreach (string path in paths)
            {
                output.Add(await path.ToStorageItemWithPath(parentFolder));
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItem>> ToStorageItemCollection(this IEnumerable<string> paths, StorageFolderWithPath parentFolder = null)
        {
            List<IStorageItem> output = new List<IStorageItem>();

            foreach (string path in paths)
            {
                output.Add(await path.ToStorageItem(parentFolder));
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItem>> ToStorageItemCollection(this IEnumerable<ListedItem> listedItems,
                                                                                    StorageFolderWithPath parentFolder = null)
        {
            List<IStorageItem> output = new List<IStorageItem>();

            foreach (ListedItem item in listedItems)
            {
                output.Add(await item.ItemPath.ToStorageItem(parentFolder));
            }

            return output;
        }
        */
    }
}