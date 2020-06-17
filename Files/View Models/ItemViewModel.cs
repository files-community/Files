using ByteSizeLib;
using Files.Common;
using Files.Enums;
using Files.Helpers;
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
using static Files.Helpers.NativeDirectoryChangesHelper;
using static Files.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.Filesystem
{
    public class ItemViewModel : INotifyPropertyChanged, IDisposable
    {
        private volatile bool MustTryToWatchAgain = false;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private IntPtr hWatchDir;
        private IAsyncAction aWatcherAction;
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
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
        private string _WorkingDirectory;
        public string WorkingDirectory
        {
            get
            {
                return _WorkingDirectory;
            }

            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _WorkingDirectory = value;

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

                    if (App.CurrentInstance.SidebarSelectedItem != item)
                    {
                        App.CurrentInstance.SidebarSelectedItem = item;
                    }

                    NotifyPropertyChanged("WorkingDirectory");
                }
            }
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
            List<string> pathComponents = new List<string>();
            // If path is a library, simplify it

            // If path is found to not be a library
            pathComponents = WorkingDirectory.Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList();
            int index = 0;
            foreach (string s in pathComponents)
            {
                string componentLabel = null;
                string tag = "";
                if (s.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // Handle the recycle bin: use the localized folder name
                    PathBoxItem item = new PathBoxItem()
                    {
                        Title = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                        Path = tag,
                    };
                    App.CurrentInstance.NavigationToolbar.PathComponents.Add(item);
                }
                else if (s.Contains(":"))
                {
                    if (App.sideBarItems.FirstOrDefault(x => x.ItemType == NavigationControlItemType.Drive && x.Path.Contains(s, StringComparison.OrdinalIgnoreCase)) != null)
                    {
                        componentLabel = App.sideBarItems.FirstOrDefault(x => x.ItemType == NavigationControlItemType.Drive && x.Path.Contains(s, StringComparison.OrdinalIgnoreCase)).Text;
                    }
                    else
                    {
                        componentLabel = @"Drive (" + s + @"\)";
                    }
                    tag = s + @"\";

                    PathBoxItem item = new PathBoxItem()
                    {
                        Title = componentLabel,
                        Path = tag,
                    };
                    App.CurrentInstance.NavigationToolbar.PathComponents.Add(item);
                }
                else
                {
                    componentLabel = s;
                    foreach (string part in pathComponents.GetRange(0, index + 1))
                    {
                        tag = tag + part + @"\";
                    }
                    if (index == 0)
                    {
                        tag = "\\\\" + tag;
                    }

                    PathBoxItem item = new PathBoxItem()
                    {
                        Title = componentLabel,
                        Path = tag,
                    };
                    App.CurrentInstance.NavigationToolbar.PathComponents.Add(item);
                }
                index++;
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
            if (!(WorkingDirectory?.StartsWith(App.AppSettings.RecycleBinPath) ?? false))
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

            App.CurrentInstance.StatusBarControl.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + ResourceController.GetTranslation("ItemsSelected/Text");

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
                        var matchingStorageItem = await StorageFile.GetFileFromPathAsync(item.ItemPath);
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
                        var matchingStorageItem = await StorageFolder.GetFolderFromPathAsync(item.ItemPath);
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
                WorkingDirectory = path;
                _filesAndFolders.Clear();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                App.InteractionViewModel.IsContentLoadingIndicatorVisible = true;

                switch (WorkingDirectory)
                {
                    case "Desktop":
                        WorkingDirectory = App.AppSettings.DesktopPath;
                        break;

                    case "Downloads":
                        WorkingDirectory = App.AppSettings.DownloadsPath;
                        break;

                    case "Documents":
                        WorkingDirectory = App.AppSettings.DocumentsPath;
                        break;

                    case "Pictures":
                        WorkingDirectory = App.AppSettings.PicturesPath;
                        break;

                    case "Music":
                        WorkingDirectory = App.AppSettings.MusicPath;
                        break;

                    case "Videos":
                        WorkingDirectory = App.AppSettings.VideosPath;
                        break;

                    case "RecycleBin":
                        WorkingDirectory = App.AppSettings.RecycleBinPath;
                        break;

                    case "OneDrive":
                        WorkingDirectory = App.AppSettings.OneDrivePath;
                        break;
                }

                App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
                App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;

                if (path.StartsWith(App.AppSettings.RecycleBinPath))
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
                Debug.WriteLine("Loading of items in " + WorkingDirectory + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
                App.CurrentInstance.NavigationToolbar.CanRefresh = true;
                App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                IsLoadingItems = false;
            }
            finally
            {
                semaphoreSlim.Release();
            }

            if (_filesAndFolders.Count == 1)
            {
                App.CurrentInstance.StatusBarControl.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + ResourceController.GetTranslation("ItemCount/Text");
            }
            else
            {
                App.CurrentInstance.StatusBarControl.DirectoryPropertiesViewModel.DirectoryItemCount = _filesAndFolders.Count + " " + ResourceController.GetTranslation("ItemsCount/Text");
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

        public async Task EnumerateItemsFromSpecialFolder(string path)
        {
            CurrentFolder = new ListedItem(null)
            {
                ItemPropertiesInitialized = true,
                ItemName = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                ItemDateModifiedReal = DateTimeOffset.Now, // Fake for now
                ItemType = ResourceController.GetTranslation("FileFolderListItem"),
                LoadFolderGlyph = true,
                FileImage = null,
                LoadFileIcon = false,
                ItemPath = App.AppSettings.RecycleBinPath,
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
                            if (App.AppSettings.ShowFileExtensions)
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
                                FileSizeBytes = (ulong)item.FileSizeBytes
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
            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(path);
                CurrentFolder = new ListedItem(_rootFolder.FolderRelativeId)
                {
                    ItemPropertiesInitialized = true,
                    ItemName = _rootFolder.Name,
                    ItemDateModifiedReal = (await _rootFolder.GetBasicPropertiesAsync()).DateModified,
                    ItemType = _rootFolder.DisplayType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = _rootFolder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                };
            }
            catch (UnauthorizedAccessException)
            {
                await App.ConsentDialogDisplay.ShowAsync();
                return;
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("FolderNotFoundDialog.Title"), ResourceController.GetTranslation("FolderNotFoundDialog.Text"));
                IsLoadingItems = false;
                return;
            }
            catch (Exception e)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("DriveUnpluggedDialog.Title"), e.Message);
                IsLoadingItems = false;
                return;
            }

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

        private void WatchForDirectoryChanges(string path)
        {
            Debug.WriteLine("WatchForDirectoryChanges: {0}", path);
            hWatchDir = CreateFileFromApp(path, 1, 1 | 2 | 4,
                IntPtr.Zero, 3, (uint)File_Attributes.BackupSemantics | (uint)File_Attributes.Overlapped, IntPtr.Zero);

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
                });
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
            if (App.AppSettings.ShowFileExtensions)
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
            long fileSize;
            if (fDataFSize < 0 && findData.nFileSizeHigh > 0)
            {
                fileSize = fDataFSize + 4294967296 + (findData.nFileSizeHigh * 4294967296);
            }
            else
            {
                if (findData.nFileSizeHigh > 0)
                {
                    fileSize = fDataFSize + (findData.nFileSizeHigh * 4294967296);
                }
                else if (fDataFSize < 0)
                {
                    fileSize = fDataFSize + 4294967296;
                }
                else
                {
                    fileSize = fDataFSize;
                }
            }
            var itemSize = ByteSize.FromBytes(fileSize).ToString();
            var itemSizeBytes = (findData.nFileSizeHigh << 32) + (ulong)findData.nFileSizeLow;
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

        private async Task AddFolder(StorageFolder folder)
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
                    //FolderTooltipText = tooltipString,
                    ItemName = folder.Name,
                    ItemDateModifiedReal = basicProperties.DateModified,
                    ItemType = folder.DisplayType,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = folder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                });

                IsFolderEmptyTextDisplayed = false;
            }
        }

        private async Task AddFile(StorageFile file)
        {
            var basicProperties = await file.GetBasicPropertiesAsync();

            var itemName = file.DisplayName;
            var itemDate = basicProperties.DateModified;
            var itemPath = file.Path;
            var itemSize = ByteSize.FromBytes(basicProperties.Size).ToString();
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
                    var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, 40, ThumbnailOptions.UseCurrentScale);
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
                    var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, 80, ThumbnailOptions.UseCurrentScale);
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
