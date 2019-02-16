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

namespace Files.Filesystem
{
    public class ItemViewModel
    {
        public static ObservableCollection<Classic_ListedFolderItem> classicFolderList = new ObservableCollection<Classic_ListedFolderItem>();
        public static ObservableCollection<Classic_ListedFolderItem> ClassicFolderList { get { return classicFolderList; } }

        public static ObservableCollection<ListedItem> classicFileList = new ObservableCollection<ListedItem>();
        public static ObservableCollection<ListedItem> ClassicFileList { get { return classicFileList; } }

        public static ObservableCollection<ListedItem> filesAndFolders = new ObservableCollection<ListedItem>();
        public static ObservableCollection<ListedItem> FilesAndFolders { get { return filesAndFolders; } }

        StorageFolder folder;
        static string gotName;
        static string gotDate;
        static string gotType;
        static string gotPath;
        static string gotFolName;
        static string gotFolDate;
        static string gotFolPath;
        static string gotFolType;
        static Visibility gotFileImgVis;
        static Visibility gotEmptyImgVis;
        static Visibility gotFolImg;
        static StorageItemThumbnail gotFileImg;
        public static ObservableCollection<Classic_ListedFolderItem> ChildrenList;
        public static IReadOnlyList<StorageFolder> folderList;
        public static IReadOnlyList<StorageFile> fileList;
        public static bool isPhotoAlbumMode;
        public static string pageName;

        public static ItemViewModel vm;
        public static ItemViewModel ViewModel { get { return vm; } set { } }

        public static BackState bs = new BackState();
        public static BackState BS
        {
            get
            {
                return bs;
            }
        }

        public static ForwardState fs = new ForwardState();
        public static ForwardState FS
        {
            get
            {
                return fs;
            }
        }

        public static ProgressUIVisibility pvis = new ProgressUIVisibility();
        public static ProgressUIVisibility PVIS
        {
            get
            {
                return pvis;
            }
        }

        private ListedItem li = new ListedItem();
        public ListedItem LI { get { return this.li; } }

        private static ProgressUIHeader pUIh = new ProgressUIHeader();
        public static ProgressUIHeader PUIH { get { return ItemViewModel.pUIh; } }

        private static ProgressUIPath pUIp = new ProgressUIPath();
        public static ProgressUIPath PUIP { get { return ItemViewModel.pUIp; } }

        private static ProgressUIButtonText buttonText = new ProgressUIButtonText();
        public static ProgressUIButtonText ButtonText { get { return ItemViewModel.buttonText; } }

        private static CollisionBoxHeader collisionBoxHeader = new CollisionBoxHeader();
        public static CollisionBoxHeader CollisionBoxHeader { get { return collisionBoxHeader; } }

        private static CollisionBoxSubHeader collisionBoxSubHeader = new CollisionBoxSubHeader();
        public static CollisionBoxSubHeader CollisionBoxSubHeader { get { return collisionBoxSubHeader; } }

        private static CollisionUIVisibility collisionUIVisibility = new CollisionUIVisibility();
        public static CollisionUIVisibility CollisionUIVisibility { get { return collisionUIVisibility; } }

        private static CollisionBoxHeader conflictBoxHeader = new CollisionBoxHeader();
        public static CollisionBoxHeader ConflictBoxHeader { get { return conflictBoxHeader; } }

        private static CollisionBoxSubHeader conflictBoxSubHeader = new CollisionBoxSubHeader();
        public static CollisionBoxSubHeader ConflictBoxSubHeader { get { return conflictBoxSubHeader; } }

        private static CollisionUIVisibility conflictUIVisibility = new CollisionUIVisibility();
        public static CollisionUIVisibility ConflictUIVisibility { get { return conflictUIVisibility; } }

        private static EmptyFolderTextState textState = new EmptyFolderTextState();
        public static EmptyFolderTextState TextState { get { return textState; } }

        public static int NumOfItems;
        public static int NumItemsRead;
        public static int NumOfFiles;
        public static int NumOfFolders;
        public static CancellationToken token;
        public static CancellationTokenSource tokenSource;

        public ItemViewModel(string viewPath, Page p)
        {

            pageName = p.Name;
            // Personalize retrieved items for view they are displayed in
            if (p.Name == "GenericItemView" || p.Name == "ClassicModePage")
            {
                isPhotoAlbumMode = false;
            }
            else if (p.Name == "PhotoAlbumViewer")
            {
                isPhotoAlbumMode = true;
            }

            if (pageName != "ClassicModePage")
            {
                GenericFileBrowser.P.path = viewPath;
                FilesAndFolders.Clear();
            }

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            MemoryFriendlyGetItemsAsync(viewPath, token);

            if (pageName != "ClassicModePage")
            {
                History.AddToHistory(viewPath);

                if (History.HistoryList.Count == 1)
                {
                    BS.isEnabled = false;
                    //Debug.WriteLine("Disabled Property");


                }
                else if (History.HistoryList.Count > 1)
                {
                    BS.isEnabled = true;
                    //Debug.WriteLine("Enabled Property");
                }
            }


        }

        private async void DisplayConsentDialog()
        {
            MessageDialog message = new MessageDialog("This app is not able to access your files. You need to allow it to by granting permission in Settings.");
            message.Title = "Permission Denied";
            message.Commands.Add(new UICommand("Allow...", Interaction.GrantAccessPermissionHandler));
            await message.ShowAsync();
        }
        string sort = "By_Name";
        SortEntry entry;
        public async void MemoryFriendlyGetItemsAsync(string path, CancellationToken token)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            PUIP.Path = path;
            try
            {

                PVIS.isVisible = Visibility.Visible;
                TextState.isVisible = Visibility.Collapsed;
                folder = await StorageFolder.GetFolderFromPathAsync(path);
                QueryOptions options = new QueryOptions()
                {
                    FolderDepth = FolderDepth.Shallow,
                    IndexerOption = IndexerOption.UseIndexerWhenAvailable
                };

                if (sort == "By_Name")
                {
                    entry = new SortEntry()
                    {
                        AscendingOrder = true,
                        PropertyName = "System.FileName"
                    };
                }
                options.SortOrder.Add(entry);

                uint index = 0;
                const uint step = 250;
                if (!folder.AreQueryOptionsSupported(options))
                {
                    options.SortOrder.Clear();
                }

                StorageFolderQueryResult folderQueryResult = folder.CreateFolderQueryWithOptions(options);
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
                StorageFileQueryResult fileQueryResult = folder.CreateFileQueryWithOptions(options);
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

                        gotName = file.Name.ToString();
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
                        if (isPhotoAlbumMode == false)
                        {
                            const uint requestedSize = 20;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.ListView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                            try
                            {
                                gotFileImg = await file.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
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
                            gotFileImg = await file.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                        }

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
                            FilesAndFolders.Add(new ListedItem() { EmptyImgVis = gotEmptyImgVis, FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
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
                    MessageDialog unsupportedDevice = new MessageDialog("This device is unsupported. Please file an issue report in Settings - About containing what device we couldn't access. Technical information: " + e, "Unsupported Device");
                    await unsupportedDevice.ShowAsync();
                }
                stopwatch.Stop();
                Debug.WriteLine("Loading of: " + path + " failed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
            }
            catch (COMException e)
            {
                stopwatch.Stop();
                Debug.WriteLine("Loading of: " + path + " failed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
                Frame rootFrame = Window.Current.Content as Frame;
                MessageDialog driveGone = new MessageDialog(e.Message, "Drive Unplugged");
                await driveGone.ShowAsync();
                rootFrame.Navigate(typeof(MainPage), new SuppressNavigationTransitionInfo());
            }

        }



        public static ProgressPercentage progressPER = new ProgressPercentage();

        public static ProgressPercentage PROGRESSPER
        {
            get
            {
                return progressPER;
            }
            set
            {

            }
        }

        public static int UpdateProgUI(int level)
        {
            PROGRESSPER.prog = level;
            return (int)level;
        }

        public static async void DisplayCollisionUIWithArgs(string header, string subHeader)
        {
            CollisionBoxHeader.Header = header;
            CollisionBoxSubHeader.SubHeader = subHeader;
            await GenericFileBrowser.collisionBox.ShowAsync();
        }

        public static async void DisplayReviewUIWithArgs(string header, string subHeader)
        {
            ConflictBoxHeader.Header = header;
            ConflictBoxSubHeader.SubHeader = subHeader;
            await GenericFileBrowser.reviewBox.ShowAsync();
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
