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
        List<IStorageItem> allItems = new List<IStorageItem>();

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
            {
                FilesAndFolders.Clear();
                allItems.Clear();
            }

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
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
                {
                    if (History.HistoryList.Count == 1)     // If this is the only item present in History, we don't want back button to be enabled
                    {
                        BS.isEnabled = false;
                    }
                    else if (History.HistoryList.Count > 1)     // Otherwise, if this is not the first item, we'll enable back click
                    {
                        BS.isEnabled = true;
                    }
                }));
                QueryOptions options = new QueryOptions();
                options.FolderDepth = FolderDepth.Shallow;
                options.IndexerOption = IndexerOption.UseIndexerWhenAvailable;
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
                GetItemsQuickly(options, token);

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
            stopwatch.Stop();
            Debug.WriteLine("Loading of: " + Universal.path + " completed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
            tokenSource = null;
        }

        public void GetItemsQuickly(QueryOptions options, CancellationToken token)
        {
            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(async (workItem) =>
            {
                var time = Stopwatch.StartNew();
                uint index = 0;
                const uint step = 250;
                StorageItemQueryResult itemQueryResult = folder.CreateItemQueryWithOptions(options);
                uint NumItems = await itemQueryResult.GetItemCountAsync();
                IReadOnlyList<IStorageItem> storageItems = await itemQueryResult.GetItemsAsync(index, step);
                while (storageItems.Count > 0)
                {
                    foreach (IStorageItem item in storageItems)
                    {
                        allItems.Add(item);
                    }
                    index += step;
                    storageItems = await itemQueryResult.GetItemsAsync(index, step);
                    Debug.WriteLine("Enumeration of IStorageItems in " + Universal.path + " in process from " + time.Elapsed.Seconds + " seconds ago.\n");
                }
                time.Stop();
                Debug.WriteLine("Enumeration of IStorageItems in " + Universal.path + " completed in " + time.Elapsed.Seconds + " seconds.\n");
                await CoreApplication.MainView.Dispatcher.RunAsync((CoreDispatcherPriority.Normal), new DispatchedHandler(() =>
                {
                    DisplayAllItemsAsync(allItems, token);
                }));
                
            });
            
        }



        private async void DisplayAllItemsAsync(IList<IStorageItem> allTheItems, CancellationToken token)
        {
            IList<IStorageItem> storageItems = allTheItems;
            StorageFile itemAsStorageFile;
            BasicProperties basicProperties;
            ObservableCollection<ListedItem> folders = new ObservableCollection<ListedItem>();
            ObservableCollection<ListedItem> files = new ObservableCollection<ListedItem>();
            folders.Clear();
            files.Clear();
            foreach (IStorageItem item in storageItems)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                basicProperties = await item.GetBasicPropertiesAsync();
                itemName = item.Name;
                itemDate = basicProperties.ItemDate.ToString();
                itemPath = item.Path;
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

                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    itemType = "Folder";
                    itemFolderImgVis = Visibility.Visible;
                    itemThumbnailImgVis = Visibility.Collapsed;
                    itemEmptyImgVis = Visibility.Collapsed;

                    if (!pageName.Contains("Classic"))
                    {
                        folders.Add(new ListedItem() { FileName = itemName, FileDate = itemDate, FileType = itemType, FolderImg = itemFolderImgVis, FileImg = null, FileIconVis = itemThumbnailImgVis, FilePath = itemPath, DotFileExtension = itemFileExtension, EmptyImgVis = itemEmptyImgVis, FileSize = null });
                    }
                    else
                    {
                        ClassicFolderList.Add(new Classic_ListedFolderItem() { FileName = itemName, FileDate = itemDate, FileExtension = itemType, FilePath = itemPath });
                    }
                }
                else
                {
                    BitmapImage icon = new BitmapImage();
                    itemAsStorageFile = item as StorageFile;
                    itemType = itemAsStorageFile.DisplayType;
                    itemFolderImgVis = Visibility.Collapsed;
                    itemFileExtension = itemAsStorageFile.FileType;
                    if (!pageName.Contains("Photo"))
                    {
                        try
                        {
                            itemThumbnailImg = await itemAsStorageFile.GetThumbnailAsync(ThumbnailMode.ListView, 20, ThumbnailOptions.UseCurrentScale);
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
                            itemThumbnailImg = await itemAsStorageFile.GetThumbnailAsync(ThumbnailMode.PicturesView, 275, ThumbnailOptions.ResizeThumbnail);
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
                        files.Add(new ListedItem() { DotFileExtension = itemFileExtension, EmptyImgVis = itemEmptyImgVis, FileImg = icon, FileIconVis = itemThumbnailImgVis, FolderImg = itemFolderImgVis, FileName = itemName, FileDate = itemDate, FileType = itemType, FilePath = itemPath, FileSize = itemSize });
                    }
                    else
                    {
                        ClassicFileList.Add(new ListedItem() { FileImg = icon, FileIconVis = itemThumbnailImgVis, FolderImg = itemFolderImgVis, FileName = itemName, FileDate = itemDate, FileType = itemType, FilePath = itemPath });
                    }
                }
            }

            if (storageItems.Count == 0)
            {
                TextState.isVisible = Visibility.Visible;
            }

            foreach (ListedItem li in folders)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                FilesAndFolders.Add(li);
            }
            foreach (ListedItem li in files)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                FilesAndFolders.Add(li);
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
