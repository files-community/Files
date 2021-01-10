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
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private IntPtr hWatchDir;
        private IAsyncAction aWatcherAction;
        private BulkObservableCollection<ListedItem> _filesAndFolders;
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public SettingsViewModel AppSettings => App.AppSettings;
        public FolderSettingsViewModel FolderSettings => AssociatedInstance?.InstanceViewModel.FolderSettings;
        private bool shouldDisplayFileExtensions = false;
        public ListedItem CurrentFolder { get; private set; }
        public CollectionViewSource viewSource;
        private CancellationTokenSource _addFilesCTS, _semaphoreCTS;
        private StorageFolder _rootFolder;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _jumpString = "";
        private readonly DispatcherTimer jumpTimer = new DispatcherTimer();

        private string _customPath;

        private IFileListCache fileListCache = FileListCacheController.GetInstance();

        public string WorkingDirectory
        {
            get
            {
                return _currentStorageFolder?.Path ?? _customPath;
            }
        }

        private StorageFolderWithPath _currentStorageFolder;
        private StorageFolderWithPath _workingRoot;

        public delegate void WorkingDirectoryModifiedEventHandler(object sender, WorkingDirectoryModifiedEventArgs e);

        public event WorkingDirectoryModifiedEventHandler WorkingDirectoryModified;

        public async Task<FilesystemResult> SetWorkingDirectoryAsync(string value)
        {
            var navigated = (FilesystemResult)true;
            if (string.IsNullOrWhiteSpace(value))
            {
                return new FilesystemResult(FilesystemErrorCode.ERROR_NOTAFOLDER);
            }

            WorkingDirectoryModified?.Invoke(this, new WorkingDirectoryModifiedEventArgs() { Path = value });

            if (!Path.IsPathRooted(value))
            {
                _workingRoot = null;
                _currentStorageFolder = null;
                _customPath = value;
            }
            else if (!Path.IsPathRooted(WorkingDirectory) || Path.GetPathRoot(WorkingDirectory) != Path.GetPathRoot(value))
            {
                _workingRoot = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(value));
            }

            if (Path.IsPathRooted(value))
            {
                var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(value, _workingRoot, _currentStorageFolder));
                if (res)
                {
                    _currentStorageFolder = res.Result;
                    _customPath = null;
                }
                else
                {
                    _currentStorageFolder = null;
                    _customPath = value;
                }
                navigated = res;
            }

            if (value == "Home" || value == "NewTab".GetLocalized())
            {
                _currentStorageFolder = null;
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
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(value, _workingRoot, _currentStorageFolder));
        }

        public async Task<FilesystemResult<StorageFile>> GetFileFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(value, _workingRoot, _currentStorageFolder));
        }

        public async Task<FilesystemResult<StorageFolderWithPath>> GetFolderWithPathFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(value, _workingRoot, _currentStorageFolder));
        }

        public async Task<FilesystemResult<StorageFileWithPath>> GetFileWithPathFromPathAsync(string value)
        {
            return await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(value, _workingRoot, _currentStorageFolder));
        }

        private bool _IsFolderEmptyTextDisplayed;

        public bool IsFolderEmptyTextDisplayed
        {
            get => _IsFolderEmptyTextDisplayed;
            set
            {
                if (value != _IsFolderEmptyTextDisplayed)
                {
                    _IsFolderEmptyTextDisplayed = value;
                    NotifyPropertyChanged(nameof(IsFolderEmptyTextDisplayed));
                }
            }
        }

        public void UpdateSortOptionStatus()
        {
            NotifyPropertyChanged(nameof(IsSortedByName));
            NotifyPropertyChanged(nameof(IsSortedByDate));
            NotifyPropertyChanged(nameof(IsSortedByType));
            NotifyPropertyChanged(nameof(IsSortedBySize));
            NotifyPropertyChanged(nameof(IsSortedByOriginalPath));
            NotifyPropertyChanged(nameof(IsSortedByDateDeleted));
            OrderFiles();
        }

        public void UpdateSortDirectionStatus()
        {
            NotifyPropertyChanged(nameof(IsSortedAscending));
            NotifyPropertyChanged(nameof(IsSortedDescending));
            OrderFiles();
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
            get
            {
                return _jumpString;
            }
            set
            {
                // If current string is "a", and the next character typed is "a",
                // search for next file that starts with "a" (a.k.a. _jumpString = "a")
                if (_jumpString.Length == 1 && value == _jumpString + _jumpString)
                {
                    value = _jumpString;
                }
                if (value != "")
                {
                    ListedItem jumpedToItem = null;
                    ListedItem previouslySelectedItem = null;
                    var candidateItems = _filesAndFolders.Where(f => f.ItemName.Length >= value.Length && f.ItemName.Substring(0, value.Length).ToLower() == value);
                    if (AssociatedInstance.ContentPage.IsItemSelected)
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
                _jumpString = value;
            }
        }

        public AppServiceConnection Connection => AssociatedInstance?.ServiceConnection;

        public ItemViewModel(IShellPage appInstance)
        {
            AssociatedInstance = appInstance;
            _filesAndFolders = new BulkObservableCollection<ListedItem>();
            FilesAndFolders = new ReadOnlyObservableCollection<ListedItem>(_filesAndFolders);
            _addFilesCTS = new CancellationTokenSource();
            _semaphoreCTS = new CancellationTokenSource();
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
                await AppSettings.DrivesManager.HandleWin32DriveEvent(eventType, deviceId);
            }
            // Complete the deferral so that the platform knows that we're done responding to the app service call.
            // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
            messageDeferral.Complete();
        }

        private void JumpTimer_Tick(object sender, object e)
        {
            _jumpString = "";
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
                _addFilesCTS.Cancel();
            }
            _filesAndFolders.Clear();
        }

        public void OrderFiles(IList<ListedItem> orderedList = null)
        {
            if (orderedList == null)
            {
                orderedList = OrderFiles2(_filesAndFolders);
            }
            //_filesAndFolders.BeginBulkOperation();
            for (var i = 0; i < orderedList.Count; i++)
            {
                if (i < _filesAndFolders.Count)
                {
                    if (_filesAndFolders[i] != orderedList[i])
                    {
                        _filesAndFolders.Insert(i, orderedList[i]);
                    }
                }
                else
                {
                    _filesAndFolders.Add(orderedList[i]);
                }
            }
            while (_filesAndFolders.Count > orderedList.Count)
            {
                _filesAndFolders.RemoveAt(_filesAndFolders.Count - 1);
            }
            //_filesAndFolders.EndBulkOperation();
        }

        private IList<ListedItem> OrderFiles2(IList<ListedItem> listToSort)
        {
            if (listToSort.Count == 0)
            {
                return listToSort.ToList();
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
            List<ListedItem> orderedList;

            if (FolderSettings.DirectorySortDirection == SortDirection.Ascending)
            {
                if (FolderSettings.DirectorySortOption == SortOption.Name)
                {
                    if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                    {
                        ordered = listToSort.OrderBy(orderFunc, naturalStringComparer);
                    }
                    else
                    {
                        ordered = listToSort.OrderBy(folderThenFileAsync).ThenBy(orderFunc, naturalStringComparer);
                    }
                }
                else
                {
                    if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                    {
                        ordered = listToSort.OrderBy(orderFunc);
                    }
                    else
                    {
                        ordered = listToSort.OrderBy(folderThenFileAsync).ThenBy(orderFunc);
                    }
                }
            }
            else
            {
                if (FolderSettings.DirectorySortOption == SortOption.Name)
                {
                    if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                    {
                        ordered = listToSort.OrderByDescending(orderFunc, naturalStringComparer);
                    }
                    else
                    {
                        ordered = listToSort.OrderByDescending(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                    }
                }
                else
                {
                    if (AppSettings.ListAndSortDirectoriesAlongsideFiles)
                    {
                        ordered = listToSort.OrderByDescending(orderFunc);
                    }
                    else
                    {
                        ordered = listToSort.OrderByDescending(folderThenFileAsync).ThenByDescending(orderFunc);
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
            orderedList = ordered.ToList();

            return orderedList;
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
        public async void LoadExtendedItemProperties(ListedItem item, uint thumbnailSize = 20)
        {
            if (!item.ItemPropertiesInitialized)
            {
                var matchingItem = _filesAndFolders.FirstOrDefault(x => x == item);
                if (matchingItem == null)
                {
                    item.ItemPropertiesInitialized = true;
                    return;
                }
                var wasSyncStatusLoaded = false;
                try
                {
                    if (item.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        var fileIconInfo = await LoadIconOverlayAsync(matchingItem.ItemPath, thumbnailSize);
                        if (fileIconInfo.Icon != null && !matchingItem.IsLinkItem)
                        {
                            matchingItem.FileImage = fileIconInfo.Icon;
                            matchingItem.LoadUnknownTypeGlyph = false;
                            matchingItem.LoadFileIcon = true;
                        }
                        matchingItem.IconOverlay = fileIconInfo.Overlay;
                        if (!item.IsShortcutItem && !item.IsHiddenItem)
                        {
                            StorageFile matchingStorageItem = await GetFileFromPathAsync(item.ItemPath);
                            if (matchingStorageItem != null)
                            {
                                if (!matchingItem.LoadFileIcon) // Loading icon from fulltrust process failed
                                {
                                    using (var Thumbnail = await matchingStorageItem.GetThumbnailAsync(ThumbnailMode.SingleItem, thumbnailSize, ThumbnailOptions.UseCurrentScale))
                                    {
                                        if (Thumbnail != null)
                                        {
                                            matchingItem.FileImage = new BitmapImage();
                                            await matchingItem.FileImage.SetSourceAsync(Thumbnail);
                                            matchingItem.LoadUnknownTypeGlyph = false;
                                            matchingItem.LoadFileIcon = true;
                                        }
                                    }
                                }
                                matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                matchingItem.ItemType = matchingStorageItem.DisplayType;
                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageItem);
                                matchingItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                wasSyncStatusLoaded = true;
                            }
                        }
                    }
                    else
                    {
                        var fileIconInfo = await LoadIconOverlayAsync(matchingItem.ItemPath, thumbnailSize);
                        if (fileIconInfo.Icon != null && fileIconInfo.IsCustom) // Only set folder icon if it's a custom icon
                        {
                            matchingItem.FileImage = fileIconInfo.Icon;
                            matchingItem.LoadUnknownTypeGlyph = false;
                            matchingItem.LoadFolderGlyph = false;
                            matchingItem.LoadFileIcon = true;
                        }
                        matchingItem.IconOverlay = fileIconInfo.Overlay;
                        if (!item.IsShortcutItem && !item.IsHiddenItem)
                        {
                            StorageFolder matchingStorageItem = await GetFolderFromPathAsync(item.ItemPath);
                            if (matchingStorageItem != null)
                            {
                                matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                matchingItem.ItemType = matchingStorageItem.DisplayType;
                                var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageItem);
                                matchingItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                wasSyncStatusLoaded = true;
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
                        matchingItem.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false }; // Reset cloud sync status icon
                    }
                    item.ItemPropertiesInitialized = true;
                }
            }
        }

        public async Task<(BitmapImage Icon, BitmapImage Overlay, bool IsCustom)> LoadIconOverlayAsync(string filePath, uint thumbnailSize)
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
                BitmapImage iconImage = null, overlayImage = null;
                var icon = response.Message.Get("Icon", (string)null);
                if (!string.IsNullOrEmpty(icon))
                {
                    iconImage = new BitmapImage();
                    byte[] bitmapData = Convert.FromBase64String(icon);
                    using (var ms = new MemoryStream(bitmapData))
                    {
                        await iconImage.SetSourceAsync(ms.AsRandomAccessStream());
                    }
                }
                var overlay = response.Message.Get("Overlay", (string)null);
                if (!string.IsNullOrEmpty(overlay))
                {
                    overlayImage = new BitmapImage();
                    byte[] bitmapData = Convert.FromBase64String(overlay);
                    using (var ms = new MemoryStream(bitmapData))
                    {
                        await overlayImage.SetSourceAsync(ms.AsRandomAccessStream());
                    }
                }
                return (iconImage, overlayImage, hasCustomIcon);
            }
            return (null, null, false);
        }

        public void RefreshItems(string previousDir)
        {
            AddItemsToCollectionAsync(WorkingDirectory, previousDir);
        }

        public async void RapidAddItemsToCollectionAsync(string path, string previousDir)
        {
            AssociatedInstance.NavigationToolbar.CanRefresh = false;

            CancelLoadAndClearFiles();

            try
            {
                // Only one instance at a time should access this function
                // Wait here until the previous one has ended
                // If we're waiting and a new update request comes through
                // simply drop this instance
                await semaphoreSlim.WaitAsync(_semaphoreCTS.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                return;
            }

            try
            {
                // Drop all the other waiting instances
                _semaphoreCTS.Cancel();
                _semaphoreCTS.Dispose();
                _semaphoreCTS = new CancellationTokenSource();

                IsLoadingItems = true;
                IsFolderEmptyTextDisplayed = false;
                _filesAndFolders.Clear();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                AssociatedInstance.NavigationToolbar.CanGoBack = AssociatedInstance.ContentFrame.CanGoBack;
                AssociatedInstance.NavigationToolbar.CanGoForward = AssociatedInstance.ContentFrame.CanGoForward;

                var cacheEntry = await fileListCache.ReadFileListFromCache(path);
                if (cacheEntry != null)
                {
                    CurrentFolder = cacheEntry.CurrentFolder;
                    var orderedList = OrderFiles2(cacheEntry.FileList);
                    OrderFiles(orderedList);
                    Debug.WriteLine($"Loading of items from cache in {WorkingDirectory} completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
                    IsLoadingIndicatorActive = false;
                }

                if (path.StartsWith(AppSettings.RecycleBinPath))
                {
                    // Recycle bin is special as files are enumerated by the fulltrust process
                    await EnumerateItemsFromSpecialFolderAsync(path);
                }
                else
                {
                    if (await EnumerateItemsFromStandardFolderAsync(path))
                    {
                        WatchForDirectoryChanges(path);
                    }
                }

                IsFolderEmptyTextDisplayed = FilesAndFolders.Count == 0;
                if (_addFilesCTS.IsCancellationRequested)
                {
                    _addFilesCTS.Dispose();
                    _addFilesCTS = new CancellationTokenSource();
                    IsLoadingItems = false;
                    return;
                }

                OrderFiles();
                stopwatch.Stop();
                Debug.WriteLine($"Loading of items in {WorkingDirectory} completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
                AssociatedInstance.NavigationToolbar.CanRefresh = true;
                IsLoadingItems = false;

                if (!string.IsNullOrWhiteSpace(previousDir))
                {
                    if (previousDir.Contains(WorkingDirectory) && !previousDir.Contains("Shell:RecycleBinFolder"))
                    {
                        // Remove the WorkingDir from previous dir
                        previousDir = previousDir.Replace(WorkingDirectory, string.Empty);

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
                        string folderToSelect = string.Format("{0}\\{1}", WorkingDirectory, previousDir);

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
                semaphoreSlim.Release();
            }

            UpdateDirectoryInfo();
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

            await Task.Run(async () =>
            {
                if (Connection != null)
                {
                    var value = new ValueSet();
                    value.Add("Arguments", "RecycleBin");
                    value.Add("action", "Enumerate");
                    // Send request to fulltrust process to enumerate recyclebin items
                    var response = await Connection.SendMessageAsync(value);
                    // If the request was canceled return now
                    if (_addFilesCTS.IsCancellationRequested)
                    {
                        return;
                    }
                    if (response.Status == AppServiceResponseStatus.Success
                        && response.Message.ContainsKey("Enumerate"))
                    {
                        var folderContentsList = JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response.Message["Enumerate"]);
                        var tempList = new List<ListedItem>();
                        for (int count = 0; count < folderContentsList.Count; count++)
                        {
                            var item = folderContentsList[count];
                            var listedItem = AddFileOrFolderFromShellFile(item, returnformat);
                            if (listedItem != null)
                            {
                                tempList.Add(listedItem);
                            }
                            if (count == 32 || count % 300 == 0 || count == folderContentsList.Count - 1)
                            {
                                var orderedList = OrderFiles2(tempList);
                                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                                {
                                    OrderFiles(orderedList);
                                });
                            }
                        }
                    }
                }
            });
        }

        public async Task<bool> EnumerateItemsFromStandardFolderAsync(string path)
        {
            // Flag to use FindFirstFileExFromApp or StorageFolder enumeration
            bool enumFromStorageFolder = false;

            var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(path).AsTask());
            if (res)
            {
                _rootFolder = res.Result;
            }
            else if (_workingRoot != null)
            {
                _rootFolder = _currentStorageFolder.Folder;
                enumFromStorageFolder = true;
            }
            else if (!CheckFolderAccessWithWin32(path)) // The folder is really inaccessible
            {
                if (res == FilesystemErrorCode.ERROR_UNAUTHORIZED)
                {
                    //TODO: proper dialog
                    await DialogDisplayHelper.ShowDialogAsync(
                        "AccessDeniedDeleteDialog/Title".GetLocalized(),
                        "SubDirectoryAccessDenied".GetLocalized());
                    return false;
                }
                else if (res == FilesystemErrorCode.ERROR_NOTFOUND)
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

            if (await CheckBitlockerStatusAsync(_rootFolder))
            {
                var bitlockerDialog = new Dialogs.BitlockerDialog(Path.GetPathRoot(WorkingDirectory));
                var bitlockerResult = await bitlockerDialog.ShowAsync();
                if (bitlockerResult == ContentDialogResult.Primary)
                {
                    var userInput = bitlockerDialog.storedPasswordInput;
                    if (Connection != null)
                    {
                        var value = new ValueSet();
                        value.Add("Arguments", "Bitlocker");
                        value.Add("action", "Unlock");
                        value.Add("drive", Path.GetPathRoot(WorkingDirectory));
                        value.Add("password", userInput);
                        await Connection.SendMessageAsync(value);

                        if (await CheckBitlockerStatusAsync(_rootFolder))
                        {
                            // Drive is still locked
                            await DialogDisplayHelper.ShowDialogAsync("BitlockerInvalidPwDialog/Title".GetLocalized(), "BitlockerInvalidPwDialog/Text".GetLocalized());
                        }
                    }
                }
            }

            // Is folder synced to cloud storage?
            var syncStatus = await CheckCloudDriveSyncStatusAsync(_rootFolder);
            AssociatedInstance.InstanceViewModel.IsPageTypeCloudDrive =
                syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown;

            if (enumFromStorageFolder)
            {
                CurrentFolder = new ListedItem(_rootFolder.FolderRelativeId, returnformat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemPropertiesInitialized = true,
                    ItemName = _rootFolder.Name,
                    ItemDateModifiedReal = (await _rootFolder.GetBasicPropertiesAsync()).DateModified,
                    ItemType = _rootFolder.DisplayType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(_rootFolder.Path) ? _currentStorageFolder.Path : _rootFolder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                };
                await EnumFromStorageFolderAsync();
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
                    await EnumFromStorageFolderAsync();
                    return false;
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        var tempList = new List<ListedItem>();
                        var hasNextFile = false;
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
                                            tempList.Add(listedItem);
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
                                                tempList.Add(listedItem);
                                                ++count;
                                            }
                                        }
                                    }
                                }
                            }
                            if (_addFilesCTS.IsCancellationRequested)
                            {
                                break;
                            }

                            hasNextFile = FindNextFile(hFile, out findData);
                            if (count == 32 || count % 300 == 0 || !hasNextFile)
                            {
                                var orderedList = OrderFiles2(tempList);
                                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                                {
                                    OrderFiles(orderedList);
                                });
                            }
                        } while (hasNextFile);

                        await fileListCache.SaveFileListToCache(path, new CacheEntry
                        {
                            CurrentFolder = CurrentFolder,
                            FileList = tempList
                        });

                        FindClose(hFile);
                    });
                    return true;
                }
            }
        }

        private async Task EnumFromStorageFolderAsync()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;

            var tempList = new List<ListedItem>();
            uint count = 0;
            while (true)
            {
                IStorageItem item = null;
                try
                {
                    var results = await _rootFolder.GetItemsAsync(count, 1);
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
                        tempList.Add(folder);
                    }
                    ++count;
                }
                else
                {
                    var file = item as StorageFile;
                    var fileEntry = await AddFileAsync(file, returnformat, true);
                    if (fileEntry != null)
                    {
                        tempList.Add(fileEntry);
                    }
                    ++count;
                }
                if (_addFilesCTS.IsCancellationRequested)
                {
                    break;
                }
                if (count == 32 || count % 300 == 0)
                {
                    OrderFiles(OrderFiles2(tempList));
                }
            }
            OrderFiles(OrderFiles2(tempList));
            stopwatch.Stop();
            await fileListCache.SaveFileListToCache(WorkingDirectory, new CacheEntry
            {
                CurrentFolder = CurrentFolder,
                FileList = tempList
            });
            Debug.WriteLine($"Enumerating items in {WorkingDirectory} (device) completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
        }

        public bool CheckFolderAccessWithWin32(string path)
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
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            Debug.WriteLine("WatchForDirectoryChanges: {0}", path);
            hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(path, 1, 1 | 2 | 4,
                IntPtr.Zero, 3, (uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped, IntPtr.Zero);
            if (hWatchDir.ToInt64() == -1)
            {
                return;
            }

            byte[] buff = new byte[4096];

            aWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
            {
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

                            const uint FILE_ACTION_ADDED = 0x00000001;
                            const uint FILE_ACTION_REMOVED = 0x00000002;
                            const uint FILE_ACTION_MODIFIED = 0x00000003;
                            const uint FILE_ACTION_RENAMED_OLD_NAME = 0x00000004;
                            const uint FILE_ACTION_RENAMED_NEW_NAME = 0x00000005;

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
                                try
                                {
                                    switch (action)
                                    {
                                        case FILE_ACTION_ADDED:
                                            Debug.WriteLine("File " + FileName + " added to working directory.");
                                            AddFileOrFolderAsync(FileName, returnformat).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_REMOVED:
                                            Debug.WriteLine("File " + FileName + " removed from working directory.");
                                            RemoveFileOrFolderAsync(FileName).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_MODIFIED:
                                            Debug.WriteLine("File " + FileName + " had attributes modified in the working directory.");
                                            UpdateFileOrFolderAsync(FileName).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_RENAMED_OLD_NAME:
                                            Debug.WriteLine("File " + FileName + " will be renamed in the working directory.");
                                            RemoveFileOrFolderAsync(FileName).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_RENAMED_NEW_NAME:
                                            Debug.WriteLine("File " + FileName + " was renamed in the working directory.");
                                            AddFileOrFolderAsync(FileName, returnformat).GetAwaiter().GetResult();
                                            break;

                                        default:
                                            Debug.WriteLine("File " + FileName + " performed an action in the working directory.");
                                            break;
                                    }
                                }
                                catch (Exception)
                                {
                                    // Prevent invalid operations
                                }

                                offset += notifyInfo.NextEntryOffset;
                            } while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

                            //ResetEvent(overlapped.hEvent);
                            Debug.WriteLine("Task running...");
                        }
                    }
                }
                CloseHandle(overlapped.hEvent);
                Debug.WriteLine("aWatcherAction done: {0}", rand);
            });

            Debug.WriteLine("Task exiting...");
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
                    ItemDateModifiedReal = item.ModifiedDateDT,
                    ItemDateDeletedReal = item.RecycleDateDT,
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
                    ItemDateModifiedReal = item.ModifiedDateDT,
                    ItemDateDeletedReal = item.RecycleDateDT,
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
            await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
            {
                _filesAndFolders.Add(item);
                IsFolderEmptyTextDisplayed = false;
            });
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
                var tempList = _filesAndFolders.ToList();
                tempList.Add(listedItem);
                var orderedList = OrderFiles2(tempList);
                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                {
                    IsFolderEmptyTextDisplayed = false;
                    OrderFiles(orderedList);
                    UpdateDirectoryInfo();
                });
            }
        }

        private void UpdateDirectoryInfo()
        {
            if (AssociatedInstance.ContentPage != null)
            {
                if (_filesAndFolders.Count == 1)
                {
                    AssociatedInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + "ItemCount/Text".GetLocalized();
                }
                else
                {
                    AssociatedInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + "ItemsCount/Text".GetLocalized();
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
            var matchingItem = FilesAndFolders.ToList().FirstOrDefault(x => x.ItemPath.Equals(path));
            if (matchingItem != null)
            {
                await UpdateFileOrFolderAsync(matchingItem);
            }
        }

        public async Task RemoveFileOrFolderAsync(ListedItem item)
        {
            await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
            {
                _filesAndFolders.Remove(item);
                IsFolderEmptyTextDisplayed = FilesAndFolders.Count == 0;
                App.JumpList.RemoveFolder(item.ItemPath);

                UpdateDirectoryInfo();
            });
        }

        public async Task RemoveFileOrFolderAsync(string path)
        {
            var matchingItem = FilesAndFolders.ToList().FirstOrDefault(x => x.ItemPath.Equals(path));
            if (matchingItem != null)
            {
                await RemoveFileOrFolderAsync(matchingItem);
            }
        }

        private ListedItem AddFolder(WIN32_FIND_DATA findData, string pathRoot, string dateReturnFormat)
        {
            if (_addFilesCTS.IsCancellationRequested)
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

            var pinned = App.SidebarPinnedController.Model.Items.Contains(itemPath);

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
                IsPinned = pinned,
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
                itemType = itemFileExtension.Trim('.') + " " + itemType;
            }

            bool itemFolderImgVis = false;
            bool itemThumbnailImgVis;
            bool itemEmptyImgVis;

            itemEmptyImgVis = true;
            itemThumbnailImgVis = false;

            if (_addFilesCTS.IsCancellationRequested)
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
                    if (_addFilesCTS.IsCancellationRequested)
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
                            ContainsFilesOrFolders = containsFilesOrFolders
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

        public void AddItemsToCollectionAsync(string path, string previousDir)
        {
            RapidAddItemsToCollectionAsync(path, previousDir);
        }

        private async Task<ListedItem> AddFolderAsync(StorageFolder folder, string dateReturnFormat)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if (!_addFilesCTS.IsCancellationRequested)
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
                    ItemPath = string.IsNullOrEmpty(folder.Path) ? Path.Combine(_currentStorageFolder.Path, folder.Name) : folder.Path,
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
            var itemPath = string.IsNullOrEmpty(file.Path) ? Path.Combine(_currentStorageFolder.Path, file.Name) : file.Path;
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
            if (_addFilesCTS.IsCancellationRequested)
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

        public void AddSearchResultsToCollection(ObservableCollection<ListedItem> searchItems, string currentSearchPath)
        {
            _filesAndFolders.Clear();
            foreach (ListedItem li in searchItems)
            {
                _filesAndFolders.Add(li);
            }
            UpdateDirectoryInfo();
            WorkingDirectoryChanged("SearchPagePathBoxOverrideText".GetLocalized() + " " + currentSearchPath);
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
            _addFilesCTS?.Dispose();
            _semaphoreCTS?.Dispose();
            CloseWatcher();
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