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
            FilesystemResult<StorageFolder> fsFolderResult = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(path)).AsTask());

            if (fsFolderResult)
            {
                FilesystemResult<IStorageItem> fsItemResult = await FilesystemTasks.Wrap(() => fsFolderResult.Result.GetItemAsync(Path.GetFileName(path)).AsTask());

                return fsItemResult.Result;
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
            FilesystemResult<StorageFolder> fsFolderResult;
            FilesystemResult<IStorageItem> fsItemResult;

            foreach (string item in paths)
            {
                
                fsFolderResult = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(item)).AsTask());

                if (fsFolderResult)
                {
                    fsItemResult = await FilesystemTasks.Wrap(() => fsFolderResult.Result.TryGetItemAsync(Path.GetFileName(item)).AsTask());

                    if (fsItemResult)
                        output.Add(fsItemResult.Result);
                }
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItem?>> ToStorageItemCollection(this IEnumerable<ListedItem> listedItems)
        {
            List<IStorageItem> output = new List<IStorageItem>();
            FilesystemResult<StorageFolder> fsFolderResult;
            FilesystemResult<IStorageItem> fsItemResult;

            foreach (ListedItem item in listedItems)
            {
                fsFolderResult = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(item.ItemPath)).AsTask());
                
                if (fsFolderResult)
                {
                    fsItemResult = await FilesystemTasks.Wrap(() => fsFolderResult.Result.TryGetItemAsync(Path.GetFileName(item.ItemPath)).AsTask());

                    if (fsItemResult)
                        output.Add(fsItemResult.Result);
                }
            }

            return output;
        }
    }

    /// <summary>
    /// Provides safety with items that are stored in Recycle Bin and <see cref="IStorageItem"/> related Helpers
    /// </summary>
    public static class SafeStorageItemHelpers // TODO: Consider merging this with StorageItemHelpers ?
    {
        public static async Task<IStorageItem> ToSafeStorageItem(this string path, IShellPage associatedInstance)
        {
            if (!await new RecycleBinHelpers(associatedInstance).IsRecycleBinItem(path))
                return await StorageItemHelpers.ToStorageItem(path);
            else
            {
                FilesystemResult<StorageFile> fsFileResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));

                if (fsFileResult)
                {
                    return (IStorageItem)fsFileResult;
                }

                FilesystemResult<StorageFolder> fsFolderResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

                if (fsFolderResult)
                {
                    return (IStorageItem)fsFolderResult;
                }
            }
            return null;
        }

        public static async Task<IEnumerable<IStorageItem>> ToSafeStorageItemCollection(this IEnumerable<string> paths, IShellPage associatedInstance)
        {
            List<IStorageItem> output = new List<IStorageItem>();
            FilesystemResult<StorageFolder> fsFolderResult;
            FilesystemResult<StorageFile> fsFileResult;

            foreach (string path in paths)
            {
                if (!await new RecycleBinHelpers(associatedInstance).IsRecycleBinItem(path))
                    output.Add(await StorageItemHelpers.ToStorageItem(path));
                else
                {
                    fsFileResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));

                    if (fsFileResult)
                    {
                        output.Add((IStorageItem)fsFileResult);
                        continue;
                    }

                    fsFolderResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

                    if (fsFolderResult)
                    {
                        output.Add((IStorageItem)fsFolderResult);
                    }
                }
            }

            return output;
        }

        public static async Task<IEnumerable<IStorageItem>> ToSafeStorageItemCollection(this IEnumerable<ListedItem> listedItems, IShellPage associatedInstance)
        {
            List<IStorageItem> output = new List<IStorageItem>();
            FilesystemResult<StorageFolder> fsFolderResult;
            FilesystemResult<StorageFile> fsFileResult;

            foreach (ListedItem item in listedItems)
            {
                if (!await new RecycleBinHelpers(associatedInstance).IsRecycleBinItem(item.ItemPath))
                    output.Add(await StorageItemHelpers.ToStorageItem(item.ItemPath));
                else
                {
                    fsFileResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.ItemPath));

                    if (fsFileResult)
                    {
                        output.Add((IStorageItem)fsFileResult.Result);
                        continue;
                    }

                    fsFolderResult = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(item.ItemPath));


                    if (fsFolderResult)
                    {
                        output.Add((IStorageItem)fsFolderResult.Result);
                    }
                }
            }

            return output;
        }
    }
}
