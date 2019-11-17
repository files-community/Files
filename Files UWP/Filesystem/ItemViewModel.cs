using ByteSizeLib;
using Files.Enums;
using Files.Navigation;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
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
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;

namespace Files.Filesystem
{
    public class ItemViewModel : INotifyPropertyChanged
    {
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public CollectionViewSource viewSource;
        public UniversalPath Universal { get; } = new UniversalPath();
        private ObservableCollection<ListedItem> _filesAndFolders;
        private StorageFolderQueryResult _folderQueryResult;
        public StorageFileQueryResult _fileQueryResult;
        private CancellationTokenSource _cancellationTokenSource;
        private StorageFolder _rootFolder;
        private QueryOptions _options;
        private volatile bool _filesRefreshing;
        private const int _step = 250;
        public event PropertyChangedEventHandler PropertyChanged;

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

        public ItemViewModel()
        {
            _filesAndFolders = new ObservableCollection<ListedItem>();

            FilesAndFolders = new ReadOnlyObservableCollection<ListedItem>(_filesAndFolders);
            if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(AddItem))
            {
                if((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null)
                {
                    (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
                }
                else if((App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
                }
            }

            App.selectedTabInstance.HomeItems.PropertyChanged += HomeItems_PropertyChanged;
            App.selectedTabInstance.ShareItems.PropertyChanged += ShareItems_PropertyChanged;
            App.selectedTabInstance.LayoutItems.PropertyChanged += LayoutItems_PropertyChanged;
            App.selectedTabInstance.AlwaysPresentCommands.PropertyChanged += AlwaysPresentCommands_PropertyChanged;

            _cancellationTokenSource = new CancellationTokenSource();

            Universal.PropertyChanged += Universal_PropertyChanged;
        }

        /*
         * Ensure that the path bar gets updated for user interaction
         * whenever the path changes. We will get the individual directories from
         * the updated, most-current path and add them to the UI.
         */

        private void Universal_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Clear the path UI
            App.selectedTabInstance.pathBoxItems.Clear();
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
                        App.selectedTabInstance.pathBoxItems.Add(item);
                    }
                    else
                    {
                        componentLabel = s;
                        foreach (string part in pathComponents.GetRange(0, index + 1))
                        {
                            tag = tag + part + @"\";
                        }

                        PathBoxItem item = new PathBoxItem()
                        {
                            Title = componentLabel,
                            Path = tag,
                        };
                        App.selectedTabInstance.pathBoxItems.Add(item);

                    }
                    index++;
                }
            }
        }

        private void AlwaysPresentCommands_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(App.selectedTabInstance.AlwaysPresentCommands.isEnabled == true)
            {
                App.selectedTabInstance.AlwaysPresentCommands.isEnabled = true;
            }
            else
            {
                App.selectedTabInstance.AlwaysPresentCommands.isEnabled = false;
            }
        }

        private void LayoutItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (App.selectedTabInstance.LayoutItems.isEnabled == true)
            {
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else
            {
                App.selectedTabInstance.LayoutItems.isEnabled = false;
            }
        }

        private void ShareItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (App.selectedTabInstance.ShareItems.isEnabled == true)
            {
                App.selectedTabInstance.ShareItems.isEnabled = true;
            }
            else
            {
                App.selectedTabInstance.ShareItems.isEnabled = false;
            }
        }

        private void HomeItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (App.selectedTabInstance.HomeItems.isEnabled == true)
            {
                App.selectedTabInstance.HomeItems.isEnabled = true;
            }
            else
            {
                App.selectedTabInstance.HomeItems.isEnabled = false;
            }

        }

        public void AddFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Add(item);
            if ((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
            {
                if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                {
                    (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
                }
                else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
                }
            }

        }

        public void RemoveFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Remove(item);
            if (_filesAndFolders.Count == 0)
            {
                if ((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                    {
                        (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Visible;
                    }
                    else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                    {
                        (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Visible;
                    }
                }
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
            App.selectedTabInstance.BackButton.IsEnabled = true;
            App.selectedTabInstance.ForwardButton.IsEnabled = true;
            App.selectedTabInstance.UpButton.IsEnabled = true;

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

        bool isLoadingItems = false;
        public async void AddItemsToCollectionAsync(string path)
        {
            App.selectedTabInstance.RefreshButton.IsEnabled = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(new DirectoryInfo(path).Name, path);
            CancelLoadAndClearFiles();
            isLoadingItems = true;
            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).TextState.isVisible = Visibility.Collapsed;
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).TextState.isVisible = Visibility.Collapsed;
            }

            Universal.path = path;
            _filesAndFolders.Clear();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Visible;
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Visible;
            }

            switch (Universal.path)
            {
                case "Desktop":
                    Universal.path = ProHome.DesktopPath;
                    break;
                case "Downloads":
                    Universal.path = ProHome.DownloadsPath;
                    break;
                case "Documents":
                    Universal.path = ProHome.DocumentsPath;
                    break;
                case "Pictures":
                    Universal.path = ProHome.PicturesPath;
                    break;
                case "Music":
                    Universal.path = ProHome.MusicPath;
                    break;
                case "Videos":
                    Universal.path = ProHome.VideosPath;
                    break;
                case "OneDrive":
                    Universal.path = ProHome.OneDrivePath;
                    break;
            }

            try
            {
                _rootFolder = await StorageFolder.GetFolderFromPathAsync(Universal.path);

                App.selectedTabInstance.BackButton.IsEnabled = App.selectedTabInstance.accessibleContentFrame.CanGoBack;
                App.selectedTabInstance.ForwardButton.IsEnabled = App.selectedTabInstance.accessibleContentFrame.CanGoForward;

                switch (await _rootFolder.GetIndexedStateAsync())
                {
                    case (IndexedState.FullyIndexed):
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.Size", "System.FileExtension" });
                        }
                        else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 80, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }
                        _options.IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;
                        break;
                    default:
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.ItemPathDisplay", "System.Size", "System.FileExtension" });
                        }
                        else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
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
                if (numFiles + numFolders == 0)
                {
                    if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource = new CancellationTokenSource();
                            isLoadingItems = false;
                            return;
                        }
                        (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).TextState.isVisible = Visibility.Visible;
                    }
                    else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource = new CancellationTokenSource();
                            isLoadingItems = false;
                            return;
                        }
                        (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).TextState.isVisible = Visibility.Visible;
                    }
                }
                OrderFiles();
                stopwatch.Stop();
                Debug.WriteLine("Loading of items in " + Universal.path + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
                App.selectedTabInstance.RefreshButton.IsEnabled = true;
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

            if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    isLoadingItems = false;
                    return;
                }
                (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Collapsed;
            }
            else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    isLoadingItems = false;
                    return;
                }
                (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Collapsed;
            }
            isLoadingItems = false;
        }

        private async Task AddFolder(StorageFolder folder)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if ((App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser)) || (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum)))
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    isLoadingItems = false;
                    return;
                }
                _filesAndFolders.Add(new ListedItem(folder.FolderRelativeId)
                {
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
                if((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null)
                {
                    (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
                }
                else if((App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
                }
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

            if (!(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum)))
            {
                try
                {
                    var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, 20, ThumbnailOptions.ResizeThumbnail);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = Visibility.Collapsed;
                        itemThumbnailImgVis = Visibility.Visible;
                        icon.DecodePixelWidth = 20;
                        icon.DecodePixelHeight = 20;
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
                    var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, 80, ThumbnailOptions.ResizeThumbnail);
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

            if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
            }
            else if(App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
            }
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
                if((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                    {
                        (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Visible;
                    }
                    else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                    {
                        (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Visible;
                    }
                }
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
                if ((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                    {
                        (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Collapsed;
                    }
                    else if (App.selectedTabInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                    {
                        (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Collapsed;
                    }
                }
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
