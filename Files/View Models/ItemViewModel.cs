using ByteSizeLib;
using Files.Enums;
using Files.Interacts;
using Files.Navigation;
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
    public class ItemViewModel : INotifyPropertyChanged
    {
        public EmptyFolderTextState EmptyTextState { get; set; } = new EmptyFolderTextState();
        public LoadingIndicator LoadIndicator { get; set; } = new LoadingIndicator();
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public ListedItem currentFolder { get => _rootFolderItem; }
        public CollectionViewSource viewSource;
        public UniversalPath Universal { get; } = new UniversalPath();
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
        private DispatcherTimer jumpTimer = new DispatcherTimer();

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
                    var candidateItems = _filesAndFolders.Where(f => f.FileName.Length >= value.Length && f.FileName.Substring(0, value.Length).ToLower() == value);
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
                        jumpedToItem = candidateItems.FirstOrDefault(f => f.FileName.CompareTo(previouslySelectedItem.FileName) > 0);
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

            Universal.PropertyChanged += Universal_PropertyChanged;

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
        private void Universal_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Clear the path UI
            App.CurrentInstance.NavigationToolbar.PathComponents.Clear();
            // Style tabStyleFixed = App.selectedTabInstance.accessiblePathTabView.Resources["PathSectionTabStyle"] as Style;
            FontWeight weight = new FontWeight()
            {
                Weight = FontWeights.SemiBold.Weight
            };
            List<string> pathComponents = new List<string>();
            if (e.PropertyName == "path")
            {
                // If path is a library, simplify it

                // If path is found to not be a library
                pathComponents = Universal.WorkingDirectory.Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList();
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
        }

        public void AddFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Add(item);
            EmptyTextState.isVisible = Visibility.Collapsed;
        }

        public void RemoveFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Remove(item);
            if (_filesAndFolders.Count == 0)
            {
                EmptyTextState.isVisible = Visibility.Visible;
            }
        }

        public void CancelLoadAndClearFiles()
        {
            if (isLoadingItems == false) { return; }

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

            object orderByNameFunc(ListedItem item) => item.FileName;
            Func<ListedItem, object> orderFunc = orderByNameFunc;
            switch (DirectorySortOption)
            {
                case SortOption.Name:
                    orderFunc = orderByNameFunc;
                    break;
                case SortOption.DateModified:
                    orderFunc = item => item.FileDateReal;
                    break;
                case SortOption.FileType:
                    orderFunc = item => item.FileType;
                    break;
                case SortOption.Size:
                    orderFunc = item => item.FileSizeBytes;
                    break;
            }

            // In ascending order, show folders first, then files.
            // So, we use != "Folder" to make the value for "Folder" = 0, and for the rest, 1.
            Func<ListedItem, bool> folderThenFile = listedItem => listedItem.FileType != "Folder";
            IOrderedEnumerable<ListedItem> ordered;
            List<ListedItem> orderedList;

            if (DirectorySortDirection == SortDirection.Ascending)
                ordered = _filesAndFolders.OrderBy(folderThenFile).ThenBy(orderFunc);
            else
            {
                if (DirectorySortOption == SortOption.FileType)
                    ordered = _filesAndFolders.OrderBy(folderThenFile).ThenByDescending(orderFunc);
                else
                    ordered = _filesAndFolders.OrderByDescending(folderThenFile).ThenByDescending(orderFunc);
            }

            // Further order by name if applicable
            if (DirectorySortOption != SortOption.Name)
            {
                if (DirectorySortDirection == SortDirection.Ascending)
                    ordered = ordered.ThenBy(orderByNameFunc);
                else
                    ordered = ordered.ThenByDescending(orderByNameFunc);
            }
            orderedList = ordered.ToList();
            _filesAndFolders.Clear();
            foreach (ListedItem i in orderedList)
                _filesAndFolders.Add(i);
        }

        public static T GetCurrentSelectedTabInstance<T>()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            var selectedTabContent = ((InstanceTabsView.tabView.SelectedItem as TabViewItem).Content as Grid);
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
        static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("api-ms-win-core-file-l1-1-0.dll")]
        static extern bool FindClose(IntPtr hFindFile);

        [DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
        static extern bool FileTimeToSystemTime(ref FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

        private bool _isLoadingItems = false;
        public bool isLoadingItems
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
                    NotifyPropertyChanged("isLoadingItems");
                }
            }
        }

        public async void LoadExtendedItemProperties(ListedItem item, uint thumbnailSize = 20)
        {
            if (!item.ItemPropertiesInitialized)
            {
                if (item.FileType != "Folder")
                {
                    BitmapImage icon = new BitmapImage();
                    var matchingItem = _filesAndFolders.FirstOrDefault(x => x == item);
                    try
                    {
                        var matchingStorageItem = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        if (matchingItem != null && matchingStorageItem != null)
                        {
                            matchingItem.FileType = matchingStorageItem.DisplayType;
                            matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
                            var Thumbnail = await matchingStorageItem.GetThumbnailAsync(ThumbnailMode.ListView, thumbnailSize, ThumbnailOptions.UseCurrentScale);
                            if (Thumbnail != null)
                            {
                                matchingItem.FileImg = icon;
                                await icon.SetSourceAsync(Thumbnail);
                                matchingItem.EmptyImgVis = Visibility.Collapsed;
                                matchingItem.FileIconVis = Visibility.Visible;
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
                        var matchingStorageItem = await StorageFolder.GetFolderFromPathAsync(item.FilePath);
                        if (matchingItem != null && matchingStorageItem != null)
                        {
                            matchingItem.FolderRelativeId = matchingStorageItem.FolderRelativeId;
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

        public async void RapidAddItemsToCollectionAsync(string path)
        {
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(new DirectoryInfo(path).Name, path);
            CancelLoadAndClearFiles();

            isLoadingItems = true;
            EmptyTextState.isVisible = Visibility.Collapsed;
            Universal.WorkingDirectory = path;
            _filesAndFolders.Clear();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            LoadIndicator.isVisible = Visibility.Visible;

            switch (Universal.WorkingDirectory)
            {
                case "Desktop":
                    Universal.WorkingDirectory = App.AppSettings.DesktopPath;
                    break;
                case "Downloads":
                    Universal.WorkingDirectory = App.AppSettings.DownloadsPath;
                    break;
                case "Documents":
                    Universal.WorkingDirectory = App.AppSettings.DocumentsPath;
                    break;
                case "Pictures":
                    Universal.WorkingDirectory = App.AppSettings.PicturesPath;
                    break;
                case "Music":
                    Universal.WorkingDirectory = App.AppSettings.MusicPath;
                    break;
                case "Videos":
                    Universal.WorkingDirectory = App.AppSettings.VideosPath;
                    break;
                case "OneDrive":
                    Universal.WorkingDirectory = App.AppSettings.OneDrivePath;
                    break;
            }

            App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
            App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;

            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(path);
            }
            catch (UnauthorizedAccessException)
            {
                await App.consentDialog.ShowAsync();
                return;
            }
            catch (FileNotFoundException)
            {
                MessageDialog folderGone = new MessageDialog("The folder you've navigated to was not found.", "Did you delete this folder?");
                await folderGone.ShowAsync();
                isLoadingItems = false;
                return;
            }
            catch (Exception e)
            {
                MessageDialog driveGone = new MessageDialog(e.Message, "Did you unplug this drive?");
                await driveGone.ShowAsync();
                isLoadingItems = false;
                return;
            }


            WIN32_FIND_DATA findData;
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoStandard;
            int additionalFlags = 0;
            findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
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
                            if (!findData.cFileName.EndsWith(".lnk"))
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

                } while (FindNextFile(hFile, out findData));

                FindClose(hFile);
            }

            if (FilesAndFolders.Count == 0)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    isLoadingItems = false;
                    return;
                }
                EmptyTextState.isVisible = Visibility.Visible;
            }


            OrderFiles();
            stopwatch.Stop();
            Debug.WriteLine("Loading of items in " + Universal.WorkingDirectory + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
            App.CurrentInstance.NavigationToolbar.CanRefresh = true;
            LoadIndicator.isVisible = Visibility.Collapsed;
            isLoadingItems = false;
        }

        private void AddFolder(WIN32_FIND_DATA findData, string pathRoot)
        {
            if ((App.CurrentInstance.CurrentPageType) == typeof(GenericFileBrowser) || (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum)))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    isLoadingItems = false;
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

                _filesAndFolders.Add(new ListedItem(null)
                {
                    //FolderTooltipText = tooltipString,
                    FileName = findData.cFileName,
                    FileDateReal = itemDate,
                    FileType = "Folder",    //TODO: Take a look at folder.DisplayType
                    FolderImg = Visibility.Visible,
                    FileImg = null,
                    FileIconVis = Visibility.Collapsed,
                    FilePath = itemPath,
                    EmptyImgVis = Visibility.Collapsed,
                    FileSize = null,
                    FileSizeBytes = 0
                });

                EmptyTextState.isVisible = Visibility.Collapsed;
            }
        }

        private void AddFile(WIN32_FIND_DATA findData, string pathRoot)
        {
            var itemPath = Path.Combine(pathRoot, findData.cFileName);

            string itemName = null;
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
            var itemSize = ByteSize.FromBytes((findData.nFileSizeHigh << 32) + (long)(uint)findData.nFileSizeLow).ToString();
            var itemSizeBytes = (findData.nFileSizeHigh << 32) + (ulong)(uint)findData.nFileSizeLow;
            string itemType = "File";
            string itemFileExtension = null;

            if (findData.cFileName.Contains('.'))
            {
                itemFileExtension = Path.GetExtension(itemPath);
                itemType = itemFileExtension + " File";
            }

            var itemFolderImgVis = Visibility.Collapsed;

            BitmapImage icon = new BitmapImage();
            Visibility itemThumbnailImgVis;
            Visibility itemEmptyImgVis;

            itemEmptyImgVis = Visibility.Visible;
            itemThumbnailImgVis = Visibility.Collapsed;


            if (_cancellationTokenSource.IsCancellationRequested)
            {
                isLoadingItems = false;
                return;
            }
            _filesAndFolders.Add(new ListedItem(null)
            {
                DotFileExtension = itemFileExtension,
                EmptyImgVis = itemEmptyImgVis,
                FileImg = icon,
                FileIconVis = itemThumbnailImgVis,
                FolderImg = itemFolderImgVis,
                FileName = itemName,
                FileDateReal = itemDate,
                FileType = itemType,
                FilePath = itemPath,
                FileSize = itemSize,
                FileSizeBytes = itemSizeBytes
            });

            EmptyTextState.isVisible = Visibility.Collapsed;
        }

        public async void AddItemsToCollectionAsync(string path)
        {
            RapidAddItemsToCollectionAsync(path);
            return;
            // Eventually add logic for user choice between item load methods
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(new DirectoryInfo(path).Name, path);
            CancelLoadAndClearFiles();

            isLoadingItems = true;
            EmptyTextState.isVisible = Visibility.Collapsed;
            Universal.WorkingDirectory = path;
            _filesAndFolders.Clear();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            LoadIndicator.isVisible = Visibility.Visible;

            switch (Universal.WorkingDirectory)
            {
                case "Desktop":
                    Universal.WorkingDirectory = App.AppSettings.DesktopPath;
                    break;
                case "Downloads":
                    Universal.WorkingDirectory = App.AppSettings.DownloadsPath;
                    break;
                case "Documents":
                    Universal.WorkingDirectory = App.AppSettings.DocumentsPath;
                    break;
                case "Pictures":
                    Universal.WorkingDirectory = App.AppSettings.PicturesPath;
                    break;
                case "Music":
                    Universal.WorkingDirectory = App.AppSettings.MusicPath;
                    break;
                case "Videos":
                    Universal.WorkingDirectory = App.AppSettings.VideosPath;
                    break;
                case "OneDrive":
                    Universal.WorkingDirectory = App.AppSettings.OneDrivePath;
                    break;
            }

            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(Universal.WorkingDirectory);
                var rootFolderProperties = await _rootFolder.GetBasicPropertiesAsync();

                _rootFolderItem = new ListedItem(_rootFolder.FolderRelativeId)
                {
                    FileName = _rootFolder.Name,
                    FileDateReal = rootFolderProperties.DateModified,
                    FileType = "Folder",    //TODO: Take a look at folder.DisplayType
                    FolderImg = Visibility.Visible,
                    FileImg = null,
                    FileIconVis = Visibility.Collapsed,
                    FilePath = _rootFolder.Path,
                    EmptyImgVis = Visibility.Collapsed,
                    FileSize = null,
                    FileSizeBytes = 0
                };

                App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
                App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;

                switch (await _rootFolder.GetIndexedStateAsync())
                {
                    case (IndexedState.FullyIndexed):
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (App.CurrentInstance.ContentFrame.SourcePageType == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 40, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.Size", "System.FileExtension" });
                        }
                        else if (App.CurrentInstance.ContentFrame.SourcePageType == typeof(PhotoAlbum))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 80, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }
                        _options.IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;
                        break;
                    default:
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (App.CurrentInstance.ContentFrame.SourcePageType == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 40, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.ItemPathDisplay", "System.Size", "System.FileExtension" });
                        }
                        else if (App.CurrentInstance.ContentFrame.SourcePageType == typeof(PhotoAlbum))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 80, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }

                        _options.IndexerOption = IndexerOption.UseIndexerWhenAvailable;
                        break;
                }

                uint index = 0;
                _folderQueryResult = _rootFolder.CreateFolderQueryWithOptions(_options);
                //_folderQueryResult.ContentsChanged += FolderContentsChanged;
                var numFolders = await _folderQueryResult.GetItemCountAsync();
                IReadOnlyList<StorageFolder> storageFolders = await _folderQueryResult.GetFoldersAsync(index, _step);
                while (storageFolders.Count > 0)
                {
                    foreach (StorageFolder folder in storageFolders)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource = new CancellationTokenSource();
                            isLoadingItems = false;
                            return;
                        }
                        await AddFolder(folder);
                    }
                    index += _step;
                    storageFolders = await _folderQueryResult.GetFoldersAsync(index, _step);
                }

                index = 0;
                _fileQueryResult = _rootFolder.CreateFileQueryWithOptions(_options);
                _fileQueryResult.ContentsChanged += FileContentsChanged;
                var numFiles = await _fileQueryResult.GetItemCountAsync();
                IReadOnlyList<StorageFile> storageFiles = await _fileQueryResult.GetFilesAsync(index, _step);
                while (storageFiles.Count > 0)
                {
                    foreach (StorageFile file in storageFiles)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource = new CancellationTokenSource();
                            isLoadingItems = false;
                            return;
                        }
                        await AddFile(file);
                    }
                    index += _step;
                    storageFiles = await _fileQueryResult.GetFilesAsync(index, _step);
                }

                if (FilesAndFolders.Count == 0)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        isLoadingItems = false;
                        return;
                    }
                    EmptyTextState.isVisible = Visibility.Visible;
                }


                OrderFiles();
                stopwatch.Stop();
                Debug.WriteLine("Loading of items in " + Universal.WorkingDirectory + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
                App.CurrentInstance.NavigationToolbar.CanRefresh = true;
            }
            catch (UnauthorizedAccessException)
            {
                await App.consentDialog.ShowAsync();
            }
            catch (COMException e)
            {
                Frame rootContentFrame = Window.Current.Content as Frame;
                MessageDialog driveGone = new MessageDialog(e.Message, "Did you unplug this drive?");
                await driveGone.ShowAsync();
                rootContentFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                isLoadingItems = false;
                return;
            }
            catch (FileNotFoundException)
            {
                Frame rootContentFrame = Window.Current.Content as Frame;
                MessageDialog folderGone = new MessageDialog("The folder you've navigated to was removed.", "Did you delete this folder?");
                await folderGone.ShowAsync();
                rootContentFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                isLoadingItems = false;
                return;
            }

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                isLoadingItems = false;
                return;
            }

            LoadIndicator.isVisible = Visibility.Collapsed;

            isLoadingItems = false;
        }

        private async Task AddFolder(StorageFolder folder)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if ((App.CurrentInstance.ContentFrame.SourcePageType == typeof(GenericFileBrowser)) || (App.CurrentInstance.ContentFrame.SourcePageType == typeof(PhotoAlbum)))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    isLoadingItems = false;
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
                    FileName = folder.Name,
                    FileDateReal = basicProperties.DateModified,
                    FileType = "Folder",    //TODO: Take a look at folder.DisplayType
                    FolderImg = Visibility.Visible,
                    FileImg = null,
                    FileIconVis = Visibility.Collapsed,
                    FilePath = folder.Path,
                    EmptyImgVis = Visibility.Collapsed,
                    FileSize = null,
                    FileSizeBytes = 0
                });

                EmptyTextState.isVisible = Visibility.Collapsed;
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
            var itemFolderImgVis = Visibility.Collapsed;
            var itemFileExtension = file.FileType;

            BitmapImage icon = new BitmapImage();
            Visibility itemThumbnailImgVis;
            Visibility itemEmptyImgVis;

            if (!(App.CurrentInstance.ContentFrame.SourcePageType == typeof(PhotoAlbum)))
            {
                try
                {
                    var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, 40, ThumbnailOptions.UseCurrentScale);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = Visibility.Collapsed;
                        itemThumbnailImgVis = Visibility.Visible;
                        icon.DecodePixelWidth = 40;
                        icon.DecodePixelHeight = 40;
                        await icon.SetSourceAsync(itemThumbnailImg);
                    }
                    else
                    {
                        itemEmptyImgVis = Visibility.Visible;
                        itemThumbnailImgVis = Visibility.Collapsed;
                    }
                }
                catch
                {
                    itemEmptyImgVis = Visibility.Visible;
                    itemThumbnailImgVis = Visibility.Collapsed;
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
                        itemEmptyImgVis = Visibility.Collapsed;
                        itemThumbnailImgVis = Visibility.Visible;
                        icon.DecodePixelWidth = 80;
                        icon.DecodePixelHeight = 80;
                        await icon.SetSourceAsync(itemThumbnailImg);
                    }
                    else
                    {
                        itemEmptyImgVis = Visibility.Visible;
                        itemThumbnailImgVis = Visibility.Collapsed;
                    }
                }
                catch
                {
                    itemEmptyImgVis = Visibility.Visible;
                    itemThumbnailImgVis = Visibility.Collapsed;

                }
            }
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                isLoadingItems = false;
                return;
            }
            _filesAndFolders.Add(new ListedItem(file.FolderRelativeId)
            {
                DotFileExtension = itemFileExtension,
                EmptyImgVis = itemEmptyImgVis,
                FileImg = icon,
                FileIconVis = itemThumbnailImgVis,
                FolderImg = itemFolderImgVis,
                FileName = itemName,
                FileDateReal = itemDate,
                FileType = itemType,
                FilePath = itemPath,
                FileSize = itemSize,
                FileSizeBytes = itemSizeBytes
            });

            EmptyTextState.isVisible = Visibility.Collapsed;
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
                LoadIndicator.isVisible = Visibility.Visible;
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
                LoadIndicator.isVisible = Visibility.Collapsed;
            });

            _filesRefreshing = false;
            Debug.WriteLine("Filesystem refresh complete");
        }
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
