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
            StorageFolder item = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(path));
            return await item.GetItemAsync(Path.GetFileName(path));
        }

        public static async Task<IEnumerable<IStorageItem>> ToStorageItemCollection(this IEnumerable<ListedItem> listedItems)
        {
            List<IStorageItem> output = new List<IStorageItem>();
            foreach (ListedItem item in listedItems)
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(item.ItemPath));
                output.Add(await folder.TryGetItemAsync(Path.GetFileName(item.ItemPath)));
            }

            return output;
        }
    }
}
