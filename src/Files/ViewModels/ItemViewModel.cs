using Files.Common;
using Files.Dialogs;
using Files.Enums;
using Files.EventArguments;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.Cloud;
using Files.Filesystem.Search;
using Files.Filesystem.StorageEnumerators;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Helpers.FileListCache;
using Files.Services;
using Files.UserControls;
using Files.ViewModels.Previews;
using FluentFTP;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Files.Helpers.NativeDirectoryChangesHelper;
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.ViewModels
{
    public class ItemViewModel : ObservableObject, IDisposable
    {
        private readonly SemaphoreSlim enumFolderSemaphore;
        private readonly ConcurrentQueue<(uint Action, string FileName)> operationQueue;
        private readonly ConcurrentDictionary<string, bool> itemLoadQueue;
        private readonly AsyncManualResetEvent operationEvent;
        private IntPtr hWatchDir;
        private IAsyncAction aWatcherAction;

        // files and folders list for manipulating
        private List<ListedItem> filesAndFolders;

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();
        private IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>();

        // only used for Binding and ApplyFilesAndFoldersChangesAsync, don't manipulate on this!
        public BulkConcurrentObservableCollection<ListedItem> FilesAndFolders { get; }
        private string folderTypeTextLocalized = "FileFolderListItem".GetLocalized();
        private SettingsViewModel AppSettings => App.AppSettings;
        private FolderSettingsViewModel folderSettings = null;
        private bool shouldDisplayFileExtensions = false;
        public ListedItem CurrentFolder { get; private set; }
        public CollectionViewSource viewSource;
        private CancellationTokenSource addFilesCTS, semaphoreCTS, loadPropsCTS;

        public event EventHandler DirectoryInfoUpdated;

        public event EventHandler<List<ListedItem>> OnSelectionRequestedEvent;

        private IFileListCache fileListCache = FileListCacheController.GetInstance();

        private NamedPipeAsAppServiceConnection connection;

        private NamedPipeAsAppServiceConnection Connection
        {
            get => connection;
            set
            {
                if (connection != null)
                {
                    connection.RequestReceived -= Connection_RequestReceived;
                }
                connection = value;
                if (connection != null)
                {
                    connection.RequestReceived += Connection_RequestReceived;
                }
            }
        }

        public string WorkingDirectory
        {
            get; private set;
        }

        private StorageFolderWithPath currentStorageFolder;
        private StorageFolderWithPath workingRoot;

        public delegate void WorkingDirectoryModifiedEventHandler(object sender, WorkingDirectoryModifiedEventArgs e);

        public event WorkingDirectoryModifiedEventHandler WorkingDirectoryModified;

        public delegate void PageTypeUpdatedEventHandler(object sender, PageTypeUpdatedEventArgs e);

        public event PageTypeUpdatedEventHandler PageTypeUpdated;

        public delegate void ItemLoadStatusChangedEventHandler(object sender, ItemLoadStatusChangedEventArgs e);

        public event ItemLoadStatusChangedEventHandler ItemLoadStatusChanged;

        public async Task SetWorkingDirectoryAsync(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
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
            }
            else if (!Path.IsPathRooted(WorkingDirectory) || Path.GetPathRoot(WorkingDirectory) != Path.GetPathRoot(value))
            {
                workingRoot = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(value));
            }

            if (value == "Home".GetLocalized())
            {
                currentStorageFolder = null;
            }
            else
            {
                App.JumpList.AddFolderToJumpList(value);
            }

            WorkingDirectory = value;
            OnPropertyChanged(nameof(WorkingDirectory));
        }

        public async Task<FilesystemResult<BaseStorageFolder>> GetFolderFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(value, workingRoot, currentStorageFolder));
        }

        public async Task<FilesystemResult<BaseStorageFile>> GetFileFromPathAsync(string value)
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

        private EmptyTextType emptyTextType;

        public EmptyTextType EmptyTextType
        {
            get => emptyTextType;
            set => SetProperty(ref emptyTextType, value);
        }

        public async void UpdateSortOptionStatus()
        {
            OnPropertyChanged(nameof(IsSortedByName));
            OnPropertyChanged(nameof(IsSortedByDate));
            OnPropertyChanged(nameof(IsSortedByType));
            OnPropertyChanged(nameof(IsSortedBySize));
            OnPropertyChanged(nameof(IsSortedByOriginalPath));
            OnPropertyChanged(nameof(IsSortedByDateDeleted));
            OnPropertyChanged(nameof(IsSortedByDateCreated));
            OnPropertyChanged(nameof(IsSortedBySyncStatus));
            OnPropertyChanged(nameof(IsSortedByFileTag));
            await OrderFilesAndFoldersAsync();
            await ApplyFilesAndFoldersChangesAsync();
        }

        public async void UpdateSortDirectionStatus()
        {
            OnPropertyChanged(nameof(IsSortedAscending));
            OnPropertyChanged(nameof(IsSortedDescending));
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
                    OnPropertyChanged(nameof(IsSortedByName));
                }
            }
        }

        public bool IsSortedByOriginalPath
        {
            get => folderSettings.DirectorySortOption == SortOption.OriginalFolder;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.OriginalFolder;
                    OnPropertyChanged(nameof(IsSortedByOriginalPath));
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
                    OnPropertyChanged(nameof(IsSortedByDateDeleted));
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
                    OnPropertyChanged(nameof(IsSortedByDate));
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
                    OnPropertyChanged(nameof(IsSortedByDateCreated));
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
                    OnPropertyChanged(nameof(IsSortedByType));
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
                    OnPropertyChanged(nameof(IsSortedBySize));
                }
            }
        }

        public bool IsSortedBySyncStatus
        {
            get => folderSettings.DirectorySortOption == SortOption.SyncStatus;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.SyncStatus;
                    OnPropertyChanged(nameof(IsSortedBySyncStatus));
                }
            }
        }

        public bool IsSortedByFileTag
        {
            get => folderSettings.DirectorySortOption == SortOption.FileTag;
            set
            {
                if (value)
                {
                    folderSettings.DirectorySortOption = SortOption.FileTag;
                    OnPropertyChanged(nameof(IsSortedByFileTag));
                }
            }
        }

        public bool IsSortedAscending
        {
            get => folderSettings.DirectorySortDirection == SortDirection.Ascending;
            set
            {
                folderSettings.DirectorySortDirection = value ? SortDirection.Ascending : SortDirection.Descending;
                OnPropertyChanged(nameof(IsSortedAscending));
                OnPropertyChanged(nameof(IsSortedDescending));
            }
        }

        public bool IsSortedDescending
        {
            get => !IsSortedAscending;
            set
            {
                folderSettings.DirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
                OnPropertyChanged(nameof(IsSortedAscending));
                OnPropertyChanged(nameof(IsSortedDescending));
            }
        }

        public ItemViewModel(FolderSettingsViewModel folderSettingsViewModel)
        {
            folderSettings = folderSettingsViewModel;
            filesAndFolders = new List<ListedItem>();
            FilesAndFolders = new BulkConcurrentObservableCollection<ListedItem>();
            operationQueue = new ConcurrentQueue<(uint Action, string FileName)>();
            itemLoadQueue = new ConcurrentDictionary<string, bool>();
            addFilesCTS = new CancellationTokenSource();
            semaphoreCTS = new CancellationTokenSource();
            loadPropsCTS = new CancellationTokenSource();
            operationEvent = new AsyncManualResetEvent();
            enumFolderSemaphore = new SemaphoreSlim(1, 1);
            shouldDisplayFileExtensions = UserSettingsService.PreferencesSettingsService.ShowFileExtensions;

            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
            FileTagsSettingsService.OnSettingImportedEvent += FileTagsSettingsService_OnSettingImportedEvent;
            AppServiceConnectionHelper.ConnectionChanged += AppServiceConnectionHelper_ConnectionChanged;
        }

        private async void FileTagsSettingsService_OnSettingImportedEvent(object sender, EventArgs e)
        {
            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
            {
                if (WorkingDirectory != "Home".GetLocalized())
                {
                    RefreshItems(null);
                }
            });
        }

        private async void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(UserSettingsService.PreferencesSettingsService.ShowFileExtensions):
                case nameof(UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible):
                case nameof(UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden):
                case nameof(UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled):
                case nameof(UserSettingsService.PreferencesSettingsService.ShowFolderSize):
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                    {
                        if (WorkingDirectory != "Home".GetLocalized())
                        {
                            RefreshItems(null);
                        }
                    });
                    break;
            }
        }

        private async void AppServiceConnectionHelper_ConnectionChanged(object sender, Task<NamedPipeAsAppServiceConnection> e)
        {
            Connection = await e;
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
            FilesAndFolders.Clear();
            CancelSearch();
        }

        public void CancelExtendedPropertiesLoading()
        {
            loadPropsCTS.Cancel();
            loadPropsCTS = new CancellationTokenSource();
        }

        public void CancelExtendedPropertiesLoadingForItem(ListedItem item)
        {
            itemLoadQueue.TryUpdate(item.ItemPath, true, false);
        }

        public async Task ApplySingleFileChangeAsync(ListedItem item)
        {
            var newIndex = filesAndFolders.IndexOf(item);
            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
            {
                FilesAndFolders.Remove(item);
                if (newIndex != -1)
                {
                    FilesAndFolders.Insert(Math.Min(newIndex, FilesAndFolders.Count), item);
                }
                if (folderSettings.DirectoryGroupOption != GroupOption.None)
                {
                    var key = FilesAndFolders.ItemGroupKeySelector?.Invoke(item);
                    var group = FilesAndFolders.GroupedCollection?.FirstOrDefault(x => x.Model.Key == key);
                    if (group != null)
                    {
                        group.OrderOne(list => SortingHelper.OrderFileList(list, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection), item);
                    }
                }
                UpdateEmptyTextType();
                DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
            });
        }

        private bool IsSearchResults { get; set; }

        private void UpdateEmptyTextType()
        {
            EmptyTextType = FilesAndFolders.Count == 0 ? (IsSearchResults ? EmptyTextType.NoSearchResultsFound : EmptyTextType.FolderEmpty) : EmptyTextType.None;
        }

        // apply changes immediately after manipulating on filesAndFolders completed
        public async Task ApplyFilesAndFoldersChangesAsync()
        {
            try
            {
                if (filesAndFolders == null || filesAndFolders.Count == 0)
                {
                    void ClearDisplay()
                    {
                        FilesAndFolders.Clear();
                        UpdateEmptyTextType();
                        DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
                    }
                    if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && CoreApplication.MainView.DispatcherQueue.HasThreadAccess)
                    {
                        ClearDisplay();
                    }
                    else
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(ClearDisplay);
                    }
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
                void ApplyChanges()
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

                    if (folderSettings.DirectoryGroupOption != GroupOption.None)
                    {
                        OrderGroups();
                    }
                }

                void UpdateUI()
                {
                    // trigger CollectionChanged with NotifyCollectionChangedAction.Reset
                    // once loading is completed so that UI can be updated
                    FilesAndFolders.EndBulkOperation();
                    UpdateEmptyTextType();
                    DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
                }

                if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && CoreApplication.MainView.DispatcherQueue.HasThreadAccess)
                {
                    await Task.Run(ApplyChanges);
                    UpdateUI();
                }
                else
                {
                    ApplyChanges();
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(UpdateUI);
                }
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
            }
        }

        private Task OrderFilesAndFoldersAsync()
        {
            // Sorting group contents is handled elsewhere
            if (folderSettings.DirectoryGroupOption != GroupOption.None)
            {
                return Task.CompletedTask;
            }

            void OrderEntries()
            {
                if (filesAndFolders.Count == 0)
                {
                    return;
                }

                filesAndFolders = SortingHelper.OrderFileList(filesAndFolders, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection).ToList();
            }

            if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && CoreApplication.MainView.DispatcherQueue.HasThreadAccess)
            {
                return Task.Run(OrderEntries);
            }
            else
            {
                OrderEntries();
                return Task.CompletedTask;
            }
        }

        private void OrderGroups(CancellationToken token = default)
        {
            var gps = FilesAndFolders.GroupedCollection?.Where(x => !x.IsSorted);
            if (gps is null)
            {
                return;
            }
            foreach (var gp in gps)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                gp.Order(list => SortingHelper.OrderFileList(list, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection));
            }
            if (!FilesAndFolders.GroupedCollection.IsSorted)
            {
                FilesAndFolders.GroupedCollection.Order(x => x.OrderBy(y => y.Model.SortIndexOverride).ThenBy(y => y.Model.Text));
                FilesAndFolders.GroupedCollection.IsSorted = true;
            }
        }

        public async Task GroupOptionsUpdated(CancellationToken token)
        {
            try
            {
                // Conflicts will occur if re-grouping is run while items are still being enumerated, so wait for enumeration to complete first
                await enumFolderSemaphore.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                FilesAndFolders.BeginBulkOperation();
                UpdateGroupOptions();
                if (FilesAndFolders.IsGrouped)
                {
                    await Task.Run(() =>
                    {
                        FilesAndFolders.ResetGroups(token);
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        OrderGroups();
                    });
                }
                else
                {
                    await OrderFilesAndFoldersAsync();
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    FilesAndFolders.EndBulkOperation();
                });
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
            }
            finally
            {
                enumFolderSemaphore.Release();
            }
        }

        public async Task ReloadItemGroupHeaderImagesAsync()
        {
            // this is needed to update the group icons for file type groups
            if (folderSettings.DirectoryGroupOption == GroupOption.FileType && FilesAndFolders.GroupedCollection != null)
            {
                await Task.Run(async () =>
                {
                    foreach (var gp in FilesAndFolders.GroupedCollection.ToList())
                    {
                        var img = await GetItemTypeGroupIcon(gp.FirstOrDefault());
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                        {
                            gp.Model.ImageSource = img;
                        }, Windows.System.DispatcherQueuePriority.Low);
                    }
                });
            }
        }

        public void UpdateGroupOptions()
        {
            FilesAndFolders.ItemGroupKeySelector = GroupingHelper.GetItemGroupKeySelector(folderSettings.DirectoryGroupOption);
            var groupInfoSelector = GroupingHelper.GetGroupInfoSelector(folderSettings.DirectoryGroupOption);
            FilesAndFolders.GetGroupHeaderInfo = groupInfoSelector.Item1;
            FilesAndFolders.GetExtendedGroupHeaderInfo = groupInfoSelector.Item2;
        }

        public Dictionary<string, BitmapImage> DefaultIcons = new Dictionary<string, BitmapImage>();

        private uint currentDefaultIconSize = 0;
        public async Task GetDefaultItemIcons(uint size)
        {
            if (currentDefaultIconSize != size)
            {
                // TODO: Add more than just the folder icon

                DefaultIcons.Clear();
                BitmapImage img = new BitmapImage();
                using var icon = await StorageItemIconHelpers.GetIconForItemType(size, IconPersistenceOptions.Persist);
                await img.SetSourceAsync(icon);
                DefaultIcons.Add(string.Empty, img);
                currentDefaultIconSize = size;
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
            }
        }

        private async Task LoadItemThumbnail(ListedItem item, uint thumbnailSize = 20, IStorageItem matchingStorageItem = null)
        {
            var wasIconLoaded = false;
            if (item.IsLibraryItem || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsZipItem)
            {
                if (!item.IsShortcutItem && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
                {
                    var matchingStorageFile = matchingStorageItem.AsBaseStorageFile() ?? await GetFileFromPathAsync(item.ItemPath);
                    if (matchingStorageFile != null)
                    {
                        var mode = thumbnailSize < 80 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

                        using var Thumbnail = await matchingStorageFile.GetThumbnailAsync(mode, thumbnailSize, ThumbnailOptions.ResizeThumbnail);
                        if (!(Thumbnail == null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
                        {
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                item.FileImage ??= new BitmapImage();
                                item.FileImage.DecodePixelType = DecodePixelType.Logical;
                                item.FileImage.DecodePixelWidth = (int)thumbnailSize;
                                await item.FileImage.SetSourceAsync(Thumbnail);
                                if (!string.IsNullOrEmpty(item.FileExtension) &&
                                    !item.IsShortcutItem && !item.IsExecutable &&
                                    !ImagePreviewViewModel.Extensions.Contains(item.FileExtension))
                                {
                                    DefaultIcons.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
                                }
                            }, Windows.System.DispatcherQueuePriority.Normal);
                            wasIconLoaded = true;
                        }

                        var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath);
                        if (overlayInfo != null)
                        {
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                item.IconOverlay = await overlayInfo.ToBitmapAsync();
                            }, Windows.System.DispatcherQueuePriority.Low);
                        }
                    }
                }

                if (!wasIconLoaded)
                {
                    var iconInfo = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize);
                    if (iconInfo.IconData != null)
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            item.FileImage = await iconInfo.IconData.ToBitmapAsync();
                            if (!string.IsNullOrEmpty(item.FileExtension) &&
                                !item.IsShortcutItem && !item.IsExecutable &&
                                !ImagePreviewViewModel.Extensions.Contains(item.FileExtension))
                            {
                                DefaultIcons.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
                            }
                        }, Windows.System.DispatcherQueuePriority.Low);
                    }

                    if (iconInfo.OverlayData != null)
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
                        }, Windows.System.DispatcherQueuePriority.Low);
                    }
                }
            }
            else
            {
                if (!item.IsShortcutItem && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
                {
                    var matchingStorageFolder = matchingStorageItem.AsBaseStorageFolder() ?? await GetFolderFromPathAsync(item.ItemPath);
                    if (matchingStorageFolder != null)
                    {
                        var mode = thumbnailSize < 80 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

                        using var Thumbnail = await matchingStorageFolder.GetThumbnailAsync(mode, thumbnailSize, ThumbnailOptions.ResizeThumbnail);
                        if (!(Thumbnail == null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
                        {
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                item.FileImage ??= new BitmapImage();
                                item.FileImage.DecodePixelType = DecodePixelType.Logical;
                                item.FileImage.DecodePixelWidth = (int)thumbnailSize;
                                await item.FileImage.SetSourceAsync(Thumbnail);
                            }, Windows.System.DispatcherQueuePriority.Normal);
                            wasIconLoaded = true;
                        }

                        var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath);
                        if (overlayInfo != null)
                        {
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                item.IconOverlay = await overlayInfo.ToBitmapAsync();
                            }, Windows.System.DispatcherQueuePriority.Low);
                        }
                    }
                }

                if (!wasIconLoaded)
                {
                    var iconInfo = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize);
                    if (iconInfo.IconData != null)
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            item.FileImage = await iconInfo.IconData.ToBitmapAsync();
                        }, Windows.System.DispatcherQueuePriority.Low);
                    }

                    if (iconInfo.OverlayData != null)
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
                        }, Windows.System.DispatcherQueuePriority.Low);
                    }
                }
            }
        }

        // This works for recycle bin as well as GetFileFromPathAsync/GetFolderFromPathAsync work
        // for file inside the recycle bin (but not on the recycle bin folder itself)
        public async Task LoadExtendedItemProperties(ListedItem item, uint thumbnailSize = 20)
        {
            if (item == null)
            {
                return;
            }

            itemLoadQueue[item.ItemPath] = false;

            var cts = loadPropsCTS;

            try
            {
                await Task.Run(async () =>
                {
                    if (itemLoadQueue.TryGetValue(item.ItemPath, out var canceled) && canceled)
                    {
                        return;
                    }

                    item.ItemPropertiesInitialized = true;
                    var wasSyncStatusLoaded = false;
                    ImageSource groupImage = null;
                    bool loadGroupHeaderInfo = false;
                    GroupedCollection<ListedItem> gp = null;
                    try
                    {
                        bool isFileTypeGroupMode = folderSettings.DirectoryGroupOption == GroupOption.FileType;
                        BaseStorageFile matchingStorageFile = null;
                        if (item.Key != null && FilesAndFolders.IsGrouped && FilesAndFolders.GetExtendedGroupHeaderInfo != null)
                        {
                            gp = FilesAndFolders.GroupedCollection.Where(x => x.Model.Key == item.Key).FirstOrDefault();
                            loadGroupHeaderInfo = !(gp is null) && !gp.Model.Initialized && !(gp.GetExtendedGroupHeaderInfo is null);
                        }

                        if (item.IsLibraryItem || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsZipItem)
                        {
                            if (!item.IsShortcutItem && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                matchingStorageFile = await GetFileFromPathAsync(item.ItemPath);
                                if (matchingStorageFile != null)
                                {
                                    cts.Token.ThrowIfCancellationRequested();
                                    await LoadItemThumbnail(item, thumbnailSize, matchingStorageFile);

                                    var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFile);
                                    var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFile);
                                    var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);

                                    cts.Token.ThrowIfCancellationRequested();
                                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        item.FolderRelativeId = matchingStorageFile.FolderRelativeId;
                                        item.ItemType = matchingStorageFile.DisplayType;
                                        item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                        item.FileFRN = fileFRN;
                                        item.FileTag = fileTag;
                                    }, Windows.System.DispatcherQueuePriority.Low);
                                    FileTagsHelper.DbInstance.SetTag(item.ItemPath, item.FileFRN, item.FileTag);
                                    wasSyncStatusLoaded = true;
                                }
                            }
                            if (!wasSyncStatusLoaded)
                            {
                                await LoadItemThumbnail(item, thumbnailSize, null);
                            }
                        }
                        else
                        {
                            if (!item.IsShortcutItem && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                BaseStorageFolder matchingStorageFolder = await GetFolderFromPathAsync(item.ItemPath);
                                if (matchingStorageFolder != null)
                                {
                                    cts.Token.ThrowIfCancellationRequested();
                                    await LoadItemThumbnail(item, thumbnailSize, matchingStorageFolder);
                                    if (matchingStorageFolder.DisplayName != item.ItemName && !matchingStorageFolder.DisplayName.StartsWith("$R", StringComparison.Ordinal))
                                    {
                                        cts.Token.ThrowIfCancellationRequested();
                                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                                        {
                                            item.ItemNameRaw = matchingStorageFolder.DisplayName;
                                        });
                                        await fileListCache.SaveFileDisplayNameToCache(item.ItemPath, matchingStorageFolder.DisplayName);
                                        if (folderSettings.DirectorySortOption == SortOption.Name && !isLoadingItems)
                                        {
                                            await OrderFilesAndFoldersAsync();
                                            await ApplySingleFileChangeAsync(item);
                                        }
                                    }

                                    cts.Token.ThrowIfCancellationRequested();
                                    var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
                                    var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFolder);
                                    var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);
                                    cts.Token.ThrowIfCancellationRequested();
                                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                                    {
                                        item.FolderRelativeId = matchingStorageFolder.FolderRelativeId;
                                        item.ItemType = matchingStorageFolder.DisplayType;
                                        item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                        item.FileFRN = fileFRN;
                                        item.FileTag = fileTag;
                                    }, Windows.System.DispatcherQueuePriority.Low);
                                    FileTagsHelper.DbInstance.SetTag(item.ItemPath, item.FileFRN, item.FileTag);
                                    wasSyncStatusLoaded = true;
                                }
                            }
                            if (!wasSyncStatusLoaded)
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                await LoadItemThumbnail(item, thumbnailSize, null);
                            }
                        }

                        if (loadGroupHeaderInfo && isFileTypeGroupMode)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            groupImage = await GetItemTypeGroupIcon(item, matchingStorageFile);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        if (!wasSyncStatusLoaded)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            await FilesystemTasks.Wrap(async () =>
                            {
                                var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);
                                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                                {
                                    item.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false }; // Reset cloud sync status icon
                                    item.FileTag = fileTag;
                                }, Windows.System.DispatcherQueuePriority.Low);
                                FileTagsHelper.DbInstance.SetTag(item.ItemPath, item.FileFRN, item.FileTag);
                            });
                        }

                        if (loadGroupHeaderInfo)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            await FilesystemTasks.Wrap(() => CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                            {
                                gp.Model.ImageSource = groupImage;
                                gp.InitializeExtendedGroupHeaderInfoAsync();
                            }));
                        }
                    }
                }, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                itemLoadQueue.TryRemove(item.ItemPath, out _);
            }
        }

        private async Task<ImageSource> GetItemTypeGroupIcon(ListedItem item, BaseStorageFile matchingStorageItem = null)
        {
            ImageSource groupImage = null;
            if (item.PrimaryItemAttribute != StorageItemTypes.Folder || item.IsZipItem)
            {
                var headerIconInfo = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(item.ItemPath, 64u);

                if (headerIconInfo != null && !item.IsShortcutItem)
                {
                    groupImage = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => headerIconInfo.ToBitmapAsync(), Windows.System.DispatcherQueuePriority.Low);
                }
                if (!item.IsShortcutItem && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
                {
                    if (groupImage == null) // Loading icon from fulltrust process failed
                    {
                        matchingStorageItem ??= await GetFileFromPathAsync(item.ItemPath);

                        if (matchingStorageItem != null)
                        {
                            using var headerThumbnail = await matchingStorageItem.GetThumbnailAsync(ThumbnailMode.DocumentsView, 36, ThumbnailOptions.UseCurrentScale);
                            if (headerThumbnail != null)
                            {
                                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                                {
                                    var bmp = new BitmapImage();
                                    await bmp.SetSourceAsync(headerThumbnail);
                                    groupImage = bmp;
                                });
                            }
                        }
                    }
                }
            }
            // This prevents both the shortcut glyph and folder icon being shown
            else if (!item.IsShortcutItem)
            {
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => groupImage = new SvgImageSource(new Uri("ms-appx:///Assets/FolderIcon2.svg"))
                {
                    RasterizePixelHeight = 128,
                    RasterizePixelWidth = 128,
                }, Windows.System.DispatcherQueuePriority.Low);
            }

            return groupImage;
        }

        public bool DisableAdaptiveLayout { get; set; }

        public void RefreshItems(string previousDir, Action postLoadCallback = null)
        {
            RapidAddItemsToCollectionAsync(WorkingDirectory, previousDir, postLoadCallback);
        }

        private async void RapidAddItemsToCollectionAsync(string path, string previousDir, Action postLoadCallback)
        {
            IsSearchResults = false;
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

                Connection ??= await AppServiceConnectionHelper.Instance;

                if (path.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal))
                {
                    if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library) && !library.IsEmpty)
                    {
                        var libItem = new LibraryItem(library);
                        foreach (var folder in library.Folders)
                        {
                            await RapidAddItemsToCollection(folder, libItem);
                        }
                    }
                }
                else
                {
                    await RapidAddItemsToCollection(path);
                }

                ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete, PreviousDirectory = previousDir, Path = path });
                IsLoadingItems = false;

                if (!DisableAdaptiveLayout)
                {
                    AdaptiveLayoutHelpers.PredictLayoutMode(folderSettings, this);
                }

                if (UserSettingsService.PreviewPaneSettingsService.PreviewPaneEnabled)
                {
                    // Find and select README file
                    foreach (var item in filesAndFolders)
                    {
                        if (item.PrimaryItemAttribute == StorageItemTypes.File && item.ItemName.Contains("readme", StringComparison.OrdinalIgnoreCase))
                        {
                            OnSelectionRequestedEvent?.Invoke(this, new List<ListedItem>() { item });
                            break;
                        }
                    }
                }
            }
            finally
            {
                DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty); // Make sure item count is updated
                enumFolderSemaphore.Release();
            }

            postLoadCallback?.Invoke();
        }

        private async Task RapidAddItemsToCollection(string path, LibraryItem library = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            await GetDefaultItemIcons(folderSettings.GetIconSize());

            var isRecycleBin = path.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal);
            if (isRecycleBin ||
                path.StartsWith(CommonPaths.NetworkFolderPath, StringComparison.Ordinal) ||
                FtpHelpers.IsFtpPath(path))
            {
                // Recycle bin and network are enumerated by the fulltrust process
                PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false, IsTypeRecycleBin = isRecycleBin });
                await EnumerateItemsFromSpecialFolderAsync(path);
            }
            else
            {
                var enumerated = await EnumerateItemsFromStandardFolderAsync(path, folderSettings.GetLayoutType(path, false), addFilesCTS.Token, library);
                IsLoadingItems = false; // Hide progressbar after enumeration
                switch (enumerated)
                {
                    case 0: // Enumerated with FindFirstFileExFromApp
                        // Is folder synced to cloud storage?
                        currentStorageFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
                        var syncStatus = await CheckCloudDriveSyncStatusAsync(currentStorageFolder?.Item);
                        PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown });

                        WatchForDirectoryChanges(path, syncStatus);
                        break;

                    case 1: // Enumerated with StorageFolder
                        PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false });
                        currentStorageFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
                        WatchForStorageFolderChanges(currentStorageFolder?.Folder);
                        break;

                    case -1: // Enumeration failed
                    default:
                        break;
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

        private void AssignDefaultIcons()
        {
            foreach (string key in DefaultIcons.Keys)
            {
                if (string.IsNullOrEmpty(key))
                {
                    var icon = DefaultIcons[key];
                    var folders = FilesAndFolders.Where(x => x.PrimaryItemAttribute == StorageItemTypes.Folder);
                    foreach (ListedItem folder in folders)
                    {
                        folder.SetDefaultIcon(icon);
                    }
                }
                else
                {
                    var icon = DefaultIcons[key];
                    var filesMatching = FilesAndFolders.Where(x => key.Equals(x.FileExtension?.ToLowerInvariant()));
                    foreach (ListedItem file in filesMatching)
                    {
                        file.SetDefaultIcon(icon);
                    }
                }
            }
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
            if (watchedItemsOperation != null)
            {
                itemQueryResult.ContentsChanged -= ItemQueryResult_ContentsChanged;
                watchedItemsOperation?.Cancel();
                watchedItemsOperation = null;
            }
        }

        public async Task EnumerateItemsFromSpecialFolderAsync(string path)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            bool isFtp = FtpHelpers.IsFtpPath(path);

            CurrentFolder = new ListedItem(null, returnformat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemPropertiesInitialized = true,
                ItemNameRaw = path.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal) ? ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin") :
                           path.StartsWith(CommonPaths.NetworkFolderPath, StringComparison.Ordinal) ? "Network".GetLocalized() : isFtp ? "FTP" : "Unknown",
                ItemDateModifiedReal = DateTimeOffset.Now, // Fake for now
                ItemDateCreatedReal = DateTimeOffset.Now, // Fake for now
                ItemType = "FileFolderListItem".GetLocalized(),
                FileImage = null,
                LoadFileIcon = false,
                ItemPath = path,
                FileSize = null,
                FileSizeBytes = 0
            };

            if (Connection != null && !isFtp)
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
                            if (count == folderContentsList.Count - 1 || sampler.CheckNow())
                            {
                                await OrderFilesAndFoldersAsync();
                                await ApplyFilesAndFoldersChangesAsync();
                            }
                        }
                    }
                });
            }
            else if (isFtp)
            {
                if (!FtpHelpers.VerifyFtpPath(path))
                {
                    // TODO: show invalid path dialog
                    return;
                }

                using var client = new FtpClient();
                client.Host = FtpHelpers.GetFtpHost(path);
                client.Port = FtpHelpers.GetFtpPort(path);
                client.Credentials = FtpManager.Credentials.Get(client.Host, FtpManager.Anonymous);

                static async Task<FtpProfile> WrappedAutoConnectFtpAsync(FtpClient client)
                {
                    try
                    {
                        return await client.AutoConnectAsync();
                    }
                    catch (FtpAuthenticationException)
                    {
                        return null;
                    }

                    throw new InvalidOperationException();
                }

                await Task.Run(async () =>
                {
                    try
                    {
                        if (!client.IsConnected && await WrappedAutoConnectFtpAsync(client) is null)
                        {
                            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(async () =>
                            {
                                var dialog = new CredentialDialog();

                                if (await dialog.TryShowAsync() == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                                {
                                    var result = await dialog.Result;

                                    if (!result.Anonymous)
                                    {
                                        client.Credentials = new NetworkCredential(result.UserName, result.Password);
                                    }
                                }
                                else
                                {
                                    return;
                                }
                            });
                        }
                        if (!client.IsConnected && await WrappedAutoConnectFtpAsync(client) is null)
                        {
                            throw new InvalidOperationException();
                        }
                        FtpManager.Credentials[client.Host] = client.Credentials;

                        var sampler = new IntervalSampler(500);
                        var list = await client.GetListingAsync(FtpHelpers.GetFtpPath(path));

                        for (var i = 0; i < list.Length; i++)
                        {
                            filesAndFolders.Add(new FtpItem(list[i], path, returnformat));

                            if (i == list.Length - 1 || sampler.CheckNow())
                            {
                                await OrderFilesAndFoldersAsync();
                                await ApplyFilesAndFoldersChangesAsync();
                            }
                        }
                    }
                    catch
                    {
                        // network issue
                        FtpManager.Credentials.Remove(client.Host);
                    }
                });
            }
        }

        public async Task<int> EnumerateItemsFromStandardFolderAsync(string path, Type sourcePageType, CancellationToken cancellationToken, LibraryItem library = null)
        {
            // Flag to use FindFirstFileExFromApp or StorageFolder enumeration
            bool enumFromStorageFolder =
                path == App.CloudDrivesManager.Drives.FirstOrDefault(x => x.Text == "Box")?.Path?.TrimEnd('\\'); // Use storage folder for Box Drive (#4629)

            BaseStorageFolder rootFolder = null;

            if (!enumFromStorageFolder && FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                // Will enumerate with FindFirstFileExFromApp, rootFolder only used for Bitlocker
                currentStorageFolder = null;
            }
            else if (workingRoot != null)
            {
                var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, workingRoot, currentStorageFolder));
                if (!res)
                {
                    return -1;
                }
                currentStorageFolder = res.Result;
                rootFolder = currentStorageFolder.Folder;
                enumFromStorageFolder = true;
            }
            else
            {
                var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
                if (res)
                {
                    currentStorageFolder = res.Result;
                    rootFolder = currentStorageFolder.Folder;
                }
                else if (res == FileSystemStatusCode.Unauthorized)
                {
                    //TODO: proper dialog
                    await DialogDisplayHelper.ShowDialogAsync(
                        "AccessDenied".GetLocalized(),
                        "SubDirectoryAccessDenied".GetLocalized());
                    return -1;
                }
                else if (res == FileSystemStatusCode.NotFound)
                {
                    await DialogDisplayHelper.ShowDialogAsync(
                        "FolderNotFoundDialog/Title".GetLocalized(),
                        "FolderNotFoundDialog/Text".GetLocalized());
                    return -1;
                }
                else
                {
                    await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalized(), res.ErrorCode.ToString());
                    return -1;
                }
            }

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            if (Path.IsPathRooted(path) && Path.GetPathRoot(path) == path)
            {
                rootFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
                if (await FolderHelpers.CheckBitlockerStatusAsync(rootFolder, WorkingDirectory))
                {
                    if (Connection != null)
                    {
                        var value = new ValueSet();
                        value.Add("Arguments", "InvokeVerb");
                        value.Add("FilePath", Path.GetPathRoot(path));
                        value.Add("Verb", "unlock-bde");
                        _ = await Connection.SendMessageForResponseAsync(value);
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
                    ItemNameRaw = rootFolder.DisplayName,
                    ItemDateModifiedReal = basicProps.DateModified,
                    ItemType = rootFolder.DisplayType,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(rootFolder.Path) ? currentStorageFolder.Path : rootFolder.Path,
                    FileSize = null,
                    FileSizeBytes = 0,
                };
                if (library == null)
                {
                    currentFolder.ItemDateCreatedReal = rootFolder.DateCreated;
                }
                CurrentFolder = currentFolder;
                await EnumFromStorageFolderAsync(path, currentFolder, rootFolder, currentStorageFolder, sourcePageType, cancellationToken);
                return 1;
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
                    itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

                    FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedTimeOutput);
                    itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
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
                    ItemNameRaw = Path.GetFileName(path.TrimEnd('\\')),
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = folderTypeTextLocalized,
                    FileImage = null,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    LoadFileIcon = false,
                    ItemPath = path,
                    FileSize = null,
                    FileSizeBytes = 0,
                };
                CurrentFolder = currentFolder;

                if (hFile == IntPtr.Zero)
                {
                    await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalized(), "");
                    return -1;
                }
                else if (hFile.ToInt64() == -1)
                {
                    await EnumFromStorageFolderAsync(path, currentFolder, rootFolder, currentStorageFolder, sourcePageType, cancellationToken);
                    return 1;
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        List<ListedItem> fileList = await Win32StorageEnumerator.ListEntries(path, returnformat, hFile, findData, Connection, cancellationToken, -1, intermediateAction: async (intermediateList) =>
                        {
                            filesAndFolders.AddRange(intermediateList);
                            await OrderFilesAndFoldersAsync();
                            await ApplyFilesAndFoldersChangesAsync();
                        }, defaultIconPairs: DefaultIcons);

                        filesAndFolders.AddRange(fileList);
                        await OrderFilesAndFoldersAsync();
                        await ApplyFilesAndFoldersChangesAsync();
                    });

                    return 0;
                }
            }
        }

        private async Task EnumFromStorageFolderAsync(string path, ListedItem currentFolder, BaseStorageFolder rootFolder, StorageFolderWithPath currentStorageFolder, Type sourcePageType, CancellationToken cancellationToken)
        {
            if (rootFolder == null)
            {
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            await Task.Run(async () =>
            {
                List<ListedItem> finalList = await UniversalStorageEnumerator.ListEntries(
                    rootFolder,
                    currentStorageFolder,
                    returnformat,
                    sourcePageType,
                    cancellationToken,
                    -1,
                    async (intermediateList) =>
                {
                    filesAndFolders.AddRange(intermediateList);
                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();
                }, defaultIconPairs: DefaultIcons);
                filesAndFolders.AddRange(finalList);
                await OrderFilesAndFoldersAsync();
                await ApplyFilesAndFoldersChangesAsync();
            });

            stopwatch.Stop();
            Debug.WriteLine($"Enumerating items in {path} (device) completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
        }

        private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatusAsync(IStorageItem item)
        {
            int? syncStatus = null;
            if (item is BaseStorageFile file && file.Properties != null)
            {
                var extraProperties = await FilesystemTasks.Wrap(() => file.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus" }).AsTask());
                if (extraProperties)
                {
                    syncStatus = (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
                }
            }
            else if (item is BaseStorageFolder folder && folder.Properties != null)
            {
                var extraProperties = await FilesystemTasks.Wrap(() => folder.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus", "System.FileOfflineAvailabilityStatus" }).AsTask());
                if (extraProperties)
                {
                    syncStatus = (int?)(uint?)extraProperties.Result["System.FileOfflineAvailabilityStatus"];
                    // If no FileOfflineAvailabilityStatus, check FilePlaceholderStatus
                    syncStatus = syncStatus ?? (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
                }
            }
            if (syncStatus == null || !Enum.IsDefined(typeof(CloudDriveSyncStatus), syncStatus))
            {
                return CloudDriveSyncStatus.Unknown;
            }
            return (CloudDriveSyncStatus)syncStatus;
        }

        private StorageItemQueryResult itemQueryResult;

        private IAsyncOperation<IReadOnlyList<IStorageItem>> watchedItemsOperation;

        private async void WatchForStorageFolderChanges(BaseStorageFolder rootFolder)
        {
            if (rootFolder == null)
            {
                return;
            }
            await Task.Run(() =>
            {
                var options = new QueryOptions()
                {
                    FolderDepth = FolderDepth.Shallow,
                    IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
                };
                options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
                options.SetThumbnailPrefetch(ThumbnailMode.ListView, 0, ThumbnailOptions.ReturnOnlyIfCached);
                if (rootFolder.AreQueryOptionsSupported(options))
                {
                    itemQueryResult = rootFolder.CreateItemQueryWithOptions(options).ToStorageItemQueryResult();
                    itemQueryResult.ContentsChanged += ItemQueryResult_ContentsChanged;
                    watchedItemsOperation = itemQueryResult.GetItemsAsync(0, 1); // Just get one item to start getting notifications
                }
            });
        }

        private async void ItemQueryResult_ContentsChanged(IStorageQueryResultBase sender, object args)
        {
            //query options have to be reapplied otherwise old results are returned
            var options = new QueryOptions()
            {
                FolderDepth = FolderDepth.Shallow,
                IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
            };
            options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
            options.SetThumbnailPrefetch(ThumbnailMode.ListView, 0, ThumbnailOptions.ReturnOnlyIfCached);

            sender.ApplyNewQueryOptions(options);

            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
            {
                RefreshItems(null);
            });
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

            var hasSyncStatus = syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown;

            var cts = new CancellationTokenSource();
            _ = Windows.System.Threading.ThreadPool.RunAsync((x) => ProcessOperationQueue(cts.Token, hasSyncStatus));

            aWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
            {
                byte[] buff = new byte[4096];
                var rand = Guid.NewGuid();
                buff = new byte[4096];
                int notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE;

                if (hasSyncStatus)
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

                                offset += notifyInfo.NextEntryOffset;
                            } while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

                            operationEvent.Set();

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

        private async void ProcessOperationQueue(CancellationToken cancellationToken, bool hasSyncStatus)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            const uint FILE_ACTION_ADDED = 0x00000001;
            const uint FILE_ACTION_REMOVED = 0x00000002;
            const uint FILE_ACTION_MODIFIED = 0x00000003;
            const uint FILE_ACTION_RENAMED_OLD_NAME = 0x00000004;
            const uint FILE_ACTION_RENAMED_NEW_NAME = 0x00000005;

            const int UPDATE_BATCH_SIZE = 32;
            var sampler = new IntervalSampler(200);
            var updateQueue = new Queue<string>();
            bool anyEdits = false;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (await operationEvent.WaitAsync(200, cancellationToken))
                    {
                        operationEvent.Reset();

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
                                        anyEdits = true;
                                        break;

                                    case FILE_ACTION_MODIFIED:
                                        if (!updateQueue.Contains(operation.FileName))
                                        {
                                            updateQueue.Enqueue(operation.FileName);
                                        }
                                        break;

                                    case FILE_ACTION_REMOVED:
                                    case FILE_ACTION_RENAMED_OLD_NAME:
                                        await RemoveFileOrFolderAsync(operation.FileName);
                                        anyEdits = true;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                App.Logger.Warn(ex, ex.Message);
                            }

                            if (anyEdits && sampler.CheckNow())
                            {
                                await OrderFilesAndFoldersAsync();
                                await ApplyFilesAndFoldersChangesAsync();
                                anyEdits = false;
                            }
                        }

                        var itemsToUpdate = new List<string>();
                        for (var i = 0; i < UPDATE_BATCH_SIZE && updateQueue.Count > 0; i++)
                        {
                            itemsToUpdate.Add(updateQueue.Dequeue());
                        }

                        await UpdateFilesOrFoldersAsync(itemsToUpdate, hasSyncStatus);
                    }


                    if (updateQueue.Count > 0)
                    {
                        var itemsToUpdate = new List<string>();
                        for (var i = 0; i < UPDATE_BATCH_SIZE && updateQueue.Count > 0; i++)
                        {
                            itemsToUpdate.Add(updateQueue.Dequeue());
                        }

                        await UpdateFilesOrFoldersAsync(itemsToUpdate, hasSyncStatus);
                    }

                    if (anyEdits && sampler.CheckNow())
                    {
                        await OrderFilesAndFoldersAsync();
                        await ApplyFilesAndFoldersChangesAsync();
                        anyEdits = false;
                    }
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
                var binItem = new RecycleBinItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemNameRaw = item.FileName,
                    ItemDateModifiedReal = item.ModifiedDate,
                    ItemDateCreatedReal = item.CreatedDate,
                    ItemDateDeletedReal = item.RecycleDate,
                    ItemType = item.FileType,
                    IsHiddenItem = false,
                    Opacity = 1,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = item.RecyclePath, // this is the true path on disk so other stuff can work as is
                    ItemOriginalPath = item.FilePath,
                    FileSize = null,
                    FileSizeBytes = 0,
                    //FolderTooltipText = tooltipString,
                };
                if (DefaultIcons.ContainsKey(string.Empty))
                {
                    binItem.SetDefaultIcon(DefaultIcons[string.Empty]);
                }
                return binItem;
            }
            else
            {
                // File
                string itemName = item.FileName;
                string itemFileExtension = null;
                if (item.FileName.Contains('.'))
                {
                    itemFileExtension = Path.GetExtension(item.FileName);
                }
                var binItem = new RecycleBinItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    FileImage = null,
                    LoadFileIcon = false,
                    IsHiddenItem = false,
                    Opacity = 1,
                    ItemNameRaw = itemName,
                    ItemDateModifiedReal = item.ModifiedDate,
                    ItemDateCreatedReal = item.CreatedDate,
                    ItemDateDeletedReal = item.RecycleDate,
                    ItemType = item.FileType,
                    ItemPath = item.RecyclePath, // this is the true path on disk so other stuff can work as is
                    ItemOriginalPath = item.FilePath,
                    FileSize = item.FileSize,
                    FileSizeBytes = (long)item.FileSizeBytes
                };
                if (!string.IsNullOrEmpty(binItem?.FileExtension))
                {
                    var lowercaseExt = binItem.FileExtension.ToLowerInvariant();
                    if (DefaultIcons.ContainsKey(lowercaseExt))
                    {
                        binItem.SetDefaultIcon(DefaultIcons[lowercaseExt]);
                    }
                }
                return binItem;
            }
        }

        private async Task AddFileOrFolderAsync(ListedItem item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            filesAndFolders.Add(item);
            enumFolderSemaphore.Release();
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
            if (isHidden && (!UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible || (isSystem && UserSettingsService.PreferencesSettingsService.AreSystemItemsHidden)))
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

            await AddFileOrFolderAsync(listedItem);
        }

        private async Task<(ListedItem Item, CloudDriveSyncStatus? SyncStatus, long? Size, DateTimeOffset Created, DateTimeOffset Modified)?> GetFileOrFolderUpdateInfoAsync(ListedItem item, bool hasSyncStatus)
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
                CloudDriveSyncStatus? syncStatus = hasSyncStatus ? await CheckCloudDriveSyncStatusAsync(storageItem) : null;
                long? size = null;
                DateTimeOffset created = default, modified = default;

                if (storageItem.IsOfType(StorageItemTypes.File))
                {
                    var properties = await storageItem.AsBaseStorageFile().GetBasicPropertiesAsync();
                    size = (long)properties.Size;
                    modified = properties.DateModified;
                    created = properties.ItemDate;
                }
                else if (storageItem.IsOfType(StorageItemTypes.Folder))
                {
                    var properties = await storageItem.AsBaseStorageFolder().GetBasicPropertiesAsync();
                    modified = properties.DateModified;
                    created = properties.ItemDate;
                }

                return (item, syncStatus, size, created, modified);
            }

            return null;
        }

        private async Task UpdateFilesOrFoldersAsync(IEnumerable<string> paths, bool hasSyncStatus)
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
                var matchingItems = filesAndFolders.Where(x => paths.Any(p => p.Equals(x.ItemPath, StringComparison.OrdinalIgnoreCase)));
                var results = await Task.WhenAll(matchingItems.Select(x => GetFileOrFolderUpdateInfoAsync(x, hasSyncStatus)));

                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    foreach (var result in results)
                    {
                        if (result != null)
                        {
                            var item = result.Value.Item;
                            item.ItemDateModifiedReal = result.Value.Modified;
                            item.ItemDateCreatedReal = result.Value.Created;

                            if (result.Value.SyncStatus != null)
                            {
                                item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(result.Value.SyncStatus.Value);
                            }

                            if (result.Value.Size != null)
                            {
                                item.FileSizeBytes = result.Value.Size.Value;
                                item.FileSize = item.FileSizeBytes.ToSizeString();
                            }
                        }
                    }
                }, Windows.System.DispatcherQueuePriority.Low);
            }
            finally
            {
                enumFolderSemaphore.Release();
            }
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
                var matchingItem = filesAndFolders.FirstOrDefault(x => x.ItemPath.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (matchingItem != null)
                {
                    filesAndFolders.Remove(matchingItem);
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

        private CancellationTokenSource searchCancellationToken;

        public async Task SearchAsync(FolderSearch search)
        {
            ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting });

            CancelSearch();
            searchCancellationToken = new CancellationTokenSource();
            filesAndFolders.Clear();
            IsLoadingItems = true;
            IsSearchResults = true;
            await ApplyFilesAndFoldersChangesAsync();
            EmptyTextType = EmptyTextType.None;

            ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress });

            var results = new List<ListedItem>();
            search.SearchTick += async (s, e) =>
            {
                filesAndFolders = new List<ListedItem>(results);
                await OrderFilesAndFoldersAsync();
                await ApplyFilesAndFoldersChangesAsync();
            };
            await search.SearchAsync(results, searchCancellationToken.Token);

            filesAndFolders = new List<ListedItem>(results);
            await OrderFilesAndFoldersAsync();
            await ApplyFilesAndFoldersChangesAsync();

            ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete });
            IsLoadingItems = false;
        }

        public void CancelSearch()
        {
            searchCancellationToken?.Cancel();
        }

        public void Dispose()
        {
            CancelLoadAndClearFiles();
            if (Connection != null)
            {
                Connection.RequestReceived -= Connection_RequestReceived;
            }
            UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
            FileTagsSettingsService.OnSettingImportedEvent -= FileTagsSettingsService_OnSettingImportedEvent;
            AppServiceConnectionHelper.ConnectionChanged -= AppServiceConnectionHelper_ConnectionChanged;
            DefaultIcons.Clear();
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
