using Files.Filesystem;
using System;
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
                        return null; /* fsFileWithPathResult.Result */ // Could be done if IStorageItemWithPath implemented IStorageItem
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
                        return null; /* fsFolderWithPathResult.Result; */ // Could be done if IStorageItemWithPath implemented IStorageItem
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
    }
}
