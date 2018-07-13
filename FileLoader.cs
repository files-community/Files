using Files;
using Navigation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using System;
using Windows.Storage.Search;
using System.Data.SqlClient;

namespace ItemListPresenter
{
    public class ListedItem
    {
        public Visibility FolderImg { get; set; }
        public Visibility FileIconVis { get; set; }
        public BitmapImage FileImg { get; set; }
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public ListedItem()
        {

        }
    }

    public class ItemViewModel
    {
        public ObservableCollection<ListedItem> folInfoList = new ObservableCollection<ListedItem>();
        public ObservableCollection<ListedItem> FolInfoList { get { return this.folInfoList; } }
        public ObservableCollection<ListedItem> fileInfoList = new ObservableCollection<ListedItem>();
        public ObservableCollection<ListedItem> FileInfoList { get { return this.fileInfoList; } }

        public static ObservableCollection<ListedItem> filesAndFolders = new ObservableCollection<ListedItem>();
        public static ObservableCollection<ListedItem> FilesAndFolders { get { return filesAndFolders; } }


        StorageFolder folder;
        string gotName;
        string gotDate;
        string gotType;
        string gotPath;
        string gotFolName;
        string gotFolDate;
        string gotFolPath;
        string gotFolType;
        Visibility gotFileImgVis;
        Visibility gotFolImg;
        StorageItemThumbnail gotFileImg;
        public IReadOnlyList<StorageFolder> folderList;
        public IReadOnlyList<StorageFile> fileList;
        public bool isPhotoAlbumMode;

        public static ItemViewModel vm;
        public static ItemViewModel ViewModel { get { return vm; } set { } }

        public static BackState bs = new BackState();
        public static BackState BS
        {
            get
            {
                return bs;
            }
            set
            {

            }
        }

        public static FowardState fs = new FowardState();
        public static FowardState FS
        {
            get
            {
                return fs;
            }
            set
            {

            }
        }

        public static ProgressUIVisibility pvis = new ProgressUIVisibility();
        public static ProgressUIVisibility PVIS
        {
            get
            {
                return pvis;
            }
            set
            {

            }
        }

        public ItemViewModel(string ViewPath, bool isInPhotoMode)
        {
            isPhotoAlbumMode = isInPhotoMode;
            GenericFileBrowser.P.path = ViewPath;
            FilesAndFolders.Clear();   
            GetItemsAsync(ViewPath);
            History.AddToHistory(ViewPath);



            if (History.HistoryList.Count == 1)
            {
                BS.isEnabled = false;
                Debug.WriteLine("Disabled Property");


            }
            else if (History.HistoryList.Count > 1)
            {
                BS.isEnabled = true;
                Debug.WriteLine("Enabled Property");
            }

        }

 

        private ListedItem li = new ListedItem();
        public ListedItem LI { get { return this.li; } }

        private static ProgressUIHeader pUIh = new ProgressUIHeader();
        public static ProgressUIHeader PUIH { get { return ItemViewModel.pUIh; } }

        private static ProgressUIPath pUIp = new ProgressUIPath();
        public static ProgressUIPath PUIP { get { return ItemViewModel.pUIp; } }

        private static EmptyFolderTextState textState = new EmptyFolderTextState();
        public static EmptyFolderTextState TextState { get { return ItemViewModel.textState; } }

        

        public async void GetItemsAsync(string path)
        {

            PUIP.Path = path;
            folder = await StorageFolder.GetFolderFromPathAsync(path);          // Set location to the current directory specified in path
            QueryOptions options = new QueryOptions()
            {
                FolderDepth = FolderDepth.Deep,
                IndexerOption = IndexerOption.UseIndexerWhenAvailable

            };
            string[] otherProperties = new string[]
            {
                SystemProperties.Title
            };
            
            options.SetPropertyPrefetch(PropertyPrefetchOptions.None, otherProperties);
            SortEntry sort = new SortEntry()
            {
                AscendingOrder = true,
                PropertyName = "System.FileName"
                
            };
            options.SortOrder.Add(sort);
            uint index = 0;

            const uint step = 100;
            StorageFileQueryResult fileQueryResult = folder.CreateFileQueryWithOptions(options);
            StorageFolderQueryResult folderQueryResult = folder.CreateFolderQueryWithOptions(options);
            folderList = await folderQueryResult.GetFoldersAsync(index, step);                          // Create a read-only list of all folders in location
            fileList = await fileQueryResult.GetFilesAsync(index, step);                                // Create a read-only list of all files in location
            int NumOfFolders = folderList.Count;                                                        // How many folders are in the list
            int NumOfFiles = fileList.Count;                                                            // How many files are in the list
            int NumOfItems = NumOfFiles + NumOfFolders;
            int NumItemsRead = 0;

            if (NumOfItems == 0)
            {
                TextState.isVisible = Visibility.Visible;
                return;
            }

            PUIH.Header = "Loading " + NumOfItems + " items";

            if (NumOfItems >= 250)
            {
                PVIS.isVisible = Visibility.Visible;
            }

            foreach (StorageFolder fol in folderList)
            {
                int ProgressReported = (NumItemsRead * 100 / NumOfItems);
                UpdateProgUI(ProgressReported);
                gotFolName = fol.Name.ToString();
                gotFolDate = fol.DateCreated.ToString();
                gotFolPath = fol.Path.ToString();
                gotFolType = "Folder";
                gotFolImg = Visibility.Visible;
                gotFileImgVis = Visibility.Collapsed;
                FilesAndFolders.Add(new ListedItem() { FileImg = null, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });

                NumItemsRead++;
            }
            foreach (StorageFile f in fileList)
            {
                int ProgressReported = (NumItemsRead * 100 / NumOfItems);
                UpdateProgUI(ProgressReported);
                gotName = f.Name.ToString();
                gotDate = f.DateCreated.ToString(); // In the future, parse date to human readable format
                gotType = f.FileType.ToString();
                gotPath = f.Path.ToString();
                gotFolImg = Visibility.Collapsed;
                if(isPhotoAlbumMode == false)
                {
                    const uint requestedSize = 20;
                    const ThumbnailMode thumbnailMode = ThumbnailMode.ListView;
                    const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                    gotFileImg = await f.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
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
                FilesAndFolders.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                NumItemsRead++;
            }
            if (NumOfItems >= 75)
            {
                PVIS.isVisible = Visibility.Collapsed;
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

        public int UpdateProgUI(int level)
        {
            PROGRESSPER.prog = level;
            //Debug.WriteLine("Status Updated For Folder Read Loop");
            return (int)level;
        }



    }
}
