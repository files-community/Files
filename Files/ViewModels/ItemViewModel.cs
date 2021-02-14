using ByteSizeLib;
using Files.Common;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.Cloud;
using Files.Helpers;
using Files.Helpers.FileListCache;
using Files.Views.LayoutModes;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.Helpers;
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
using Windows.UI.Text;
using Windows.UI.Xaml;
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
        private IShellPage AssociatedInstance = null;
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
        public FolderSettingsViewModel FolderSettings => AssociatedInstance?.InstanceViewModel.FolderSettings;
        private bool shouldDisplayFileExtensions = false;
        public ListedItem CurrentFolder { get; private set; }
        public CollectionViewSource viewSource;
        private CancellationTokenSource addFilesCTS, semaphoreCTS, loadPropsCTS;
        private StorageFolder rootFolder;

        public event PropertyChangedEventHandler PropertyChanged;

        private string jumpString = "";
        private readonly DispatcherTimer jumpTimer = new DispatcherTimer();

        private string customPath;

        private IFileListCache fileListCache = FileListCacheController.GetInstance();

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

        public async Task<FilesystemResult> SetWorkingDirectoryAsync(string value)
        {
            var navigated = (FilesystemResult)true;
            if (string.IsNullOrWhiteSpace(value))
            {
                return new FilesystemResult(FileSystemStatusCode.NotAFolder);
            }

            WorkingDirectoryModified?.Invoke(this, new WorkingDirectoryModifiedEventArgs() { Path = value });

            if (!Path.IsPathRooted(value))
            {
                workingRoot = null;
                currentStorageFolder = null;
                customPath = value;
            }
            else if (!Path.IsPathRooted(WorkingDirectory) || Path.GetPathRoot(WorkingDirectory) != Path.GetPathRoot(value))
            {
                workingRoot = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(value));
            }

            if (Path.IsPathRooted(value))
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
            get => FolderSettings.DirectorySortOption == SortOption.Name;
            set
            {
                if (value)
                {
                    FolderSettings.DirectorySortOption = SortOption.Name;
                    NotifyPropertyChanged(nameof(IsSortedByName));
                }
            }
        }

        public bool IsSortedByOriginalPath
        {
            get => FolderSettings.DirectorySortOption == SortOption.OriginalPath;
            set
            {
                if (value)
                {
                    FolderSettings.DirectorySortOption = SortOption.OriginalPath;
                    NotifyPropertyChanged(nameof(IsSortedByOriginalPath));
                }
            }
        }

        public bool IsSortedByDateDeleted
        {
            get => FolderSettings.DirectorySortOption == SortOption.DateDeleted;
            set
            {
                if (value)
                {
                    FolderSettings.DirectorySortOption = SortOption.DateDeleted;
                    NotifyPropertyChanged(nameof(IsSortedByDateDeleted));
                }
            }
        }

        public bool IsSortedByDate
        {
            get => FolderSettings.DirectorySortOption == SortOption.DateModified;
            set
            {
                if (value)
                {
                    FolderSettings.DirectorySortOption = SortOption.DateModified;
                    NotifyPropertyChanged(nameof(IsSortedByDate));
                }
            }
        }

        public bool IsSortedByType
        {
            get => FolderSettings.DirectorySortOption == SortOption.FileType;
            set
            {
                if (value)
                {
                    FolderSettings.DirectorySortOption = SortOption.FileType;
                    NotifyPropertyChanged(nameof(IsSortedByType));
                }
            }
        }

        public bool IsSortedBySize
        {
            get => FolderSettings.DirectorySortOption == SortOption.Size;
            set
            {
                if (value)
                {
                    FolderSettings.DirectorySortOption = SortOption.Size;
                    NotifyPropertyChanged(nameof(IsSortedBySize));
                }
            }
        }

        public bool IsSortedAscending
        {
            get => FolderSettings.DirectorySortDirection == SortDirection.Ascending;
            set
            {
                FolderSettings.DirectorySortDirection = value ? SortDirection.Ascending : SortDirection.Descending;
                NotifyPropertyChanged(nameof(IsSortedAscending));
                NotifyPropertyChanged(nameof(IsSortedDescending));
            }
        }

        public bool IsSortedDescending
        {
            get => !IsSortedAscending;
            set
            {
                FolderSettings.DirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
                NotifyPropertyChanged(nameof(IsSortedAscending));
                NotifyPropertyChanged(nameof(IsSortedDescending));
            }
        }

        public string JumpString
        {
            get => jumpString;
            set
            {
                // If current string is "a", and the next character typed is "a",
                // search for next file that starts with "a" (a.k.a. _jumpString = "a")
                if (jumpString.Length == 1 && value == jumpString + jumpString)
                {
                    value = jumpString;
                }
                if (value != "")
                {
                    ListedItem jumpedToItem = null;
                    ListedItem previouslySelectedItem = null;

                    // use FilesAndFolders because only displayed entries should be jumped to
                    var candidateItems = FilesAndFolders.Where(f => f.ItemName.Length >= value.Length && f.ItemName.Substring(0, value.Length).ToLower() == value);

                if (AssociatedInstance.ContentPage != null && AssociatedInstance.ContentPage.IsItemSelected)
                {
                    previouslySelectedItem = AssociatedInstance.ContentPage.SelectedItem;
                }

                    // If the user is trying to cycle through items
                    // starting with the same letter
                    if (value.Length == 1 && previouslySelectedItem != null)
                    {
                        // Try to select item lexicographically bigger than the previous item
                        jumpedToItem = candidateItems.FirstOrDefault(f => f.ItemName.CompareTo(previouslySelectedItem.ItemName) > 0);
                    }
                    if (jumpedToItem == null)
                    {
                        jumpedToItem = candidateItems.FirstOrDefault();
                    }

                    if (jumpedToItem != null)
                    {
                        AssociatedInstance.ContentPage.SetSelectedItemOnUi(jumpedToItem);
                        AssociatedInstance.ContentPage.ScrollIntoView(jumpedToItem);
                    }

                    // Restart the timer
                    jumpTimer.Start();
                }
                jumpString = value;
            }
        }

        public AppServiceConnection Connection => AssociatedInstance?.ServiceConnection;

        public ItemViewModel(IShellPage appInstance)
        {
            AssociatedInstance = appInstance;
            filesAndFolders = new List<ListedItem>();
            FilesAndFolders = new BulkConcurrentObservableCollection<ListedItem>();
            addFilesCTS = new CancellationTokenSource();
            semaphoreCTS = new CancellationTokenSource();
            loadPropsCTS = new CancellationTokenSource();
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;
            jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
            jumpTimer.Tick += JumpTimer_Tick;
        }

        public void OnAppServiceConnectionChanged()
        {
            if (Connection != null)
            {
                Connection.RequestReceived += Connection_RequestReceived;
            }
        }

        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            var messageDeferral = args.GetDeferral();

            // The fulltrust process signaled that something in the recycle bin folder has changed
            if (args.Request.Message.ContainsKey("FileSystem"))
            {
                var folderPath = (string)args.Request.Message["FileSystem"];
                var itemPath = (string)args.Request.Message["Path"];
                var changeType = (string)args.Request.Message["Type"];
                var newItem = JsonConvert.DeserializeObject<ShellFileItem>(args.Request.Message.Get("Item", ""));
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
                            }
                            break;

                        case "Deleted":
                            await RemoveFileOrFolderAsync(itemPath);
                            break;

                        default:
                            await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                            {
                                RefreshItems(null);
                            });
                            break;
                    }
                }
            }
            // The fulltrust process signaled that a drive has been connected/disconnected
            else if (args.Request.Message.ContainsKey("DeviceID"))
            {
                var deviceId = (string)args.Request.Message["DeviceID"];
                var eventType = (DeviceEvent)(int)args.Request.Message["EventType"];
                await App.DrivesManager.HandleWin32DriveEvent(eventType, deviceId);
            }
            // Complete the deferral so that the platform knows that we're done responding to the app service call.
            // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
            messageDeferral.Complete();
        }

        private void JumpTimer_Tick(object sender, object e)
        {
            jumpString = "";
            jumpTimer.Stop();
        }

        /*
         * Ensure that the path bar gets updated for user interaction
         * whenever the path changes. We will get the individual directories from
         * the updated, most-current path and add them to the UI.
         */

        private void WorkingDirectoryChanged(string singleItemOverride = null)
        {
            // Clear the path UI
            AssociatedInstance.NavigationToolbar.PathComponents.Clear();
            // Style tabStyleFixed = App.selectedTabInstance.accessiblePathTabView.Resources["PathSectionTabStyle"] as Style;
            FontWeight weight = new FontWeight()
            {
                Weight = FontWeights.SemiBold.Weight
            };

            if (string.IsNullOrWhiteSpace(singleItemOverride))
            {
                foreach (var component in StorageFileExtensions.GetDirectoryPathComponents(WorkingDirectory))
                {
                    AssociatedInstance.NavigationToolbar.PathComponents.Add(component);
                }
            }
            else
            {
                AssociatedInstance.NavigationToolbar.PathComponents.Add(new Views.PathBoxItem() { Path = null, Title = singleItemOverride });
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
            loadPropsCTS.Dispose();
            loadPropsCTS = new CancellationTokenSource();
        }

        // apply changes immediately after manipulating on filesAndFolders completed
        public async Task ApplyFilesAndFoldersChangesAsync()
        {
            try
            {
                if (filesAndFolders == null || filesAndFolders.Count == 0)
                {
                    await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                    {
                        FilesAndFolders.Clear();
                        IsFolderEmptyTextDisplayed = true;
                        UpdateDirectoryInfo();
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

                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                {
                    // trigger CollectionChanged with NotifyCollectionChangedAction.Reset
                    // once loading is completed so that UI can be updated
                    FilesAndFolders.EndBulkOperation();
                    IsFolderEmptyTextDisplayed = FilesAndFolders.Count == 0;
                    UpdateDirectoryInfo();
                });
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
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
                switch (FolderSettings.DirectorySortOption)
                {
                    case SortOption.Name:
                        orderFunc = orderByNameFunc;
                        break;

                    case SortOption.DateModified:
                        orderFunc = item => item.ItemDateModifiedReal;
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

                if (FolderSettings.DirectorySortDirection == SortDirection.Ascending)
                {
                    if (FolderSettings.DirectorySortOption == SortOption.Name)
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
                    if (FolderSettings.DirectorySortOption == SortOption.Name)
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
                if (FolderSettings.DirectorySortOption != SortOption.Name)
                {
                    if (FolderSettings.DirectorySortDirection == SortDirection.Ascending)
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
                catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
                {
                    return;
                }
                var wasSyncStatusLoaded = false;
                try
                {
                    if (item.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        var fileIconInfo = await LoadIconOverlayAsync(item.ItemPath, thumbnailSize);

                        await CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                        {
                            if (fileIconInfo.IconData != null && !item.IsLinkItem)
                            {
                                item.FileImage = await fileIconInfo.IconData.ToBitmapAsync();
                                item.LoadUnknownTypeGlyph = false;
                                item.LoadFileIcon = true;
                            }
                            item.IconOverlay = await fileIconInfo.OverlayData.ToBitmapAsync();
                        }, Windows.UI.Core.CoreDispatcherPriority.Low);
                        if (!item.IsShortcutItem && !item.IsHiddenItem)
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
                                            await CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                                            {
                                                item.FileImage = new BitmapImage();
                                                await item.FileImage.SetSourceAsync(Thumbnail);
                                                item.LoadUnknownTypeGlyph = false;
                                                item.LoadFileIcon = true;
                                            });
                                        }
                                    }
                                }

                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageItem);
                                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                                {
                                    item.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                    item.ItemType = matchingStorageItem.DisplayType;
                                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                }, Windows.UI.Core.CoreDispatcherPriority.Low);
                                wasSyncStatusLoaded = true;
                            }
                        }
                    }
                    else
                    {
                        var fileIconInfo = await LoadIconOverlayAsync(item.ItemPath, thumbnailSize);

                        await CoreApplication.MainView.ExecuteOnUIThreadAsync(async () =>
                        {
                            if (fileIconInfo.IconData != null && fileIconInfo.IsCustom) // Only set folder icon if it's a custom icon
                            {
                                item.FileImage = await fileIconInfo.IconData.ToBitmapAsync();
                                item.LoadUnknownTypeGlyph = false;
                                item.LoadFolderGlyph = false;
                                item.LoadFileIcon = true;
                            }
                            item.IconOverlay = await fileIconInfo.OverlayData.ToBitmapAsync();
                        }, Windows.UI.Core.CoreDispatcherPriority.Low);
                        if (!item.IsShortcutItem && !item.IsHiddenItem)
                        {
                            StorageFolder matchingStorageItem = await GetFolderFromPathAsync(item.ItemPath);
                            if (matchingStorageItem != null)
                            {
                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageItem);
                                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                                {
                                    item.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                    item.ItemType = matchingStorageItem.DisplayType;
                                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                    wasSyncStatusLoaded = true;
                                }, Windows.UI.Core.CoreDispatcherPriority.Low);
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
                        await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                        {
                            item.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false }; // Reset cloud sync status icon
                        }, Windows.UI.Core.CoreDispatcherPriority.Low);
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
                var response = await Connection.SendMessageAsync(value);
                var hasCustomIcon = (response.Status == AppServiceResponseStatus.Success)
                    && response.Message.Get("HasCustomIcon", false);
                var icon = response.Message.Get("Icon", (string)null);
                var overlay = response.Message.Get("Overlay", (string)null);

                // BitmapImage can only be created on UI thread, so return raw data and create
                // BitmapImage later to prevent exceptions once SynchorizationContext lost
                return (icon == null ? null : Convert.FromBase64String(icon),
                    overlay == null ? null : Convert.FromBase64String(overlay),
                    hasCustomIcon);
            }
            return (null, null, false);
        }

        public void RefreshItems(string previousDir, bool useCache = true)
        {
            AddItemsToCollectionAsync(WorkingDirectory, previousDir, useCache);
        }

        public async void RapidAddItemsToCollectionAsync(string path, string previousDir, bool useCache = true)
        {
            AssociatedInstance.NavigationToolbar.CanRefresh = false;

            CancelLoadAndClearFiles();

            try
            {
                // Only one instance at a time should access this function
                // Wait here until the previous one has ended
                // If we're waiting and a new update request comes through
                // simply drop this instance
                await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                return;
            }

            try
            {
                // Drop all the other waiting instances
                semaphoreCTS.Cancel();
                semaphoreCTS.Dispose();
                semaphoreCTS = new CancellationTokenSource();

                IsLoadingItems = true;

                filesAndFolders.Clear();
                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                {
                    FilesAndFolders.Clear();
                });

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                AssociatedInstance.NavigationToolbar.CanGoBack = AssociatedInstance.ContentFrame.CanGoBack;
                AssociatedInstance.NavigationToolbar.CanGoForward = AssociatedInstance.ContentFrame.CanGoForward;

                List<string> cacheResult = null;

                if (useCache)
                {
                    cacheResult = await Task.Run(async () =>
                    {
                        var sampler = new IntervalSampler(500);
                        CacheEntry cacheEntry;
                        try
                        {
                            cacheEntry = await fileListCache.ReadFileListFromCache(path, addFilesCTS.Token);
                        }
                        catch
                        {
                            cacheEntry = null;
                        }

                        if (cacheEntry != null)
                        {
                            for (var i = 0; i < cacheEntry.FileList.Count; i++)
                            {
                                filesAndFolders.Add(cacheEntry.FileList[i]);
                                if (addFilesCTS.IsCancellationRequested)
                                {
                                    break;
                                }

                                if (i == 32 || sampler.CheckNow())
                                {
                                    await OrderFilesAndFoldersAsync();
                                    await ApplyFilesAndFoldersChangesAsync();
                                }
                            }
                            return filesAndFolders.Select(i => i.ItemPath).ToList();
                        }
                        return null;
                    });
                }

                if (path.StartsWith(AppSettings.RecycleBinPath))
                {
                    // Recycle bin is special as files are enumerated by the fulltrust process
                    await EnumerateItemsFromSpecialFolderAsync(path);
                }
                else
                {
                    if (await EnumerateItemsFromStandardFolderAsync(path, cacheResult))
                    {
                        WatchForDirectoryChanges(path);
                    }
                }

                if (addFilesCTS.IsCancellationRequested)
                {
                    addFilesCTS.Dispose();
                    addFilesCTS = new CancellationTokenSource();
                    IsLoadingItems = false;
                    return;
                }

                await OrderFilesAndFoldersAsync();
                await ApplyFilesAndFoldersChangesAsync();

                stopwatch.Stop();
                Debug.WriteLine($"Loading of items in {path} completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
                AssociatedInstance.NavigationToolbar.CanRefresh = true;
                IsLoadingItems = false;

                if (!string.IsNullOrWhiteSpace(previousDir))
                {
                    if (previousDir.Contains(path) && !previousDir.Contains("Shell:RecycleBinFolder"))
                    {
                        // Remove the WorkingDir from previous dir
                        previousDir = previousDir.Replace(path, string.Empty);

                        // Get previous dir name
                        if (previousDir.StartsWith('\\'))
                        {
                            previousDir = previousDir.Remove(0, 1);
                        }
                        if (previousDir.Contains('\\'))
                        {
                            previousDir = previousDir.Split('\\')[0];
                        }

                        // Get the first folder and combine it with WorkingDir
                        string folderToSelect = string.Format("{0}\\{1}", path, previousDir);

                        // Make sure we don't get double \\ in the path
                        folderToSelect = folderToSelect.Replace("\\\\", "\\");

                        if (folderToSelect.EndsWith('\\'))
                        {
                            folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);
                        }

                        ListedItem itemToSelect = AssociatedInstance.FilesystemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

                        if (itemToSelect != null)
                        {
                            AssociatedInstance.ContentPage.SetSelectedItemOnUi(itemToSelect);
                            AssociatedInstance.ContentPage.ScrollIntoView(itemToSelect);
                        }
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
            }
            finally
            {
                enumFolderSemaphore.Release();
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
        }

        public async Task EnumerateItemsFromSpecialFolderAsync(string path)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;

            CurrentFolder = new ListedItem(null, returnformat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemPropertiesInitialized = true,
                ItemName = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                ItemDateModifiedReal = DateTimeOffset.Now, // Fake for now
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
                    value.Add("Arguments", "RecycleBin");
                    value.Add("action", "Enumerate");
                    // Send request to fulltrust process to enumerate recyclebin items
                    var (status, response) = await Connection.SendMessageWithRetryAsync(value, TimeSpan.FromSeconds(10));
                    // If the request was canceled return now
                    if (addFilesCTS.IsCancellationRequested)
                    {
                        return;
                    }
                    if (status == AppServiceResponseStatus.Success
                        && response.Message.ContainsKey("Enumerate"))
                    {
                        var folderContentsList = JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response.Message["Enumerate"]);
                        for (int count = 0; count < folderContentsList.Count; count++)
                        {
                            var item = folderContentsList[count];
                            var listedItem = AddFileOrFolderFromShellFile(item, returnformat);
                            if (listedItem != null)
                            {
                                filesAndFolders.Add(listedItem);
                            }
                            if (count == 32 || sampler.CheckNow())
                            {
                                await OrderFilesAndFoldersAsync();
                                await ApplyFilesAndFoldersChangesAsync();
                            }
                        }
                    }
                });
            }
        }

        public async Task<bool> EnumerateItemsFromStandardFolderAsync(string path, List<string> skipItems)
        {
            // Flag to use FindFirstFileExFromApp or StorageFolder enumeration
            bool enumFromStorageFolder = false;

            var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(path).AsTask());
            if (res)
            {
                rootFolder = res.Result;
            }
            else if (workingRoot != null)
            {
                if (currentStorageFolder == null)
                {
                    return false;
                }
                rootFolder = currentStorageFolder.Folder;
                enumFromStorageFolder = true;
            }
            else if (!CheckFolderAccessWithWin32(path)) // The folder is really inaccessible
            {
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
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;

            if (await CheckBitlockerStatusAsync(rootFolder))
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

                        if (await CheckBitlockerStatusAsync(rootFolder))
                        {
                            // Drive is still locked
                            await DialogDisplayHelper.ShowDialogAsync("BitlockerInvalidPwDialog/Title".GetLocalized(), "BitlockerInvalidPwDialog/Text".GetLocalized());
                        }
                    }
                }
            }

            // Is folder synced to cloud storage?
            var syncStatus = await CheckCloudDriveSyncStatusAsync(rootFolder);
            AssociatedInstance.InstanceViewModel.IsPageTypeCloudDrive =
                syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown;

            if (enumFromStorageFolder)
            {
                CurrentFolder = new ListedItem(rootFolder.FolderRelativeId, returnformat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemPropertiesInitialized = true,
                    ItemName = rootFolder.Name,
                    ItemDateModifiedReal = (await rootFolder.GetBasicPropertiesAsync()).DateModified,
                    ItemType = rootFolder.DisplayType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(rootFolder.Path) ? currentStorageFolder.Path : rootFolder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                };
                await EnumFromStorageFolderAsync(path, skipItems);
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

                DateTime itemDate = DateTime.UtcNow;
                try
                {
                    FileTimeToSystemTime(ref findData.ftLastWriteTime, out SYSTEMTIME systemTimeOutput);
                    itemDate = new DateTime(
                        systemTimeOutput.Year, systemTimeOutput.Month, systemTimeOutput.Day,
                        systemTimeOutput.Hour, systemTimeOutput.Minute, systemTimeOutput.Second, systemTimeOutput.Milliseconds,
                        DateTimeKind.Utc);
                }
                catch (ArgumentException) { }

                bool isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
                double opacity = 1;

                if (isHidden)
                {
                    opacity = 0.4;
                }

                CurrentFolder = new ListedItem(null, returnformat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemPropertiesInitialized = true,
                    ItemName = Path.GetFileName(path.TrimEnd('\\')),
                    ItemDateModifiedReal = itemDate,
                    ItemType = "FileFolderListItem".GetLocalized(),
                    LoadFolderGlyph = true,
                    FileImage = null,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    LoadFileIcon = false,
                    ItemPath = path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                };

                var count = 0;
                if (hFile == IntPtr.Zero)
                {
                    await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalized(), "");
                    return false;
                }
                else if (hFile.ToInt64() == -1)
                {
                    await EnumFromStorageFolderAsync(path, skipItems);
                    return false;
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        var hasNextFile = false;
                        var sampler = new IntervalSampler(500);
                        do
                        {
                            var itemPath = Path.Combine(path, findData.cFileName);
                            if (((FileAttributes)findData.dwFileAttributes & FileAttributes.System) != FileAttributes.System || !AppSettings.AreSystemItemsHidden)
                            {
                                if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) != FileAttributes.Hidden || AppSettings.AreHiddenItemsVisible)
                                {
                                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                                    {
                                        var listedItem = await AddFile(findData, path, returnformat);
                                        if (listedItem != null)
                                        {
                                            if (skipItems?.Contains(listedItem.ItemPath) ?? false)
                                            {
                                                skipItems.Remove(listedItem.ItemPath);
                                            }
                                            else
                                            {
                                                filesAndFolders.Add(listedItem);
                                            }
                                            ++count;
                                        }
                                    }
                                    else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                                    {
                                        if (findData.cFileName != "." && findData.cFileName != "..")
                                        {
                                            var listedItem = AddFolder(findData, path, returnformat);
                                            if (listedItem != null)
                                            {
                                                if (skipItems?.Contains(listedItem.ItemPath) ?? false)
                                                {
                                                    skipItems.Remove(listedItem.ItemPath);
                                                }
                                                else
                                                {
                                                    filesAndFolders.Add(listedItem);
                                                }
                                                ++count;
                                            }
                                        }
                                    }
                                }
                            }
                            if (addFilesCTS.IsCancellationRequested)
                            {
                                break;
                            }

                            hasNextFile = FindNextFile(hFile, out findData);
                            if (count == 32 || sampler.CheckNow())
                            {
                                await OrderFilesAndFoldersAsync();
                                await ApplyFilesAndFoldersChangesAsync();
                            }
                        } while (hasNextFile);
                    });

                    if (skipItems != null)
                    {
                        // remove invalid cache entries
                        var invalidEntries = filesAndFolders.Where(i => skipItems.Contains(i.ItemPath)).ToList();
                        foreach (var i in invalidEntries)
                        {
                            filesAndFolders.Remove(i);
                        }
                    }

                    if (!addFilesCTS.IsCancellationRequested)
                    {
                        await fileListCache.SaveFileListToCache(path, new CacheEntry
                        {
                            CurrentFolder = CurrentFolder,
                            // since filesAndFolders could be mutated, memory cache needs a copy of current list
                            FileList = filesAndFolders.ToList()
                        });
                    }
                    else
                    {
                        await fileListCache.SaveFileListToCache(path, null);
                    }

                    FindClose(hFile);

                    return true;
                }
            }
        }

        private async Task EnumFromStorageFolderAsync(string path, List<string> skipItems)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;

            uint count = 0;
            var sampler = new IntervalSampler(500);
            while (true)
            {
                IStorageItem item = null;
                try
                {
                    var results = await rootFolder.GetItemsAsync(count, 1);
                    item = results?.FirstOrDefault();
                    if (item == null)
                    {
                        break;
                    }
                }
                catch (NotImplementedException)
                {
                    break;
                }
                catch (Exception ex) when (
                    ex is UnauthorizedAccessException
                    || ex is FileNotFoundException
                    || (uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
                {
                    ++count;
                    continue;
                }
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    var folder = await AddFolderAsync(item as StorageFolder, returnformat);
                    if (folder != null)
                    {
                        if (skipItems?.Contains(folder.ItemPath) ?? false)
                        {
                            skipItems.Remove(folder.ItemPath);
                        }
                        else
                        {
                            filesAndFolders.Add(folder);
                        }
                    }
                    ++count;
                }
                else
                {
                    var file = item as StorageFile;
                    var fileEntry = await AddFileAsync(file, returnformat, true);
                    if (fileEntry != null)
                    {
                        if (skipItems?.Contains(fileEntry.ItemPath) ?? false)
                        {
                            skipItems.Remove(fileEntry.ItemPath);
                        }
                        else
                        {
                            filesAndFolders.Add(fileEntry);
                        }
                    }
                    ++count;
                }
                if (addFilesCTS.IsCancellationRequested)
                {
                    break;
                }
                if (count == 32 || sampler.CheckNow())
                {
                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();
                }
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
            if (!addFilesCTS.IsCancellationRequested)
            {
                await SaveCurrentListToCacheAsync(path);
            }
            else
            {
                await fileListCache.SaveFileListToCache(path, null);
            }
            Debug.WriteLine($"Enumerating items in {path} (device) completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
        }

        public static bool CheckFolderAccessWithWin32(string path)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
            IntPtr hFileTsk = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                additionalFlags);
            if (hFileTsk.ToInt64() != -1)
            {
                FindClose(hFileTsk);
                return true;
            }
            return false;
        }

        public bool CheckFolderForHiddenAttribute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
            IntPtr hFileTsk = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                additionalFlags);
            if (hFileTsk.ToInt64() == -1)
            {
                return false;
            }
            var isHidden = ((FileAttributes)findDataTsk.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            FindClose(hFileTsk);
            return isHidden;
        }

        private async Task<bool> CheckBitlockerStatusAsync(StorageFolder rootFolder)
        {
            if (rootFolder == null || rootFolder.Properties == null)
            {
                return false;
            }
            if (Path.IsPathRooted(WorkingDirectory) && Path.GetPathRoot(WorkingDirectory) == WorkingDirectory)
            {
                IDictionary<string, object> extraProperties = await rootFolder.Properties.RetrievePropertiesAsync(new string[] { "System.Volume.BitLockerProtection" });
                return (int?)extraProperties["System.Volume.BitLockerProtection"] == 6; // Drive is bitlocker protected and locked
            }
            return false;
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

        private void WatchForDirectoryChanges(string path)
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
                if (AssociatedInstance.InstanceViewModel.IsPageTypeCloudDrive)
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
                cts.Dispose();
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
                            NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                        }

                        if (sampler.CheckNow())
                        {
                            await OrderFilesAndFoldersAsync();
                            await ApplyFilesAndFoldersChangesAsync();
                        }
                    }

                    await OrderFilesAndFoldersAsync();
                    await ApplyFilesAndFoldersChangesAsync();
                    await SaveCurrentListToCacheAsync(WorkingDirectory);
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
                if (shouldDisplayFileExtensions && !item.FileName.EndsWith(".lnk") && !item.FileName.EndsWith(".url"))
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
                    ItemDateDeletedReal = item.RecycleDate,
                    ItemType = item.FileType,
                    ItemPath = item.RecyclePath, // this is the true path on disk so other stuff can work as is
                    ItemOriginalPath = item.FilePath,
                    FileSize = item.FileSize,
                    FileSizeBytes = (long)item.FileSizeBytes
                };
            }
        }

        private Task AddFileOrFolderAsync(ListedItem item)
        {
            filesAndFolders.Add(item);
            return Task.CompletedTask;
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

            ListedItem listedItem = null;
            if ((findData.dwFileAttributes & 0x10) > 0) // FILE_ATTRIBUTE_DIRECTORY
            {
                listedItem = AddFolder(findData, Directory.GetParent(fileOrFolderPath).FullName, dateReturnFormat);
            }
            else
            {
                listedItem = await AddFile(findData, Directory.GetParent(fileOrFolderPath).FullName, dateReturnFormat);
            }

            if (listedItem != null)
            {
                filesAndFolders.Add(listedItem);
            }
        }

        private void UpdateDirectoryInfo()
        {
            if (AssociatedInstance.ContentPage != null)
            {
                if (filesAndFolders.Count == 1)
                {
                    AssociatedInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{filesAndFolders.Count} {"ItemCount/Text".GetLocalized()}";
                }
                else
                {
                    AssociatedInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{filesAndFolders.Count} {"ItemsCount/Text".GetLocalized()}";
                }
            }
        }

        private async Task UpdateFileOrFolderAsync(ListedItem item)
        {
            IStorageItem storageItem = null;
            if (item.PrimaryItemAttribute == StorageItemTypes.File)
            {
                storageItem = await StorageFile.GetFileFromPathAsync(item.ItemPath);
            }
            else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                storageItem = await StorageFolder.GetFolderFromPathAsync(item.ItemPath);
            }
            if (storageItem != null)
            {
                var syncStatus = await CheckCloudDriveSyncStatusAsync(storageItem);
                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
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
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
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

        private Task SaveCurrentListToCacheAsync(string path)
        {
            return fileListCache.SaveFileListToCache(path, new CacheEntry
            {
                CurrentFolder = CurrentFolder,
                // since filesAndFolders could be mutated, memory cache needs a copy of current list
                FileList = filesAndFolders.ToList()
            });
        }

        public async Task RemoveFileOrFolderAsync(ListedItem item)
        {
            filesAndFolders.Remove(item);
            await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
            {
                App.JumpList.RemoveFolder(item.ItemPath);
            });
        }

        public async Task RemoveFileOrFolderAsync(string path)
        {
            try
            {
                await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                return;
            }

            try
            {
                var matchingItem = filesAndFolders.FirstOrDefault(x => x.ItemPath.Equals(path));

                if (matchingItem != null)
                {
                    await RemoveFileOrFolderAsync(matchingItem);
                }
            }
            finally
            {
                enumFolderSemaphore.Release();
            }
        }

        private ListedItem AddFolder(WIN32_FIND_DATA findData, string pathRoot, string dateReturnFormat)
        {
            if (addFilesCTS.IsCancellationRequested)
            {
                return null;
            }

            DateTime itemDate;
            try
            {
                FileTimeToSystemTime(ref findData.ftLastWriteTime, out SYSTEMTIME systemTimeOutput);
                itemDate = new DateTime(
                    systemTimeOutput.Year, systemTimeOutput.Month, systemTimeOutput.Day,
                    systemTimeOutput.Hour, systemTimeOutput.Minute, systemTimeOutput.Second, systemTimeOutput.Milliseconds,
                    DateTimeKind.Utc);
            }
            catch (ArgumentException)
            {
                // Invalid date means invalid findData, do not add to list
                return null;
            }
            var itemPath = Path.Combine(pathRoot, findData.cFileName);

            bool isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
            double opacity = 1;

            if (isHidden)
            {
                opacity = 0.4;
            }

            return new ListedItem(null, dateReturnFormat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemName = findData.cFileName,
                ItemDateModifiedReal = itemDate,
                ItemType = "FileFolderListItem".GetLocalized(),
                LoadFolderGlyph = true,
                FileImage = null,
                IsHiddenItem = isHidden,
                Opacity = opacity,
                LoadFileIcon = false,
                ItemPath = itemPath,
                LoadUnknownTypeGlyph = false,
                FileSize = null,
                FileSizeBytes = 0,
                ContainsFilesOrFolders = CheckForFilesFolders(itemPath),
                //FolderTooltipText = tooltipString,
            };
        }

        private async Task<ListedItem> AddFile(WIN32_FIND_DATA findData, string pathRoot, string dateReturnFormat)
        {
            var itemPath = Path.Combine(pathRoot, findData.cFileName);

            string itemName;
            if (shouldDisplayFileExtensions && !findData.cFileName.EndsWith(".lnk") && !findData.cFileName.EndsWith(".url"))
            {
                itemName = findData.cFileName; // never show extension for shortcuts
            }
            else
            {
                if (findData.cFileName.StartsWith("."))
                {
                    itemName = findData.cFileName; // Always show full name for dotfiles.
                }
                else
                {
                    itemName = Path.GetFileNameWithoutExtension(itemPath);
                }
            }

            DateTime itemModifiedDate, itemCreatedDate, itemLastAccessDate;
            try
            {
                FileTimeToSystemTime(ref findData.ftLastWriteTime, out SYSTEMTIME systemModifiedDateOutput);
                itemModifiedDate = new DateTime(
                    systemModifiedDateOutput.Year, systemModifiedDateOutput.Month, systemModifiedDateOutput.Day,
                    systemModifiedDateOutput.Hour, systemModifiedDateOutput.Minute, systemModifiedDateOutput.Second, systemModifiedDateOutput.Milliseconds,
                    DateTimeKind.Utc);

                FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedDateOutput);
                itemCreatedDate = new DateTime(
                    systemCreatedDateOutput.Year, systemCreatedDateOutput.Month, systemCreatedDateOutput.Day,
                    systemCreatedDateOutput.Hour, systemCreatedDateOutput.Minute, systemCreatedDateOutput.Second, systemCreatedDateOutput.Milliseconds,
                    DateTimeKind.Utc);

                FileTimeToSystemTime(ref findData.ftLastAccessTime, out SYSTEMTIME systemLastAccessOutput);
                itemLastAccessDate = new DateTime(
                    systemLastAccessOutput.Year, systemLastAccessOutput.Month, systemLastAccessOutput.Day,
                    systemLastAccessOutput.Hour, systemLastAccessOutput.Minute, systemLastAccessOutput.Second, systemLastAccessOutput.Milliseconds,
                    DateTimeKind.Utc);
            }
            catch (ArgumentException)
            {
                // Invalid date means invalid findData, do not add to list
                return null;
            }

            long itemSizeBytes = findData.GetSize();
            var itemSize = ByteSize.FromBytes(itemSizeBytes).ToBinaryString().ConvertSizeAbbreviation();
            string itemType = "ItemTypeFile".GetLocalized();
            string itemFileExtension = null;

            if (findData.cFileName.Contains('.'))
            {
                itemFileExtension = Path.GetExtension(itemPath);
                itemType = $"{itemFileExtension.Trim('.')} {itemType}";
            }

            bool itemFolderImgVis = false;
            bool itemThumbnailImgVis;
            bool itemEmptyImgVis;

            itemEmptyImgVis = true;
            itemThumbnailImgVis = false;

            if (addFilesCTS.IsCancellationRequested)
            {
                return null;
            }
            if (findData.cFileName.EndsWith(".lnk") || findData.cFileName.EndsWith(".url"))
            {
                if (Connection != null)
                {
                    var response = await Connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "ParseLink" },
                        { "filepath", itemPath }
                    });
                    // If the request was canceled return now
                    if (addFilesCTS.IsCancellationRequested)
                    {
                        return null;
                    }
                    if (response.Status == AppServiceResponseStatus.Success
                        && response.Message.ContainsKey("TargetPath"))
                    {
                        var isUrl = findData.cFileName.EndsWith(".url");
                        string target = (string)response.Message["TargetPath"];
                        bool containsFilesOrFolders = false;

                        if ((bool)response.Message["IsFolder"])
                        {
                            containsFilesOrFolders = CheckForFilesFolders(target);
                        }

                        bool isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
                        double opacity = 1;

                        if (isHidden)
                        {
                            opacity = 0.4;
                        }

                        return new ShortcutItem(null, dateReturnFormat)
                        {
                            PrimaryItemAttribute = (bool)response.Message["IsFolder"] ? StorageItemTypes.Folder : StorageItemTypes.File,
                            FileExtension = itemFileExtension,
                            IsHiddenItem = isHidden,
                            Opacity = opacity,
                            FileImage = null,
                            LoadFileIcon = !(bool)response.Message["IsFolder"] && itemThumbnailImgVis,
                            LoadUnknownTypeGlyph = !(bool)response.Message["IsFolder"] && !isUrl && itemEmptyImgVis,
                            LoadFolderGlyph = (bool)response.Message["IsFolder"],
                            ItemName = itemName,
                            ItemDateModifiedReal = itemModifiedDate,
                            ItemDateAccessedReal = itemLastAccessDate,
                            ItemDateCreatedReal = itemCreatedDate,
                            ItemType = isUrl ? "ShortcutWebLinkFileType".GetLocalized() : "ShortcutFileType".GetLocalized(),
                            ItemPath = itemPath,
                            FileSize = itemSize,
                            FileSizeBytes = itemSizeBytes,
                            TargetPath = target,
                            Arguments = (string)response.Message["Arguments"],
                            WorkingDirectory = (string)response.Message["WorkingDirectory"],
                            RunAsAdmin = (bool)response.Message["RunAsAdmin"],
                            IsUrl = isUrl,
                            ContainsFilesOrFolders = containsFilesOrFolders,
                        };
                    }
                }
            }
            else
            {
                bool isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
                double opacity = 1;

                if (isHidden)
                {
                    opacity = 0.4;
                }

                return new ListedItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    LoadUnknownTypeGlyph = itemEmptyImgVis,
                    FileImage = null,
                    LoadFileIcon = itemThumbnailImgVis,
                    LoadFolderGlyph = itemFolderImgVis,
                    ItemName = itemName,
                    IsHiddenItem = isHidden,
                    Opacity = opacity,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateAccessedReal = itemLastAccessDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = itemSizeBytes
                };
            }
            return null;
        }

        public void AddItemsToCollectionAsync(string path, string previousDir, bool useCache = true)
        {
            RapidAddItemsToCollectionAsync(path, previousDir, useCache);
        }

        private async Task<ListedItem> AddFolderAsync(StorageFolder folder, string dateReturnFormat)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if (!addFilesCTS.IsCancellationRequested)
            {
                return new ListedItem(folder.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = folder.Name,
                    ItemDateModifiedReal = basicProperties.DateModified,
                    ItemType = folder.DisplayType,
                    IsHiddenItem = false,
                    Opacity = 1,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(folder.Path) ? Path.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                    //FolderTooltipText = tooltipString,
                };
            }
            return null;
        }

        private async Task<ListedItem> AddFileAsync(StorageFile file, string dateReturnFormat, bool suppressThumbnailLoading = false)
        {
            var basicProperties = await file.GetBasicPropertiesAsync();
            // Display name does not include extension
            var itemName = string.IsNullOrEmpty(file.DisplayName) || shouldDisplayFileExtensions ?
                file.Name : file.DisplayName;
            var itemDate = basicProperties.DateModified;
            var itemPath = string.IsNullOrEmpty(file.Path) ? Path.Combine(currentStorageFolder.Path, file.Name) : file.Path;
            var itemSize = ByteSize.FromBytes(basicProperties.Size).ToBinaryString().ConvertSizeAbbreviation();
            var itemSizeBytes = basicProperties.Size;
            var itemType = file.DisplayType;
            var itemFolderImgVis = false;
            var itemFileExtension = file.FileType;

            BitmapImage icon = new BitmapImage();
            bool itemThumbnailImgVis;
            bool itemEmptyImgVis;

            if (!(AssociatedInstance.ContentFrame.SourcePageType == typeof(GridViewBrowser)))
            {
                try
                {
                    var itemThumbnailImg = suppressThumbnailLoading ? null :
                        await file.GetThumbnailAsync(ThumbnailMode.ListView, 40, ThumbnailOptions.UseCurrentScale);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = false;
                        itemThumbnailImgVis = true;
                        icon.DecodePixelWidth = 40;
                        icon.DecodePixelHeight = 40;
                        await icon.SetSourceAsync(itemThumbnailImg);
                    }
                    else
                    {
                        itemEmptyImgVis = true;
                        itemThumbnailImgVis = false;
                    }
                }
                catch
                {
                    itemEmptyImgVis = true;
                    itemThumbnailImgVis = false;
                    // Catch here to avoid crash
                }
            }
            else
            {
                try
                {
                    var itemThumbnailImg = suppressThumbnailLoading ? null :
                        await file.GetThumbnailAsync(ThumbnailMode.ListView, 80, ThumbnailOptions.UseCurrentScale);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = false;
                        itemThumbnailImgVis = true;
                        icon.DecodePixelWidth = 80;
                        icon.DecodePixelHeight = 80;
                        await icon.SetSourceAsync(itemThumbnailImg);
                    }
                    else
                    {
                        itemEmptyImgVis = true;
                        itemThumbnailImgVis = false;
                    }
                }
                catch
                {
                    itemEmptyImgVis = true;
                    itemThumbnailImgVis = false;
                }
            }
            if (addFilesCTS.IsCancellationRequested)
            {
                return null;
            }

            if (file.Name.EndsWith(".lnk") || file.Name.EndsWith(".url"))
            {
                // This shouldn't happen, StorageFile api does not support shortcuts
                Debug.WriteLine("Something strange: StorageFile api returned a shortcut");
            }
            else
            {
                return new ListedItem(file.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    IsHiddenItem = false,
                    Opacity = 1,
                    LoadUnknownTypeGlyph = itemEmptyImgVis,
                    FileImage = icon,
                    LoadFileIcon = itemThumbnailImgVis,
                    LoadFolderGlyph = itemFolderImgVis,
                    ItemName = itemName,
                    ItemDateModifiedReal = itemDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = (long)itemSizeBytes,
                };
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
            WorkingDirectoryChanged($"{"SearchPagePathBoxOverrideText".GetLocalized()} {currentSearchPath}");
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (propertyName.Equals("WorkingDirectory", StringComparison.OrdinalIgnoreCase))
            {
                WorkingDirectoryChanged();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            CancelLoadAndClearFiles();
            addFilesCTS?.Dispose();
            semaphoreCTS?.Dispose();
            loadPropsCTS?.Dispose();
        }

        /// <summary>
        /// This function is used to determine whether or not a folder has any contents.
        /// </summary>
        /// <param name="targetPath">The path to the target folder</param>
        ///
        public bool CheckForFilesFolders(string targetPath)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(targetPath + "\\*.*", findInfoLevel, out WIN32_FIND_DATA _, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
            FindNextFile(hFile, out _);
            var result = FindNextFile(hFile, out _);
            FindClose(hFile);
            return result;
        }
    }

    public class WorkingDirectoryModifiedEventArgs : EventArgs
    {
        public string Path { get; set; }
    }
}