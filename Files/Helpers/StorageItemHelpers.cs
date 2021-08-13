using Files.Common;
using Files.Enums;
using Files.Filesystem;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;

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

        public static async Task<TOut> ToStorageItem<TOut>(string path, IShellPage associatedInstance = null) where TOut : IStorageItem
        {
            FilesystemResult<StorageFile> file = null;
            FilesystemResult<StorageFolder> folder = null;

            if (typeof(IStorageFile).IsAssignableFrom(typeof(TOut)))
            {
                await GetFile();
            }
            else if (typeof(IStorageFolder).IsAssignableFrom(typeof(TOut)))
            {
                await GetFolder();
            }
            else if (typeof(IStorageItem).IsAssignableFrom(typeof(TOut)))
            {
                if (Path.HasExtension(path)) // Probably a file
                {
                    await GetFile();
                }
                else // Possibly a folder
                {
                    await GetFolder();

                    if (!folder)
                    {
                        // It wasn't a folder, so check file then because it wasn't checked
                        await GetFile();
                    }
                }
            }

            if (file != null && file == FileSystemStatusCode.Unauthorized)
            {
                var fileName = new System.Text.RegularExpressions.Regex(@"\.(lnk|url)$").Replace(Path.GetFileName(path), ".sht");
                file = await FilesystemTasks.Wrap(() => StorageFile.CreateStreamedFileAsync(fileName, new StreamedFileDataRequestedHandler(async (request) =>
                {
                    try
                    {
                        var connection = await AppServiceConnectionHelper.Instance;
                        if (connection != null)
                        {
                            var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                            {
                                { "Arguments", "FileOperation" },
                                { "fileop", "GetFileHandle" },
                                { "filepath", path },
                                { "processid", System.Diagnostics.Process.GetCurrentProcess().Id },
                            });
                            if (status == AppServiceResponseStatus.Success && response.Get("Success", false))
                            {
                                using (var hFile = new SafeFileHandle(new IntPtr((long)response["Handle"]), true))
                                using (var inStream = new FileStream(hFile, FileAccess.Read))
                                using (var outStream = request.AsStreamForWrite())
                                {
                                    await inStream.CopyToAsync(outStream);
                                }
                                request.Dispose();
                                return;
                            }
                        }
                        request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                    }
                    catch (Exception ex)
                    {
                        request.FailAndClose(StreamedFileFailureMode.Failed);
                        App.Logger.Warn(ex, "Error converting link to StorageFile.");
                    }
                }), null).AsTask());
            }
            else if (folder != null && folder == FileSystemStatusCode.Unauthorized)
            {
                // TODO
            }

            if (file != null && file)
            {
                return (TOut)(IStorageItem)file.Result;
            }
            else if (folder != null && folder)
            {
                return (TOut)(IStorageItem)folder.Result;
            }

            return default;

            // Extensions

            async Task GetFile()
            {
                if (associatedInstance == null)
                {
                    file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
                }
                else
                {
                    file = await associatedInstance?.FilesystemViewModel?.GetFileFromPathAsync(path);
                }
            }

            async Task GetFolder()
            {
                if (associatedInstance == null)
                {
                    folder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
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