using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Files.Uwp.Extensions;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using static Files.Backend.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.Uwp.Filesystem.Search
{
    public class FolderSearch
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>();

        private const uint defaultStepSize = 500;

        public string Query { get; set; }
        public string Folder { get; set; }

        public uint MaxItemCount { get; set; } = 0; // 0: no limit
        public uint ThumbnailSize { get; set; } = 24;
        public bool SearchUnindexedItems { get; set; } = false;

        private uint UsedMaxItemCount => MaxItemCount > 0 ? MaxItemCount : uint.MaxValue;

        public EventHandler SearchTick;

        private bool IsAQSQuery => Query is not null && (Query.StartsWith('$') || Query.Contains(":", StringComparison.Ordinal));

        private string QueryWithWildcard
        {
            get
            {
                if (!string.IsNullOrEmpty(Query) && Query.Contains('.')) // ".docx" -> "*.docx"
                {
                    var split = Query.Split('.');
                    var leading = string.Join('.', split.SkipLast(1));
                    var query = $"{leading}*.{split.Last()}";
                    return $"{query}*";
                }
                return $"{Query}*";
            }
        }

        public string AQSQuery
        {
            get
            {
                // if the query starts with a $, assume the query is in aqs format, otherwise assume the user is searching for the file name
                if (Query is not null && Query.StartsWith('$'))
                {
                    return Query.Substring(1);
                }
                else if (Query is not null && Query.Contains(":", StringComparison.Ordinal))
                {
                    return Query;
                }
                else
                {
                    return $"System.FileName:\"{QueryWithWildcard}\"";
                }
            }
        }

        public async Task SearchAsync(IList<ListedItem> results, CancellationToken token)
        {
            try
            {
                if (App.LibraryManager.TryGetLibrary(Folder, out var library))
                {
                    await AddItemsAsyncForLibrary(library, results, token);
                }
                else if (Folder == "Home".GetLocalized())
                {
                    await AddItemsAsyncForHome(results, token);
                }
                else
                {
                    await AddItemsAsync(Folder, results, token);
                }
            }
            catch (Exception e)
            {
                App.Logger.Warn(e, "Search failure");
            }
        }

        private async Task AddItemsAsyncForHome(IList<ListedItem> results, CancellationToken token)
        {
            foreach (var drive in App.DrivesManager.Drives.Where(x => !x.IsNetwork))
            {
                await AddItemsAsync(drive.Path, results, token);
            }
        }

        public async Task<ObservableCollection<ListedItem>> SearchAsync()
        {
            ObservableCollection<ListedItem> results = new ObservableCollection<ListedItem>();
            try
            {
                var token = CancellationToken.None;
                if (App.LibraryManager.TryGetLibrary(Folder, out var library))
                {
                    await AddItemsAsyncForLibrary(library, results, token);
                }
                else if (Folder == "Home".GetLocalized())
                {
                    await AddItemsAsyncForHome(results, token);
                }
                else
                {
                    await AddItemsAsync(Folder, results, token);
                }
            }
            catch (Exception e)
            {
                App.Logger.Warn(e, "Search failure");
            }

            return results;
        }

        private async Task SearchAsync(BaseStorageFolder folder, IList<ListedItem> results, CancellationToken token)
        {
            //var sampler = new IntervalSampler(500);
            uint index = 0;
            var stepSize = Math.Min(defaultStepSize, UsedMaxItemCount);
            var options = ToQueryOptions();

            var queryResult = folder.CreateItemQueryWithOptions(options);
            var items = await queryResult.GetItemsAsync(0, stepSize);

            while (items.Count > 0)
            {
                foreach (IStorageItem item in items)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        var startWithDot = item.Name.StartsWith(".");
                        bool shouldBeListed = !startWithDot || UserSettingsService.PreferencesSettingsService.ShowDotFiles;
                        if (shouldBeListed)
                        {
                            results.Add(await GetListedItemAsync(item));
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Warn(ex, "Error creating ListedItem from StorageItem");
                    }

                    if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
                    {
                        SearchTick?.Invoke(this, EventArgs.Empty);
                    }
                }

                index += (uint)items.Count;
                stepSize = Math.Min(defaultStepSize, UsedMaxItemCount - (uint)results.Count);
                items = await queryResult.GetItemsAsync(index, stepSize);
            }
        }

        private async Task AddItemsAsyncForLibrary(LibraryLocationItem library, IList<ListedItem> results, CancellationToken token)
        {
            foreach (var folder in library.Folders)
            {
                await AddItemsAsync(folder, results, token);
            }
        }

        private async Task SearchTagsAsync(string folder, IList<ListedItem> results, CancellationToken token)
        {
            //var sampler = new IntervalSampler(500);
            var tagName = AQSQuery.Substring("tag:".Length);
            var tags = FileTagsSettingsService.SearchTagsByName(tagName);
            if (!tags.Any())
            {
                return;
            }
            var matches = FileTagsHelper.DbInstance.GetAllUnderPath(folder).Where(x => tags.Select(t => t.Uid).Intersect(x.Tags).Any());
            foreach (var match in matches)
            {
                (IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
                {
                    int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
                    IntPtr hFileTsk = FindFirstFileExFromApp(match.FilePath, FINDEX_INFO_LEVELS.FindExInfoBasic,
                        out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
                    return (hFileTsk, findDataTsk);
                }).WithTimeoutAsync(TimeSpan.FromSeconds(5));

                if (hFile != IntPtr.Zero && hFile.ToInt64() != -1)
                {
                    var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
                    var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                    var startWithDot = findData.cFileName.StartsWith(".");
                    
                    bool shouldBeListed = (!isHidden || 
                        (UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible && 
                        (!isSystem || !UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden))) && 
                        (!startWithDot || UserSettingsService.PreferencesSettingsService.ShowDotFiles);
                    
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
                        IStorageItem item = (BaseStorageFile)await GetStorageFileAsync(match.FilePath);
                        item ??= (BaseStorageFolder)await GetStorageFolderAsync(match.FilePath);
                        var startWithDot = item.Name.StartsWith(".");
                        bool shouldBeListed = !startWithDot || UserSettingsService.PreferencesSettingsService.ShowDotFiles;
                        if (shouldBeListed)
                        {
                            results.Add(await GetListedItemAsync(item));
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Logger.Warn(ex, "Error creating ListedItem from StorageItem");
                    }
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
                {
                    SearchTick?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private async Task AddItemsAsync(string folder, IList<ListedItem> results, CancellationToken token)
        {
            if (AQSQuery.StartsWith("tag:", StringComparison.Ordinal))
            {
                await SearchTagsAsync(folder, results, token);
            }
            else
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
        }

        private async Task SearchWithWin32Async(string folder, bool hiddenOnly, uint maxItemCount, IList<ListedItem> results, CancellationToken token)
        {
            //var sampler = new IntervalSampler(500);
            (IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
            {
                int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
                IntPtr hFileTsk = FindFirstFileExFromApp($"{folder}\\{QueryWithWildcard}", FINDEX_INFO_LEVELS.FindExInfoBasic,
                    out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
                return (hFileTsk, findDataTsk);
            }).WithTimeoutAsync(TimeSpan.FromSeconds(5));

            if (hFile != IntPtr.Zero && hFile.ToInt64() != -1)
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
                        var startWithDot = findData.cFileName.StartsWith(".");

                        bool shouldBeListed = (hiddenOnly ?
                            isHidden && (!isSystem || !UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden) :
                            !isHidden || (UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible && (!isSystem || !UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden))) &&
                            (!startWithDot || UserSettingsService.PreferencesSettingsService.ShowDotFiles);
                        
                        if (shouldBeListed)
                        {
                            var item = GetListedItemAsync(itemPath, findData);
                            if (item != null)
                            {
                                results.Add(item);
                            }
                        }

                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
                        {
                            SearchTick?.Invoke(this, EventArgs.Empty);
                        }

                        hasNextFile = FindNextFile(hFile, out findData);
                    } while (hasNextFile);

                    FindClose(hFile);
                });
            }
        }

        private ListedItem GetListedItemAsync(string itemPath, WIN32_FIND_DATA findData)
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
        }

        private async Task<ListedItem> GetListedItemAsync(IStorageItem item)
        {
            ListedItem listedItem = null;
            if (item.IsOfType(StorageItemTypes.Folder))
            {
                var folder = item.AsBaseStorageFolder();
                var props = await folder.GetBasicPropertiesAsync();
                if (folder is BinStorageFolder binFolder)
                {
                    listedItem = new RecycleBinItem(null)
                    {
                        PrimaryItemAttribute = StorageItemTypes.Folder,
                        ItemNameRaw = folder.DisplayName,
                        ItemPath = folder.Path,
                        ItemDateModifiedReal = props.DateModified,
                        ItemDateCreatedReal = folder.DateCreated,
                        NeedsPlaceholderGlyph = false,
                        Opacity = 1,
                        ItemDateDeletedReal = binFolder.DateDeleted,
                        ItemOriginalPath = binFolder.OriginalPath
                    };
                }
                else
                {
                    listedItem = new ListedItem(null)
                    {
                        PrimaryItemAttribute = StorageItemTypes.Folder,
                        ItemNameRaw = folder.DisplayName,
                        ItemPath = folder.Path,
                        ItemDateModifiedReal = props.DateModified,
                        ItemDateCreatedReal = folder.DateCreated,
                        NeedsPlaceholderGlyph = false,
                        Opacity = 1
                    };
                }
            }
            else if (item.IsOfType(StorageItemTypes.File))
            {
                var file = item.AsBaseStorageFile();
                var props = await file.GetBasicPropertiesAsync();
                string itemFileExtension = null;
                string itemType = null;
                if (file.Name.Contains(".", StringComparison.Ordinal))
                {
                    itemFileExtension = Path.GetExtension(file.Path);
                    itemType = itemFileExtension.Trim('.') + " " + itemType;
                }

                var itemSize = props.Size.ToSizeString();

                if (file is BinStorageFile binFile)
                {
                    listedItem = new RecycleBinItem(null)
                    {
                        PrimaryItemAttribute = StorageItemTypes.File,
                        ItemNameRaw = file.Name,
                        ItemPath = file.Path,
                        LoadFileIcon = false,
                        FileExtension = itemFileExtension,
                        FileSizeBytes = (long)props.Size,
                        FileSize = itemSize,
                        ItemDateModifiedReal = props.DateModified,
                        ItemDateCreatedReal = file.DateCreated,
                        ItemType = itemType,
                        NeedsPlaceholderGlyph = false,
                        Opacity = 1,
                        ItemDateDeletedReal = binFile.DateDeleted,
                        ItemOriginalPath = binFile.OriginalPath
                    };
                }
                else
                {
                    listedItem = new ListedItem(null)
                    {
                        PrimaryItemAttribute = StorageItemTypes.File,
                        ItemNameRaw = file.Name,
                        ItemPath = file.Path,
                        LoadFileIcon = false,
                        FileExtension = itemFileExtension,
                        FileSizeBytes = (long)props.Size,
                        FileSize = itemSize,
                        ItemDateModifiedReal = props.DateModified,
                        ItemDateCreatedReal = file.DateCreated,
                        ItemType = itemType,
                        NeedsPlaceholderGlyph = false,
                        Opacity = 1
                    };
                }
            }
            if (listedItem != null && MaxItemCount > 0) // Only load icon for searchbox suggestions
            {
                var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(item, ThumbnailSize, ThumbnailMode.ListView);
                if (iconData != null)
                {
                    listedItem.FileImage = await iconData.ToBitmapAsync();
                }
                else
                {
                    listedItem.NeedsPlaceholderGlyph = true;
                }
            }
            return listedItem;
        }

        private QueryOptions ToQueryOptions()
        {
            var query = new QueryOptions
            {
                FolderDepth = FolderDepth.Deep,
                UserSearchFilter = AQSQuery ?? string.Empty,
            };

            query.IndexerOption = SearchUnindexedItems
                ? IndexerOption.DoNotUseIndexer
                : IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;

            query.SortOrder.Clear();
            query.SortOrder.Add(new SortEntry { PropertyName = "System.Search.Rank", AscendingOrder = false });

            query.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, null);
            query.SetThumbnailPrefetch(ThumbnailMode.ListView, 24, ThumbnailOptions.UseCurrentScale);

            return query;
        }

        private static async Task<FilesystemResult<BaseStorageFolder>> GetStorageFolderAsync(string path)
            => await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

        private static async Task<FilesystemResult<BaseStorageFile>> GetStorageFileAsync(string path)
            => await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
    }
}
