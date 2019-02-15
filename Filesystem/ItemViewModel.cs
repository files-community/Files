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
using Files.Interacts;
using Files.Navigation;
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

        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

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

        public ItemViewModel(string viewPath, Page p)
        {
            pageName = p.Name;
            // Personalize retrieved items for view they are displayed in
            if(p.Name == "GenericItemView" || p.Name == "ClassicModePage")
            {
                isPhotoAlbumMode = false;
            }
            else if (p.Name == "PhotoAlbumViewer")
            {
                isPhotoAlbumMode = true;
            }
            
            if(pageName != "ClassicModePage")
            {
                GenericFileBrowser.P.path = viewPath;
                FilesAndFolders.Clear();
            }
                
            GetItemsAsync(viewPath);

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

        private ListedItem li = new ListedItem();
        public ListedItem LI { get { return li; } }

        private static ProgressUIHeader pUIh = new ProgressUIHeader();
        public static ProgressUIHeader PUIH { get { return pUIh; } }

        private static ProgressUIPath pUIp = new ProgressUIPath();
        public static ProgressUIPath PUIP { get { return pUIp; } }

        private static ProgressUIButtonText buttonText = new ProgressUIButtonText();
        public static ProgressUIButtonText ButtonText { get { return buttonText; } }

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
        public static bool IsStopRequested;
        public static bool IsTerminated = true;

        public static int NumOfItems;
        public static int NumItemsRead;
        public static int NumOfFiles;
        public static int NumOfFolders;
        public async void GetItemsAsync(string path)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            IsTerminated = false;
            PUIP.Path = path;
            try
            {
                folder = await StorageFolder.GetFolderFromPathAsync(path);          // Set location to the current directory specified in path
                folderList = await folder.GetFoldersAsync();                        // Create a read-only list of all folders in location
                fileList = await folder.GetFilesAsync();                            // Create a read-only list of all files in location
                NumOfFolders = folderList.Count;                                    // How many folders are in the list
                NumOfFiles = fileList.Count;                                        // How many files are in the list
                NumOfItems = NumOfFiles + NumOfFolders;
                NumItemsRead = 0;

                if (NumOfItems == 0)
                {
                    TextState.isVisible = Visibility.Visible;
                }

                PUIH.Header = "Loading " + NumOfItems + " items";
                ButtonText.buttonText = "Hide";
                
                if (NumOfItems >= 250)
                {
                    PVIS.isVisible = Visibility.Visible;
                }
                if (NumOfFolders > 0)
                {
                    foreach (StorageFolder fol in folderList)
                    {
                        if (IsStopRequested)
                        {
                            IsStopRequested = false;
                            IsTerminated = true;
                            return;
                        }
                        int progressReported = NumItemsRead * 100 / NumOfItems;
                        UpdateProgUI(progressReported);
                        gotFolName = fol.Name;
                        gotFolDate = fol.DateCreated.ToString();
                        gotFolPath = fol.Path;
                        gotFolType = "Folder";
                        gotFolImg = Visibility.Visible;
                        gotFileImgVis = Visibility.Collapsed;
                        
                        if (pageName == "ClassicModePage")
                        {
                            ClassicFolderList.Add(new Classic_ListedFolderItem { FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }
                        else
                        {
                            FilesAndFolders.Add(new ListedItem { ItemIndex = FilesAndFolders.Count, FileImg = null, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }
                        
                        NumItemsRead++;
                    }
                }

                if (NumOfFiles > 0)
                {
                    foreach (StorageFile f in fileList)
                    {
                        if (IsStopRequested)
                        {
                            IsStopRequested = false;
                            IsTerminated = true;
                            return;
                        }

                        var progressReported = NumItemsRead * 100 / NumOfItems;
                        UpdateProgUI(progressReported);
                        gotName = f.Name;
                        gotDate = f.DateCreated.ToString(); // In the future, parse date to human readable format
                        gotType = f.FileType == ".exe" ? "Executable" : f.DisplayType;
                        gotPath = f.Path;
                        gotFolImg = Visibility.Collapsed;
                        if (isPhotoAlbumMode == false)
                        {
                            const uint requestedSize = 20;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.ListView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                            try
                            {
                                gotFileImg = await f.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
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
                            gotFileImg = await f.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                        }

                        BitmapImage icon = new BitmapImage();
                        if (gotFileImg != null)
                        {
                            icon.SetSource(gotFileImg.CloneStream());
                        }
                        gotFileImgVis = Visibility.Visible;

                        if (pageName == "ClassicModePage")
                        {
                            ClassicFileList.Add(new ListedItem { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                        }
                        else
                        {
                            FilesAndFolders.Add(new ListedItem { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                        }
                        NumItemsRead++;
                    }
                }
                if (pageName != "ClassicModePage")
                {
                    PVIS.isVisible = Visibility.Collapsed;
                }

                IsTerminated = true;
            }
            catch (UnauthorizedAccessException)
            {
                DisplayConsentDialog();
            }
            catch (COMException e)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                MessageDialog driveGone = new MessageDialog(e.Message, "Drive Not Found");
                await driveGone.ShowAsync();
                rootFrame.Navigate(typeof(MainPage), new SuppressNavigationTransitionInfo());
            }
            stopwatch.Stop();
            Debug.WriteLine("Loading of: " + path + " completed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");
        }

        public static ProgressPercentage PROGRESSPER { get; } = new ProgressPercentage();

        public static int UpdateProgUI(int level)
        {
            // Isn't this just setting something? 
            // why return if it comes from outside?
            PROGRESSPER.prog = level;
            return level;
        }

        public static async void DisplayCollisionUIWithArgs(string header, string subHeader)
        {
            CollisionBoxHeader.Header = header;
            CollisionBoxSubHeader.SubHeader = subHeader;
            await GenericFileBrowser.collisionBox.ShowAsync();
            //CollisionUIVisibility.isVisible = Visibility.Visible;
        }

        public static async void DisplayReviewUIWithArgs(string header, string subHeader)
        {
            ConflictBoxHeader.Header = header;
            ConflictBoxSubHeader.SubHeader = subHeader;
            await GenericFileBrowser.reviewBox.ShowAsync();
            //ConflictUIVisibility.isVisible = Visibility.Visible;
        }

        public static async void FillTreeNode(object item, TreeView entireControl)
        {
            var pathToFillFrom = (item as Classic_ListedFolderItem)?.FilePath;
            StorageFolder folderFromPath = await StorageFolder.GetFolderFromPathAsync(pathToFillFrom);
            IReadOnlyList<StorageFolder> subFolderList = await folderFromPath.GetFoldersAsync();
            foreach(StorageFolder fol in subFolderList)
            {
                var name = fol.Name;
                var date = fol.DateCreated.LocalDateTime.ToString(CultureInfo.InvariantCulture);
                var ext = fol.DisplayType;
                var path = fol.Path;
                (item as Classic_ListedFolderItem)?.Children.Add(new Classic_ListedFolderItem { FileName = name, FilePath = path, FileDate = date, FileExtension = ext});

            }
        }
    }
}