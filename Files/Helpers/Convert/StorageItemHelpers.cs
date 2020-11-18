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
        public static async Task<IStorageItem> ToStorageItem(this string path) // TODO: If the TODO of IStorageItemWithPath is implemented, change return type to IStorageItem
        {
            StorageFolderWithPath rootFolder = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));

            FilesystemResult<StorageFile> fsFileResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path, rootFolder));

            if (fsFileResult)
            {
                if (!string.IsNullOrWhiteSpace(fsFileResult.Result.Path))
                    return fsFileResult.Result;
                else
                {
                    FilesystemResult<StorageFileWithPath> fsFileWithPathResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(path, rootFolder));

                    if (fsFileWithPathResult)
                        return /* fsFileWithPathResult.Result */ null; // Could be done if IStorageItemWithPath implemented IStorageItem
                }
            }

            FilesystemResult<StorageFolder> fsFolderResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

            if (fsFolderResult)
            {
                if (!string.IsNullOrWhiteSpace(fsFolderResult.Result.Path))
                    return fsFolderResult.Result;
                else
                {
                    FilesystemResult<StorageFolderWithPath> fsFolderWithPathResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, rootFolder));

                    if (fsFolderWithPathResult)
                        return /* fsFolderWithPathResult.Result; */ null; // Could be done if IStorageItemWithPath implemented IStorageItem
                }
            }

            return null;
        }

        public static async Task<IStorageItemWithPath> ToStorageItemWithPath(this string path)
        {
            StorageFolderWithPath rootFolder = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));

            FilesystemResult<StorageFileWithPath> fsFileWithPathResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(path, rootFolder));

            if (fsFileWithPathResult)
                return fsFileWithPathResult.Result;

            FilesystemResult<StorageFolderWithPath> fsFolderWithPathResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, rootFolder));

            if (fsFolderWithPathResult)
                return fsFolderWithPathResult.Result;

            return null;
        }

        public static async Task<bool?> IsOfType(this string path, StorageItemTypes type)
        {
            IStorageItem item = await path.ToStorageItem() is IStorageItem storageItem ? storageItem : null;
            return item?.IsOfType(type);
        }

        public static async Task<IEnumerable<IStorageItemWithPath>> ToStorageItemWithPathCollection(this IEnumerable<ListedItem> listedItems)
        {
            List<IStorageItemWithPath> output = new List<IStorageItemWithPath>();

            foreach (ListedItem item in listedItems)
            {
                output.Add(await item.ItemPath.ToStorageItemWithPath());
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItemWithPath>> ToStorageItemWithPathCollection(this IEnumerable<string> paths)
        {
            List<IStorageItemWithPath> output = new List<IStorageItemWithPath>();

            foreach (string path in paths)
            {
                output.Add(await path.ToStorageItemWithPath());
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItem>> ToStorageItemCollection(this IEnumerable<string> paths)
        {
            List<IStorageItem> output = new List<IStorageItem>();

            foreach (string path in paths)
            {
                output.Add(await path.ToStorageItem());
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItem>> ToStorageItemCollection(this IEnumerable<ListedItem> listedItems)
        {
            List<IStorageItem> output = new List<IStorageItem>();

            foreach (ListedItem item in listedItems)
            {
                output.Add(await item.ItemPath.ToStorageItem());
            }

            return output;
        }
    }
}
