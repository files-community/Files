using ByteSizeLib;
using Files.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
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
        public ObservableCollection<Classic_ListedFolderItem> ClassicFolderList { get; set; } = new ObservableCollection<Classic_ListedFolderItem>();
        public ObservableCollection<ListedItem> ClassicFileList { get; set; } = new ObservableCollection<ListedItem>();
        public ObservableCollection<ListedItem> FilesAndFolders { get; set; } = new ObservableCollection<ListedItem>();
        public ObservableCollection<Classic_ListedFolderItem> ChildrenList;
        public ListedItem LI { get; } = new ListedItem();
        public UniversalPath Universal { get; } = new UniversalPath();
        public EmptyFolderTextState TextState { get; set; } = new EmptyFolderTextState();
        public BackState BS { get; set; } = new BackState();
        public ForwardState FS { get; set; } = new ForwardState();
        public ProgressUIVisibility PVIS { get; set; } = new ProgressUIVisibility();
        public CancellationTokenSource TokenSource { get; private set; }

        private StorageFolderQueryResult _folderQueryResult;
        private StorageFileQueryResult _fileQueryResult;

        public ItemViewModel()
        {
            TokenSource = new CancellationTokenSource();
        }

        private async void DisplayConsentDialog()
        {
            await MainPage.permissionBox.ShowAsync();
        }

        public async void AddItemsToCollectionAsync(string path, Page currentPage)
        {
            TokenSource.Cancel();
            TokenSource = new CancellationTokenSource();
            var tokenSourceCopy = TokenSource;

            TextState.isVisible = Visibility.Collapsed;
            
            var pageName = currentPage.Name;
            Universal.path = path;

            if (!pageName.Contains("Classic"))
                FilesAndFolders.Clear();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            PVIS.isVisible = Visibility.Visible;

            switch (Universal.path)
            {
                case "Desktop":
                    Universal.path = MainPage.DesktopPath;
                    break;
                case "Downloads":
                    Universal.path = MainPage.DownloadsPath;
                    break;
                case "Documents":
                    Universal.path = MainPage.DocumentsPath;
                    break;
                case "Pictures":
                    Universal.path = MainPage.PicturesPath;
                    break;
                case "Music":
                    Universal.path = MainPage.MusicPath;
                    break;
                case "Videos":
                    Universal.path = MainPage.VideosPath;
                    break;
                case "OneDrive":
                    Universal.path = MainPage.OneDrivePath;
                    break;
            }

            try
            {
                var rootFolder = await StorageFolder.GetFolderFromPathAsync(Universal.path);
                History.AddToHistory(Universal.path);
                if (History.HistoryList.Count == 1)     // If this is the only item present in History, we don't want back button to be enabled
                {
                    BS.isEnabled = false;
                }
                else if (History.HistoryList.Count > 1)     // Otherwise, if this is not the first item, we'll enable back click
                {
                    BS.isEnabled = true;
                }

                QueryOptions options = null;
                switch (await rootFolder.GetIndexedStateAsync())
                {
                    case (IndexedState.FullyIndexed):
                        options = new QueryOptions();
                        options.FolderDepth = FolderDepth.Shallow;
                        if (pageName.Contains("Generic"))
                        {
                            options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
                            options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.Size", "System.FileExtension" });
                        }
                        else if (pageName.Contains("Photo"))
                        {
                            options.SetThumbnailPrefetch(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
                            options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }
                        options.IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;
                        break;
                    default:
                        options = new QueryOptions();
                        options.FolderDepth = FolderDepth.Shallow;
                        if (pageName.Contains("Generic"))
                        {
                            options.SetThumbnailPrefetch(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
                            options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.DateModified", "System.ContentType", "System.ItemPathDisplay", "System.Size", "System.FileExtension" });
                        }
                        else if (pageName.Contains("Photo"))
                        {
                            options.SetThumbnailPrefetch(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
                            options.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, new string[] { "System.FileExtension" });
                        }
                        options.IndexerOption = IndexerOption.UseIndexerWhenAvailable;
                        break;
                }

                SortEntry sort = new SortEntry()
                {
                    PropertyName = "System.FileName",
                    AscendingOrder = true
                };
                options.SortOrder.Add(sort);
                if (!rootFolder.AreQueryOptionsSupported(options))
                {
                    options.SortOrder.Clear();
                }

                uint index = 0;
                const uint step = 250;
                _folderQueryResult = rootFolder.CreateFolderQueryWithOptions(options);
                uint NumFolItems = await _folderQueryResult.GetItemCountAsync();
                IReadOnlyList<StorageFolder> storageFolders = await _folderQueryResult.GetFoldersAsync(index, step);
                while (storageFolders.Count > 0)
                {
                    foreach (StorageFolder folder in storageFolders)
                    {
                        if (tokenSourceCopy.IsCancellationRequested) { return; }
                        await AddFolder(folder, pageName, tokenSourceCopy.Token);
                    }
                    index += step;
                    storageFolders = await _folderQueryResult.GetFoldersAsync(index, step);
                }

                index = 0;
                _fileQueryResult = rootFolder.CreateFileQueryWithOptions(options);
                uint NumFileItems = await _fileQueryResult.GetItemCountAsync();
                IReadOnlyList<StorageFile> storageFiles = await _fileQueryResult.GetFilesAsync(index, step);
                while (storageFiles.Count > 0)
                {
                    foreach (StorageFile file in storageFiles)
                    {
                        if (tokenSourceCopy.IsCancellationRequested) { return; }
                        await AddFile(file, pageName, tokenSourceCopy.Token);
                    }
                    index += step;
                    storageFiles = await _fileQueryResult.GetFilesAsync(index, step);
                }
                if (NumFolItems + NumFileItems == 0)
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
                rootFrame.Navigate(typeof(MainPage), new SuppressNavigationTransitionInfo());
                return;
            }

            if (!pageName.Contains("Classic"))
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

        private static string GetFriendlyDate(DateTime d)
        {
            if (d.Year == DateTime.Now.Year)              // If item is accessed in the same year as stored
            {
                if (d.Month == DateTime.Now.Month)        // If item is accessed in the same month as stored
                {
                    if ((DateTime.Now.Day - d.Day) < 7) // If item is accessed on the same week
                    {
                        if (d.DayOfWeek == DateTime.Now.DayOfWeek)   // If item is accessed on the same day as stored
                        {
                            if ((DateTime.Now.Hour - d.Hour) > 1)
                            {
                                return DateTime.Now.Hour - d.Hour + " hours ago";
                            }
                            else
                            {
                                return DateTime.Now.Hour - d.Hour + " hour ago";
                            }
                        }
                        else                                                        // If item is from a previous day of the same week
                        {
                            return d.DayOfWeek + " at " + d.ToShortTimeString();
                        }
                    }
                    else                                                          // If item is from a previous week of the same month
                    {
                        string monthAsString = "Month";
                        switch (d.Month)
                        {
                            case 1:
                                monthAsString = "January";
                                break;
                            case 2:
                                monthAsString = "February";
                                break;
                            case 3:
                                monthAsString = "March";
                                break;
                            case 4:
                                monthAsString = "April";
                                break;
                            case 5:
                                monthAsString = "May";
                                break;
                            case 6:
                                monthAsString = "June";
                                break;
                            case 7:
                                monthAsString = "July";
                                break;
                            case 8:
                                monthAsString = "August";
                                break;
                            case 9:
                                monthAsString = "September";
                                break;
                            case 10:
                                monthAsString = "October";
                                break;
                            case 11:
                                monthAsString = "November";
                                break;
                            case 12:
                                monthAsString = "December";
                                break;
                        }
                        return monthAsString + " " + d.Day;
                    }

                }
                else                                                            // If item is from a past month of the same year
                {
                    string monthAsString = "Month";
                    switch (d.Month)
                    {
                        case 1:
                            monthAsString = "January";
                            break;
                        case 2:
                            monthAsString = "February";
                            break;
                        case 3:
                            monthAsString = "March";
                            break;
                        case 4:
                            monthAsString = "April";
                            break;
                        case 5:
                            monthAsString = "May";
                            break;
                        case 6:
                            monthAsString = "June";
                            break;
                        case 7:
                            monthAsString = "July";
                            break;
                        case 8:
                            monthAsString = "August";
                            break;
                        case 9:
                            monthAsString = "September";
                            break;
                        case 10:
                            monthAsString = "October";
                            break;
                        case 11:
                            monthAsString = "November";
                            break;
                        case 12:
                            monthAsString = "December";
                            break;
                    }
                    return monthAsString + " " + d.Day;
                }
            }
            else                                                                // If item is from a past year
            {
                string monthAsString = "Month";
                switch (d.Month)
                {
                    case 1:
                        monthAsString = "January";
                        break;
                    case 2:
                        monthAsString = "February";
                        break;
                    case 3:
                        monthAsString = "March";
                        break;
                    case 4:
                        monthAsString = "April";
                        break;
                    case 5:
                        monthAsString = "May";
                        break;
                    case 6:
                        monthAsString = "June";
                        break;
                    case 7:
                        monthAsString = "July";
                        break;
                    case 8:
                        monthAsString = "August";
                        break;
                    case 9:
                        monthAsString = "September";
                        break;
                    case 10:
                        monthAsString = "October";
                        break;
                    case 11:
                        monthAsString = "November";
                        break;
                    case 12:
                        monthAsString = "December";
                        break;
                }
                return monthAsString + " " + d.Day + ", " + d.Year;
            }
        }

        private async Task AddFolder(StorageFolder folder, string pageName, CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }

            var basicProperties = await folder.GetBasicPropertiesAsync();

            if (!pageName.Contains("Classic"))
            {
                if (token.IsCancellationRequested) { return; }

                FilesAndFolders.Add(new ListedItem()
                {
                    FileName = folder.Name,
                    FileDate = GetFriendlyDate(basicProperties.ItemDate.LocalDateTime),
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

                ClassicFolderList.Add(new Classic_ListedFolderItem()
                {
                    FileName = folder.Name,
                    FileDate = GetFriendlyDate(basicProperties.ItemDate.LocalDateTime),
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
            var itemDate = GetFriendlyDate(basicProperties.DateModified.LocalDateTime);
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

                FilesAndFolders.Add(new ListedItem()
                {
                    DotFileExtension = itemFileExtension,
                    EmptyImgVis = itemEmptyImgVis,
                    FileImg = icon,
                    FileIconVis = itemThumbnailImgVis,
                    FolderImg = itemFolderImgVis,
                    FileName = itemName,
                    FileDate = itemDate,
                    FileType = itemType,
                    FilePath = itemPath,
                    FileSize = itemSize
                });
            }
            else
            {
                if (token.IsCancellationRequested) { return; }

                ClassicFileList.Add(new ListedItem()
                {
                    FileImg = icon,
                    FileIconVis = itemThumbnailImgVis,
                    FolderImg = itemFolderImgVis,
                    FileName = itemName,
                    FileDate = itemDate,
                    FileType = itemType,
                    FilePath = itemPath
                });
            }
        }
    }
}
