using ByteSizeLib;
using Files.Interacts;
using Files.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;

namespace Files.Filesystem
{
    public class ItemViewModel<PageType> where PageType : class
    {
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public ReadOnlyObservableCollection<Classic_ListedFolderItem> ClassicFolderList { get; }
        public ReadOnlyObservableCollection<ListedItem> ClassicFileList { get; }
        public UniversalPath Universal { get; } = new UniversalPath();

        private ObservableCollection<ListedItem> _filesAndFolders;
        private ObservableCollection<ListedItem> _classicFileList;
        private ObservableCollection<Classic_ListedFolderItem> _classicFolderList;

        private StorageFolderQueryResult _folderQueryResult;
        public StorageFileQueryResult _fileQueryResult;
        private CancellationTokenSource _cancellationTokenSource;
        private StorageFolder _rootFolder;
        private QueryOptions _options;

        private volatile bool _filesRefreshing;
        //private volatile bool _foldersRefreshing;

        private const int _step = 250;

        private ProHome tabInstance;

        public ItemViewModel()
        {
            tabInstance = GetCurrentSelectedTabInstance<ProHome>();
            _filesAndFolders = new ObservableCollection<ListedItem>();
            _classicFileList = new ObservableCollection<ListedItem>();
            _classicFolderList = new ObservableCollection<Classic_ListedFolderItem>();

            FilesAndFolders = new ReadOnlyObservableCollection<ListedItem>(_filesAndFolders);
            ClassicFileList = new ReadOnlyObservableCollection<ListedItem>(_classicFileList);
            ClassicFolderList = new ReadOnlyObservableCollection<Classic_ListedFolderItem>(_classicFolderList);
            if(typeof(PageType) == typeof(GenericFileBrowser))
            {
                (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
            }
            else if(typeof(PageType) == typeof(PhotoAlbum))
            {
                (tabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
            }
            else if(typeof(PageType) == typeof(AddItem))
            {
                if((tabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null)
                {
                    (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
                }
                else if((tabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    (tabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
                }
            }

            tabInstance.HomeItems.PropertyChanged += HomeItems_PropertyChanged;
            tabInstance.ShareItems.PropertyChanged += ShareItems_PropertyChanged;
            tabInstance.LayoutItems.PropertyChanged += LayoutItems_PropertyChanged;
            tabInstance.AlwaysPresentCommands.PropertyChanged += AlwaysPresentCommands_PropertyChanged;

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
            GetCurrentSelectedTabInstance<ProHome>().accessiblePathTabView.Items.Clear();
            Style tabStyleFixed = GetCurrentSelectedTabInstance<ProHome>().accessiblePathTabView.Resources["PathSectionTabStyle"] as Style;
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

                        Microsoft.UI.Xaml.Controls.TabViewItem item = new Microsoft.UI.Xaml.Controls.TabViewItem()
                        {
                            Header = componentLabel + " >",
                            Tag = tag,
                            CornerRadius = new CornerRadius(0),
                            Style = tabStyleFixed,
                            FontWeight = weight
                        };
                        item.Tapped += Item_Tapped;
                        GetCurrentSelectedTabInstance<ProHome>().accessiblePathTabView.Items.Add(item);
                    }
                    else
                    {
                        componentLabel = s;
                        foreach (string part in pathComponents.GetRange(0, index + 1))
                        {
                            tag = tag + part + @"\";
                        }

                        Microsoft.UI.Xaml.Controls.TabViewItem item = new Microsoft.UI.Xaml.Controls.TabViewItem()
                        {
                            Header = componentLabel + " >",
                            Tag = tag,
                            CornerRadius = new CornerRadius(0),
                            Style = tabStyleFixed,
                            FontWeight = weight
                        };
                        item.Tapped += Item_Tapped;
                        GetCurrentSelectedTabInstance<ProHome>().accessiblePathTabView.Items.Add(item);

                    }
                    index++;
                }
            }
        }

        private void Item_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var itemTappedTag = (sender as Microsoft.UI.Xaml.Controls.TabViewItem).Tag.ToString();
            ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), itemTappedTag, new SuppressNavigationTransitionInfo());
        }

        private void AlwaysPresentCommands_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(tabInstance.AlwaysPresentCommands.isEnabled == true)
            {
                tabInstance.AlwaysPresentCommands.isEnabled = true;
            }
            else
            {
                tabInstance.AlwaysPresentCommands.isEnabled = false;
            }
        }

        private void LayoutItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (tabInstance.LayoutItems.isEnabled == true)
            {
                tabInstance.LayoutItems.isEnabled = true;
            }
            else
            {
                tabInstance.LayoutItems.isEnabled = false;
            }
        }

        private void ShareItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (tabInstance.ShareItems.isEnabled == true)
            {
                tabInstance.ShareItems.isEnabled = true;
            }
            else
            {
                tabInstance.ShareItems.isEnabled = false;
            }
        }

        private void HomeItems_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (tabInstance.HomeItems.isEnabled == true)
            {
                tabInstance.HomeItems.isEnabled = true;
            }
            else
            {
                tabInstance.HomeItems.isEnabled = false;
            }

        }

        public void AddFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Add(item);
            if ((tabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (tabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
            {
                if (typeof(PageType) == typeof(GenericFileBrowser))
                {
                    (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
                }
                else if (typeof(PageType) == typeof(PhotoAlbum))
                {
                    (tabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
                }
            }

        }

        public void RemoveFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Remove(item);
            if (_filesAndFolders.Count == 0)
            {
                if ((tabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (tabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    if (typeof(PageType) == typeof(GenericFileBrowser))
                    {
                        (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Visible;
                    }
                    else if (typeof(PageType) == typeof(PhotoAlbum))
                    {
                        (tabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        public void CancelLoadAndClearFiles()
        {
            if (_cancellationTokenSource == null) { return; }

            _cancellationTokenSource.Cancel();
            _filesAndFolders.Clear();

            //_folderQueryResult.ContentsChanged -= FolderContentsChanged;
            if(_fileQueryResult != null)
            {
                _fileQueryResult.ContentsChanged -= FileContentsChanged;
            }
        }

        public static T GetCurrentSelectedTabInstance<T>()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            var selectedTabContent = ((instanceTabsView.tabView.SelectedItem as TabViewItem).Content as Grid);
            foreach (UIElement uiElement in selectedTabContent.Children)
            {
                if (uiElement.GetType() == typeof(Frame))
                {
                    return (T) ((uiElement as Frame).Content);
                }
            }
            return default;
        }

        public async void DisplayConsentDialog()
        {
            await tabInstance.consentDialog.ShowAsync();
        }

        public async void AddItemsToCollectionAsync(string path, Page currentPage)
        {
            ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().RefreshButton.IsEnabled = false;

            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabHeader(new DirectoryInfo(path).Name);
            CancelLoadAndClearFiles();

            _cancellationTokenSource = new CancellationTokenSource();

            //tabInstance.TextState.isVisible = Visibility.Collapsed;
            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).TextState.isVisible = Visibility.Collapsed;
            }
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                (tabInstance.accessibleContentFrame.Content as PhotoAlbum).TextState.isVisible = Visibility.Collapsed;
            }

            _filesAndFolders.Clear();
            Universal.path = path;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Visible;
            }
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                (tabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Visible;
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

                //History.AddToHistory(Universal.path);

                tabInstance.BackButton.IsEnabled = tabInstance.accessibleContentFrame.CanGoBack;
                tabInstance.ForwardButton.IsEnabled = tabInstance.accessibleContentFrame.CanGoForward;

                switch (await _rootFolder.GetIndexedStateAsync())
                {
                    case (IndexedState.FullyIndexed):
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (typeof(PageType) == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.Size", "System.FileExtension" });
                        }
                        else if (typeof(PageType) == typeof(PhotoAlbum))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }
                        _options.IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;
                        break;
                    default:
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;

                        if (typeof(PageType) == typeof(GenericFileBrowser))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.ItemPathDisplay", "System.Size", "System.FileExtension" });
                        }
                        else if (typeof(PageType) == typeof(PhotoAlbum))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }

                        _options.IndexerOption = IndexerOption.UseIndexerWhenAvailable;
                        break;
                }

                SortEntry sort = new SortEntry()
                {
                    PropertyName = "System.FileName",
                    AscendingOrder = true
                };
                _options.SortOrder.Add(sort);
                if (!_rootFolder.AreQueryOptionsSupported(_options))
                {
                    _options.SortOrder.Clear();
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
                        if (_cancellationTokenSource.IsCancellationRequested) { return; }
                        await AddFolder(folder, _cancellationTokenSource.Token);
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
                        if (_cancellationTokenSource.IsCancellationRequested) { return; }
                        await AddFile(file, _cancellationTokenSource.Token);
                    }
                    index += _step;
                    storageFiles = await _fileQueryResult.GetFilesAsync(index, _step);
                }
                if (numFiles + numFolders == 0)
                {
                    if (typeof(PageType) == typeof(GenericFileBrowser))
                    {
                        if (_cancellationTokenSource.IsCancellationRequested) { return; }
                        (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).TextState.isVisible = Visibility.Visible;
                    }
                    else if (typeof(PageType) == typeof(PhotoAlbum))
                    {
                        if (_cancellationTokenSource.IsCancellationRequested) { return; }
                        (tabInstance.accessibleContentFrame.Content as PhotoAlbum).TextState.isVisible = Visibility.Visible;
                    }
                }
                stopwatch.Stop();
                Debug.WriteLine("Loading of items in " + Universal.path + " completed in " + stopwatch.ElapsedMilliseconds + " milliseconds.\n");
                ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().RefreshButton.IsEnabled = true;
            }
            catch (UnauthorizedAccessException e)
            {
                if (path.Contains(@"C:\"))
                {
                    DisplayConsentDialog();
                }
                else
                {
                    MessageDialog unsupportedDevice = new MessageDialog("This device may be unsupported. Please file an issue report containing what device we couldn't access. Technical information: " + e, "Unsupported Device");
                    await unsupportedDevice.ShowAsync();
                    return;
                }
            }
            catch (COMException e)
            {
                Frame rootContentFrame = Window.Current.Content as Frame;
                MessageDialog driveGone = new MessageDialog(e.Message, "Did you unplug this drive?");
                await driveGone.ShowAsync();
                rootContentFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                return;
            }
            catch (FileNotFoundException)
            {
                Frame rootContentFrame = Window.Current.Content as Frame;
                MessageDialog folderGone = new MessageDialog("The folder you've navigated to was removed.", "Did you delete this folder?");
                await folderGone.ShowAsync();
                rootContentFrame.Navigate(typeof(InstanceTabsView), null, new SuppressNavigationTransitionInfo());
                return;
            }

            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                if (_cancellationTokenSource.IsCancellationRequested) { return; }
                (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Collapsed;
            }
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                if (_cancellationTokenSource.IsCancellationRequested) { return; }
                (tabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Collapsed;
            }
        }

        public async void FillTreeNode(object item, TreeView EntireControl)
        {
            var pathToFillFrom = (item as Classic_ListedFolderItem)?.FilePath;
            StorageFolder folderFromPath = await StorageFolder.GetFolderFromPathAsync(pathToFillFrom);
            IReadOnlyList<StorageFolder> SubFolderList = await folderFromPath.GetFoldersAsync();
            foreach (StorageFolder fol in SubFolderList)
            {
                var name = fol.Name;
                var date = fol.DateCreated.LocalDateTime.ToString(CultureInfo.InvariantCulture);
                var ext = fol.DisplayType;
                var path = fol.Path;
                (item as Classic_ListedFolderItem)?.Children.Add(new Classic_ListedFolderItem() { FileName = name, FilePath = path, FileDate = date, FileExtension = ext });

            }
        }

        private async Task AddFolder(StorageFolder folder, CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }

            var basicProperties = await folder.GetBasicPropertiesAsync();

            if ((typeof(PageType) == typeof(GenericFileBrowser)) || (typeof(PageType) == typeof(PhotoAlbum)))
            {
                if (token.IsCancellationRequested) { return; }

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
                    RowIndex = _filesAndFolders.Count
                });
                if((tabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null)
                {
                    (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
                }
                else if((tabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    (tabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
                }
            }
            else if (typeof(PageType) == typeof(ClassicMode))
            {
                if (token.IsCancellationRequested) { return; }

                _classicFolderList.Add(new Classic_ListedFolderItem()
                {
                    FileName = folder.Name,
                    FileDate = ListedItem.GetFriendlyDate(basicProperties.DateModified),
                    FileExtension = "Folder",
                    FilePath = folder.Path
                });
            }
        }

        private async Task AddFile(StorageFile file, CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }

            var basicProperties = await file.GetBasicPropertiesAsync();

            var itemName = file.DisplayName;
            var itemDate = basicProperties.DateModified;
            var itemPath = file.Path;
            var itemSize = ByteSize.FromBytes(basicProperties.Size).ToString();
            var itemType = file.DisplayType;
            var itemFolderImgVis = Visibility.Collapsed;
            var itemFileExtension = file.FileType;

            BitmapImage icon = new BitmapImage();
            Visibility itemThumbnailImgVis;
            Visibility itemEmptyImgVis;

            if (!(typeof(PageType) == typeof(PhotoAlbum)))
            {
                try
                {
                    var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = Visibility.Collapsed;
                        itemThumbnailImgVis = Visibility.Visible;
                        await icon.SetSourceAsync(itemThumbnailImg.CloneStream());
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
                    var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = Visibility.Collapsed;
                        itemThumbnailImgVis = Visibility.Visible;
                        await icon.SetSourceAsync(itemThumbnailImg.CloneStream());
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

            if (!(typeof(PageType) == typeof(ClassicMode)))
            {
                if (token.IsCancellationRequested) { return; }

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
                    RowIndex = _filesAndFolders.Count
                });

                if(typeof(PageType) == typeof(GenericFileBrowser))
                {
                    (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).emptyTextGFB.Visibility = Visibility.Collapsed;
                }
                else if(typeof(PageType) == typeof(PhotoAlbum))
                {
                    (tabInstance.accessibleContentFrame.Content as PhotoAlbum).EmptyTextPA.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                if (token.IsCancellationRequested) { return; }

                _classicFileList.Add(new ListedItem(file.FolderRelativeId)
                {
                    FileImg = icon,
                    FileIconVis = itemThumbnailImgVis,
                    FolderImg = itemFolderImgVis,
                    FileName = itemName,
                    FileDateReal = itemDate,
                    FileType = itemType,
                    FilePath = itemPath
                });
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
                if((tabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (tabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    if (typeof(PageType) == typeof(GenericFileBrowser))
                    {
                        (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Visible;
                    }
                    else if (typeof(PageType) == typeof(PhotoAlbum))
                    {
                        (tabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Visible;
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

            var cancellationTokenSourceCopy = _cancellationTokenSource;

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
                    await AddFile(toAdd, cancellationTokenSourceCopy.Token);
                });
            }
            foreach (var folder in addedFolders)
            {
                var toAdd = folders.First(f => f.FolderRelativeId == folder);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    await AddFolder(toAdd, cancellationTokenSourceCopy.Token);
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
                if ((tabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null || (tabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    if (typeof(PageType) == typeof(GenericFileBrowser))
                    {
                        (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).progressBar.Visibility = Visibility.Collapsed;
                    }
                    else if (typeof(PageType) == typeof(PhotoAlbum))
                    {
                        (tabInstance.accessibleContentFrame.Content as PhotoAlbum).progressBar.Visibility = Visibility.Collapsed;
                    }
                }
            });

            _filesRefreshing = false;
            Debug.WriteLine("Filesystem refresh complete");
        }
    }
}
