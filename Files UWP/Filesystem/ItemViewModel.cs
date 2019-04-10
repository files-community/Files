using ByteSizeLib;
using Files.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;

namespace Files.Filesystem
{
    public class ItemViewModel
    {
        public ReadOnlyObservableCollection<ListedItem> FilesAndFolders { get; }
        public ReadOnlyObservableCollection<Classic_ListedFolderItem> ClassicFolderList { get; }
        public ReadOnlyObservableCollection<ListedItem> ClassicFileList { get; }

        public UniversalPath Universal { get; } = new UniversalPath();
        public EmptyFolderTextState TextState { get; set; } = new EmptyFolderTextState();
        public BackState BS { get; set; } = new BackState();
        public ForwardState FS { get; set; } = new ForwardState();
        public ProgressUIVisibility PVIS { get; set; } = new ProgressUIVisibility();

        private ObservableCollection<ListedItem> _filesAndFolders;
        private ObservableCollection<ListedItem> _classicFileList;
        private ObservableCollection<Classic_ListedFolderItem> _classicFolderList;

        private StorageFolderQueryResult _folderQueryResult;
        private StorageFileQueryResult _fileQueryResult;
        private CancellationTokenSource _cancellationTokenSource;
        private StorageFolder _rootFolder;
        private QueryOptions _options;
        private string _pageName;

        private volatile bool _filesRefreshing;
        //private volatile bool _foldersRefreshing;

        private const int _step = 250;

        public ItemViewModel()
        {
            _filesAndFolders = new ObservableCollection<ListedItem>();
            _classicFileList = new ObservableCollection<ListedItem>();
            _classicFolderList = new ObservableCollection<Classic_ListedFolderItem>();

            FilesAndFolders = new ReadOnlyObservableCollection<ListedItem>(_filesAndFolders);
            ClassicFileList = new ReadOnlyObservableCollection<ListedItem>(_classicFileList);
            ClassicFolderList = new ReadOnlyObservableCollection<Classic_ListedFolderItem>(_classicFolderList);
        }

        public void AddFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Add(item);
        }

        public void RemoveFileOrFolder(ListedItem item)
        {
            _filesAndFolders.Remove(item);
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

        public static async void DisplayConsentDialog()
        {
            await ProHome.permissionBox.ShowAsync();
        }

        public async void AddItemsToCollectionAsync(string path, Page currentPage)
        {
            CancelLoadAndClearFiles();

            _cancellationTokenSource = new CancellationTokenSource();
            var tokenSourceCopy = _cancellationTokenSource;

            TextState.isVisible = Visibility.Collapsed;

            _pageName = currentPage.Name;
            Universal.path = path;

            if (!_pageName.Contains("Classic"))
            {
                _filesAndFolders.Clear();
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            PVIS.isVisible = Visibility.Visible;

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

                History.AddToHistory(Universal.path);
                if (History.HistoryList.Count == 1)     // If this is the only item present in History, we don't want back button to be enabled
                {
                    BS.isEnabled = false;
                }
                else if (History.HistoryList.Count > 1)     // Otherwise, if this is not the first item, we'll enable back click
                {
                    BS.isEnabled = true;
                }

                switch (await _rootFolder.GetIndexedStateAsync())
                {
                    case (IndexedState.FullyIndexed):
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;
                        if (_pageName.Contains("Generic"))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.Size", "System.FileExtension" });
                        }
                        else if (_pageName.Contains("Photo"))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }
                        _options.IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;
                        break;
                    default:
                        _options = new QueryOptions();
                        _options.FolderDepth = FolderDepth.Shallow;
                        if (_pageName.Contains("Generic"))
                        {
                            _options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
                            _options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.ItemPathDisplay", "System.Size", "System.FileExtension" });
                        }
                        else if (_pageName.Contains("Photo"))
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
                        if (tokenSourceCopy.IsCancellationRequested) { return; }
                        await AddFolder(folder, _pageName, tokenSourceCopy.Token);
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
                        if (tokenSourceCopy.IsCancellationRequested) { return; }
                        await AddFile(file, _pageName, tokenSourceCopy.Token);
                    }
                    index += _step;
                    storageFiles = await _fileQueryResult.GetFilesAsync(index, _step);
                }
                if (numFiles + numFolders == 0)
                {
                    TextState.isVisible = Visibility.Visible;
                }
                stopwatch.Stop();
                Debug.WriteLine("Loading of items in " + Universal.path + " completed in " + stopwatch.Elapsed.Seconds + " seconds.\n");

            }
            catch (UnauthorizedAccessException e)
            {
                if (path.Contains(@"C:\"))
                {
                    DisplayConsentDialog();
                }
                else
                {
                    MessageDialog unsupportedDevice = new MessageDialog("This device may be unsupported. Please file an issue report in Settings - About containing what device we couldn't access. Technical information: " + e, "Unsupported Device");
                    await unsupportedDevice.ShowAsync();
                    return;
                }
            }
            catch (COMException e)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                MessageDialog driveGone = new MessageDialog(e.Message, "Drive Unplugged");
                await driveGone.ShowAsync();
                rootFrame.Navigate(typeof(ProHome), new SuppressNavigationTransitionInfo());
                return;
            }

            if (!_pageName.Contains("Classic"))
            {
                PVIS.isVisible = Visibility.Collapsed;
            }

            PVIS.isVisible = Visibility.Collapsed;
        }

        public static async void FillTreeNode(object item, TreeView EntireControl)
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

        private async Task AddFolder(StorageFolder folder, string pageName, CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }

            var basicProperties = await folder.GetBasicPropertiesAsync();

            if (!pageName.Contains("Classic"))
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
                    FileSize = null
                });
            }
            else
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

        private async Task AddFile(StorageFile file, string pageName, CancellationToken token)
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

            if (!pageName.Contains("Photo"))
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

            if (!pageName.Contains("Classic"))
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
                    FileSize = itemSize
                });
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

        private async void FileContentsChanged(IStorageQueryResultBase sender, object args)
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
                    await AddFile(toAdd, _pageName, cancellationTokenSourceCopy.Token);
                });
            }
            foreach (var folder in addedFolders)
            {
                var toAdd = folders.First(f => f.FolderRelativeId == folder);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    await AddFolder(toAdd, _pageName, cancellationTokenSourceCopy.Token);
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

            _filesRefreshing = false;
            Debug.WriteLine("Filesystem refresh complete");
        }

        //private async void FolderContentsChanged(IStorageQueryResultBase sender, object args)
        //{
        //    if (_foldersRefreshing) { return; }

        //    _foldersRefreshing = true;

        //    _foldersRefreshing = false;
        //}
    }
}
