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
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.BulkAccess;
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
        private ObservableCollection<ListedItem> _filesAndFolders;
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
                    if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
                    {
                        previouslySelectedItem = (App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.SelectedItem as ListedItem;
                    }
                    else if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(PhotoAlbum))
                    {
                        previouslySelectedItem = (App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.SelectedItem as ListedItem;
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
                        if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
                        {
                            (App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.SelectedItem = jumpedToItem;
                            (App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.ScrollIntoView(jumpedToItem, null);
                        }
                        else if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(PhotoAlbum))
                        {
                            (App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.SelectedItem = jumpedToItem;
                            (App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.ScrollIntoView(jumpedToItem);
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
            App.OccupiedInstance.HomeItems.PropertyChanged += HomeItems_PropertyChanged;
            App.OccupiedInstance.ShareItems.PropertyChanged += ShareItems_PropertyChanged;
            App.OccupiedInstance.LayoutItems.PropertyChanged += LayoutItems_PropertyChanged;
            App.OccupiedInstance.AlwaysPresentCommands.PropertyChanged += AlwaysPresentCommands_PropertyChanged;
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
            App.OccupiedInstance.pathBoxItems.Clear();
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
                pathComponents =  Universal.path.Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList();
                int index = 0;
                foreach(string s in pathComponents)
                {
                    string componentLabel = null;
                    string tag = "";
                    if (s.Contains(":"))
                    {
                        if (s == @"C:" || s == @"c:")
                        {
                            componentLabel = @"Local Disk (C:\)";
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
                        App.OccupiedInstance.pathBoxItems.Add(item);
                    }
                    else
                    {

                        componentLabel = s;
                        foreach (string part in pathComponents.GetRange(0, index + 1))
                        {
                            tag = tag + part + @"\";
                        }
                        if(index == 0)
                        {
                            tag = "\\\\" + tag;
                        }

                        PathBoxItem item = new PathBoxItem()
                        {
                            Title = componentLabel,
                            Path = tag,
                        };
                        App.OccupiedInstance.pathBoxItems.Add(item);

                    }
                    index++;
                }
            }
        }

        private void AlwaysPresentCommands_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(App.OccupiedInstance.AlwaysPresentCommands.isEnabled == true)
            {
                App.OccupiedInstance.AlwaysPresentCommands.isEnabled = true;
            }
            else
            {
                App.OccupiedInstance.AlwaysPresentCommands.isEnabled = false;
            }
        }

        private void LayoutItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (App.OccupiedInstance.LayoutItems.isEnabled == true)
            {
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else
            {
                App.OccupiedInstance.LayoutItems.isEnabled = false;
            }
        }

        private void ShareItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (App.OccupiedInstance.ShareItems.isEnabled == true)
            {
                App.OccupiedInstance.ShareItems.isEnabled = true;
            }
            else
            {
                App.OccupiedInstance.ShareItems.isEnabled = false;
            }
        }

        private void HomeItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (App.OccupiedInstance.HomeItems.isEnabled == true)
            {
                App.OccupiedInstance.HomeItems.isEnabled = true;
            }
            else
            {
                App.OccupiedInstance.HomeItems.isEnabled = false;
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
            App.OccupiedInstance.RibbonArea.Back.IsEnabled = true;
            App.OccupiedInstance.RibbonArea.Forward.IsEnabled = true;
            App.OccupiedInstance.RibbonArea.Up.IsEnabled = true;

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
                    return (T) ((uiElement as Frame).Content);
                }
            }
            return default;
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

        bool isLoadingItems = false;

        class PartialStorageItem
        {
            public string ItemName { get; set; }
            public string ContentType { get; set; }
            public StorageItemThumbnail Thumbnail { get; set; }
            public string RelativeId { get; set; }
        }

        public async void RapidAddItemsToCollectionAsync(string path)
        {
            App.OccupiedInstance.RibbonArea.Refresh.IsEnabled = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(new DirectoryInfo(path).Name, path);
            CancelLoadAndClearFiles();

            isLoadingItems = true;
            EmptyTextState.isVisible = Visibility.Collapsed;
            Universal.path = path;
            _filesAndFolders.Clear();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            LoadIndicator.isVisible = Visibility.Visible;

            switch (Universal.path)
            {
                case "Desktop":
                    Universal.path = App.DesktopPath;
                    break;
                case "Downloads":
                    Universal.path = App.DownloadsPath;
                    break;
                case "Documents":
                    Universal.path = App.DocumentsPath;
                    break;
                case "Pictures":
                    Universal.path = App.PicturesPath;
                    break;
                case "Music":
                    Universal.path = App.MusicPath;
                    break;
                case "Videos":
                    Universal.path = App.VideosPath;
                    break;
                case "OneDrive":
                    Universal.path = App.OneDrivePath;
                    break;
            }

            _rootFolder = await StorageFolder.GetFolderFromPathAsync(path);
            QueryOptions options = new QueryOptions()
            {
                IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties,
                FolderDepth = FolderDepth.Shallow
            };
            var query = _rootFolder.CreateFileQueryWithOptions(options);
            options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
            options.SetThumbnailPrefetch(ThumbnailMode.ListView, 40, ThumbnailOptions.ReturnOnlyIfCached);
            FileInformationFactory thumbnailFactory = new FileInformationFactory(query, ThumbnailMode.ListView, 40, ThumbnailOptions.ReturnOnlyIfCached, false);
            
            var singlePurposedFiles = await thumbnailFactory.GetFilesAsync();
            ObservableCollection<PartialStorageItem> partialFiles = new System.Collections.ObjectModel.ObservableCollection<PartialStorageItem>();
            foreach(FileInformation info in singlePurposedFiles)
            {
                partialFiles.Add(new PartialStorageItem() { RelativeId = info.FolderRelativeId, Thumbnail = info.Thumbnail, ItemName = info.Name, ContentType = info.DisplayType });
            }

            var singlePurposedFolders = await thumbnailFactory.GetFoldersAsync();
            ObservableCollection<PartialStorageItem> partialFolders = new System.Collections.ObjectModel.ObservableCollection<PartialStorageItem>();
            foreach(FolderInformation info in singlePurposedFolders)
            {
                partialFolders.Add(new PartialStorageItem() { RelativeId = info.FolderRelativeId, ItemName = info.Name, ContentType = null, Thumbnail = null });
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
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        AddFile(findData, path, partialFiles.FirstOrDefault(x => x.ItemName == findData.cFileName));
                        ++count;
                    }
                    else if(((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        AddFolder(findData, path, partialFolders.FirstOrDefault(x => x.ItemName == findData.cFileName));
                        ++count;
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
            Debug.WriteLine("Loading of items in " + Universal.path + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
            App.OccupiedInstance.RibbonArea.Refresh.IsEnabled = true;
            LoadIndicator.isVisible = Visibility.Collapsed;
            isLoadingItems = false;
        }

        private void AddFolder(WIN32_FIND_DATA findData, string pathRoot, PartialStorageItem partialStorageItem)
        {
            if ((App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser)) || (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum)))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    isLoadingItems = false;
                    return;
                }
                var itemDate = DateTime.FromFileTimeUtc((findData.ftLastWriteTime.dwHighDateTime << 32) + (long)(uint)findData.ftLastWriteTime.dwLowDateTime);
                var itemPath = Path.Combine(pathRoot, findData.cFileName);

                _filesAndFolders.Add(new ListedItem(partialStorageItem?.RelativeId)
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

        private async void AddFile(WIN32_FIND_DATA findData, string pathRoot, PartialStorageItem partialStorageFile)
        {

            var itemName = findData.cFileName;
            var itemDate = DateTime.FromFileTimeUtc((findData.ftLastWriteTime.dwHighDateTime << 32) + (long) (uint) findData.ftLastWriteTime.dwLowDateTime);
            var itemPath = Path.Combine(pathRoot, findData.cFileName);
            var itemSize = ByteSize.FromBytes((findData.nFileSizeHigh << 32) + (long)(uint)findData.nFileSizeLow).ToString();
            var itemSizeBytes = (findData.nFileSizeHigh << 32) + (ulong)(uint)findData.nFileSizeLow;
            string itemType = "File";
            if(partialStorageFile != null)
            {
                itemType = partialStorageFile.ContentType;
            }
            else
            {
                if (findData.cFileName.Contains('.'))
                {
                    itemType = findData.cFileName.Split('.')[1].ToUpper() + " File";
                }
            }

            var itemFolderImgVis = Visibility.Collapsed;
            string itemFileExtension = null;
            if (findData.cFileName.Contains('.'))
            {
                itemFileExtension = findData.cFileName.Split('.')[1];
            }

            BitmapImage icon = new BitmapImage();
            Visibility itemThumbnailImgVis;
            Visibility itemEmptyImgVis;

            try
            {

                var itemThumbnailImg = partialStorageFile != null ? partialStorageFile.Thumbnail.CloneStream() : null;
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
                // TODO maybe some logging could be added in the future...
            }
            
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                isLoadingItems = false;
                return;
            }
            _filesAndFolders.Add(new ListedItem(partialStorageFile?.RelativeId)
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

            App.OccupiedInstance.RibbonArea.Refresh.IsEnabled = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(new DirectoryInfo(path).Name, path);
            CancelLoadAndClearFiles();

            isLoadingItems = true;
            EmptyTextState.isVisible = Visibility.Collapsed;
            Universal.path = path;
            _filesAndFolders.Clear();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            LoadIndicator.isVisible = Visibility.Visible;

            switch (Universal.path)
            {
                case "Desktop":
                    Universal.path = App.DesktopPath;
                    break;
                case "Downloads":
                    Universal.path = App.DownloadsPath;
                    break;
                case "Documents":
                    Universal.path = App.DocumentsPath;
                    break;
                case "Pictures":
                    Universal.path = App.PicturesPath;
                    break;
                case "Music":
                    Universal.path = App.MusicPath;
                    break;
                case "Videos":
                    Universal.path = App.VideosPath;
                    break;
                case "OneDrive":
                    Universal.path = App.OneDrivePath;
                    break;
            }

            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(Universal.path);
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

                App.OccupiedInstance.RibbonArea.Back.IsEnabled = App.OccupiedInstance.ItemDisplayFrame.CanGoBack;
                App.OccupiedInstance.RibbonArea.Forward.IsEnabled = App.OccupiedInstance.ItemDisplayFrame.CanGoForward;

                switch (await _rootFolder.GetIndexedStateAsync())
                {
                    case (IndexedState.FullyIndexed):
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 40, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.Size", "System.FileExtension" });
                        }
                        else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 80, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }
                        _options.IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;
                        break;
                    default:
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 40, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.ItemPathDisplay", "System.Size", "System.FileExtension" });
                        }
                        else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
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

                if(FilesAndFolders.Count == 0)
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
                Debug.WriteLine("Loading of items in " + Universal.path + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
                App.OccupiedInstance.RibbonArea.Refresh.IsEnabled = true;
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

            if ((App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser)) || (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum)))
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

            if (!(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum)))
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
                    // TODO maybe some logging could be added in the future...
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
