using Files.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
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
        private async Task AddItemsAsync(string folder, ObservableCollection<ListedItem> results)
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
                        if (((FileAttributes)findData.dwFileAttributes & FileAttributes.System) != FileAttributes.System
                            || !App.AppSettings.AreSystemItemsHidden)
                        {
                            var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                            if ((!isHidden && !hiddenOnly) || (isHidden && App.AppSettings.AreHiddenItemsVisible))
                            {
                                if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                                {
                                    string itemFileExtension = null;
                                    string itemType = null;
                                    if (findData.cFileName.Contains("."))
                                    {
                                        itemFileExtension = Path.GetExtension(itemPath);
                                        itemType = itemFileExtension.Trim('.') + " " + itemType;
                                    }

                                    results.Add(new ListedItem(null)
                                    {
                                        PrimaryItemAttribute = StorageItemTypes.File,
                                        ItemName = findData.cFileName,
                                        ItemPath = itemPath,
                                        IsHiddenItem = true,
                                        LoadFileIcon = false,
                                        LoadUnknownTypeGlyph = true,
                                        LoadFolderGlyph = false,
                                        ItemPropertiesInitialized = false, // Load thumbnail
                                        FileExtension = itemFileExtension,
                                        ItemType = itemType,
                                        Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
                                    });
                                }
                                else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                                {
                                    if (findData.cFileName != "." && findData.cFileName != "..")
                                    {
                                        results.Add(new ListedItem(null)
                                        {
                                            PrimaryItemAttribute = StorageItemTypes.Folder,
                                            ItemName = findData.cFileName,
                                            ItemPath = itemPath,
                                            IsHiddenItem = true,
                                            LoadFileIcon = false,
                                            LoadUnknownTypeGlyph = false,
                                            LoadFolderGlyph = true,
                                            ItemPropertiesInitialized = true,
                                            Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
                                        });
                                    }
                                }
                            }
                        }

                        hasNextFile = FindNextFile(hFile, out findData);
                    } while (hasNextFile);

                    FindClose(hFile);
                });
            }
            return results;
        }

        private async Task<ListedItem> GetListedItemAsync(IStorageItem item)
        {
            if (item.IsOfType(StorageItemTypes.Folder))
            {
                var folder = (StorageFolder)item;
                return new ListedItem(null)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = folder.DisplayName,
                    ItemPath = folder.Path,
                    LoadFolderGlyph = true,
                    LoadUnknownTypeGlyph = false,
                    ItemPropertiesInitialized = true,
                    Opacity = 1
                };
            }
            else if (item.IsOfType(StorageItemTypes.File))
            {
                var file = (StorageFile)item;
                var bitmapIcon = new BitmapImage();
                var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.ListView, ThumbnailSize, ThumbnailOptions.UseCurrentScale);

                string itemFileExtension = null;
                string itemType = null;
                if (file.Name.Contains("."))
                {
                    itemFileExtension = Path.GetExtension(file.Path);
                    itemType = itemFileExtension.Trim('.') + " " + itemType;
                }

                if (thumbnail != null)
                {
                    await bitmapIcon.SetSourceAsync(thumbnail);
                    return new ListedItem(null)
                    {
                        PrimaryItemAttribute = StorageItemTypes.File,
                        ItemName = file.DisplayName,
                        ItemPath = file.Path,
                        LoadFileIcon = true,
                        FileImage = bitmapIcon,
                        LoadUnknownTypeGlyph = false,
                        LoadFolderGlyph = false,
                        ItemPropertiesInitialized = true,
                        FileExtension = itemFileExtension,
                        ItemType = itemType,
                        Opacity = 1
                    };
                }
                else
                {
                    return new ListedItem(null)
                    {
                        PrimaryItemAttribute = StorageItemTypes.File,
                        ItemName = file.DisplayName,
                        ItemPath = file.Path,
                        LoadFileIcon = false,
                        LoadUnknownTypeGlyph = true,
                        LoadFolderGlyph = false,
                        ItemPropertiesInitialized = true,
                        Opacity = 1
                    };
                }
            }
            return null;
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
    }
}