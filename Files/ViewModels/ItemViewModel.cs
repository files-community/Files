using Files.Common;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.Cloud;
using Files.Filesystem.StorageEnumerators;
using Files.Helpers;
using Files.Helpers.FileListCache;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using static Files.Helpers.NativeDirectoryChangesHelper;
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.ViewModels
{
    public class ItemViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly SemaphoreSlim enumFolderSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim loadExtendedPropsSemaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        private readonly ConcurrentQueue<(uint Action, string FileName)> operationQueue = new ConcurrentQueue<(uint Action, string FileName)>();
        private readonly SemaphoreSlim operationSemaphore = new SemaphoreSlim(1, 1);
        private IntPtr hWatchDir;
        private IAsyncAction aWatcherAction;

        // files and folders list for manipulating
        private List<ListedItem> filesAndFolders;

        // only used for Binding and ApplyFilesAndFoldersChangesAsync, don't manipulate on this!
        public BulkConcurrentObservableCollection<ListedItem> FilesAndFolders { get; }

        public SettingsViewModel AppSettings => App.AppSettings;
        public FolderSettingsViewModel folderSettings = null;
        private bool shouldDisplayFileExtensions = false;
        public ListedItem CurrentFolder { get; private set; }
        public CollectionViewSource viewSource;
        private CancellationTokenSource addFilesCTS, semaphoreCTS, loadPropsCTS;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler DirectoryInfoUpdated;

        private string customPath;

        private IFileListCache fileListCache = FileListCacheController.GetInstance();

        private NamedPipeAsAppServiceConnection Connection;

        public string WorkingDirectory
        {
            get
            {
                return currentStorageFolder?.Path ?? customPath;
            }
        }

        private StorageFolderWithPath currentStorageFolder;
        private StorageFolderWithPath workingRoot;

        public delegate void WorkingDirectoryModifiedEventHandler(object sender, WorkingDirectoryModifiedEventArgs e);

        public event WorkingDirectoryModifiedEventHandler WorkingDirectoryModified;

        public delegate void PageTypeUpdatedEventHandler(object sender, PageTypeUpdatedEventArgs e);

        public event PageTypeUpdatedEventHandler PageTypeUpdated;

        public delegate void ItemLoadStatusChangedEventHandler(object sender, ItemLoadStatusChangedEventArgs e);

        public event ItemLoadStatusChangedEventHandler ItemLoadStatusChanged;

        public async Task<FilesystemResult> SetWorkingDirectoryAsync(string value)
        {
            var navigated = (FilesystemResult)true;
            if (string.IsNullOrWhiteSpace(value))
            {
                return new FilesystemResult(FileSystemStatusCode.NotAFolder);
            }

            bool isLibrary = false;
            string name = null;
            if (App.LibraryManager.TryGetLibrary(value, out LibraryLocationItem library))
            {
                isLibrary = true;
                name = library.Text;
            }

            WorkingDirectoryModified?.Invoke(this, new WorkingDirectoryModifiedEventArgs { Path = value, IsLibrary = isLibrary, Name = name });

            if (isLibrary || !Path.IsPathRooted(value))
            {
                workingRoot = null;
                currentStorageFolder = null;
                customPath = value;
            }
            else if (!Path.IsPathRooted(WorkingDirectory) || Path.GetPathRoot(WorkingDirectory) != Path.GetPathRoot(value))
            {
                workingRoot = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(value));
            }

            if (!isLibrary && Path.IsPathRooted(value))
            {
                var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(value, workingRoot, currentStorageFolder));
                if (res)
                {
                    currentStorageFolder = res.Result;
                    customPath = null;
                }
                else
                {
                    currentStorageFolder = null;
                    customPath = value;
                }
                navigated = res;
            }

            if (value == "Home" || value == "NewTab".GetLocalized())
            {
                currentStorageFolder = null;
            }
            else
            {
                App.JumpList.AddFolderToJumpList(value);
            }

            NotifyPropertyChanged(nameof(WorkingDirectory));
            return navigated;
        }

        public async Task<FilesystemResult<StorageFolder>> GetFolderFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(value, workingRoot, currentStorageFolder));
        }

        public async Task<FilesystemResult<StorageFile>> GetFileFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(value, workingRoot, currentStorageFolder));
        }

        public async Task<FilesystemResult<StorageFolderWithPath>> GetFolderWithPathFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(value, workingRoot, currentStorageFolder));
        }

        public async Task<FilesystemResult<StorageFileWithPath>> GetFileWithPathFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(value, workingRoot, currentStorageFolder));
        }

        private bool isFolderEmptyTextDisplayed;

        public bool IsFolderEmptyTextDisplayed
        {
            get => isFolderEmptyTextDisplayed;
            set
            {
                if (value != isFolderEmptyTextDisplayed)
                {
                    isFolderEmptyTextDisplayed = value;
                    NotifyPropertyChanged(nameof(IsFolderEmptyTextDisplayed));
                }
            }
        }

        public async void UpdateSortOptionStatus()
        {
            NotifyPropertyChanged(nameof(IsSortedByName));
            NotifyPropertyChanged(nameof(IsSortedByDate));
            NotifyPropertyChanged(nameof(IsSortedByType));
            NotifyPropertyChanged(nameof(IsSortedBySize));
            NotifyPropertyChanged(nameof(IsSortedByOriginalPath));
            NotifyPropertyChanged(nameof(IsSortedByDateDeleted));
            NotifyPropertyChanged(nameof(IsSortedByDateCreated));
            await OrderFilesAndFoldersAsync();
            await ApplyFilesAndFoldersChangesAsync();
        }

        public async void UpdateSortDirectionStatus()
        {
            NotifyPropertyChanged(nameof(IsSortedAscending));
            NotifyPropertyChanged(nameof(IsSortedDescending));
            await OrderFilesAndFoldersAsync();
            await ApplyFilesAndFoldersChangesAsync();
        }

        public bool IsSortedByName
        {
            get => folderSettings.DirectorySortOption == SortOption.Name;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.Name;
                    NotifyPropertyChanged(nameof(IsSortedByName));
                }
            }
        }

        public bool IsSortedByOriginalPath
        {
            get => folderSettings.DirectorySortOption == SortOption.OriginalPath;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.OriginalPath;
                    NotifyPropertyChanged(nameof(IsSortedByOriginalPath));
                }
            }
        }

        public bool IsSortedByDateDeleted
        {
            get => folderSettings.DirectorySortOption == SortOption.DateDeleted;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.DateDeleted;
                    NotifyPropertyChanged(nameof(IsSortedByDateDeleted));
                }
            }
        }

        public bool IsSortedByDate
        {
            get => folderSettings.DirectorySortOption == SortOption.DateModified;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.DateModified;
                    NotifyPropertyChanged(nameof(IsSortedByDate));
                }
            }
        }

        public bool IsSortedByDateCreated
        {
            get => folderSettings.DirectorySortOption == SortOption.DateCreated;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.DateCreated;
                    NotifyPropertyChanged(nameof(IsSortedByDateCreated));
                }
            }
        }

        public bool IsSortedByType
        {
            get => folderSettings.DirectorySortOption == SortOption.FileType;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.FileType;
                    NotifyPropertyChanged(nameof(IsSortedByType));
                }
            }
        }

        public bool IsSortedBySize
        {
            get => folderSettings.DirectorySortOption == SortOption.Size;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.Size;
                    NotifyPropertyChanged(nameof(IsSortedBySize));
                }
            }
        }

        public bool IsSortedAscending
        {
            get => folderSettings.DirectorySortDirection == SortDirection.Ascending;
            set
            {
                folderSettings.DirectorySortDirection = value ? SortDirection.Ascending : SortDirection.Descending;
                NotifyPropertyChanged(nameof(IsSortedAscending));
                NotifyPropertyChanged(nameof(IsSortedDescending));
            }
        }

        public bool IsSortedDescending
        {
            get => !IsSortedAscending;
            set
            {
                folderSettings.DirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
                NotifyPropertyChanged(nameof(IsSortedAscending));
                NotifyPropertyChanged(nameof(IsSortedDescending));
            }
        }

        public ItemViewModel(FolderSettingsViewModel folderSettingsViewModel)
        {
            folderSettings = folderSettingsViewModel;
            filesAndFolders = new List<ListedItem>();
            FilesAndFolders = new BulkConcurrentObservableCollection<ListedItem>();
            addFilesCTS = new CancellationTokenSource();
            semaphoreCTS = new CancellationTokenSource();
            loadPropsCTS = new CancellationTokenSource();
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;
        }

        public void OnAppServiceConnectionChanged(NamedPipeAsAppServiceConnection connection)
        {
            if (Connection != null)
            {
                Connection.RequestReceived -= Connection_RequestReceived;
            }
            Connection = connection;
            if (Connection != null)
            {
                Connection.RequestReceived += Connection_RequestReceived;
            }
        }

        private async void Connection_RequestReceived(object sender, Dictionary<string, object> message)
        {
            // The fulltrust process signaled that something in the recycle bin folder has changed
            if (message.ContainsKey("FileSystem"))
            {
                var folderPath = (string)message["FileSystem"];
                var itemPath = (string)message["Path"];
                var changeType = (string)message["Type"];
                var newItem = JsonConvert.DeserializeObject<ShellFileItem>(message.Get("Item", ""));
                Debug.WriteLine("{0}: {1}", folderPath, changeType);
                // If we are currently displaying the reycle bin lets refresh the items
                if (CurrentFolder?.ItemPath == folderPath)
                {
                    switch (changeType)
                    {
                        case "Created":
                            var newListedItem = AddFileOrFolderFromShellFile(newItem);
                            if (newListedItem != null)
                            {
                                await AddFileOrFolderAsync(newListedItem);
                                await OrderFilesAndFoldersAsync();
                                await ApplySingleFileChangeAsync(newListedItem);
                            }
                            break;

                        case "Deleted":
                            var removedItem = await RemoveFileOrFolderAsync(itemPath);
                            if (removedItem != null)
                            {
                                await ApplySingleFileChangeAsync(removedItem);
                            }
                            break;

                        default:
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                            {
                                RefreshItems(null);
                            });
                            break;
                    }
                }
            }
            // The fulltrust process signaled that a drive has been connected/disconnected
            else if (message.ContainsKey("DeviceID"))
            {
                var deviceId = (string)message["DeviceID"];
                var eventType = (DeviceEvent)(long)message["EventType"];
                await App.DrivesManager.HandleWin32DriveEvent(eventType, deviceId);
            }
            else if (message.ContainsKey("Library"))
            {
                await App.LibraryManager.HandleWin32LibraryEvent(JsonConvert.DeserializeObject<ShellLibraryItem>(message.Get("Item", "")), message.Get("OldPath", ""));
            }
        }

        public void CancelLoadAndClearFiles()
        {
            Debug.WriteLine("CancelLoadAndClearFiles");
            CloseWatcher();
            if (IsLoadingItems)
            {
                addFilesCTS.Cancel();
            }
            CancelExtendedPropertiesLoading();
            filesAndFolders.Clear();
        }

        public void CancelExtendedPropertiesLoading()
        {
            loadPropsCTS.Cancel();
            loadPropsCTS = new CancellationTokenSource();
        }

        public async Task ApplySingleFileChangeAsync(ListedItem item)
        {
            var newIndex = filesAndFolders.IndexOf(item);
            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
            {
                FilesAndFolders.Remove(item);
                if (newIndex != -1)
                {
                    FilesAndFolders.Insert(newIndex, item);
                }
            });
        }

        // apply changes immediately after manipulating on filesAndFolders completed
        public async Task ApplyFilesAndFoldersChangesAsync()
        {
            try
            {
                if (filesAndFolders == null || filesAndFolders.Count == 0)
                {
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                    {
                        FilesAndFolders.Clear();
                        IsFolderEmptyTextDisplayed = FilesAndFolders.Count == 0;
                        DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
                    });
                    return;
                }

                // CollectionChanged will cause UI update, which may cause significant performance degradation,
                // so suppress CollectionChanged event here while loading items heavily.

                // Note that both DataGrid and GridView don't support multi-items changes notification, so here
                // we have to call BeginBulkOperation to suppress CollectionChanged and call EndBulkOperation
                // in the end to fire a CollectionChanged event with NotifyCollectionChangedAction.Reset
                FilesAndFolders.BeginBulkOperation();

                // After calling BeginBulkOperation, ObservableCollection.CollectionChanged is suppressed
                // so modifies to FilesAndFolders won't trigger UI updates, hence below operations can be
                // run safely without needs of dispatching to UI thread
                await Task.Run(() =>
                {
                    var startIndex = -1;
                    var tempList = new List<ListedItem>();

                    void ApplyBulkInsertEntries()
                    {
                        if (startIndex != -1)
                        {
                            FilesAndFolders.ReplaceRange(startIndex, tempList);
                            startIndex = -1;
                            tempList.Clear();
                        }
                    }

                    for (var i = 0; i < filesAndFolders.Count; i++)
                    {
                        if (addFilesCTS.IsCancellationRequested)
                        {
                            return;
                        }

                        if (i < FilesAndFolders.Count)
                        {
                            if (FilesAndFolders[i] != filesAndFolders[i])
                            {
                                if (startIndex == -1)
                                {
                                    startIndex = i;
                                }
                                tempList.Add(filesAndFolders[i]);
                            }
                            else
                            {
                                ApplyBulkInsertEntries();
                            }
                        }
                        else
                        {
                            ApplyBulkInsertEntries();
                            FilesAndFolders.InsertRange(i, filesAndFolders.Skip(i));
                            break;
                        }
                    }

                    ApplyBulkInsertEntries();
                    if (FilesAndFolders.Count > filesAndFolders.Count)
                    {
                        FilesAndFolders.RemoveRange(filesAndFolders.Count, FilesAndFolders.Count - filesAndFolders.Count);
                    }
                });

                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    // trigger CollectionChanged with NotifyCollectionChangedAction.Reset
                    // once loading is completed so that UI can be updated
                    FilesAndFolders.EndBulkOperation();
                    IsFolderEmptyTextDisplayed = FilesAndFolders.Count == 0;
                    DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
                });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
            }
        }

        private Task OrderFilesAndFoldersAsync()
        {
            return Task.Run(() =>
            {
                if (filesAndFolders.Count == 0)
                {
                    return Task.CompletedTask;
                }

                static object orderByNameFunc(ListedItem item) => item.ItemName;
                Func<ListedItem, object> orderFunc = orderByNameFunc;
                var naturalStringComparer = NaturalStringComparer.GetForProcessor();
                switch (folderSettings.DirectorySortOption)
                {
                    case SortOption.Name:
                        orderFunc = orderByNameFunc;
                        break;

                    case SortOption.DateModified:
                        orderFunc = item => item.ItemDateModifiedReal;
                        break;

                    case SortOption.DateCreated:
                        orderFunc = item => item.ItemDateCreatedReal;
                        break;

                    case SortOption.FileType:
                        orderFunc = item => item.ItemType;
                        break;

                    case SortOption.Size:
                        orderFunc = item => item.FileSizeBytes;
                        break;

                    case SortOption.OriginalPath:
                        orderFunc = item => ((RecycleBinItem)item).ItemOriginalFolder;
                        break;

                    case SortOption.DateDeleted:
                        orderFunc = item => ((RecycleBinItem)item).ItemDateDeletedReal;
                        break;
                }

                // In ascending order, show folders first, then files.
                // So, we use == StorageItemTypes.File to make the value for a folder equal to 0, and equal to 1 for the rest.
                static bool folderThenFileAsync(ListedItem listedItem) => (listedItem.PrimaryItemAttribute == StorageItemTypes.File);
                IOrderedEnumerable<ListedItem> ordered;

                if (folderSettings.DirectorySortDirection == SortDirection.Ascending)
                {
                    if (folderSettings.DirectorySortOption == SortOption.Name)
                    {
                        if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                        {
                            ordered = filesAndFolders.OrderBy(orderFunc, naturalStringComparer);
                        }
                        else
                        {
                            ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc, naturalStringComparer);
                        }
                    }
                    else
                    {
                        if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                        {
                            ordered = filesAndFolders.OrderBy(orderFunc);
                        }
                        else
                        {
                            ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc);
                        }
                    }
                }
                else
                {
                    if (folderSettings.DirectorySortOption == SortOption.Name)
                    {
                        if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                        {
                            ordered = filesAndFolders.OrderByDescending(orderFunc, naturalStringComparer);
                        }
                        else
                        {
                            ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                        }
                    }
                    else
                    {
                        if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                        {
                            ordered = filesAndFolders.OrderByDescending(orderFunc);
                        }
                        else
                        {
                            ordered = filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc);
                        }
                    }
                }

                // Further order by name if applicable
                if (folderSettings.DirectorySortOption != SortOption.Name)
                {
                    if (folderSettings.DirectorySortDirection == SortDirection.Ascending)
                    {
                        ordered = ordered.ThenBy(orderByNameFunc, naturalStringComparer);
                    }
                    else
                    {
                        ordered = ordered.ThenByDescending(orderByNameFunc, naturalStringComparer);
                    }
                }

                filesAndFolders = ordered.ToList();

                return Task.CompletedTask;
            });
        }

        private bool isLoadingIndicatorActive = false;

        public bool IsLoadingIndicatorActive
        {
            get
            {
                return isLoadingIndicatorActive;
            }
            set
            {
                if (isLoadingIndicatorActive != value)
                {
                    isLoadingIndicatorActive = value;
                    NotifyPropertyChanged(nameof(IsLoadingIndicatorActive));
                }
            }
        }

        private bool isLoadingItems = false;

        public bool IsLoadingItems
        {
            get
            {
                return isLoadingItems;
            }
            set
            {
                isLoadingItems = value;
                IsLoadingIndicatorActive = value;
            }
        }

        // This works for recycle bin as well as GetFileFromPathAsync/GetFolderFromPathAsync work
        // for file inside the recycle bin (but not on the recycle bin folder itself)
        public async Task LoadExtendedItemProperties(ListedItem item, uint thumbnailSize = 20)
        {
            await Task.Run(async () =>
            {
                if (item == null)
                {
                    return;
                }

                try
                {
                    await loadExtendedPropsSemaphore.WaitAsync(loadPropsCTS.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                var wasSyncStatusLoaded = false;
                try
                {
                    if (item.IsLibraryItem || item.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        var fileIconInfo = await LoadIconOverlayAsync(item.ItemPath, thumbnailSize);

                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            if (fileIconInfo.IconData != null)
                            {
                                item.FileImage = await fileIconInfo.IconData.ToBitmapAsync();
                                item.CustomIconData = fileIconInfo.IconData;
                                item.LoadUnknownTypeGlyph = false;
                                item.LoadWebShortcutGlyph = false;
                                item.LoadFileIcon = true;
                            }
                            item.IconOverlay = await fileIconInfo.OverlayData.ToBitmapAsync();
                        }, Windows.System.DispatcherQueuePriority.Low);
                        if (!item.IsShortcutItem && !item.IsHiddenItem && !item.ItemPath.StartsWith("ftp:"))
                        {
                            StorageFile matchingStorageItem = await GetFileFromPathAsync(item.ItemPath);
                            if (matchingStorageItem != null)
                            {
                                if (!item.LoadFileIcon) // Loading icon from fulltrust process failed
                                {
                                    using (var Thumbnail = await matchingStorageItem.GetThumbnailAsync(ThumbnailMode.SingleItem, thumbnailSize, ThumbnailOptions.UseCurrentScale))
                                    {
                                        if (Thumbnail != null)
                                        {
                                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                                            {
                                                item.FileImage = new BitmapImage();
                                                item.CustomIconData = await Thumbnail.ToByteArrayAsync();
                                                await item.FileImage.SetSourceAsync(Thumbnail);
                                                item.LoadUnknownTypeGlyph = false;
                                                item.LoadFileIcon = true;
                                            });
                                        }
                                    }
                                }

                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageItem);
                                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                                {
                                    item.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                    item.ItemType = matchingStorageItem.DisplayType;
                                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                }, Windows.System.DispatcherQueuePriority.Low);
                                wasSyncStatusLoaded = true;
                            }
                        }
                    }
                    else
                    {
                        var fileIconInfo = await LoadIconOverlayAsync(item.ItemPath, thumbnailSize);

                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            if (fileIconInfo.IconData != null && fileIconInfo.IsCustom) // Only set folder icon if it's a custom icon
                            {
                                item.FileImage = await fileIconInfo.IconData.ToBitmapAsync();
                                item.CustomIconData = fileIconInfo.IconData;
                                item.LoadUnknownTypeGlyph = false;
                                item.LoadFolderGlyph = false;
                                item.LoadFileIcon = true;
                            }
                            item.IconOverlay = await fileIconInfo.OverlayData.ToBitmapAsync();
                        }, Windows.System.DispatcherQueuePriority.Low);
                        if (!item.IsShortcutItem && !item.IsHiddenItem && !item.ItemPath.StartsWith("ftp:"))
                        {
                            StorageFolder matchingStorageItem = await GetFolderFromPathAsync(item.ItemPath);
                            if (matchingStorageItem != null)
                            {
                                if (matchingStorageItem.DisplayName != item.ItemName && !matchingStorageItem.DisplayName.StartsWith("$R"))
                                {
                                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        item.ItemName = matchingStorageItem.DisplayName;
                                    });
                                    await fileListCache.SaveFileDisplayNameToCache(item.ItemPath, matchingStorageItem.DisplayName);
                                    if (folderSettings.DirectorySortOption == SortOption.Name && !isLoadingItems)
                                    {
                                        await OrderFilesAndFoldersAsync();
                                        await ApplySingleFileChangeAsync(item);
                                    }
                                }
                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageItem);
                                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                                {
                                    item.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                    item.ItemType = matchingStorageItem.DisplayType;
                                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                    wasSyncStatusLoaded = true;
                                }, Windows.System.DispatcherQueuePriority.Low);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (!wasSyncStatusLoaded)
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                        {
                            item.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false }; // Reset cloud sync status icon
                        }, Windows.System.DispatcherQueuePriority.Low);
                    }
                    loadExtendedPropsSemaphore.Release();
                }
            });
        }

        public async Task<(byte[] IconData, byte[] OverlayData, bool IsCustom)> LoadIconOverlayAsync(string filePath, uint thumbnailSize)
        {
            if (Connection != null)
            {
                var value = new ValueSet();
                value.Add("Arguments", "GetIconOverlay");
                value.Add("filePath", filePath);
                value.Add("thumbnailSize", (int)thumbnailSize);
                var (status, response) = await Connection.SendMessageForResponseAsync(value);
                if (status == AppServiceResponseStatus.Success)
                {
                    var hasCustomIcon = response.Get("HasCustomIcon", false);
                    var icon = response.Get("Icon", (string)null);
                    var overlay = response.Get("Overlay", (string)null);

                    // BitmapImage can only be created on UI thread, so return raw data and create
                    // BitmapImage later to prevent exceptions once SynchorizationContext lost
                    return (icon == null ? null : Convert.FromBase64String(icon),
                        overlay == null ? null : Convert.FromBase64String(overlay),
                        hasCustomIcon);
                }
            }
            return (null, null, false);
        }

        public void RefreshItems(string previousDir, bool useCache = true)
        {
            RapidAddItemsToCollectionAsync(WorkingDirectory, previousDir, useCache);
        }

        private async void RapidAddItemsToCollectionAsync(string path, string previousDir, bool useCache = true)
        {
            ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting });

            CancelLoadAndClearFiles();

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                // Only one instance at a time should access this function
                // Wait here until the previous one has ended
                // If we're waiting and a new update request comes through
                // simply drop this instance
                await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                // Drop all the other waiting instances
                semaphoreCTS.Cancel();
                semaphoreCTS = new CancellationTokenSource();

                IsLoadingItems = true;

                filesAndFolders.Clear();
                FilesAndFolders.Clear();

                ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress });

                if (path.ToLower().EndsWith(ShellLibraryItem.EXTENSION))
                {
                    if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library) && !library.IsEmpty)
                    {
                        var libItem = new LibraryItem(library);
                        foreach (var folder in library.Folders)
                        {
                            await RapidAddItemsToCollection(folder, useCache, libItem);
                        }
                    }
                }
                else
                {
                    await RapidAddItemsToCollection(path, useCache);
                }

                ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete, PreviousDirectory = previousDir, Path = path });
                IsLoadingItems = false;

                AdaptiveLayoutHelpers.PredictLayoutMode(folderSettings, this);
            }
            finally
            {
                enumFolderSemaphore.Release();
            }
        }

        private async Task RapidAddItemsToCollection(string path, bool useCache, LibraryItem library = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<string> cacheResult = null;

            if (useCache)
            {
                cacheResult = await Task.Run(async () =>
                {
                    CacheEntry cacheEntry = await fileListCache.ReadFileListFromCache(path, addFilesCTS.Token);

                    if (cacheEntry != null)
                    {
                        for (var i = 0; i < Math.Min(32, cacheEntry.FileList.Count); i++)
                        {
                            var entry = cacheEntry.FileList[i];
                            if (!entry.IsHiddenItem || AppSettings.AreHiddenItemsVisible)
                            {
                                if (entry.FileImage == null)
                                {
                                    entry.LoadFolderGlyph = entry.PrimaryItemAttribute == StorageItemTypes.Folder;
                                    entry.LoadUnknownTypeGlyph = entry.PrimaryItemAttribute == StorageItemTypes.File && !entry.IsLinkItem;
                                    entry.LoadWebShortcutGlyph = entry.PrimaryItemAttribute == StorageItemTypes.File && entry.IsLinkItem;
                                    entry.LoadFileIcon = false;
                                }
                                filesAndFolders.Add(entry);
                            }
                            if (addFilesCTS.IsCancellationRequested)
                            {
                                return null;
                            }
                        }
                        return filesAndFolders.Select(i => i.ItemPath).ToList();
                    }
                    return null;
                });
                if (cacheResult != null)
                {
                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();
                    IsLoadingItems = false; // Hide progress indicator if loaded from cache
                }
            }

            var isRecycleBin = path.StartsWith(AppSettings.RecycleBinPath);
            if (isRecycleBin ||
                path.StartsWith(AppSettings.NetworkFolderPath) ||
                path.StartsWith("ftp:"))
            {
                // Recycle bin and network are enumerated by the fulltrust process
                PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false, IsTypeRecycleBin = isRecycleBin });
                await EnumerateItemsFromSpecialFolderAsync(path);
            }
            else
            {
                var storageFolder = currentStorageFolder;
                if (Path.IsPathRooted(path))
                {
                    var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
                    if (res)
                    {
                        storageFolder = res.Result;
                    }
                }
                if (await EnumerateItemsFromStandardFolderAsync(path, storageFolder, folderSettings.GetLayoutType(path, false), addFilesCTS.Token, cacheResult, cacheOnly: false, library))
                {
                    // Is folder synced to cloud storage?
                    var syncStatus = await CheckCloudDriveSyncStatusAsync(currentStorageFolder?.Item);
                    PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown });

                    WatchForDirectoryChanges(path, syncStatus);
                }

                var parallelLimit = App.AppSettings.PreemptiveCacheParallelLimit;
                if (App.AppSettings.UseFileListCache && App.AppSettings.UsePreemptiveCache && parallelLimit > 0 && !addFilesCTS.IsCancellationRequested)
                {
                    void CacheSubfolders(IEnumerable<string> folderPaths, StorageFolderWithPath parentFolder, LibraryItem library)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await folderPaths.AsyncParallelForEach(async folderPath =>
                                {
                                    if (addFilesCTS.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                    StorageFolderWithPath storageFolder = null;
                                    if (Path.IsPathRooted(folderPath))
                                    {
                                        var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(folderPath, null, parentFolder));
                                        if (res)
                                        {
                                            storageFolder = res.Result;
                                        }
                                    }
                                    await EnumerateItemsFromStandardFolderAsync(folderPath, storageFolder, folderSettings.GetLayoutType(folderPath, false), addFilesCTS.Token, null, cacheOnly: true, library);
                                }, maxDegreeOfParallelism: parallelLimit);
                            }
                            catch (Exception ex)
                            {
                                // ignore exception. This is fine, it's only a caching that can fail
                                NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                            }
                        }).Forget();
                    }

                    // run background tasks to iterate through folders and cache all of them preemptively
                    CacheSubfolders(filesAndFolders.Where(e => e.PrimaryItemAttribute == StorageItemTypes.Folder && !e.IsLibraryItem).Select(e => e.ItemPath), storageFolder, library);

                    // run background tasks to iterate through library folders and cache all of them preemptively
                    foreach (var libFile in filesAndFolders.Where(e => e.IsLibraryItem))
                    {
                        if (App.LibraryManager.TryGetLibrary(libFile.ItemPath, out LibraryLocationItem lib) && !lib.IsEmpty)
                        {
                            CacheSubfolders(lib.Folders, null, new LibraryItem(lib));
                        }
                    }
                }
            }

            if (addFilesCTS.IsCancellationRequested)
            {
                addFilesCTS = new CancellationTokenSource();
                IsLoadingItems = false;
                return;
            }

            stopwatch.Stop();
            Debug.WriteLine($"Loading of items in {path} completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
        }

        public void CloseWatcher()
        {
            if (aWatcherAction != null)
            {
                aWatcherAction?.Cancel();

                if (aWatcherAction.Status != AsyncStatus.Started)
                {
                    aWatcherAction = null;  // Prevent duplicate execution of this block
                    Debug.WriteLine("watcher canceled");
                    CancelIoEx(hWatchDir, IntPtr.Zero);
                    Debug.WriteLine("watcher handle closed");
                    CloseHandle(hWatchDir);
                }
            }
        }

        public async Task EnumerateItemsFromSpecialFolderAsync(string path)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            CurrentFolder = new ListedItem(null, returnformat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemPropertiesInitialized = true,
                ItemName = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                ItemDateModifiedReal = DateTimeOffset.Now, // Fake for now
                ItemDateCreatedReal = DateTimeOffset.Now, // Fake for now
                ItemType = "FileFolderListItem".GetLocalized(),
                LoadFolderGlyph = true,
                FileImage = null,
                LoadFileIcon = false,
                ItemPath = AppSettings.RecycleBinPath,
                LoadUnknownTypeGlyph = false,
                FileSize = null,
                FileSizeBytes = 0
            };

            if (Connection != null)
            {
                await Task.Run(async () =>
                {
                    var sampler = new IntervalSampler(500);
                    var value = new ValueSet();
                    value.Add("Arguments", "ShellFolder");
                    value.Add("action", "Enumerate");
                    value.Add("folder", path);
                    // Send request to fulltrust process to enumerate recyclebin items
                    var (status, response) = await Connection.SendMessageForResponseAsync(value);
                    // If the request was canceled return now
                    if (addFilesCTS.IsCancellationRequested)
                    {
                        return;
                    }
                    if (status == AppServiceResponseStatus.Success
                        && response.ContainsKey("Enumerate"))
                    {
                        var folderContentsList = JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response["Enumerate"]);
                        for (int count = 0; count < folderContentsList.Count; count++)
                        {
                            var item = folderContentsList[count];
                            var listedItem = AddFileOrFolderFromShellFile(item, returnformat);
                            if (listedItem != null)
                            {
                                filesAndFolders.Add(listedItem);
                            }
                            if (count == 32 || count == folderContentsList.Count - 1 || sampler.CheckNow())
                            {
                                await OrderFilesAndFoldersAsync();
                                await ApplyFilesAndFoldersChangesAsync();
                            }
                        }
                    }
                });
            }
        }

        public async Task<bool> EnumerateItemsFromStandardFolderAsync(string path, StorageFolderWithPath storageFolderForGivenPath, Type sourcePageType, CancellationToken cancellationToken, List<string> skipItems, bool cacheOnly = false, LibraryItem library = null)
        {
            // Flag to use FindFirstFileExFromApp or StorageFolder enumeration
            bool enumFromStorageFolder = false;

            StorageFolder rootFolder = null;
            var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(path).AsTask());
            if (res)
            {
                rootFolder = res.Result;
            }
            else if (workingRoot != null)
            {
                if (storageFolderForGivenPath == null)
                {
                    return false;
                }
                rootFolder = storageFolderForGivenPath.Folder;
                enumFromStorageFolder = true;
            }
            else if (!FolderHelpers.CheckFolderAccessWithWin32(path)) // The folder is really inaccessible
            {
                if (cacheOnly)
                {
                    return false;
                }

                if (res == FileSystemStatusCode.Unauthorized)
                {
                    //TODO: proper dialog
                    await DialogDisplayHelper.ShowDialogAsync(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "SubDirectoryAccessDenied".GetLocalized());
                    return false;
                }
                else if (res == FileSystemStatusCode.NotFound)
                {
                    await DialogDisplayHelper.ShowDialogAsync(
                        "FolderNotFoundDialog/Title".GetLocalized(),
                        "FolderNotFoundDialog/Text".GetLocalized());
                    return false;
                }
                else
                {
                    await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalized(), res.ErrorCode.ToString());
                    return false;
                }
            }

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            if (!cacheOnly && await FolderHelpers.CheckBitlockerStatusAsync(rootFolder, WorkingDirectory))
            {
                var bitlockerDialog = new Files.Dialogs.BitlockerDialog(Path.GetPathRoot(WorkingDirectory));
                var bitlockerResult = await bitlockerDialog.ShowAsync();
                if (bitlockerResult == ContentDialogResult.Primary)
                {
                    var userInput = bitlockerDialog.storedPasswordInput;
                    if (Connection != null)
                    {
                        var value = new ValueSet();
                        value.Add("Arguments", "Bitlocker");
                        value.Add("action", "Unlock");
                        value.Add("drive", Path.GetPathRoot(path));
                        value.Add("password", userInput);
                        await Connection.SendMessageAsync(value);

                        if (await FolderHelpers.CheckBitlockerStatusAsync(rootFolder, WorkingDirectory))
                        {
                            // Drive is still locked
                            await DialogDisplayHelper.ShowDialogAsync("BitlockerInvalidPwDialog/Title".GetLocalized(), "BitlockerInvalidPwDialog/Text".GetLocalized());
                        }
                    }
                }
            }

            if (enumFromStorageFolder)
            {
                var basicProps = await rootFolder.GetBasicPropertiesAsync();
                var currentFolder = library ?? new ListedItem(rootFolder.FolderRelativeId, returnformat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemPropertiesInitialized = true,
                    ItemName = rootFolder.DisplayName,
                    ItemDateModifiedReal = basicProps.DateModified,
                    ItemType = rootFolder.DisplayType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(rootFolder.Path) ? storageFolderForGivenPath.Path : rootFolder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0,
                };
                if (library == null)
                {
                    var extraProps = await basicProps.RetrievePropertiesAsync(new[] { "System.DateCreated" });
                    if (DateTimeOffset.TryParse(extraProps["System.DateCreated"] as string, out var dateCreated))
                    {
                        currentFolder.ItemDateCreatedReal = dateCreated;
                    }
                }
                if (!cacheOnly)
                {
                    CurrentFolder = currentFolder;
                }
                await EnumFromStorageFolderAsync(path, currentFolder, rootFolder, storageFolderForGivenPath, sourcePageType, cancellationToken, skipItems, cacheOnly);
                return true;
            }
            else
            {
                (IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
                {
                    FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
                    int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
                    IntPtr hFileTsk = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                        additionalFlags);
                    return (hFileTsk, findDataTsk);
                }).WithTimeoutAsync(TimeSpan.FromSeconds(5));

                var itemModifiedDate = DateTime.Now;
                var itemCreatedDate = DateTime.Now;
                try
                {
                    FileTimeToSystemTime(ref findData.ftLastWriteTime, out var systemModifiedTimeOutput);
                    itemModifiedDate = new DateTime(
                        systemModifiedTimeOutput.Year, systemModifiedTimeOutput.Month, systemModifiedTimeOutput.Day,
                        systemModifiedTimeOutput.Hour, systemModifiedTimeOutput.Minute, systemModifiedTimeOutput.Second, systemModifiedTimeOutput.Milliseconds,
                        DateTimeKind.Utc);

                    FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedTimeOutput);
                    itemCreatedDate = new DateTime(
                        systemCreatedTimeOutput.Year, systemCreatedTimeOutput.Month, systemCreatedTimeOutput.Day,
                        systemCreatedTimeOutput.Hour, systemCreatedTimeOutput.Minute, systemCreatedTimeOutput.Second, systemCreatedTimeOutput.Milliseconds,
                        DateTimeKind.Utc);
                }
                catch (ArgumentException) { }

                bool isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
                double opacity = 1;

                if (isHidden)
                {
                    opacity = Constants.UI.DimItemOpacity;
                }

                var currentFolder = library ?? new ListedItem(null, returnformat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemPropertiesInitialized = true,
                    ItemName = Path.GetFileName(path.TrimEnd('\\')),
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = "FileFolderListItem".GetLocalized(),
                    LoadFolderGlyph = true,
                    FileImage = null,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    LoadFileIcon = false,
                    ItemPath = path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0,
                };
                if (!cacheOnly)
                {
                    CurrentFolder = currentFolder;
                }

                if (hFile == IntPtr.Zero)
                {
                    if (!cacheOnly)
                    {
                        await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalized(), "");
                    }
                    return false;
                }
                else if (hFile.ToInt64() == -1)
                {
                    await EnumFromStorageFolderAsync(path, currentFolder, rootFolder, storageFolderForGivenPath, sourcePageType, cancellationToken, skipItems, cacheOnly);
                    return false;
                }
                else
                {
                    List<ListedItem> fileList;
                    if (cacheOnly)
                    {
                        fileList = await Win32StorageEnumerator.ListEntries(path, returnformat, hFile, findData, Connection, cancellationToken, skipItems, 32, null);
                        await SaveFileListToCacheAsync(path, fileList);
                    }
                    else
                    {
                        fileList = await Win32StorageEnumerator.ListEntries(path, returnformat, hFile, findData, Connection, cancellationToken, skipItems, -1, intermediateAction: async (intermediateList) =>
                        {
                            filesAndFolders.AddRange(intermediateList);
                            await OrderFilesAndFoldersAsync();
                            await ApplyFilesAndFoldersChangesAsync();
                        });

                        filesAndFolders.AddRange(fileList);
                        await OrderFilesAndFoldersAsync();
                        await ApplyFilesAndFoldersChangesAsync();
                    }

                    if (skipItems != null)
                    {
                        // remove invalid cache entries
                        var invalidEntries = filesAndFolders.Where(i => skipItems.Contains(i.ItemPath)).ToList();
                        foreach (var i in invalidEntries)
                        {
                            filesAndFolders.Remove(i);
                        }
                    }

                    if (!cacheOnly)
                    {
                        if (!addFilesCTS.IsCancellationRequested)
                        {
                            await SaveFileListToCacheAsync(path, filesAndFolders);
                        }
                        else
                        {
                            await fileListCache.SaveFileListToCache(path, null);
                        }
                    }
                    return true;
                }
            }
        }

        private async Task EnumFromStorageFolderAsync(string path, ListedItem currentFolder, StorageFolder rootFolder, StorageFolderWithPath currentStorageFolder, Type sourcePageType, CancellationToken cancellationToken, List<string> skipItems, bool cacheOnly)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            List<ListedItem> finalList;
            if (cacheOnly)
            {
                finalList = await UniversalStorageEnumerator.ListEntries(
                    rootFolder,
                    currentStorageFolder,
                    returnformat,
                    sourcePageType,
                    cancellationToken,
                    null,
                    32,
                    null);
                await SaveFileListToCacheAsync(path, finalList);
            }
            else
            {
                finalList = await UniversalStorageEnumerator.ListEntries(
                    rootFolder,
                    currentStorageFolder,
                    returnformat,
                    sourcePageType,
                    cancellationToken,
                    skipItems,
                    -1,
                    async (intermediateList) =>
                {
                    filesAndFolders.AddRange(intermediateList);
                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();
                });
                filesAndFolders.AddRange(finalList);
                await OrderFilesAndFoldersAsync();
                await ApplyFilesAndFoldersChangesAsync();
            }

            if (skipItems != null)
            {
                // remove invalid cache entries
                var invalidEntries = filesAndFolders.Where(i => skipItems.Contains(i.ItemPath)).ToList();
                foreach (var i in invalidEntries)
                {
                    filesAndFolders.Remove(i);
                }
            }
            stopwatch.Stop();
            if (!cacheOnly)
            {
                if (!addFilesCTS.IsCancellationRequested)
                {
                    await SaveFileListToCacheAsync(path, filesAndFolders);
                }
                else
                {
                    await fileListCache.SaveFileListToCache(path, null);
                }
            }
            Debug.WriteLine($"Enumerating items in {path} (device) completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
        }

        private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatusAsync(IStorageItem item)
        {
            int? syncStatus = null;
            if (item is StorageFile)
            {
                IDictionary<string, object> extraProperties = await ((StorageFile)item).Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus" });
                syncStatus = (int?)(uint?)extraProperties["System.FilePlaceholderStatus"];
            }
            else if (item is StorageFolder)
            {
                IDictionary<string, object> extraProperties = await ((StorageFolder)item).Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus", "System.FileOfflineAvailabilityStatus" });
                syncStatus = (int?)(uint?)extraProperties["System.FileOfflineAvailabilityStatus"];
                // If no FileOfflineAvailabilityStatus, check FilePlaceholderStatus
                syncStatus = syncStatus ?? (int?)(uint?)extraProperties["System.FilePlaceholderStatus"];
            }
            if (syncStatus == null || !Enum.IsDefined(typeof(CloudDriveSyncStatus), syncStatus))
            {
                return CloudDriveSyncStatus.Unknown;
            }
            return (CloudDriveSyncStatus)syncStatus;
        }

        private void WatchForDirectoryChanges(string path, CloudDriveSyncStatus syncStatus)
        {
            Debug.WriteLine("WatchForDirectoryChanges: {0}", path);
            hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(path, 1, 1 | 2 | 4,
                IntPtr.Zero, 3, (uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped, IntPtr.Zero);
            if (hWatchDir.ToInt64() == -1)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _ = Windows.System.Threading.ThreadPool.RunAsync((x) => ProcessOperationQueue(cts.Token));

            aWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
            {
                byte[] buff = new byte[4096];
                var rand = Guid.NewGuid();
                buff = new byte[4096];
                int notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME;

                if (syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown)
                {
                    notifyFilters |= FILE_NOTIFY_CHANGE_ATTRIBUTES;
                }

                OVERLAPPED overlapped = new OVERLAPPED();
                overlapped.hEvent = CreateEvent(IntPtr.Zero, false, false, null);
                const uint INFINITE = 0xFFFFFFFF;

                while (x.Status != AsyncStatus.Canceled)
                {
                    unsafe
                    {
                        fixed (byte* pBuff = buff)
                        {
                            ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
                            if (x.Status != AsyncStatus.Canceled)
                            {
                                NativeDirectoryChangesHelper.ReadDirectoryChangesW(hWatchDir, pBuff,
                                4096, false,
                                notifyFilters, null,
                                ref overlapped, null);
                            }
                            else
                            {
                                break;
                            }

                            Debug.WriteLine("waiting: {0}", rand);
                            if (x.Status == AsyncStatus.Canceled)
                            {
                                break;
                            }
                            var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);
                            Debug.WriteLine("wait done: {0}", rand);

                            uint offset = 0;
                            ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
                            if (x.Status == AsyncStatus.Canceled)
                            {
                                break;
                            }

                            do
                            {
                                notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
                                string FileName = null;
                                unsafe
                                {
                                    fixed (char* name = notifyInfo.FileName)
                                    {
                                        FileName = Path.Combine(path, new string(name, 0, (int)notifyInfo.FileNameLength / 2));
                                    }
                                }

                                uint action = notifyInfo.Action;

                                Debug.WriteLine("action: {0}", action);

                                operationQueue.Enqueue((action, FileName));

                                try
                                {
                                    operationSemaphore.Release();
                                }
                                catch (Exception)
                                {
                                    // Prevent semaphore handles exceeding
                                }

                                offset += notifyInfo.NextEntryOffset;
                            } while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

                            //ResetEvent(overlapped.hEvent);
                            Debug.WriteLine("Task running...");
                        }
                    }
                }
                CloseHandle(overlapped.hEvent);
                operationQueue.Clear();
                cts.Cancel();
                Debug.WriteLine("aWatcherAction done: {0}", rand);
            });

            Debug.WriteLine("Task exiting...");
        }

        private async void ProcessOperationQueue(CancellationToken cancellationToken)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            const uint FILE_ACTION_ADDED = 0x00000001;
            const uint FILE_ACTION_REMOVED = 0x00000002;
            const uint FILE_ACTION_MODIFIED = 0x00000003;
            const uint FILE_ACTION_RENAMED_OLD_NAME = 0x00000004;
            const uint FILE_ACTION_RENAMED_NEW_NAME = 0x00000005;

            var sampler = new IntervalSampler(500);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await operationSemaphore.WaitAsync(cancellationToken);
                    while (operationQueue.TryDequeue(out var operation))
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        try
                        {
                            switch (operation.Action)
                            {
                                case FILE_ACTION_ADDED:
                                case FILE_ACTION_RENAMED_NEW_NAME:
                                    await AddFileOrFolderAsync(operation.FileName, returnformat);
                                    break;

                                case FILE_ACTION_MODIFIED:
                                    await UpdateFileOrFolderAsync(operation.FileName);
                                    break;

                                case FILE_ACTION_REMOVED:
                                case FILE_ACTION_RENAMED_OLD_NAME:
                                    await RemoveFileOrFolderAsync(operation.FileName);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                        }

                        if (sampler.CheckNow())
                        {
                            await OrderFilesAndFoldersAsync();
                            await ApplyFilesAndFoldersChangesAsync();
                        }
                    }

                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();
                    await SaveFileListToCacheAsync(WorkingDirectory, filesAndFolders);
                }
            }
            catch
            {
                // Prevent disposed cancellation token
            }
        }

        public ListedItem AddFileOrFolderFromShellFile(ShellFileItem item, string dateReturnFormat = null)
        {
            if (dateReturnFormat == null)
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                dateReturnFormat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            }

            if (item.IsFolder)
            {
                // Folder
                return new RecycleBinItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = item.FileName,
                    ItemDateModifiedReal = item.ModifiedDate,
                    ItemDateCreatedReal = item.CreatedDate,
                    ItemDateDeletedReal = item.RecycleDate,
                    ItemType = item.FileType,
                    IsHiddenItem = false,
                    Opacity = 1,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = item.RecyclePath, // this is the true path on disk so other stuff can work as is
                    ItemOriginalPath = item.FilePath,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0,
                    //FolderTooltipText = tooltipString,
                };
            }
            else
            {
                // File
                string itemName;
                if (App.AppSettings.ShowFileExtensions && !item.FileName.EndsWith(".lnk") && !item.FileName.EndsWith(".url"))
                {
                    itemName = item.FileName; // never show extension for shortcuts
                }
                else
                {
                    if (item.FileName.StartsWith("."))
                    {
                        itemName = item.FileName; // Always show full name for dotfiles.
                    }
                    else
                    {
                        itemName = Path.GetFileNameWithoutExtension(item.FileName);
                    }
                }

                string itemFileExtension = null;
                if (item.FileName.Contains('.'))
                {
                    itemFileExtension = Path.GetExtension(item.FileName);
                }
                return new RecycleBinItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    LoadUnknownTypeGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    LoadFolderGlyph = false,
                    IsHiddenItem = false,
                    Opacity = 1,
                    ItemName = itemName,
                    ItemDateModifiedReal = item.ModifiedDate,
                    ItemDateCreatedReal = item.CreatedDate,
                    ItemDateDeletedReal = item.RecycleDate,
                    ItemType = item.FileType,
                    ItemPath = item.RecyclePath, // this is the true path on disk so other stuff can work as is
                    ItemOriginalPath = item.FilePath,
                    FileSize = item.FileSize,
                    FileSizeBytes = (long)item.FileSizeBytes
                };
            }
        }

        private async Task AddFileOrFolderAsync(ListedItem item)
        {
            try
            {
                await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                if (item != null)
                {
                    filesAndFolders.Add(item);
                }
            }
            finally
            {
                enumFolderSemaphore.Release();
            }
        }

        private async Task AddFileOrFolderAsync(string fileOrFolderPath, string dateReturnFormat)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_CASE_SENSITIVE;

            IntPtr hFile = FindFirstFileExFromApp(fileOrFolderPath, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);
            if (hFile.ToInt64() == -1)
            {
                // If we cannot find the file (probably since it doesn't exist anymore)
                // simply exit without adding it
                return;
            }

            FindClose(hFile);

            var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
            var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            if ((isSystem && !AppSettings.AreSystemItemsHidden) || (isHidden && !AppSettings.AreHiddenItemsVisible))
            {
                // Do not add to file list if hidden/system attribute is set and system/hidden file are not to be shown
                return;
            }

            ListedItem listedItem;
            if ((findData.dwFileAttributes & 0x10) > 0) // FILE_ATTRIBUTE_DIRECTORY
            {
                listedItem = await Win32StorageEnumerator.GetFolder(findData, Directory.GetParent(fileOrFolderPath).FullName, dateReturnFormat, addFilesCTS.Token);
            }
            else
            {
                listedItem = await Win32StorageEnumerator.GetFile(findData, Directory.GetParent(fileOrFolderPath).FullName, dateReturnFormat, Connection, addFilesCTS.Token);
            }

            if (listedItem != null)
            {
                filesAndFolders.Add(listedItem);
            }
        }

        private async Task UpdateFileOrFolderAsync(ListedItem item)
        {
            IStorageItem storageItem = null;
            if (item.PrimaryItemAttribute == StorageItemTypes.File)
            {
                storageItem = (await GetFileFromPathAsync(item.ItemPath)).Result;
            }
            else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                storageItem = (await GetFolderFromPathAsync(item.ItemPath)).Result;
            }
            if (storageItem != null)
            {
                var syncStatus = await CheckCloudDriveSyncStatusAsync(storageItem);
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                });
            }
        }

        private async Task UpdateFileOrFolderAsync(string path)
        {
            try
            {
                await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                var matchingItem = filesAndFolders.FirstOrDefault(x => x.ItemPath.Equals(path));

                if (matchingItem != null)
                {
                    await UpdateFileOrFolderAsync(matchingItem);
                }
            }
            finally
            {
                enumFolderSemaphore.Release();
            }
        }

        private Task SaveFileListToCacheAsync(string path, IEnumerable<ListedItem> fileList)
        {
            return fileListCache.SaveFileListToCache(path, new CacheEntry
            {
                CurrentFolder = CurrentFolder,
                // since filesAndFolders could be mutated, memory cache needs a copy of current list
                FileList = fileList.Take(32).ToList()
            });
        }

        private async Task RemoveFileOrFolderAsync(ListedItem item)
        {
            filesAndFolders.Remove(item);
            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
            {
                App.JumpList.RemoveFolder(item.ItemPath);
            });
        }

        public async Task<ListedItem> RemoveFileOrFolderAsync(string path)
        {
            try
            {
                await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            try
            {
                var matchingItem = filesAndFolders.FirstOrDefault(x => x.ItemPath.Equals(path));

                if (matchingItem != null)
                {
                    await RemoveFileOrFolderAsync(matchingItem);
                    return matchingItem;
                }
            }
            finally
            {
                enumFolderSemaphore.Release();
            }
            return null;
        }

        public async Task AddSearchResultsToCollection(ObservableCollection<ListedItem> searchItems, string currentSearchPath)
        {
            filesAndFolders.Clear();
            foreach (ListedItem li in searchItems)
            {
                filesAndFolders.Add(li);
            }
            await OrderFilesAndFoldersAsync();
            await ApplyFilesAndFoldersChangesAsync();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            CancelLoadAndClearFiles();
            if (Connection != null)
            {
                Connection.RequestReceived -= Connection_RequestReceived;
            }
        }
    }

    public class PageTypeUpdatedEventArgs
    {
        public bool IsTypeCloudDrive { get; set; }
        public bool IsTypeRecycleBin { get; set; }
    }

    public class WorkingDirectoryModifiedEventArgs : EventArgs
    {
        public string Path { get; set; }

        public string Name { get; set; }

        public bool IsLibrary { get; set; }
    }

    public class ItemLoadStatusChangedEventArgs : EventArgs
    {
        public enum ItemLoadStatus
        {
            Starting,
            InProgress,
            Complete
        }

        public ItemLoadStatus Status { get; set; }

        /// <summary>
        /// This property may not be provided consistently if Status is not Complete
        /// </summary>
        public string PreviousDirectory { get; set; }

        /// <summary>
        /// This property may not be provided consistently if Status is not Complete
        /// </summary>
        public string Path { get; set; }
    }
}