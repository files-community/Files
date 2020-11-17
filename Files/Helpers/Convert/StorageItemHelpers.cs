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
        public static async Task<IStorageItem> ToStorageItem(this string path) 
        {
            FilesystemResult<StorageFile> fsFileResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));

            if (fsFileResult)
            {
                return fsFileResult.Result;
            }

            FilesystemResult<StorageFolder> fsFolderResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

            if (fsFolderResult)
            {
                return fsFolderResult.Result;
            }

            return null;
        }

        public static async Task<bool?> IsOfType(this string path, StorageItemTypes type)
        {
            IStorageItem item = await path.ToStorageItem();
            return item?.IsOfType(type);
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
