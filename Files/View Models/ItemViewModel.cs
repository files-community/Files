using ByteSizeLib;
using Files.Enums;
using Files.Helpers;
using Files.Interacts;
using Files.Views.Pages;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using FileAttributes = System.IO.FileAttributes;

namespace Files.Filesystem
{
    public class ItemViewModel : INotifyPropertyChanged, IDisposable
    {
        public EmptyFolderTextState EmptyTextState { get; set; } = new EmptyFolderTextState();
        public LoadingIndicator LoadIndicator { get; set; } = new LoadingIndicator();
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public ListedItem CurrentFolder { get => _rootFolderItem; }
        public CollectionViewSource viewSource;
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

                    App.CurrentInstance.SidebarSelectedItem = App.sideBarItems.FirstOrDefault(x => x.Path != null && value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
                    if (App.CurrentInstance.SidebarSelectedItem == null)
                    {
                        App.CurrentInstance.SidebarSelectedItem = App.sideBarItems.FirstOrDefault(x => x.Path != null && x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
                    }

                    NotifyPropertyChanged("WorkingDirectory");
                }
            }
        }

        public ObservableCollection<ListedItem> _filesAndFolders;
        private StorageFolderQueryResult _folderQueryResult;
        public StorageFileQueryResult _fileQueryResult;
        private CancellationTokenSource _cancellationTokenSource;
        private StorageFolder _rootFolder;
        private ListedItem _rootFolderItem;
        private QueryOptions _options;
        private volatile bool _filesRefreshing;
        private const int _step = 250;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _jumpString = "";
        private readonly DispatcherTimer jumpTimer = new DispatcherTimer();

        private SortOption _directorySortOption = SortOption.Name;
        private SortDirection _directorySortDirection = SortDirection.Ascending;

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
                    if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                    {
                        previouslySelectedItem = (App.CurrentInstance.ContentPage as GenericFileBrowser).AllView.SelectedItem as ListedItem;
                    }
                    else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
                    {
                        previouslySelectedItem = (App.CurrentInstance.ContentPage as PhotoAlbum).FileList.SelectedItem as ListedItem;
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
                        if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                        {
                            (App.CurrentInstance.ContentPage as GenericFileBrowser).AllView.SelectedItem = jumpedToItem;
                            (App.CurrentInstance.ContentPage as GenericFileBrowser).AllView.ScrollIntoView(jumpedToItem, null);
                        }
                        else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
                        {
                            (App.CurrentInstance.ContentPage as PhotoAlbum).FileList.SelectedItem = jumpedToItem;
                            (App.CurrentInstance.ContentPage as PhotoAlbum).FileList.ScrollIntoView(jumpedToItem);
                        }
                    }

                    // Restart the timer
                    jumpTimer.Start();
                }
                _jumpString = value;
            }
        }

        public ItemViewModel()
        {
            _filesAndFolders = new ObservableCollection<ListedItem>();
            FilesAndFolders = new ReadOnlyObservableCollection<ListedItem>(_filesAndFolders);
            //(App.CurrentInstance as ProHome).RibbonArea.RibbonViewModel.HomeItems.PropertyChanged += HomeItems_PropertyChanged;
            //(App.CurrentInstance as ProHome).RibbonArea.RibbonViewModel.ShareItems.PropertyChanged += ShareItems_PropertyChanged;
            //(App.CurrentInstance as ProHome).RibbonArea.RibbonViewModel.LayoutItems.PropertyChanged += LayoutItems_PropertyChanged;
            //(App.CurrentInstance as ProHome).RibbonArea.RibbonViewModel.AlwaysPresentCommands.PropertyChanged += AlwaysPresentCommands_PropertyChanged;
            _cancellationTokenSource = new CancellationTokenSource();

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
                if (s.Contains(":"))
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

        public void AddFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Add(item);
            EmptyTextState.IsVisible = Visibility.Collapsed;
        }

        public void RemoveFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Remove(item);
            if (_filesAndFolders.Count == 0)
            {
                EmptyTextState.IsVisible = Visibility.Visible;
            }
        }

        public void CancelLoadAndClearFiles()
        {
            if (IsLoadingItems == false) { return; }

            _cancellationTokenSource.Cancel();
            _filesAndFolders.Clear();
            //_folderQueryResult.ContentsChanged -= FolderContentsChanged;
            if (_fileQueryResult != null)
            {
                _fileQueryResult.ContentsChanged -= FileContentsChanged;
            }
            App.CurrentInstance.NavigationToolbar.CanGoBack = true;
            App.CurrentInstance.NavigationToolbar.CanGoForward = true;
            App.CurrentInstance.NavigationToolbar.CanNavigateToParent = true;
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
            for (var i = 0; i < originalList.Count; i++)
            {
                if (originalList[i] != orderedList[i])
                {
                    _filesAndFolders.RemoveAt(i);
                    _filesAndFolders.Insert(i, orderedList[i]);
                }
            }
        }

        public static T GetCurrentSelectedTabInstance<T>()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            var selectedTabContent = ((instanceTabsView.TabStrip.SelectedItem as TabViewItem).Content as Grid);
            foreach (UIElement uiElement in selectedTabContent.Children)
            {
                if (uiElement.GetType() == typeof(Frame))
                {
                    return (T)((uiElement as Frame).Content);
                }
            }
            return default;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            [MarshalAs(UnmanagedType.U2)] public short Year;
            [MarshalAs(UnmanagedType.U2)] public short Month;
            [MarshalAs(UnmanagedType.U2)] public short DayOfWeek;
            [MarshalAs(UnmanagedType.U2)] public short Day;
            [MarshalAs(UnmanagedType.U2)] public short Hour;
            [MarshalAs(UnmanagedType.U2)] public short Minute;
            [MarshalAs(UnmanagedType.U2)] public short Second;
            [MarshalAs(UnmanagedType.U2)] public short Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                dt = dt.ToUniversalTime();  // SetSystemTime expects the SYSTEMTIME in UTC
                Year = (short)dt.Year;
                Month = (short)dt.Month;
                DayOfWeek = (short)dt.DayOfWeek;
                Day = (short)dt.Day;
                Hour = (short)dt.Hour;
                Minute = (short)dt.Minute;
                Second = (short)dt.Second;
                Milliseconds = (short)dt.Millisecond;
            }
        }

        public enum FINDEX_INFO_LEVELS
        {
            FindExInfoStandard = 0,
            FindExInfoBasic = 1
        }

        public enum FINDEX_SEARCH_OPS
        {
            FindExSearchNameMatch = 0,
            FindExSearchLimitToDirectories = 1,
            FindExSearchLimitToDevices = 2
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileExFromApp(
            string lpFileName,
            FINDEX_INFO_LEVELS fInfoLevelId,
            out WIN32_FIND_DATA lpFindFileData,
            FINDEX_SEARCH_OPS fSearchOp,
            IntPtr lpSearchFilter,
            int dwAdditionalFlags);

        public const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
        public const int FIND_FIRST_EX_LARGE_FETCH = 2;

        [DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("api-ms-win-core-file-l1-1-0.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        [DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
        private static extern bool FileTimeToSystemTime(ref FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

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
                            using (var Thumbnail = await matchingStorageItem.GetThumbnailAsync(ThumbnailMode.ListView, thumbnailSize, ThumbnailOptions.ReturnOnlyIfCached))
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

        public async Task RefreshItems()
        {
            await AddItemsToCollectionAsync(WorkingDirectory);
        }

        public async Task RapidAddItemsToCollectionAsync(string path)
        {
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(new DirectoryInfo(path).Name, path);
            CancelLoadAndClearFiles();

            IsLoadingItems = true;
            EmptyTextState.IsVisible = Visibility.Collapsed;
            WorkingDirectory = path;
            _filesAndFolders.Clear();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            LoadIndicator.IsVisible = Visibility.Visible;

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

                case "OneDrive":
                    WorkingDirectory = App.AppSettings.OneDrivePath;
                    break;
            }

            App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
            App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;

            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(path);
                _rootFolderItem = new ListedItem(_rootFolder.FolderRelativeId)
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
                MessageDialog folderGone = new MessageDialog("The folder you've navigated to was not found.", "Did you delete this folder?");
                await folderGone.ShowAsync();
                IsLoadingItems = false;
                return;
            }
            catch (Exception e)
            {
                MessageDialog driveGone = new MessageDialog(e.Message, "Did you unplug this drive?");
                await driveGone.ShowAsync();
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
                    if (_cancellationTokenSource.IsCancellationRequested)
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

            if (FilesAndFolders.Count == 0)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                    IsLoadingItems = false;
                    return;
                }
                EmptyTextState.IsVisible = Visibility.Visible;
            }

            OrderFiles();
            stopwatch.Stop();
            Debug.WriteLine("Loading of items in " + WorkingDirectory + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
            App.CurrentInstance.NavigationToolbar.CanRefresh = true;
            LoadIndicator.IsVisible = Visibility.Collapsed;
            IsLoadingItems = false;
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
            if ((App.CurrentInstance.CurrentPageType) == typeof(GenericFileBrowser) || (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum)))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
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
                    systemTimeOutput.Milliseconds);
                var itemPath = Path.Combine(pathRoot, findData.cFileName);
                //var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
                //var typeText = resourceLoader.GetString("Folder");

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

                EmptyTextState.IsVisible = Visibility.Collapsed;
            }
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
                systemTimeOutput.Milliseconds);
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

            if (_cancellationTokenSource.IsCancellationRequested)
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

            EmptyTextState.IsVisible = Visibility.Collapsed;
        }

        public async Task AddItemsToCollectionAsync(string path)
        {
            await RapidAddItemsToCollectionAsync(path);
            return;
        }

        private async Task AddFolder(StorageFolder folder)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if ((App.CurrentInstance.ContentFrame.SourcePageType == typeof(GenericFileBrowser)) || (App.CurrentInstance.ContentFrame.SourcePageType == typeof(PhotoAlbum)))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    IsLoadingItems = false;
                    return;
                }
                //string dateCreatedText = folder.DateCreated.DateTime.ToString();
                //var firstFiles = (await folder.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, 3)).Select(x => x.Name);
                //string firstFilesText = "No Files";
                //if(firstFiles.Count() > 0)
                //{
                //    firstFilesText = string.Join(',', firstFiles.ToArray());
                //}

                //var firstFolders = (await folder.GetFoldersAsync(CommonFolderQuery.DefaultQuery, 0, 3)).Select(x => x.Name);
                //string firstFoldersText = "No Folders";
                //if (firstFolders.Count() > 0)
                //{
                //    firstFoldersText = string.Join(',', firstFolders.ToArray());
                //}

                //string tooltipString = dateCreatedText + "\n" + "Folders: " + firstFoldersText + "\n" + "Files: " + firstFilesText;
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

                EmptyTextState.IsVisible = Visibility.Collapsed;
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

            if (!(App.CurrentInstance.ContentFrame.SourcePageType == typeof(PhotoAlbum)))
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
            if (_cancellationTokenSource.IsCancellationRequested)
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

            EmptyTextState.IsVisible = Visibility.Collapsed;
        }

        public async void FileContentsChanged(IStorageQueryResultBase sender, object args)
        {
            if (_filesRefreshing)
            {
                Debug.WriteLine("Filesystem change event fired but refresh is already running");
                return;
            }
            else
            {
                Debug.WriteLine("Filesystem change event fired. Refreshing...");
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                LoadIndicator.IsVisible = Visibility.Visible;
            });
            _filesRefreshing = true;

            //query options have to be reapplied otherwise old results are returned
            _fileQueryResult.ApplyNewQueryOptions(_options);
            _folderQueryResult.ApplyNewQueryOptions(_options);

            var fileCount = await _fileQueryResult.GetItemCountAsync();
            var folderCount = await _folderQueryResult.GetItemCountAsync();
            var files = await _fileQueryResult.GetFilesAsync();
            var folders = await _folderQueryResult.GetFoldersAsync();

            // modifying a file also results in a new unique FolderRelativeId so no need to check for DateModified explicitly

            var addedFiles = files.Select(f => f.FolderRelativeId).Except(_filesAndFolders.Select(f => f.FolderRelativeId));
            var addedFolders = folders.Select(f => f.FolderRelativeId).Except(_filesAndFolders.Select(f => f.FolderRelativeId));
            var removedFilesAndFolders = _filesAndFolders
                .Select(f => f.FolderRelativeId)
                .Except(files.Select(f => f.FolderRelativeId))
                .Except(folders.Select(f => f.FolderRelativeId))
                .ToArray();

            foreach (var file in addedFiles)
            {
                var toAdd = files.First(f => f.FolderRelativeId == file);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    await AddFile(toAdd);
                });
            }
            foreach (var folder in addedFolders)
            {
                var toAdd = folders.First(f => f.FolderRelativeId == folder);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    await AddFolder(toAdd);
                });
            }
            foreach (var item in removedFilesAndFolders)
            {
                var toRemove = _filesAndFolders.First(f => f.FolderRelativeId == item);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    RemoveFileOrFolder(toRemove);
                });
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                LoadIndicator.IsVisible = Visibility.Collapsed;
            });

            _filesRefreshing = false;
            Debug.WriteLine("Filesystem refresh complete");
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
            _cancellationTokenSource?.Dispose();
        }
    }
}