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
        private string gotName;
        private string gotDate;
        private string gotType;
        private string gotPath;
        private string gotFolName;
        private string gotFolDate;
        private string gotFolPath;
        private string gotDotFileExtension;
        private string gotFolType;
        private Visibility gotFileImgVis;
        private Visibility gotEmptyImgVis;
        private Visibility gotFolImg;
        private StorageItemThumbnail gotFileImg;
        private bool isPhotoAlbumMode;
        public string pageName;
        private StorageFileQueryResult fileQueryResult;
        private StorageFolderQueryResult folderQueryResult;
        public CancellationTokenSource tokenSource;


        public ItemViewModel()
        {
            
        }

        private async void DisplayConsentDialog()
        {
            await MainPage.permissionBox.ShowAsync();
        }
        public async void MemoryFriendlyGetItemsAsync(string path, Page passedPage)
        {
            TextState.isVisible = Visibility.Collapsed;
            tokenSource = new CancellationTokenSource();
            CancellationToken token = App.ViewModel.tokenSource.Token;
            pageName = passedPage.Name;
            Universal.path = path;
            // Personalize retrieved items for view they are displayed in
            switch (pageName)
            {
                case "GenericItemView":
                    isPhotoAlbumMode = false;
                    break;
                case "PhotoAlbumViewer":
                    isPhotoAlbumMode = true;
                    break;
                case "ClassicModePage":
                    isPhotoAlbumMode = false;
                    break;
            }

            if (pageName != "ClassicModePage")
            {
                FilesAndFolders.Clear();
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Universal.path = path;      // Set visible path to reflect new navigation
            try
            {

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

                folder = await StorageFolder.GetFolderFromPathAsync(Universal.path);

                History.AddToHistory(Universal.path);

                if (History.HistoryList.Count == 1)
                {
                    BS.isEnabled = false;
                }
                else if (History.HistoryList.Count > 1)
                {
                    BS.isEnabled = true;
                }

                QueryOptions options = new QueryOptions()
                {
                    FolderDepth = FolderDepth.Shallow,
                    IndexerOption = IndexerOption.UseIndexerWhenAvailable
                };
                string sort = "By_Name";
                if (sort == "By_Name")
                {
                    SortEntry entry = new SortEntry()
                    {
                        AscendingOrder = true,
                        PropertyName = "System.FileName"
                    };
                    options.SortOrder.Add(entry);
                }

                uint index = 0;
                const uint step = 250;
                if (!folder.AreQueryOptionsSupported(options))
                {
                    options.SortOrder.Clear();
                }

                folderQueryResult = folder.CreateFolderQueryWithOptions(options);
                IReadOnlyList<StorageFolder> folders = await folderQueryResult.GetFoldersAsync(index, step);
                int foldersCountSnapshot = folders.Count;
                while (folders.Count != 0)
                {
                    foreach (StorageFolder folder in folders)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        gotFolName = folder.Name.ToString();
                        gotFolDate = folder.DateCreated.ToString();
                        gotFolPath = folder.Path.ToString();
                        gotFolType = "Folder";
                        gotFolImg = Visibility.Visible;
                        gotFileImgVis = Visibility.Collapsed;
                        gotEmptyImgVis = Visibility.Collapsed;


                        if (pageName == "ClassicModePage")
                        {
                            ClassicFolderList.Add(new Classic_ListedFolderItem() { FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }
                        else
                        {
                            FilesAndFolders.Add(new ListedItem() { EmptyImgVis = gotEmptyImgVis, ItemIndex = FilesAndFolders.Count, FileImg = null, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }
                    }
                    index += step;
                    folders = await folderQueryResult.GetFoldersAsync(index, step);
                }

                index = 0;
                fileQueryResult = folder.CreateFileQueryWithOptions(options);
                IReadOnlyList<StorageFile> files = await fileQueryResult.GetFilesAsync(index, step);
                int filesCountSnapshot = files.Count;
                while (files.Count != 0)
                {
                    foreach (StorageFile file in files)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        gotName = file.DisplayName.ToString();
                        gotDate = file.DateCreated.ToString(); // In the future, parse date to human readable format
                        if (file.FileType.ToString() == ".exe")
                        {
                            gotType = "Executable";
                        }
                        else
                        {
                            gotType = file.DisplayType;
                        }
                        gotPath = file.Path.ToString();
                        gotFolImg = Visibility.Collapsed;
                        gotDotFileExtension = file.FileType;
                        if (isPhotoAlbumMode == false)
                        {
                            const uint requestedSize = 20;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.ListView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                            try
                            {
                                gotFileImg = await file.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                                BitmapImage icon = new BitmapImage();
                                if (gotFileImg != null)
                                {
                                    gotEmptyImgVis = Visibility.Collapsed;
                                    icon.SetSource(gotFileImg.CloneStream());
                                }
                                else
                                {
                                    gotEmptyImgVis = Visibility.Visible;
                                }
                                gotFileImgVis = Visibility.Visible;

                                if (pageName == "ClassicModePage")
                                {
                                    ClassicFileList.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                                }
                                else
                                {
                                    FilesAndFolders.Add(new ListedItem() { DotFileExtension = gotDotFileExtension, EmptyImgVis = gotEmptyImgVis, FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                                }
                            }
                            catch
                            {
                                // Silent catch here to avoid crash
                                // TODO maybe some logging could be added in the future...
                            }
                        }
                        else
                        {
                            const uint requestedSize = 275;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.PicturesView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.ResizeThumbnail;
                            try
                            {
                                gotFileImg = await file.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                                BitmapImage icon = new BitmapImage();
                                if (gotFileImg != null)
                                {
                                    gotEmptyImgVis = Visibility.Collapsed;
                                    icon.SetSource(gotFileImg.CloneStream());
                                }
                                else
                                {
                                    gotEmptyImgVis = Visibility.Visible;
                                }
                                gotFileImgVis = Visibility.Visible;

                                if (pageName == "ClassicModePage")
                                {
                                    ClassicFileList.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                                }
                                else
                                {
                                    FilesAndFolders.Add(new ListedItem() { DotFileExtension = gotDotFileExtension, EmptyImgVis = gotEmptyImgVis, FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                                }
                            }
                            catch
                            {
                                // Silent catch here to avoid crash
                                // TODO maybe some logging could be added in the future...
                            }
                        }

                        
                    }
                    index += step;
                    files = await fileQueryResult.GetFilesAsync(index, step);
                }
                if (foldersCountSnapshot + filesCountSnapshot == 0)
                {
                    TextState.isVisible = Visibility.Visible;
                }
                if (pageName != "ClassicModePage")
                {
                    PVIS.isVisible = Visibility.Collapsed;
                }
                PVIS.isVisible = Visibility.Collapsed;
                stopwatch.Stop();
                Debug.WriteLine("Loading of: " + path + " completed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
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
                }
                stopwatch.Stop();
                Debug.WriteLine("Loading of: " + Universal.path + " failed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
            }
            catch (COMException e)
            {
                stopwatch.Stop();
                Debug.WriteLine("Loading of: " + Universal.path + " failed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
                Frame rootFrame = Window.Current.Content as Frame;
                MessageDialog driveGone = new MessageDialog(e.Message, "Drive Unplugged");
                await driveGone.ShowAsync();
                rootFrame.Navigate(typeof(MainPage), new SuppressNavigationTransitionInfo());
            }

            tokenSource = null;
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
