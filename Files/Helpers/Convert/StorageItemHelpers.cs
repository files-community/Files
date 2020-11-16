using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Helpers
{
    public static class StorageItemHelpers
    {
        public static async Task<IStorageItem> ToStorageItem(this string path)
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(path));
            return await folder.GetItemAsync(Path.GetFileName(path));
        }

        public static async Task<bool> IsOfType(this string path, StorageItemTypes type)
        {
            IStorageItem item = await path.ToStorageItem();
            return item.IsOfType(type);
        }

        public static async Task<IEnumerable<IStorageItem>> ToStorageItemCollection(this IEnumerable<string> paths)
        {
            List<IStorageItem> output = new List<IStorageItem>();
            StorageFolder folder;
            foreach (string item in paths)
            {
                folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(item));
                output.Add(await folder.TryGetItemAsync(Path.GetFileName(item)));
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItem>> ToStorageItemCollection(this IEnumerable<ListedItem> listedItems)
        {
            List<IStorageItem> output = new List<IStorageItem>();
            StorageFolder folder;
            foreach (ListedItem item in listedItems)
            {
                folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(item.ItemPath));
                output.Add(await folder.TryGetItemAsync(Path.GetFileName(item.ItemPath)));
            }

            return output;
        }
    }
}
