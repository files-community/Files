using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Files
{

    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock textBlock;
        static DataGrid data;


        public GenericFileBrowser()
        {
            this.InitializeComponent();

            string env = Environment.ExpandEnvironmentVariables("%userprofile%");
            
            this.IsTextScaleFactorEnabled = true;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(100, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            ProgressBox.Visibility = Visibility.Collapsed;
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            ItemViewModel.PVIS.isVisible = Visibility.Collapsed;
            data = AllView;
            RemoveHiddenColumns();

        }


        public static void RemoveHiddenColumns()
        {
            if (data.Columns.Count > 5)
            {   
                data.Columns[5].Visibility = Visibility.Collapsed;
                data.Columns[6].Visibility = Visibility.Collapsed;
                data.Columns[7].Visibility = Visibility.Collapsed;
                data.Columns[8].Visibility = Visibility.Collapsed;
                data.Columns[9].Visibility = Visibility.Collapsed;
                data.Columns[10].Visibility = Visibility.Collapsed;
                data.Columns[11].Visibility = Visibility.Collapsed;
            }
            else
            {
                Debug.WriteLine("Less than 4 columns in datagrid");
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = (string)eventArgs.Parameter;
            this.ViewModel = new ItemViewModel(parameters);
            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
                VisiblePath.Text = "Desktop";
            }else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                VisiblePath.Text = "Documents";
            }else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                VisiblePath.Text = "Downloads";
            }else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                VisiblePath.Text = "Pictures";
            }else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                VisiblePath.Text = "Music";
            }else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                VisiblePath.Text = "Videos";
            }
            else
            {
                VisiblePath.Text = parameters;
            }

        }

        public ItemViewModel ViewModel { get; set; }

        
        private async void AllView_ItemClick(object sender, SelectionChangedEventArgs e)
        {

            if (AllView.SelectedItems.Count > 0)
            {

                var index = AllView.SelectedIndex;
                var clickedOnItem = ViewModel.filesAndFolders[index];

                if (clickedOnItem.FileExtension == "Folder")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    History.FowardList.Clear();
                    ItemViewModel.FS.isEnabled = false;
                    ViewModel.filesAndFolders.Clear();
                    this.ViewModel = new ItemViewModel(clickedOnItem.FilePath);

                    //Debug.WriteLine(clickedOnItem.FilePath);



                    VisiblePath.Text = clickedOnItem.FilePath;
                    this.Bindings.Update();
                }
                else
                {
                    //Debug.WriteLine(clickedOnItem.FilePath);
                    
                    StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                    var options = new Windows.System.LauncherOptions();
                    options.DisplayApplicationPicker = true;
                    await Launcher.LaunchFileAsync(file, options);

                }

            }

        }


        private void Back_Click(object sender, RoutedEventArgs e)
        {

            if (History.HistoryList.Count() > 1)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                Debug.WriteLine("\nBefore Removals");
                ArrayDiag.DumpArray();
                History.AddToFowardList(History.HistoryList[History.HistoryList.Count() - 1]);
                History.HistoryList.RemoveAt(History.HistoryList.Count() - 1);
                Debug.WriteLine("\nAfter Removals");
                ArrayDiag.DumpArray();
                this.ViewModel = new ItemViewModel(History.HistoryList[History.HistoryList.Count() - 1]);     // To take into account the correct index without interference from the folder being navigated to
                ViewModel.filesAndFolders.Clear();
                VisiblePath.Text = History.HistoryList[History.HistoryList.Count() - 1];
                this.Bindings.Update();
                
                if(History.FowardList.Count == 0)
                {
                    ItemViewModel.FS.isEnabled = false;
                }
                else if(History.FowardList.Count > 0)
                {
                    ItemViewModel.FS.isEnabled = true;
                }
                

            }
            

        }

        private void Foward_Click(object sender, RoutedEventArgs e)
        {
            if(History.FowardList.Count() > 0)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                this.ViewModel = new ItemViewModel(History.FowardList[History.FowardList.Count() - 1]);     // To take into account the correct index without interference from the folder being navigated to
                ViewModel.filesAndFolders.Clear();
                VisiblePath.Text = History.FowardList[History.FowardList.Count() - 1];
                History.FowardList.RemoveAt(History.FowardList.Count() - 1);
                this.Bindings.Update();
                ArrayDiag.DumpFowardArray();

                if (History.FowardList.Count == 0)
                {
                    ItemViewModel.FS.isEnabled = false;
                }
                else if (History.FowardList.Count > 0)
                {
                    ItemViewModel.FS.isEnabled = true;
                }

            }
        }

        private void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid) sender;
            RightClickContextMenu.ShowAt(dataGrid, e.GetPosition(dataGrid));

        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ShareItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ScanItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {

        }

    }
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

        public ObservableCollection<ListedItem> filesAndFolders = new ObservableCollection<ListedItem>();
        public ObservableCollection<ListedItem> FilesAndFolders { get { return this.filesAndFolders; } }

        
        StorageFolder folder;
        string gotName;
        string gotDate;
        string gotType;
        string gotPath;
        string gottenPath;
        string gotFolName;
        string gotFolDate;
        string gotFolPath;
        string gotFolType;
        Visibility gotFileImgVis;
        Visibility gotFolImg;
        StorageItemThumbnail gotFileImg;
        public IReadOnlyList<StorageFolder> folderList;
        public IReadOnlyList<StorageFile> fileList;


        public static BackState bs = new BackState();
        public static BackState BS {
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

        public ItemViewModel(string ViewPath)
        {

            gottenPath = ViewPath;
            GetItemsAsync(ViewPath);

            History.AddToHistory(ViewPath);
            //ArrayDiag.DumpArray();
            
            
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
            // ArrayDiag.DumpArray();
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
            folderList = await folder.GetFoldersAsync();                        // Create a read-only list of all folders in location
            fileList = await folder.GetFilesAsync();                            // Create a read-only list of all files in location
            int NumOfFolders = folderList.Count;                                // How many folders are in the list
            int NumOfFiles = fileList.Count;                                    // How many files are in the list
            int NumOfItems = NumOfFiles + NumOfFolders;
            int NumItemsRead = 0;
            
            if (NumOfItems == 0)
            {
                GenericFileBrowser.RemoveHiddenColumns();
                TextState.isVisible = Visibility.Visible;
                return;
            }

            PUIH.Header = "Loading " + NumOfItems + " items";

            if(NumOfItems >= 75)
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
                this.filesAndFolders.Add(new ListedItem() { FileImg = null, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                    
                NumItemsRead++;
                GenericFileBrowser.RemoveHiddenColumns();
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
                    const uint requestedSize = 20;
                    const ThumbnailMode thumbnailMode = ThumbnailMode.ListView;
                    const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                    gotFileImg = await f.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                    BitmapImage icon = new BitmapImage();
                    if (gotFileImg != null)
                    {
                        icon.SetSource(gotFileImg.CloneStream());
                    }
                    gotFileImgVis = Visibility.Visible;
                    this.filesAndFolders.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath});
                    NumItemsRead++;
                    GenericFileBrowser.RemoveHiddenColumns();
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
             return (int) level;
        }

        

    }

    public class History
    {
        public static List<string> HistoryList = new List<string>();                // The list of paths previously navigated to
        public static void AddToHistory(string PathToBeAdded)
        {
            if(HistoryList.Count < 25)                                              // If HistoryList is currently less than 25 items and 
            {
                if(HistoryList.Count > 0)                                           // If there are items in HistoryList
                {
                    if(HistoryList[HistoryList.Count - 1] != PathToBeAdded)         // Make sure the item being added is not already added
                    {
                        HistoryList.Add(PathToBeAdded);
                    }
                }
                else                                                                // If there are no items in HistoryList
                {
                    HistoryList.Add(PathToBeAdded);
                }
                
            }
            else if( (HistoryList.Count >= 25) && (HistoryList[HistoryList.Count - 1] != PathToBeAdded) )     // If History list is exactly 25 items (or greater) and the item being added is not already added
            {
                for (int i = 0; i < (HistoryList.Count - 1); i++)
                {
                    HistoryList[i] = HistoryList[i + 1];                // Shift list contents left by one to delete first item, effectively making space for next item 
                }
                HistoryList[24] = PathToBeAdded;                        // Add new item in freed spot
            }
        }

        public static List<string> FowardList = new List<string>();
        public static void AddToFowardList(string PathToBeAdded)
        {
            if(FowardList.Count > 0)
            {
                if (FowardList[FowardList.Count - 1] != PathToBeAdded)
                {
                    FowardList.Add(PathToBeAdded);
                }
            }
            else
            {
                FowardList.Add(PathToBeAdded);
            }

        }
    }

    public class ArrayDiag
    {

        public static void DumpArray()
        {
            foreach (string s in History.HistoryList)
            {
                Debug.Write(s + ", ");
            }
            Debug.WriteLine(" ");
        }

        public static void DumpFowardArray()
        {
            foreach (string s in History.FowardList)
            {
                Debug.Write(s + ", ");
            }
            Debug.WriteLine(" ");
        }
    }

    public class BackState : INotifyPropertyChanged
    {
       

        public bool _isEnabled;
        public bool isEnabled
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    NotifyPropertyChanged("isEnabled");
                    Debug.WriteLine("NotifyPropertyChanged was called successfully");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }

    public class FowardState : INotifyPropertyChanged
    {


        public bool _isEnabled;
        public bool isEnabled
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    NotifyPropertyChanged("isEnabled");
                    Debug.WriteLine("NotifyPropertyChanged was called successfully");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }

    public class ProgressPercentage : INotifyPropertyChanged
    {
        public int _prog;
        public int prog
        {
            get
            {
                return _prog;
            }

            set
            {
                if (value != _prog)
                {
                    _prog = value;
                    NotifyPropertyChanged("prog");
                    //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

    public class ProgressUIVisibility : INotifyPropertyChanged
    {
        public Visibility _isVisible;
        public Visibility isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    NotifyPropertyChanged("isVisible");
                    //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI Visibility");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

    public class ProgressUIHeader : INotifyPropertyChanged
    {
        public string _header;
        public string Header
        {
            get
            {
                return _header;
            }

            set
            {
                if (value != _header)
                {
                    _header = value;
                    NotifyPropertyChanged("Header");
                    //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI Visibility");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

    public class ProgressUIPath : INotifyPropertyChanged
    {
        public string _path;
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                if (value != _path)
                {
                    _path = value;
                    NotifyPropertyChanged("Path");
                    //Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI Visibility");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

    public class EmptyFolderTextState : INotifyPropertyChanged
    {


        public Visibility _isVisible;
        public Visibility isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    NotifyPropertyChanged("isVisible");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }
}