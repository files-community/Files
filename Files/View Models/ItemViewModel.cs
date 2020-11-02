using ByteSizeLib;
using Files.Common;
using Files.Dialogs;
using Files.Enums;
using Files.Helpers;
using Files.View_Models;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
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
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using static Files.Helpers.NativeDirectoryChangesHelper;
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.Filesystem
{
    public class ItemViewModel : INotifyPropertyChanged, IDisposable
    {
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private IntPtr hWatchDir;
        private IAsyncAction aWatcherAction;
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public SettingsViewModel AppSettings => App.AppSettings;
        private bool shouldDisplayFileExtensions = false;
        public ListedItem CurrentFolder { get; private set; }
        public CollectionViewSource viewSource;
        public BulkObservableCollection<ListedItem> _filesAndFolders;
        private CancellationTokenSource _addFilesCTS, _semaphoreCTS;
        private StorageFolder _rootFolder;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _jumpString = "";
        private readonly DispatcherTimer jumpTimer = new DispatcherTimer();

        private string _customPath;

        public string WorkingDirectory
        {
            get
            {
                return _currentStorageFolder?.Path ?? _customPath;
            }
        }

        private StorageFolderWithPath _currentStorageFolder;
        private StorageFolderWithPath _workingRoot;

        public async Task SetWorkingDirectory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = MainPage.sideBarItems.Where(x => !string.IsNullOrWhiteSpace(x.Path)).ToList();

            item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
            }

            if (!Path.IsPathRooted(value))
            {
                _workingRoot = null;
            }
            else if (!Path.IsPathRooted(WorkingDirectory) || Path.GetPathRoot(WorkingDirectory) != Path.GetPathRoot(value))
            {
                _workingRoot = await DrivesManager.GetRootFromPath(value);
            }

            if (App.CurrentInstance.SidebarSelectedItem != item)
            {
                App.CurrentInstance.SidebarSelectedItem = item;
            }

            if (!Path.IsPathRooted(value))
            {
                _currentStorageFolder = null;
                _customPath = value;
            }
            else
            {
                _currentStorageFolder = await StorageFileExtensions.GetFolderWithPathFromPathAsync(value, _workingRoot, _currentStorageFolder);
                _customPath = null;
            }

            if (value == "Home")
            {
                _currentStorageFolder = null;
            }
            else
            {
                App.JumpList.AddFolderToJumpList(value);
            }

            NotifyPropertyChanged(nameof(WorkingDirectory));
        }

        public static async Task<StorageFolder> GetFolderFromPathAsync(string value, IShellPage appInstance = null)
        {
            var instance = appInstance == null ? App.CurrentInstance.FilesystemViewModel : appInstance.FilesystemViewModel;
            return await StorageFileExtensions.GetFolderFromPathAsync(value, instance._workingRoot, instance._currentStorageFolder);
        }

        public static async Task<StorageFile> GetFileFromPathAsync(string value, IShellPage appInstance = null)
        {
            var instance = appInstance == null ? App.CurrentInstance.FilesystemViewModel : appInstance.FilesystemViewModel;
            return await StorageFileExtensions.GetFileFromPathAsync(value, instance._workingRoot, instance._currentStorageFolder);
        }

        public static async Task<StorageFolderWithPath> GetFolderWithPathFromPathAsync(string value)
        {
            var instance = App.CurrentInstance.FilesystemViewModel;
            return await StorageFileExtensions.GetFolderWithPathFromPathAsync(value, instance._workingRoot, instance._currentStorageFolder);
        }

        public static async Task<StorageFileWithPath> GetFileWithPathFromPathAsync(string value)
        {
            var instance = App.CurrentInstance.FilesystemViewModel;
            return await StorageFileExtensions.GetFileWithPathFromPathAsync(value, instance._workingRoot, instance._currentStorageFolder);
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
            get => AppSettings.DirectorySortOption == SortOption.Name;
            set
            {
                if (value)
                {
                    AppSettings.DirectorySortOption = SortOption.Name;
                    NotifyPropertyChanged(nameof(IsSortedByName));
                }
            }
        }

        public bool IsSortedByDate
        {
            get => AppSettings.DirectorySortOption == SortOption.DateModified;
            set
            {
                if (value)
                {
                    AppSettings.DirectorySortOption = SortOption.DateModified;
                    NotifyPropertyChanged(nameof(IsSortedByDate));
                }
            }
        }

        public bool IsSortedByType
        {
            get => AppSettings.DirectorySortOption == SortOption.FileType;
            set
            {
                if (value)
                {
                    AppSettings.DirectorySortOption = SortOption.FileType;
                    NotifyPropertyChanged(nameof(IsSortedByType));
                }
            }
        }

        public bool IsSortedBySize
        {
            get => AppSettings.DirectorySortOption == SortOption.Size;
            set
            {
                if (value)
                {
                    AppSettings.DirectorySortOption = SortOption.Size;
                    NotifyPropertyChanged(nameof(IsSortedBySize));
                }
            }
        }

        public bool IsSortedAscending
        {
            get => AppSettings.DirectorySortDirection == SortDirection.Ascending;
            set
            {
                AppSettings.DirectorySortDirection = value ? SortDirection.Ascending : SortDirection.Descending;
                NotifyPropertyChanged(nameof(IsSortedAscending));
                NotifyPropertyChanged(nameof(IsSortedDescending));
            }
        }

        public bool IsSortedDescending
        {
            get => !IsSortedAscending;
            set
            {
                AppSettings.DirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
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
                    if (App.CurrentInstance.ContentPage.IsItemSelected)
                    {
                        previouslySelectedItem = App.CurrentInstance.ContentPage.SelectedItem;
                    }

                    // If the user is trying to cycle through items
                    // starting with the same letter
                    if (value.Length == 1 && previouslySelectedItem != null)
                    {
                        // Try to select item lexicographically bigger than the previous item
                        jumpedToItem = candidateItems.FirstOrDefault(f => f.ItemName.CompareTo(previouslySelectedItem.ItemName) > 0);
                    }
                    if (jumpedToItem == null)
                        jumpedToItem = candidateItems.FirstOrDefault();

                    if (jumpedToItem != null)
                    {
                        App.CurrentInstance.ContentPage.SetSelectedItemOnUi(jumpedToItem);
                        App.CurrentInstance.ContentPage.ScrollIntoView(jumpedToItem);
                    }

                    // Restart the timer
                    jumpTimer.Start();
                }
                _jumpString = value;
            }
        }

        public ItemViewModel()
        {
            _filesAndFolders = new BulkObservableCollection<ListedItem>();
            FilesAndFolders = new ReadOnlyObservableCollection<ListedItem>(_filesAndFolders);
            _addFilesCTS = new CancellationTokenSource();
            _semaphoreCTS = new CancellationTokenSource();
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;
            jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
            jumpTimer.Tick += JumpTimer_Tick;
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

        private void WorkingDirectoryChanged()
        {
            // Clear the path UI
            App.CurrentInstance.NavigationToolbar.PathComponents.Clear();
            // Style tabStyleFixed = App.selectedTabInstance.accessiblePathTabView.Resources["PathSectionTabStyle"] as Style;
            FontWeight weight = new FontWeight()
            {
                Weight = FontWeights.SemiBold.Weight
            };
            foreach (var component in StorageFileExtensions.GetDirectoryPathComponents(WorkingDirectory))
            {
                App.CurrentInstance.NavigationToolbar.PathComponents.Add(component);
            }
        }

        public void CancelLoadAndClearFiles()
        {
            Debug.WriteLine("CancelLoadAndClearFiles");
            CloseWatcher();

            App.CurrentInstance.NavigationToolbar.CanRefresh = true;
            if (IsLoadingItems == false) { return; }

            _addFilesCTS.Cancel();
            _filesAndFolders.Clear();
            App.CurrentInstance.NavigationToolbar.CanGoBack = true;
            App.CurrentInstance.NavigationToolbar.CanGoForward = true;
            if (!(WorkingDirectory?.StartsWith(AppSettings.RecycleBinPath) ?? false))
            {
                // Can't go up from recycle bin
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = true;
            }
        }

        public void OrderFiles()
        {
            if (_filesAndFolders.Count == 0)
                return;

            static object orderByNameFunc(ListedItem item) => item.ItemName;
            Func<ListedItem, object> orderFunc = orderByNameFunc;
            NaturalStringComparer naturalStringComparer = new NaturalStringComparer();
            switch (AppSettings.DirectorySortOption)
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
            }

            // In ascending order, show folders first, then files.
            // So, we use == StorageItemTypes.File to make the value for a folder equal to 0, and equal to 1 for the rest.
            static bool folderThenFileAsync(ListedItem listedItem) => (listedItem.PrimaryItemAttribute == StorageItemTypes.File);
            IOrderedEnumerable<ListedItem> ordered;
            List<ListedItem> orderedList;

            if (AppSettings.DirectorySortDirection == SortDirection.Ascending)
            {
                if (AppSettings.DirectorySortOption == SortOption.Name)
                    ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc, naturalStringComparer);
                else
                    ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc);
            }
            else
            {
                if (AppSettings.DirectorySortOption == SortOption.FileType)
                {
                    if (AppSettings.DirectorySortOption == SortOption.Name)
                        ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                    else
                        ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc);
                }
                else
                {
                    if (AppSettings.DirectorySortOption == SortOption.Name)
                        ordered = _filesAndFolders.OrderByDescending(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                    else
                        ordered = _filesAndFolders.OrderByDescending(folderThenFileAsync).ThenByDescending(orderFunc);
                }
            }

            // Further order by name if applicable
            if (AppSettings.DirectorySortOption != SortOption.Name)
            {
                if (AppSettings.DirectorySortDirection == SortDirection.Ascending)
                    ordered = ordered.ThenBy(orderByNameFunc, naturalStringComparer);
                else
                    ordered = ordered.ThenByDescending(orderByNameFunc, naturalStringComparer);
            }
            orderedList = ordered.ToList();

            List<ListedItem> originalList = _filesAndFolders.ToList();

            _filesAndFolders.BeginBulkOperation();
            for (var i = 0; i < originalList.Count; i++)
            {
                if (originalList[i] != orderedList[i])
                {
                    _filesAndFolders.RemoveAt(i);
                    _filesAndFolders.Insert(i, orderedList[i]);
                }
            }
            _filesAndFolders.EndBulkOperation();
        }

        private bool _isLoadingItems = false;

        public bool IsLoadingItems
        {
            get
            {
                return _isLoadingItems;
            }
            internal set
            {
                if (_isLoadingItems != value)
                {
                    _isLoadingItems = value;
                    NotifyPropertyChanged(nameof(IsLoadingItems));
                }
            }
        }

        // This works for recycle bin as well as GetFileFromPathAsync/GetFolderFromPathAsync work
        // for file inside the recycle bin (but not on the recycle bin folder itself)
        public async void LoadExtendedItemProperties(ListedItem item, uint thumbnailSize = 20)
        {
            if (!item.ItemPropertiesInitialized)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    var matchingItem = _filesAndFolders.FirstOrDefault(x => x == item);
                    try
                    {
                        StorageFile matchingStorageItem = await StorageFileExtensions.GetFileFromPathAsync((item as ShortcutItem)?.TargetPath ?? item.ItemPath, _workingRoot, _currentStorageFolder);
                        if (matchingItem != null && matchingStorageItem != null)
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
                            if (item.IsShortcutItem)
                            {
                                // Reset cloud sync status icon
                                matchingItem.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false };
                            }
                            else
                            {
                                matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                matchingItem.ItemType = matchingStorageItem.DisplayType;
                                var syncStatus = await CheckCloudDriveSyncStatus(matchingStorageItem);
                                matchingItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                matchingItem.IconOverlay = (await LoadIconOverlay(matchingItem.ItemPath)).Icon;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (matchingItem != null)
                        {
                            // Reset cloud sync status icon
                            matchingItem.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false };
                        }
                        item.ItemPropertiesInitialized = true;
                        return;
                    }
                }
                else
                {
                    var matchingItem = _filesAndFolders.FirstOrDefault(x => x == item);
                    try
                    {
                        StorageFolder matchingStorageItem = await StorageFileExtensions.GetFolderFromPathAsync((item as ShortcutItem)?.TargetPath ?? item.ItemPath, _workingRoot, _currentStorageFolder);
                        if (matchingItem != null && matchingStorageItem != null)
                        {
                            var iconOverlay = await LoadIconOverlay(matchingItem.ItemPath);
                            if (iconOverlay.IsCustom)
                            {
                                // Only set folder icon if it's a custom icon
                                using (var Thumbnail = await matchingStorageItem.GetThumbnailAsync(ThumbnailMode.SingleItem, thumbnailSize, ThumbnailOptions.UseCurrentScale))
                                {
                                    if (Thumbnail != null)
                                    {
                                        matchingItem.FileImage = new BitmapImage();
                                        await matchingItem.FileImage.SetSourceAsync(Thumbnail);
                                        matchingItem.LoadUnknownTypeGlyph = false;
                                        matchingItem.LoadFolderGlyph = false;
                                        matchingItem.LoadFileIcon = true;
                                    }
                                }
                            }
                            if (item.IsShortcutItem)
                            {
                                // Reset cloud sync status icon
                                matchingItem.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false };
                            }
                            else
                            {
                                matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                                matchingItem.ItemType = matchingStorageItem.DisplayType;
                                var syncStatus = await CheckCloudDriveSyncStatus(matchingStorageItem);
                                matchingItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                                matchingItem.IconOverlay = iconOverlay.Icon;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (matchingItem != null)
                        {
                            // Reset cloud sync status icon
                            matchingItem.SyncStatusUI = new CloudDriveSyncStatusUI() { LoadSyncStatus = false };
                        }
                        item.ItemPropertiesInitialized = true;
                        return;
                    }
                }

                item.ItemPropertiesInitialized = true;
            }
        }

        private async Task<(BitmapImage Icon, bool IsCustom)> LoadIconOverlay(string filePath)
        {
            if (App.Connection != null)
            {
                var value = new ValueSet();
                value.Add("Arguments", "GetIconOverlay");
                value.Add("filePath", filePath);
                var response = await App.Connection.SendMessageAsync(value);
                var hasCustomIcon = (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
                    && response.Message.Get("HasCustomIcon", false);
                var iconOverlay = response.Message.Get("IconOverlay", (string)null);
                if (iconOverlay != null)
                {
                    var image = new BitmapImage();
                    byte[] bitmapData = Convert.FromBase64String(iconOverlay);
                    using (var ms = new MemoryStream(bitmapData))
                    {
                        await image.SetSourceAsync(ms.AsRandomAccessStream());
                    }
                    return (image, hasCustomIcon);
                }
                return (null, hasCustomIcon);
            }
            return (null, false);
        }

        public void RefreshItems()
        {
            AddItemsToCollectionAsync(WorkingDirectory);
        }

        public async void RapidAddItemsToCollectionAsync(string path)
        {
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;

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
                App.InteractionViewModel.IsContentLoadingIndicatorVisible = true;

                switch (path)
                {
                    case "Desktop":
                        await SetWorkingDirectory(App.AppSettings.DesktopPath);
                        break;

                    case "Downloads":
                        await SetWorkingDirectory(App.AppSettings.DownloadsPath);
                        break;

                    case "Documents":
                        await SetWorkingDirectory(App.AppSettings.DocumentsPath);
                        break;

                    case "Pictures":
                        await SetWorkingDirectory(App.AppSettings.PicturesPath);
                        break;

                    case "Music":
                        await SetWorkingDirectory(App.AppSettings.MusicPath);
                        break;

                    case "Videos":
                        await SetWorkingDirectory(App.AppSettings.VideosPath);
                        break;

                    case "RecycleBin":
                        await SetWorkingDirectory(App.AppSettings.RecycleBinPath);
                        break;

                    case "OneDrive":
                        await SetWorkingDirectory(App.AppSettings.OneDrivePath);
                        break;

                    default:
                        await SetWorkingDirectory(path);
                        break;
                }

                App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
                App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;

                if (path.StartsWith(AppSettings.RecycleBinPath))
                {
                    // Recycle bin is special as files are enumerated by the fulltrust process
                    await EnumerateItemsFromSpecialFolder(path);
                }
                else
                {
                    if (await EnumerateItemsFromStandardFolder(path))
                    {
                        WatchForDirectoryChanges(path);
                    }
                }

                if (FilesAndFolders.Count == 0)
                {
                    if (_addFilesCTS.IsCancellationRequested)
                    {
                        _addFilesCTS.Dispose();
                        _addFilesCTS = new CancellationTokenSource();
                        IsLoadingItems = false;
                        return;
                    }
                    IsFolderEmptyTextDisplayed = true;
                }
                else
                {
                    IsFolderEmptyTextDisplayed = false;
                }

                OrderFiles();
                stopwatch.Stop();
                Debug.WriteLine($"Loading of items in {WorkingDirectory} completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
                App.CurrentInstance.NavigationToolbar.CanRefresh = true;
                App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                IsLoadingItems = false;
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

        public async Task EnumerateItemsFromSpecialFolder(string path)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
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

            if (App.Connection != null)
            {
                var value = new ValueSet();
                value.Add("Arguments", "RecycleBin");
                value.Add("action", "Enumerate");
                // Send request to fulltrust process to enumerate recyclebin items
                var response = await App.Connection.SendMessageAsync(value);
                // If the request was canceled return now
                if (_addFilesCTS.IsCancellationRequested)
                {
                    IsLoadingItems = false;
                    return;
                }
                if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                    && response.Message.ContainsKey("Enumerate"))
                {
                    var folderContentsList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response.Message["Enumerate"]);
                    for (int count = 0; count < folderContentsList.Count; count++)
                    {
                        var item = folderContentsList[count];
                        AddFileOrFolderFromShellFile(item, returnformat);
                        if (count % 64 == 0)
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.YieldAsync();
                        }
                    }
                }
            }
        }

        public async Task<bool> EnumerateItemsFromStandardFolder(string path)
        {
            // Flag to use FindFirstFileExFromApp or StorageFolder enumeration
            bool enumFromStorageFolder = false;

            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(path);
            }
            catch (UnauthorizedAccessException)
            {
                var consentDialogDisplay = new ConsentDialog();
                await consentDialogDisplay.ShowAsync(ContentDialogPlacement.Popup);
                return false;
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialog(
                    "FolderNotFoundDialog/Title".GetLocalized(),
                    "FolderNotFoundDialog/Text".GetLocalized());
                IsLoadingItems = false;
                return false;
            }
            catch (Exception e)
            {
                if (_workingRoot != null)
                {
                    _rootFolder = _currentStorageFolder.Folder;
                    enumFromStorageFolder = true;
                }
                else
                {
                    await DialogDisplayHelper.ShowDialog("DriveUnpluggedDialog/Title".GetLocalized(), e.Message);
                    IsLoadingItems = false;
                    return false;
                }
            }

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;

            if (await CheckBitlockerStatus(_rootFolder))
            {
                var bitlockerDialog = new Dialogs.BitlockerDialog(Path.GetPathRoot(WorkingDirectory));
                var bitlockerResult = await bitlockerDialog.ShowAsync();
                if (bitlockerResult == ContentDialogResult.Primary)
                {
                    var userInput = bitlockerDialog.storedPasswordInput;
                    if (App.Connection != null)
                    {
                        var value = new ValueSet();
                        value.Add("Arguments", "Bitlocker");
                        value.Add("action", "Unlock");
                        value.Add("drive", Path.GetPathRoot(WorkingDirectory));
                        value.Add("password", userInput);
                        await App.Connection.SendMessageAsync(value);

                        if (await CheckBitlockerStatus(_rootFolder))
                        {
                            // Drive is still locked
                            await DialogDisplayHelper.ShowDialog("BitlockerInvalidPwDialog/Title".GetLocalized(), "BitlockerInvalidPwDialog/Text".GetLocalized());
                        }
                    }
                }
            }

            // Is folder synced to cloud storage?
            var syncStatus = await CheckCloudDriveSyncStatus(_rootFolder);
            App.CurrentInstance.InstanceViewModel.IsPageTypeCloudDrive =
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
                await EnumFromStorageFolder();
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
                }).WithTimeout(TimeSpan.FromSeconds(5));

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

                CurrentFolder = new ListedItem(_rootFolder.FolderRelativeId, returnformat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemPropertiesInitialized = true,
                    ItemName = _rootFolder.Name,
                    ItemDateModifiedReal = itemDate,
                    ItemType = _rootFolder.DisplayType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(_rootFolder.Path) ? _currentStorageFolder.Path : _rootFolder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                };

                var count = 0;
                if (hFile == IntPtr.Zero)
                {
                    await DialogDisplayHelper.ShowDialog("DriveUnpluggedDialog/Title".GetLocalized(), "");
                    IsLoadingItems = false;
                    return false;
                }
                else if (hFile.ToInt64() == -1)
                {
                    await EnumFromStorageFolder();
                    return false;
                }
                else
                {
                    do
                    {
                        if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) != FileAttributes.Hidden && ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) != FileAttributes.System)
                        {
                            if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                            {
                                AddFile(findData, path, returnformat);
                                ++count;
                            }
                            else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                if (findData.cFileName != "." && findData.cFileName != "..")
                                {
                                    AddFolder(findData, path, returnformat);
                                    ++count;
                                }
                            }
                        }
                        if (_addFilesCTS.IsCancellationRequested)
                        {
                            break;
                        }

                        if (count % 64 == 0)
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.YieldAsync();
                        }
                    } while (FindNextFile(hFile, out findData));

                    FindClose(hFile);
                    return true;
                }
            }
        }

        private async Task EnumFromStorageFolder()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;

            uint count = 0;
            while (true)
            {
                IStorageItem item = null;
                try
                {
                    var results = await _rootFolder.GetItemsAsync(count, 1);
                    item = results?.FirstOrDefault();
                    if (item == null) break;
                }
                catch (UnauthorizedAccessException)
                {
                    ++count;
                    continue;
                }
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    await AddFolder(item as StorageFolder, returnformat);
                    ++count;
                }
                else
                {
                    var file = item as StorageFile;
                    await AddFile(file, returnformat, true);
                    ++count;
                }
                if (_addFilesCTS.IsCancellationRequested)
                {
                    break;
                }
                if (count % 64 == 0)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.YieldAsync();
                }
            }
            stopwatch.Stop();
            Debug.WriteLine($"Enumerating items in {WorkingDirectory} (device) completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
        }

        private async Task<bool> CheckBitlockerStatus(StorageFolder rootFolder)
        {
            if (Path.IsPathRooted(WorkingDirectory) && Path.GetPathRoot(WorkingDirectory) == WorkingDirectory)
            {
                IDictionary<string, object> extraProperties = await rootFolder.Properties.RetrievePropertiesAsync(new string[] { "System.Volume.BitLockerProtection" });
                return (int?)extraProperties["System.Volume.BitLockerProtection"] == 6; // Drive is bitlocker protected and locked
            }
            return false;
        }

        private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatus(IStorageItem item)
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
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            Debug.WriteLine("WatchForDirectoryChanges: {0}", path);
            hWatchDir = CreateFileFromApp(path, 1, 1 | 2 | 4,
                IntPtr.Zero, 3, (uint)File_Attributes.BackupSemantics | (uint)File_Attributes.Overlapped, IntPtr.Zero);
            if (hWatchDir.ToInt64() == -1) return;

            byte[] buff = new byte[4096];

            aWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
            {
                var rand = Guid.NewGuid();
                buff = new byte[4096];
                int notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME;
                if (App.CurrentInstance.InstanceViewModel.IsPageTypeCloudDrive)
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
                            if (x.Status == AsyncStatus.Canceled) { break; }
                            var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);
                            Debug.WriteLine("wait done: {0}", rand);

                            const uint FILE_ACTION_ADDED = 0x00000001;
                            const uint FILE_ACTION_REMOVED = 0x00000002;
                            const uint FILE_ACTION_MODIFIED = 0x00000003;
                            const uint FILE_ACTION_RENAMED_OLD_NAME = 0x00000004;
                            const uint FILE_ACTION_RENAMED_NEW_NAME = 0x00000005;

                            uint offset = 0;
                            ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
                            if (x.Status == AsyncStatus.Canceled) { break; }

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
                                            AddFileOrFolder(FileName, returnformat).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_REMOVED:
                                            Debug.WriteLine("File " + FileName + " removed from working directory.");
                                            RemoveFileOrFolder(FileName).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_MODIFIED:
                                            Debug.WriteLine("File " + FileName + " had attributes modified in the working directory.");
                                            UpdateFileOrFolder(FileName).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_RENAMED_OLD_NAME:
                                            Debug.WriteLine("File " + FileName + " will be renamed in the working directory.");
                                            RemoveFileOrFolder(FileName).GetAwaiter().GetResult();
                                            break;

                                        case FILE_ACTION_RENAMED_NEW_NAME:
                                            Debug.WriteLine("File " + FileName + " was renamed in the working directory.");
                                            AddFileOrFolder(FileName, returnformat).GetAwaiter().GetResult();
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

        public void AddFileOrFolderFromShellFile(ShellFileItem item, string dateReturnFormat = null)
        {
            if (dateReturnFormat == null)
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                dateReturnFormat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            }

            if (item.IsFolder)
            {
                // Folder
                _filesAndFolders.Add(new RecycleBinItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = item.FileName,
                    ItemDateModifiedReal = item.RecycleDate,
                    ItemType = item.FileType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = item.RecyclePath, // this is the true path on disk so other stuff can work as is
                    ItemOriginalPath = item.FilePath,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0,
                    //FolderTooltipText = tooltipString,
                });
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

                _filesAndFolders.Add(new RecycleBinItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    LoadUnknownTypeGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    LoadFolderGlyph = false,
                    ItemName = itemName,
                    ItemDateModifiedReal = item.RecycleDate,
                    ItemType = item.FileType,
                    ItemPath = item.RecyclePath, // this is the true path on disk so other stuff can work as is
                    ItemOriginalPath = item.FilePath,
                    FileSize = item.FileSize,
                    FileSizeBytes = (long)item.FileSizeBytes
                });
            }

            IsFolderEmptyTextDisplayed = false;
            UpdateDirectoryInfo();
        }

        private void AddFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Add(item);
            IsFolderEmptyTextDisplayed = false;
        }

        private async Task AddFileOrFolder(string fileOrFolderPath, string dateReturnFormat)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_CASE_SENSITIVE;

            IntPtr hFile = FindFirstFileExFromApp(fileOrFolderPath, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);
            FindClose(hFile);

            await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
            {
                if ((findData.dwFileAttributes & 0x10) > 0) // FILE_ATTRIBUTE_DIRECTORY
                {
                    AddFolder(findData, Directory.GetParent(fileOrFolderPath).FullName, dateReturnFormat);
                }
                else
                {
                    AddFile(findData, Directory.GetParent(fileOrFolderPath).FullName, dateReturnFormat);
                }
                UpdateDirectoryInfo();
            });
        }

        private void UpdateDirectoryInfo()
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                if (_filesAndFolders.Count == 1)
                {
                    App.CurrentInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + "ItemCount/Text".GetLocalized();
                }
                else
                {
                    App.CurrentInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + "ItemsCount/Text".GetLocalized();
                }
            }
        }

        private async Task UpdateFileOrFolder(ListedItem item)
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
                var syncStatus = await CheckCloudDriveSyncStatus(storageItem);
                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
                {
                    item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
                });
            }
        }

        private async Task UpdateFileOrFolder(string path)
        {
            var matchingItem = FilesAndFolders.ToList().FirstOrDefault(x => x.ItemPath.Equals(path));
            if (matchingItem != null)
            {
                await UpdateFileOrFolder(matchingItem);
            }
        }

        public async Task RemoveFileOrFolder(ListedItem item)
        {
            await CoreApplication.MainView.ExecuteOnUIThreadAsync(() =>
            {
                _filesAndFolders.Remove(item);
                if (_filesAndFolders.Count == 0)
                {
                    IsFolderEmptyTextDisplayed = true;
                }
                App.JumpList.RemoveFolder(item.ItemPath);

                UpdateDirectoryInfo();
            });
        }

        public async Task RemoveFileOrFolder(string path)
        {
            var matchingItem = FilesAndFolders.ToList().FirstOrDefault(x => x.ItemPath.Equals(path));
            if (matchingItem != null)
            {
                await RemoveFileOrFolder(matchingItem);
            }
        }

        private void AddFolder(WIN32_FIND_DATA findData, string pathRoot, string dateReturnFormat)
        {
            if (_addFilesCTS.IsCancellationRequested)
            {
                IsLoadingItems = false;
                return;
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
                return;
            }
            var itemPath = Path.Combine(pathRoot, findData.cFileName);

            _filesAndFolders.Add(new ListedItem(null, dateReturnFormat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemName = findData.cFileName,
                ItemDateModifiedReal = itemDate,
                ItemType = "FileFolderListItem".GetLocalized(),
                LoadFolderGlyph = true,
                FileImage = null,
                LoadFileIcon = false,
                ItemPath = itemPath,
                LoadUnknownTypeGlyph = false,
                FileSize = null,
                FileSizeBytes = 0,
                ContainsFilesOrFolders = CheckForFilesFolders(itemPath)
                //FolderTooltipText = tooltipString,
            });

            IsFolderEmptyTextDisplayed = false;
        }

        private void AddFile(WIN32_FIND_DATA findData, string pathRoot, string dateReturnFormat)
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
                return;
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
            BitmapImage icon = new BitmapImage();
            bool itemThumbnailImgVis;
            bool itemEmptyImgVis;

            itemEmptyImgVis = true;
            itemThumbnailImgVis = false;

            if (_addFilesCTS.IsCancellationRequested)
            {
                IsLoadingItems = false;
                return;
            }

            if (findData.cFileName.EndsWith(".lnk") || findData.cFileName.EndsWith(".url"))
            {
                if (App.Connection != null)
                {
                    var response = App.Connection.SendMessageAsync(new ValueSet() {
                        { "Arguments", "FileOperation" },
                        { "fileop", "ParseLink" },
                        { "filepath", itemPath } }).AsTask().Result;
                    // If the request was canceled return now
                    if (_addFilesCTS.IsCancellationRequested)
                    {
                        IsLoadingItems = false;
                        return;
                    }
                    if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                        && response.Message.ContainsKey("TargetPath"))
                    {
                        var isUrl = findData.cFileName.EndsWith(".url");
                        string target = (string)response.Message["TargetPath"];
                        bool containsFilesOrFolders = false;

                        if ((bool)response.Message["IsFolder"])
                        {
                            containsFilesOrFolders = CheckForFilesFolders(target);
                        }

                        _filesAndFolders.Add(new ShortcutItem(null, dateReturnFormat)
                        {
                            PrimaryItemAttribute = (bool)response.Message["IsFolder"] ? StorageItemTypes.Folder : StorageItemTypes.File,
                            FileExtension = itemFileExtension,
                            FileImage = !(bool)response.Message["IsFolder"] ? icon : null,
                            LoadFileIcon = !(bool)response.Message["IsFolder"] && itemThumbnailImgVis,
                            LoadUnknownTypeGlyph = !(bool)response.Message["IsFolder"] && !isUrl && itemEmptyImgVis,
                            LoadFolderGlyph = (bool)response.Message["IsFolder"],
                            ItemName = itemName,
                            ItemDateModifiedReal = itemModifiedDate,
                            ItemDateAccessedReal = itemLastAccessDate,
                            ItemDateCreatedReal = itemCreatedDate,
                            ItemType = isUrl ? "ShortcutWebLinkFileType" : "ShortcutFileType".GetLocalized(),
                            ItemPath = itemPath,
                            FileSize = itemSize,
                            FileSizeBytes = itemSizeBytes,
                            TargetPath = target,
                            Arguments = (string)response.Message["Arguments"],
                            WorkingDirectory = (string)response.Message["WorkingDirectory"],
                            RunAsAdmin = (bool)response.Message["RunAsAdmin"],
                            IsUrl = isUrl,
                            ContainsFilesOrFolders = containsFilesOrFolders
                        });
                    }
                }
            }
            else
            {
                _filesAndFolders.Add(new ListedItem(null, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    LoadUnknownTypeGlyph = itemEmptyImgVis,
                    FileImage = icon,
                    LoadFileIcon = itemThumbnailImgVis,
                    LoadFolderGlyph = itemFolderImgVis,
                    ItemName = itemName,
                    ItemDateModifiedReal = itemModifiedDate,
                    ItemDateAccessedReal = itemLastAccessDate,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = itemSizeBytes
                });
            }

            IsFolderEmptyTextDisplayed = false;
        }

        public void AddItemsToCollectionAsync(string path)
        {
            RapidAddItemsToCollectionAsync(path);
        }

        public async Task AddFolder(StorageFolder folder, string dateReturnFormat)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if ((App.CurrentInstance.ContentFrame.SourcePageType == typeof(GenericFileBrowser)) || (App.CurrentInstance.ContentFrame.SourcePageType == typeof(GridViewBrowser)))
            {
                if (_addFilesCTS.IsCancellationRequested)
                {
                    IsLoadingItems = false;
                    return;
                }

                _filesAndFolders.Add(new ListedItem(folder.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = folder.Name,
                    ItemDateModifiedReal = basicProperties.DateModified,
                    ItemType = folder.DisplayType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(folder.Path) ? Path.Combine(_currentStorageFolder.Path, folder.Name) : folder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                    //FolderTooltipText = tooltipString,
                });

                IsFolderEmptyTextDisplayed = false;
            }
        }

        public async Task AddFile(StorageFile file, string dateReturnFormat, bool suppressThumbnailLoading = false)
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

            if (!(App.CurrentInstance.ContentFrame.SourcePageType == typeof(GridViewBrowser)))
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
                IsLoadingItems = false;
                return;
            }

            if (file.Name.EndsWith(".lnk") || file.Name.EndsWith(".url"))
            {
                // This shouldn't happen, StorageFile api does not support shortcuts
                Debug.WriteLine("Something strange: StorageFile api returned a shortcut");
            }
            else
            {
                _filesAndFolders.Add(new ListedItem(file.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    LoadUnknownTypeGlyph = itemEmptyImgVis,
                    FileImage = icon,
                    LoadFileIcon = itemThumbnailImgVis,
                    LoadFolderGlyph = itemFolderImgVis,
                    ItemName = itemName,
                    ItemDateModifiedReal = itemDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = (long)itemSizeBytes
                });
            }

            IsFolderEmptyTextDisplayed = false;
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
}