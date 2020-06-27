using ByteSizeLib;
using Files.Common;
using Files.Enums;
using Files.Helpers;
using Files.View_Models;
using Files.Views.Pages;
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
using FileAttributes = System.IO.FileAttributes;
using static Files.Helpers.NativeDirectoryChangesHelper;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files.Filesystem
{
    public class ItemViewModel : INotifyPropertyChanged, IDisposable
    {
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private IntPtr hWatchDir;
        private IAsyncAction aWatcherAction;
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public SettingsViewModel AppSettings => App.AppSettings;

        public ListedItem CurrentFolder { get; private set; }
        public CollectionViewSource viewSource;
        public BulkObservableCollection<ListedItem> _filesAndFolders;
        private CancellationTokenSource _addFilesCTS, _semaphoreCTS;
        private StorageFolder _rootFolder;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _jumpString = "";
        private readonly DispatcherTimer jumpTimer = new DispatcherTimer();
        private SortOption _directorySortOption = SortOption.Name;
        private SortDirection _directorySortDirection = SortDirection.Ascending;

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
                return;
            if (WorkingDirectory == value)
                return;

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = App.sideBarItems.Where(x => !string.IsNullOrWhiteSpace(x.Path)).ToList();

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

            NotifyPropertyChanged("WorkingDirectory");
        }

        public static async Task<StorageFolder> GetFolderFromPathAsync(string value)
        {
            var instance = App.CurrentInstance.FilesystemViewModel;
            return await StorageFileExtensions.GetFolderFromPathAsync(value, instance._workingRoot, instance._currentStorageFolder);
        }
        public static async Task<StorageFile> GetFileFromPathAsync(string value)
        {
            var instance = App.CurrentInstance.FilesystemViewModel;
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
                    NotifyPropertyChanged("IsFolderEmptyTextDisplayed");
                }
            }
        }

        public SortOption DirectorySortOption
        {
            get
            {
                return _directorySortOption;
            }
            set
            {
                if (value != _directorySortOption)
                {
                    _directorySortOption = value;
                    NotifyPropertyChanged("DirectorySortOption");
                    NotifyPropertyChanged("IsSortedByName");
                    NotifyPropertyChanged("IsSortedByDate");
                    NotifyPropertyChanged("IsSortedByType");
                    NotifyPropertyChanged("IsSortedBySize");
                    OrderFiles();
                }
            }
        }

        public SortDirection DirectorySortDirection
        {
            get
            {
                return _directorySortDirection;
            }
            set
            {
                if (value != _directorySortDirection)
                {
                    _directorySortDirection = value;
                    NotifyPropertyChanged("DirectorySortDirection");
                    NotifyPropertyChanged("IsSortedAscending");
                    NotifyPropertyChanged("IsSortedDescending");
                    OrderFiles();
                }
            }
        }

        public bool IsSortedByName
        {
            get => DirectorySortOption == SortOption.Name;
            set
            {
                if (value)
                {
                    DirectorySortOption = SortOption.Name;
                    NotifyPropertyChanged("IsSortedByName");
                    NotifyPropertyChanged("DirectorySortOption");
                }
            }
        }

        public bool IsSortedByDate
        {
            get => DirectorySortOption == SortOption.DateModified;
            set
            {
                if (value)
                {
                    DirectorySortOption = SortOption.DateModified;
                    NotifyPropertyChanged("IsSortedByDate");
                    NotifyPropertyChanged("DirectorySortOption");
                }
            }
        }

        public bool IsSortedByType
        {
            get => DirectorySortOption == SortOption.FileType;
            set
            {
                if (value)
                {
                    DirectorySortOption = SortOption.FileType;
                    NotifyPropertyChanged("IsSortedByType");
                    NotifyPropertyChanged("DirectorySortOption");
                }
            }
        }

        public bool IsSortedBySize
        {
            get => DirectorySortOption == SortOption.Size;
            set
            {
                if (value)
                {
                    DirectorySortOption = SortOption.Size;
                    NotifyPropertyChanged("IsSortedBySize");
                    NotifyPropertyChanged("DirectorySortOption");
                }
            }
        }

        public bool IsSortedAscending
        {
            get => DirectorySortDirection == SortDirection.Ascending;
            set
            {
                DirectorySortDirection = value ? SortDirection.Ascending : SortDirection.Descending;
                NotifyPropertyChanged("IsSortedAscending");
                NotifyPropertyChanged("IsSortedDescending");
                NotifyPropertyChanged("DirectorySortDirection");
            }
        }

        public bool IsSortedDescending
        {
            get => !IsSortedAscending;
            set
            {
                DirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
                NotifyPropertyChanged("IsSortedAscending");
                NotifyPropertyChanged("IsSortedDescending");
                NotifyPropertyChanged("DirectorySortDirection");
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
            switch (DirectorySortOption)
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

            if (DirectorySortDirection == SortDirection.Ascending)
            {
                if (DirectorySortOption == SortOption.Name)
                    ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc, naturalStringComparer);
                else
                    ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenBy(orderFunc);
            }
            else
            {
                if (DirectorySortOption == SortOption.FileType)
                {
                    if (DirectorySortOption == SortOption.Name)
                        ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                    else
                        ordered = _filesAndFolders.OrderBy(folderThenFileAsync).ThenByDescending(orderFunc);
                }
                else
                {
                    if (DirectorySortOption == SortOption.Name)
                        ordered = _filesAndFolders.OrderByDescending(folderThenFileAsync).ThenByDescending(orderFunc, naturalStringComparer);
                    else
                        ordered = _filesAndFolders.OrderByDescending(folderThenFileAsync).ThenByDescending(orderFunc);
                }
            }

            // Further order by name if applicable
            if (DirectorySortOption != SortOption.Name)
            {
                if (DirectorySortDirection == SortDirection.Ascending)
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
                    NotifyPropertyChanged("IsLoadingItems");
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
                    BitmapImage icon = new BitmapImage();
                    var matchingItem = _filesAndFolders.FirstOrDefault(x => x == item);
                    try
                    {
                        StorageFile matchingStorageItem = await StorageFileExtensions.GetFileFromPathAsync(item.ItemPath, _workingRoot);
                        if (matchingItem != null && matchingStorageItem != null)
                        {
                            matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                            matchingItem.ItemType = matchingStorageItem.DisplayType;
                            using (var Thumbnail = await matchingStorageItem.GetThumbnailAsync(ThumbnailMode.SingleItem, thumbnailSize, ThumbnailOptions.UseCurrentScale))
                            {
                                if (Thumbnail != null)
                                {
                                    matchingItem.FileImage = icon;
                                    await icon.SetSourceAsync(Thumbnail);
                                    matchingItem.LoadUnknownTypeGlyph = false;
                                    matchingItem.LoadFileIcon = true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        item.ItemPropertiesInitialized = true;
                        return;
                    }
                }
                else
                {
                    var matchingItem = _filesAndFolders.FirstOrDefault(x => x == item);
                    try
                    {
                        StorageFolder matchingStorageItem = await StorageFileExtensions.GetFolderFromPathAsync(item.ItemPath, _workingRoot);
                        if (matchingItem != null && matchingStorageItem != null)
                        {
                            matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                            matchingItem.ItemType = matchingStorageItem.DisplayType;
                        }
                    }
                    catch (Exception)
                    {
                        item.ItemPropertiesInitialized = true;
                        return;
                    }
                }

                item.ItemPropertiesInitialized = true;
            }
        }

        public void RefreshItems()
        {
            AddItemsToCollectionAsync(WorkingDirectory);
        }

        public async void RapidAddItemsToCollectionAsync(string path)
        {
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(new DirectoryInfo(path).Name, path);
            CancelLoadAndClearFiles();

            try
            {
                // Only one instance at a time should access this function
                // Wait here until the previous one has ended
                // If we're waiting and a new update request comes through
                // simply drop this instance
                await semaphoreSlim.WaitAsync(_semaphoreCTS.Token);
            }
            catch (OperationCanceledException)
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
                    await EnumerateItemsFromStandardFolder(path);
                    WatchForDirectoryChanges(path);
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
            CurrentFolder = new ListedItem(null)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemPropertiesInitialized = true,
                ItemName = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                ItemDateModifiedReal = DateTimeOffset.Now, // Fake for now
                ItemType = ResourceController.GetTranslation("FileFolderListItem"),
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
                        if (item.IsFolder)
                        {
                            // Folder
                            _filesAndFolders.Add(new ListedItem(null)
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
                                FileSizeBytes = 0
                                //FolderTooltipText = tooltipString,
                            });
                        }
                        else
                        {
                            // File
                            string itemName;
                            if (AppSettings.ShowFileExtensions)
                                itemName = item.FileName;
                            else
                                itemName = Path.GetFileNameWithoutExtension(item.FileName);

                            string itemFileExtension = null;
                            if (item.FileName.Contains('.'))
                            {
                                itemFileExtension = Path.GetExtension(item.FileName);
                            }

                            _filesAndFolders.Add(new ListedItem(null)
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
                        if (count % 64 == 0)
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.YieldAsync();
                        }
                    }
                }
            }
        }

        public async Task EnumerateItemsFromStandardFolder(string path)
        {
            // Flag to use FindFirstFileExFromApp or StorageFolder enumeration
            bool enumFromStorageFolder = false;

            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(path);
            }
            catch (UnauthorizedAccessException)
            {
                await App.ConsentDialogDisplay.ShowAsync();
                return;
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialog(
                    ResourceController.GetTranslation("FolderNotFoundDialog/Title"),
                    ResourceController.GetTranslation("FolderNotFoundDialog/Text"));
                IsLoadingItems = false;
                return;
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
                    await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("DriveUnpluggedDialog/Title"), e.Message);
                    IsLoadingItems = false;
                    return;
                }
            }

            CurrentFolder = new ListedItem(_rootFolder.FolderRelativeId)
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
                            await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("BitlockerInvalidPwDialog/Title"), ResourceController.GetTranslation("BitlockerInvalidPwDialog/Text"));
                        }
                    }
                }
            }

            if (enumFromStorageFolder)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
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
                        await AddFolder(item as StorageFolder);
                        ++count;
                    }
                    else
                    {
                        var file = item as StorageFile;
                        if (!file.Name.EndsWith(".lnk") && !file.Name.EndsWith(".url"))
                        {
                            await AddFile(file, true);
                        }
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
            else
            {
                FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
                int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

                IntPtr hFile = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                      additionalFlags);

                var count = 0;
                if (hFile.ToInt64() != -1)
                {
                    do
                    {
                        if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) != FileAttributes.Hidden && ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) != FileAttributes.System)
                        {
                            if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                            {
                                if (!findData.cFileName.EndsWith(".lnk") && !findData.cFileName.EndsWith(".url"))
                                {
                                    AddFile(findData, path);
                                    ++count;
                                }
                            }
                            else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                if (findData.cFileName != "." && findData.cFileName != "..")
                                {
                                    AddFolder(findData, path);
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
                }
            }
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

        private void WatchForDirectoryChanges(string path)
        {
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
                                             AddFileOrFolder(FileName);
                                             Debug.WriteLine("File " + FileName + " added to working directory.");
                                             break;

                                         case FILE_ACTION_REMOVED:
                                             RemoveFileOrFolder(FilesAndFolders.ToList().First(x => x.ItemPath.Equals(FileName)));
                                             Debug.WriteLine("File " + FileName + " removed from working directory.");
                                             break;

                                         case FILE_ACTION_MODIFIED:
                                             Debug.WriteLine("File " + FileName + " had attributes modified in the working directory.");
                                             break;

                                         case FILE_ACTION_RENAMED_OLD_NAME:
                                             RemoveFileOrFolder(FilesAndFolders.ToList().First(x => x.ItemPath.Equals(FileName)));
                                             Debug.WriteLine("File " + FileName + " will be renamed in the working directory.");
                                             break;

                                         case FILE_ACTION_RENAMED_NEW_NAME:
                                             AddFileOrFolder(FileName);
                                             Debug.WriteLine("File " + FileName + " was renamed in the working directory.");
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

        public void AddFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Add(item);
            IsFolderEmptyTextDisplayed = false;
        }

        public async void AddFileOrFolder(string path)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    try
                    {
                        await StorageFile.GetFileFromPathAsync(path);
                        AddFile(path);
                    }
                    catch (Exception)
                    {
                        AddFolder(path);
                    }

                    UpdateDirectoryInfo();
                });
        }

        private void UpdateDirectoryInfo()
        {
            if (_filesAndFolders.Count == 1)
            {
                App.CurrentInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + ResourceController.GetTranslation("ItemCount/Text");
            }
            else
            {
                App.CurrentInstance.ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + ResourceController.GetTranslation("ItemsCount/Text");
            }
        }

        public async void RemoveFileOrFolder(ListedItem item)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    _filesAndFolders.Remove(item);
                    if (_filesAndFolders.Count == 0)
                    {
                        IsFolderEmptyTextDisplayed = true;
                    }

                    UpdateDirectoryInfo();
                });
        }

        public void AddFolder(string folderPath)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_CASE_SENSITIVE;

            IntPtr hFile = FindFirstFileExFromApp(folderPath, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);
            AddFolder(findData, Directory.GetParent(folderPath).FullName);
        }

        private void AddFolder(WIN32_FIND_DATA findData, string pathRoot)
        {
            if (_addFilesCTS.IsCancellationRequested)
            {
                IsLoadingItems = false;
                return;
            }

            FileTimeToSystemTime(ref findData.ftLastWriteTime, out SYSTEMTIME systemTimeOutput);
            var itemDate = new DateTime(
                systemTimeOutput.Year,
                systemTimeOutput.Month,
                systemTimeOutput.Day,
                systemTimeOutput.Hour,
                systemTimeOutput.Minute,
                systemTimeOutput.Second,
                systemTimeOutput.Milliseconds,
                DateTimeKind.Utc);
            var itemPath = Path.Combine(pathRoot, findData.cFileName);

            _filesAndFolders.Add(new ListedItem(null)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemName = findData.cFileName,
                ItemDateModifiedReal = itemDate,
                ItemType = ResourceController.GetTranslation("FileFolderListItem"),
                LoadFolderGlyph = true,
                FileImage = null,
                LoadFileIcon = false,
                ItemPath = itemPath,
                LoadUnknownTypeGlyph = false,
                FileSize = null,
                FileSizeBytes = 0
                //FolderTooltipText = tooltipString,
            });

            IsFolderEmptyTextDisplayed = false;
        }

        public void AddFile(string filePath)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_CASE_SENSITIVE;

            IntPtr hFile = FindFirstFileExFromApp(filePath, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);
            AddFile(findData, Directory.GetParent(filePath).FullName);
        }

        private void AddFile(WIN32_FIND_DATA findData, string pathRoot)
        {
            var itemPath = Path.Combine(pathRoot, findData.cFileName);

            string itemName;
            if (AppSettings.ShowFileExtensions)
                itemName = findData.cFileName;
            else
                itemName = Path.GetFileNameWithoutExtension(itemPath);

            FileTimeToSystemTime(ref findData.ftLastWriteTime, out SYSTEMTIME systemTimeOutput);
            var itemDate = new DateTime(
                systemTimeOutput.Year,
                systemTimeOutput.Month,
                systemTimeOutput.Day,
                systemTimeOutput.Hour,
                systemTimeOutput.Minute,
                systemTimeOutput.Second,
                systemTimeOutput.Milliseconds,
                DateTimeKind.Utc);
            long fDataFSize = findData.nFileSizeLow;
            long itemSizeBytes;
            if (fDataFSize < 0 && findData.nFileSizeHigh > 0)
            {
                itemSizeBytes = fDataFSize + 4294967296 + (findData.nFileSizeHigh * 4294967296);
            }
            else
            {
                if (findData.nFileSizeHigh > 0)
                {
                    itemSizeBytes = fDataFSize + (findData.nFileSizeHigh * 4294967296);
                }
                else if (fDataFSize < 0)
                {
                    itemSizeBytes = fDataFSize + 4294967296;
                }
                else
                {
                    itemSizeBytes = fDataFSize;
                }
            }
            var itemSize = ByteSize.FromBytes(itemSizeBytes).ToBinaryString().ConvertSizeAbbreviation();
            string itemType = ResourceController.GetTranslation("ItemTypeFile");
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
            _filesAndFolders.Add(new ListedItem(null)
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
                FileSizeBytes = itemSizeBytes
            });

            IsFolderEmptyTextDisplayed = false;
        }

        public void AddItemsToCollectionAsync(string path)
        {
            RapidAddItemsToCollectionAsync(path);
        }

        public async Task AddFolder(StorageFolder folder)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if ((App.CurrentInstance.ContentFrame.SourcePageType == typeof(GenericFileBrowser)) || (App.CurrentInstance.ContentFrame.SourcePageType == typeof(GridViewBrowser)))
            {
                if (_addFilesCTS.IsCancellationRequested)
                {
                    IsLoadingItems = false;
                    return;
                }

                _filesAndFolders.Add(new ListedItem(folder.FolderRelativeId)
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
                    FileSizeBytes = 0,
                    //FolderTooltipText = tooltipString,
                });

                IsFolderEmptyTextDisplayed = false;
            }
        }

        public async Task AddFile(StorageFile file, bool suppressThumbnailLoading = false)
        {
            var basicProperties = await file.GetBasicPropertiesAsync();

            // Display name does not include extension
            var itemName = string.IsNullOrEmpty(file.DisplayName) || App.AppSettings.ShowFileExtensions ?
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
            _filesAndFolders.Add(new ListedItem(file.FolderRelativeId)
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
    }
}