using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Files.Backend.Item.Tools.NativeFindStorageItemHelper;

namespace Files.Backend.Item
{
    internal class SearchItemProvider : IFileItemProvider
    {
        private readonly IDriveManager driveManager = Ioc.Default.GetRequiredService<IDriveManager>();

        public string Query { get; init; } = string.Empty;
        public string Folder { get; init; } = string.Empty;

        public uint ThumbnailSize { get; init; } = 24;

        public bool IncludeHiddenItems { get; init; } = false;
        public bool IncludeSystemItems { get; init; } = false;
        public bool IncludeUnindexedItems { get; init; } = false;

        public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

        IAsyncEnumerable<IItem> IItemProvider.ProvideItems() => ProvideItems();
        public async IAsyncEnumerable<IFileItem> ProvideItems()
        {
            var items = ProvideItemsInLocalDrives();
            await foreach (var item in items)
            {
                yield return item;
            }
        }

        private async IAsyncEnumerable<IFileItem> ProvideItemsInLocalDrives()
        {
            var localDrives = driveManager.Drives.Where(drive => !drive.DriveType.HasFlag(DriveTypes.Network));
            foreach (var localDrive in localDrives)
            {
                var localItems = ProvideItemsInPath(localDrive.Path);
                await foreach (var localItem in localItems)
                {
                    yield return localItem;
                }
            }

        }
        private async IAsyncEnumerable<IFileItem> ProvideItemsInPath(string path)
        {
            await Task.Yield();
            yield return null;
        }


        /*IAsyncEnumerable<IItem> IItemProvider.ProvideItems() => ProvideItems();
        public IAsyncEnumerable<IFileItem> ProvideItems()
        {
            var workingFolder = await GetStorageFolderAsync(folder);

            var hiddenOnlyFromWin32 = false;
            if (workingFolder)
            {
                await SearchAsync(workingFolder, results, token);
                hiddenOnlyFromWin32 = (results.Count != 0);
            }

            if (!IsAQSQuery && (!hiddenOnlyFromWin32 || UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible))
            {
                await SearchWithWin32Async(folder, hiddenOnlyFromWin32, UsedMaxItemCount - (uint)results.Count, results, token);
            }
        }

        private static async Task<FilesystemResult<BaseStorageFolder>> GetStorageFolderAsync(string path)
            => await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

        private static async Task<FilesystemResult<BaseStorageFile>> GetStorageFileAsync(string path)
            => await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
        */


        /*private async IAsyncEnumerable<IFileItem> SearchWithWin32Async(string folderPath)
        {
            (IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
            {
                IntPtr hFileTsk = FindFirstFileExFromApp(@$"{folderPath}\{Query}", FINDEX_INFO_LEVELS.FindExInfoBasic,
                    out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FIND_FIRST_EX_LARGE_FETCH);
                return (hFileTsk, findDataTsk);
            }).WithTimeoutAsync(TimeSpan.FromSeconds(5));

            if (hFile != IntPtr.Zero && hFile.ToInt64() != -1)
            {
                await Task.Run(() =>
                {
                    var hasNextFile = false;
                    do
                    {
                        var itemPath = Path.Combine(folderPath, findData.cFileName);

                        var isSystemInclude = IncludeSystemItems || ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
                        var isHiddenInclude = IncludeHiddenItems || ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;

                        if (isSystemInclude && isHiddenInclude)
                        {
                            var item = GetListedItemAsync(itemPath, findData);
                            if (item != null)
                            {
                                yield return item;
                            }
                        }

                        if (CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        hasNextFile = FindNextFile(hFile, out findData);
                    } while (hasNextFile);

                    FindClose(hFile);
                });
            }
        }

        private IFileItem GetListedItemAsync(string itemPath, WIN32_FIND_DATA findData)
        {
            ListedItem listedItem = null;
            var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
            {
                string itemFileExtension = null;
                string itemType = null;
                if (findData.cFileName.Contains(".", StringComparison.Ordinal))
                {
                    itemFileExtension = Path.GetExtension(itemPath);
                    itemType = itemFileExtension.Trim('.') + " " + itemType;
                }

                listedItem = new ListedItem(null)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    ItemNameRaw = findData.cFileName,
                    ItemPath = itemPath,
                    IsHiddenItem = isHidden,
                    LoadFileIcon = false,
                    FileExtension = itemFileExtension,
                    ItemType = itemType,
                    Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
                };
            }
            else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                if (findData.cFileName != "." && findData.cFileName != "..")
                {
                    listedItem = new ListedItem(null)
                    {
                        PrimaryItemAttribute = StorageItemTypes.Folder,
                        ItemNameRaw = findData.cFileName,
                        ItemPath = itemPath,
                        IsHiddenItem = isHidden,
                        LoadFileIcon = false,
                        Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
                    };
                }
            }
            if (listedItem != null && MaxItemCount > 0) // Only load icon for searchbox suggestions
            {
                _ = FileThumbnailHelper.LoadIconFromPathAsync(listedItem.ItemPath, ThumbnailSize, ThumbnailMode.ListView)
                    .ContinueWith((t) =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            _ = FilesystemTasks.Wrap(() => CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                listedItem.FileImage = await t.Result.ToBitmapAsync();
                            }, Windows.System.DispatcherQueuePriority.Low));
                        }
                    });
            }
            return listedItem;
        }*/
    }
}
