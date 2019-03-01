using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading;
using Files.Interacts;
using Files.Navigation;
using Windows.Storage.Search;
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;
using Windows.UI.Core;
using ByteSizeLib;
using Windows.Storage.Pickers;
using Microsoft.Win32.SafeHandles;
using System.IO;
using Windows.Foundation;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

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
        private StorageFolder folder;
        private string itemName;
        private string itemDate;
        private string itemType;
        private string itemPath;
        private string itemSize;
        private string itemFileExtension;
        private Visibility itemThumbnailImgVis;
        private Visibility itemEmptyImgVis;
        private Visibility itemFolderImgVis;
        private StorageItemThumbnail itemThumbnailImg;
        public string pageName;
        public CancellationTokenSource tokenSource;

        public ItemViewModel()
        {
            
        }

        private async void DisplayConsentDialog()
        {
            await MainPage.permissionBox.ShowAsync();
        }

        public async void AddItemsToCollectionAsync(string path, Page currentPage)
        {
            TextState.isVisible = Visibility.Collapsed;
            tokenSource = new CancellationTokenSource();
            CancellationToken token = App.ViewModel.tokenSource.Token;
            pageName = currentPage.Name;
            Universal.path = path;

            if (!pageName.Contains("Classic"))
                FilesAndFolders.Clear();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            PVIS.isVisible = Visibility.Visible;
            TextState.isVisible = Visibility.Collapsed;

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

                folder = await StorageFolder.GetFolderFromPathAsync(Universal.path);
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
                switch(await folder.GetIndexedStateAsync())
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
                if (!folder.AreQueryOptionsSupported(options))
                {
                    options.SortOrder.Clear();
                }

                uint index = 0;
                const uint step = 250;
                BasicProperties basicProperties;
                StorageFolderQueryResult folderQueryResult = folder.CreateFolderQueryWithOptions(options);
                uint NumItems = await folderQueryResult.GetItemCountAsync();
                IReadOnlyList<StorageFolder> storageFolders = await folderQueryResult.GetFoldersAsync(index, step);
                while (storageFolders.Count > 0)
                {
                    foreach (StorageFolder folder in storageFolders)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        basicProperties = await folder.GetBasicPropertiesAsync();
                        itemName = folder.Name;
                        SetAsFriendlyDate(basicProperties.ItemDate.LocalDateTime);
                        itemPath = folder.Path;
                        if (ByteSize.FromBytes(basicProperties.Size).KiloBytes < 1)
                        {
                            itemSize = "0 KB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).KiloBytes >= 1 && ByteSize.FromBytes(basicProperties.Size).MegaBytes < 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).KiloBytes) + " KB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).MegaBytes >= 1 && ByteSize.FromBytes(basicProperties.Size).GigaBytes < 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).MegaBytes) + " MB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).GigaBytes >= 1 && ByteSize.FromBytes(basicProperties.Size).TeraBytes < 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).GigaBytes) + " GB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).TeraBytes >= 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).TeraBytes) + " TB";
                        }

                        itemType = "Folder";
                        itemFolderImgVis = Visibility.Visible;
                        itemThumbnailImgVis = Visibility.Collapsed;
                        itemEmptyImgVis = Visibility.Collapsed;

                        if (!pageName.Contains("Classic"))
                        {
                            if (token.IsCancellationRequested)
                            {
                                tokenSource = null;
                                return;
                            }
                            FilesAndFolders.Add(new ListedItem() { FileName = itemName, FileDate = itemDate, FileType = itemType, FolderImg = itemFolderImgVis, FileImg = null, FileIconVis = itemThumbnailImgVis, FilePath = itemPath, DotFileExtension = itemFileExtension, EmptyImgVis = itemEmptyImgVis, FileSize = null });
                        }
                        else
                        {
                            if (token.IsCancellationRequested)
                            {
                                tokenSource = null;
                                return;
                            }
                            ClassicFolderList.Add(new Classic_ListedFolderItem() { FileName = itemName, FileDate = itemDate, FileExtension = itemType, FilePath = itemPath });
                        }

                        
                    }
                    index += step;
                    storageFolders = await folderQueryResult.GetFoldersAsync(index, step);
                }

                index = 0;
                StorageFileQueryResult fileQueryResult = folder.CreateFileQueryWithOptions(options);
                uint NumFileItems = await fileQueryResult.GetItemCountAsync();
                IReadOnlyList<StorageFile> storageFiles = await fileQueryResult.GetFilesAsync(index, step);
                List<string> propertiesToRetrieve = new List<string>();
                propertiesToRetrieve.Add("System.FileExtension");
                while (storageFiles.Count > 0)
                {
                    foreach (StorageFile file in storageFiles)
                    {
                        basicProperties = await file.GetBasicPropertiesAsync();
                        var props = await file.Properties.RetrievePropertiesAsync(propertiesToRetrieve);
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        itemName = file.DisplayName;
                        SetAsFriendlyDate(basicProperties.ItemDate.LocalDateTime);
                        itemPath = file.Path;
                        if (ByteSize.FromBytes(basicProperties.Size).KiloBytes < 1)
                        {
                            itemSize = "0 KB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).KiloBytes >= 1 && ByteSize.FromBytes(basicProperties.Size).MegaBytes < 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).KiloBytes) + " KB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).MegaBytes >= 1 && ByteSize.FromBytes(basicProperties.Size).GigaBytes < 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).MegaBytes) + " MB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).GigaBytes >= 1 && ByteSize.FromBytes(basicProperties.Size).TeraBytes < 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).GigaBytes) + " GB";
                        }
                        else if (ByteSize.FromBytes(basicProperties.Size).TeraBytes >= 1)
                        {
                            itemSize = decimal.Round((decimal)ByteSize.FromBytes(basicProperties.Size).TeraBytes) + " TB";
                        }
                        BitmapImage icon = new BitmapImage();
                        itemType = file.DisplayType;
                        itemFolderImgVis = Visibility.Collapsed;
                        itemFileExtension = props["System.FileExtension"].ToString();
                        if (!pageName.Contains("Photo"))
                        {
                            try
                            {
                                itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
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
                                itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
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
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            FilesAndFolders.Add(new ListedItem() { DotFileExtension = itemFileExtension, EmptyImgVis = itemEmptyImgVis, FileImg = icon, FileIconVis = itemThumbnailImgVis, FolderImg = itemFolderImgVis, FileName = itemName, FileDate = itemDate, FileType = itemType, FilePath = itemPath, FileSize = itemSize });
                        }
                        else
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            ClassicFileList.Add(new ListedItem() { FileImg = icon, FileIconVis = itemThumbnailImgVis, FolderImg = itemFolderImgVis, FileName = itemName, FileDate = itemDate, FileType = itemType, FilePath = itemPath });
                        }
                    }
                    index += step;
                    storageFiles = await fileQueryResult.GetFilesAsync(index, step);
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
            tokenSource = null;
        }

        public void SetAsFriendlyDate(DateTime d)
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
                                itemDate = DateTime.Now.Hour - d.Hour + " hours ago";
                            }
                            else
                            {
                                itemDate = DateTime.Now.Hour - d.Hour + " hour ago";
                            }
                        }
                        else                                                        // If item is from a previous day of the same week
                        {
                            itemDate = d.DayOfWeek + " at " + d.ToShortTimeString();
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
                        itemDate = monthAsString + " " + d.Day;
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
                    itemDate = monthAsString + " " + d.Day;
                }
            }
            else                                                                // If item is from a past year
            {
                itemDate = d.Month + " " + d.Day + ", " + d.Year;
            }
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
    }
}
