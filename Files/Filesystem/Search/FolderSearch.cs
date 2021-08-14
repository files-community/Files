﻿using Files.Common;
using Files.Extensions;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Xaml.Media.Imaging;
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.Filesystem.Search
{
    internal class FolderSearch
    {
        private const uint defaultStepSize = 500;

        public string Query { get; set; }
        public string Folder { get; set; }

        public uint MaxItemCount { get; set; } = 0; // 0: no limit
        public uint ThumbnailSize { get; set; } = 24;
        public bool SearchUnindexedItems { get; set; } = false;

        private uint UsedMaxItemCount => MaxItemCount > 0 ? MaxItemCount : uint.MaxValue;

        public async Task<ObservableCollection<ListedItem>> SearchAsync()
        {
            var results = new ObservableCollection<ListedItem>();

            if (App.LibraryManager.TryGetLibrary(Folder, out var library))
            {
                await AddItemsAsync(library, results);
            }
            else
            {
                await AddItemsAsync(Folder, results);
            }

            return results;
        }

        private async Task<IList<ListedItem>> SearchAsync(StorageFolder folder)
        {
            uint index = 0;
            var results = new List<ListedItem>();
            var stepSize = Math.Min(defaultStepSize, UsedMaxItemCount);
            var options = ToQueryOptions();

            var queryResult = folder.CreateItemQueryWithOptions(options);
            var items = await queryResult.GetItemsAsync(0, stepSize);

            while (items.Count > 0)
            {
                foreach (IStorageItem item in items)
                {
                    try
                    {
                        results.Add(await GetListedItemAsync(item));
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Warn(ex, "Error creating ListedItem from StorageItem");
                    }
                }

                index += (uint)items.Count;
                stepSize = Math.Min(defaultStepSize, UsedMaxItemCount - (uint)results.Count);
                items = await queryResult.GetItemsAsync(index, stepSize);
            }
            return results;
        }

        private async Task AddItemsAsync(LibraryLocationItem library, ObservableCollection<ListedItem> results)
        {
            foreach (var folder in library.Folders)
            {
                await AddItemsAsync(folder, results);
            }
        }

        private async Task SearchTagsAsync(string folder, ObservableCollection<ListedItem> results)
        {
            var tagName = Query.Substring("tag:".Length);
            var tags = App.AppSettings.FileTagsSettings.GetTagsByName(tagName);
            if (!tags.Any())
            {
                return;
            }
            var matches = FileTagsHelper.DbInstance.GetAllUnderPath(folder).Where(x => tags.Any(t => x.Tag == t.Uid));
            foreach (var match in matches)
            {
                (IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
                {
                    int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
                    IntPtr hFileTsk = FindFirstFileExFromApp(match.FilePath, FINDEX_INFO_LEVELS.FindExInfoBasic,
                        out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
                    return (hFileTsk, findDataTsk);
                }).WithTimeoutAsync(TimeSpan.FromSeconds(5));

                if (hFile != IntPtr.Zero)
                {
                    var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
                    var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                    bool shouldBeListed = !isHidden || (App.AppSettings.AreHiddenItemsVisible && (!isSystem || !App.AppSettings.AreSystemItemsHidden));

                    if (shouldBeListed)
                    {
                        var item = GetListedItemAsync(match.FilePath, findData);
                        if (item != null)
                        {
                            results.Add(item);
                        }
                    }

                    FindClose(hFile);
                }
                else
                {
                    try
                    {
                        IStorageItem item = (StorageFile)await GetStorageFileAsync(match.FilePath);
                        item ??= (StorageFolder)await GetStorageFolderAsync(match.FilePath);
                        results.Add(await GetListedItemAsync(item));
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Warn(ex, "Error creating ListedItem from StorageItem");
                    }
                }
            }
        }

        private async Task AddItemsAsync(string folder, ObservableCollection<ListedItem> results)
        {
            if (Query.StartsWith("tag:"))
            {
                await SearchTagsAsync(folder, results);
            }
            else
            {
                var workingFolder = await GetStorageFolderAsync(folder);

                var hiddenOnlyFromWin32 = false;
                if (workingFolder)
                {
                    foreach (var item in await SearchAsync(workingFolder))
                    {
                        results.Add(item);
                    }
                    hiddenOnlyFromWin32 = true;
                }

                if (!hiddenOnlyFromWin32 || App.AppSettings.AreHiddenItemsVisible)
                {
                    foreach (var item in await SearchWithWin32Async(folder, hiddenOnlyFromWin32, UsedMaxItemCount - (uint)results.Count))
                    {
                        results.Add(item);
                    }
                }
            }
        }

        private async Task<IList<ListedItem>> SearchWithWin32Async(string folder, bool hiddenOnly, uint maxItemCount)
        {
            var results = new List<ListedItem>();
            (IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
            {
                int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
                IntPtr hFileTsk = FindFirstFileExFromApp($"{folder}\\*{Query}*.*", FINDEX_INFO_LEVELS.FindExInfoBasic,
                    out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
                return (hFileTsk, findDataTsk);
            }).WithTimeoutAsync(TimeSpan.FromSeconds(5));

            if (hFile != IntPtr.Zero)
            {
                await Task.Run(() =>
                {
                    var hasNextFile = false;
                    do
                    {
                        if (results.Count >= maxItemCount)
                        {
                            break;
                        }
                        var itemPath = Path.Combine(folder, findData.cFileName);

                        var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
                        var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        bool shouldBeListed = hiddenOnly ?
                            isHidden && (!isSystem || !App.AppSettings.AreSystemItemsHidden) :
                            !isHidden || (App.AppSettings.AreHiddenItemsVisible && (!isSystem || !App.AppSettings.AreSystemItemsHidden));

                        if (shouldBeListed)
                        {
                            var item = GetListedItemAsync(itemPath, findData);
                            if (item != null)
                            {
                                results.Add(item);
                            }
                        }

                        hasNextFile = FindNextFile(hFile, out findData);
                    } while (hasNextFile);

                    FindClose(hFile);
                });
            }
            return results;
        }

        private ListedItem GetListedItemAsync(string itemPath, WIN32_FIND_DATA findData)
        {
            ListedItem listedItem = null;
            var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
            {
                string itemFileExtension = null;
                string itemType = null;
                if (findData.cFileName.Contains("."))
                {
                    itemFileExtension = Path.GetExtension(itemPath);
                    itemType = itemFileExtension.Trim('.') + " " + itemType;
                }

                listedItem = new ListedItem(null)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    ItemName = findData.cFileName,
                    ItemPath = itemPath,
                    IsHiddenItem = isHidden,
                    LoadFileIcon = false,
                    LoadUnknownTypeGlyph = true,
                    LoadFolderGlyph = false,
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
                        ItemName = findData.cFileName,
                        ItemPath = itemPath,
                        IsHiddenItem = isHidden,
                        LoadFileIcon = false,
                        LoadUnknownTypeGlyph = false,
                        LoadFolderGlyph = true,
                        Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
                    };
                }
            }
            if (listedItem != null && MaxItemCount > 0) // Only load icon for searchbox suggestions
            {
                _ = FileThumbnailHelper.LoadIconWithoutOverlayAsync(listedItem.ItemPath, ThumbnailSize)
                    .ContinueWith((t) =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            _ = FilesystemTasks.Wrap(() => CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                listedItem.FileImage = await t.Result.ToBitmapAsync();
                                listedItem.CustomIconData = t.Result;
                                listedItem.LoadFolderGlyph = false;
                                listedItem.LoadUnknownTypeGlyph = false;
                                listedItem.LoadWebShortcutGlyph = false;
                                listedItem.LoadFileIcon = true;
                            }, Windows.System.DispatcherQueuePriority.Low));
                        }
                    });
            }
            return listedItem;
        }

        private async Task<ListedItem> GetListedItemAsync(IStorageItem item)
        {
            ListedItem listedItem = null;
            if (item.IsOfType(StorageItemTypes.Folder))
            {
                var folder = (StorageFolder)item;
                listedItem = new ListedItem(null)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = folder.DisplayName,
                    ItemPath = folder.Path,
                    LoadFolderGlyph = true,
                    LoadUnknownTypeGlyph = false,
                    Opacity = 1
                };
            }
            else if (item.IsOfType(StorageItemTypes.File))
            {
                var file = (StorageFile)item;
                string itemFileExtension = null;
                string itemType = null;
                if (file.Name.Contains("."))
                {
                    itemFileExtension = Path.GetExtension(file.Path);
                    itemType = itemFileExtension.Trim('.') + " " + itemType;
                }

                listedItem = new ListedItem(null)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    ItemName = file.DisplayName,
                    ItemPath = file.Path,
                    LoadFileIcon = false,
                    LoadUnknownTypeGlyph = true,
                    LoadFolderGlyph = false,
                    FileExtension = itemFileExtension,
                    ItemType = itemType,
                    Opacity = 1
                };
            }
            if (listedItem != null && MaxItemCount > 0) // Only load icon for searchbox suggestions
            {
                var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(item, ThumbnailSize, ThumbnailMode.ListView);
                if (iconData != null)
                {
                    listedItem.CustomIconData = iconData;
                    listedItem.FileImage = await listedItem.CustomIconData.ToBitmapAsync();
                    listedItem.LoadUnknownTypeGlyph = false;
                    listedItem.LoadWebShortcutGlyph = false;
                    listedItem.LoadFolderGlyph = false;
                    listedItem.LoadFileIcon = true;
                }
            }
            return listedItem;
        }

        private QueryOptions ToQueryOptions()
        {
            var query = new QueryOptions
            {
                FolderDepth = FolderDepth.Deep,
                UserSearchFilter = Query ?? string.Empty,
            };

            query.IndexerOption = SearchUnindexedItems
                ? IndexerOption.DoNotUseIndexer
                : IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;

            query.SortOrder.Clear();
            query.SortOrder.Add(new SortEntry { PropertyName = "System.Search.Rank", AscendingOrder = false });

            query.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
            query.SetThumbnailPrefetch(ThumbnailMode.ListView, 24, ThumbnailOptions.UseCurrentScale);

            return query;
        }

        private static async Task<FilesystemResult<StorageFolder>> GetStorageFolderAsync(string path)
            => await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

        private static async Task<FilesystemResult<StorageFile>> GetStorageFileAsync(string path)
            => await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
    }
}