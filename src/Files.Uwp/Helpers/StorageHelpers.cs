using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.Uwp.Helpers
{
    /// <summary>
    /// <see cref="IStorageItem"/> related Helpers
    /// </summary>
    public static class StorageHelpers
    {
        public static async Task<IStorageItem> ToStorageItem(this IStorageItemWithPath item, IShellPage associatedInstance = null)
        {
            return (await item.ToStorageItemResult(associatedInstance)).Result;
        }

        public static async Task<TRequested> ToStorageItem<TRequested>(string path, IShellPage associatedInstance = null) where TRequested : IStorageItem
        {
            FilesystemResult<BaseStorageFile> file = null;
            FilesystemResult<BaseStorageFolder> folder = null;

            if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: In the future, when IStorageItemWithPath will inherit from IStorageItem,
                // we could implement this code here for getting .lnk files
                // for now, we can't
                return default;
            }

            // Fast get attributes
            bool exists = NativeFileOperationsHelper.GetFileAttributesExFromApp(path, NativeFileOperationsHelper.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out NativeFileOperationsHelper.WIN32_FILE_ATTRIBUTE_DATA itemAttributes);
            if (exists) // Exists on local storage
            {
                // Directory
                if (itemAttributes.dwFileAttributes.HasFlag(System.IO.FileAttributes.Directory))
                {
                    if (typeof(IStorageFile).IsAssignableFrom(typeof(TRequested))) // Wanted file
                    {
                        // NotAFile
                        return default;
                    }
                    else // Just get the directory
                    {
                        await GetFolder();
                    }
                }
                else // File
                {
                    if (typeof(IStorageFolder).IsAssignableFrom(typeof(TRequested))) // Wanted directory
                    {
                        // NotAFile
                        return default;
                    }
                    else // Just get the file
                    {
                        await GetFile();
                    }
                }
            }
            else // Does not exist or is not present on local storage
            {
                Debug.WriteLine($"Path does not exist. Trying to find storage item manually (HRESULT: {Marshal.GetLastWin32Error()})");

                if (typeof(IStorageFile).IsAssignableFrom(typeof(TRequested)))
                {
                    await GetFile();
                }
                else if (typeof(IStorageFolder).IsAssignableFrom(typeof(TRequested)))
                {
                    await GetFolder();
                }
                else if (typeof(IStorageItem).IsAssignableFrom(typeof(TRequested)))
                {
                    if (System.IO.Path.HasExtension(path)) // Possibly a file
                    {
                        await GetFile();
                    }

                    if (!file || file.Result == null) // Possibly a folder
                    {
                        await GetFolder();

                        if (file == null && (!folder || folder.Result == null))
                        {
                            // Try file because it wasn't checked
                            await GetFile();
                        }
                    }
                }
            }

            if (file != null && file)
            {
                return (TRequested)(IStorageItem)file.Result;
            }
            else if (folder != null && folder)
            {
                return (TRequested)(IStorageItem)folder.Result;
            }

            return default;

            // Extensions

            async Task GetFile()
            {
                if (associatedInstance == null || associatedInstance.FilesystemViewModel == null)
                {
                    var rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
                    file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path, rootItem));
                }
                else
                {
                    file = await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(path);
                }
            }

            async Task GetFolder()
            {
                if (associatedInstance == null)
                {
                    var rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
                    folder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, rootItem));
                }
                else
                {
                    folder = await associatedInstance?.FilesystemViewModel?.GetFolderFromPathAsync(path);
                }
            }
        }

        public static async Task<long> GetFileSize(this IStorageFile file)
        {
            BasicProperties properties = await file.GetBasicPropertiesAsync();
            return (long)properties.Size;
        }

        public static async Task<FilesystemResult<IStorageItem>> ToStorageItemResult(this IStorageItemWithPath item, IShellPage associatedInstance = null)
        {
            var returnedItem = new FilesystemResult<IStorageItem>(null, FileSystemStatusCode.Generic);
            var rootItem = associatedInstance == null ? await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(item.Path)) : null;
            if (!string.IsNullOrEmpty(item.Path))
            {
                returnedItem = (item.ItemType == FilesystemItemType.File) ?
                    ToType<IStorageItem, BaseStorageFile>(associatedInstance != null ?
                        await associatedInstance.FilesystemViewModel.GetFileFromPathAsync(item.Path) :
                        await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path, rootItem))) :
                    ToType<IStorageItem, BaseStorageFolder>(associatedInstance != null ?
                        await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(item.Path) :
                        await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(item.Path, rootItem)));
            }
            if (returnedItem.Result == null && item.Item != null)
            {
                returnedItem = new FilesystemResult<IStorageItem>(item.Item, FileSystemStatusCode.Success);
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
            IStorageItem item = await ToStorageItem<IStorageItem>(path, associatedInstance);

            return item == null ? FilesystemItemType.File : (item.IsOfType(StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File);
        }

        public static bool Exists(string path)
        {
            return NativeFileOperationsHelper.GetFileAttributesExFromApp(path, NativeFileOperationsHelper.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out _);
        }

        public static IStorageItemWithPath FromStorageItem(this IStorageItem item, string customPath = null, FilesystemItemType? itemType = null)
        {
            if (item == null)
            {
                return FromPathAndType(customPath, itemType);
            }
            else if (item.IsOfType(StorageItemTypes.File))
            {
                return new StorageFileWithPath(item.AsBaseStorageFile(), string.IsNullOrEmpty(item.Path) ? customPath : item.Path);
            }
            else if (item.IsOfType(StorageItemTypes.Folder))
            {
                return new StorageFolderWithPath(item.AsBaseStorageFolder(), string.IsNullOrEmpty(item.Path) ? customPath : item.Path);
            }
            return null;
        }

        public static FilesystemResult<T> ToType<T, V>(FilesystemResult<V> result) where T : class
        {
            return new FilesystemResult<T>(result.Result as T, result.ErrorCode);
        }
    }
}