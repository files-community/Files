using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Files
{

    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock textBlock;



        public GenericFileBrowser()
        {
            this.InitializeComponent();

            //pathToView = @"C:\";
            string env = Environment.ExpandEnvironmentVariables("%userprofile%");
            
            this.IsTextScaleFactorEnabled = true;
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 500));
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(100, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);

        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = (string)eventArgs.Parameter;
            this.ViewModel = new ItemViewModel(parameters);
            VisiblePath.Text = parameters;
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
                   
                    ViewModel.filesAndFolders.Clear();
                    this.ViewModel = new ItemViewModel(clickedOnItem.FilePath);

                    Debug.WriteLine(clickedOnItem.FilePath);



                    VisiblePath.Text = clickedOnItem.FilePath;
                    this.Bindings.Update();
                }
                else
                {
                    Debug.WriteLine(clickedOnItem.FilePath);
                    
                    StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                    var options = new Windows.System.LauncherOptions();
                    options.DisplayApplicationPicker = true;
                    await Launcher.LaunchFileAsync(file, options);

                }

            }

        }

        bool CancelledBefore = false;

        private void Back_Click(object sender, RoutedEventArgs e)
        {

            if (HistoryList.HistoryListArr.Count() > 1)
            {
                if (!CancelledBefore)
                {
                    //ItemViewModel.CancellationTokenSource.Cancel();
                    
                }
                Debug.WriteLine("\nBefore Removals");
                ArrayDiag.DumpArray();
                this.ViewModel = new ItemViewModel(HistoryList.HistoryListArr[HistoryList.HistoryListArr.Count() - 2]);
                HistoryList.HistoryListArr.RemoveAt(HistoryList.HistoryListArr.Count() - 1);
                Debug.WriteLine("\nAfter Removals");
                ArrayDiag.DumpArray();
                ViewModel.filesAndFolders.Clear();
                VisiblePath.Text = HistoryList.HistoryListArr[HistoryList.HistoryListArr.Count() - 1];
                this.Bindings.Update();
                if (!CancelledBefore)
                {
                    //ItemViewModel.CancellationTokenSource = new CancellationTokenSource();
                    CancelledBefore = true;
                }

            }
            else if (HistoryList.HistoryListArr.Count() == 1)
            {

            }

        }

        private void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView) sender;
            RightClickContextMenu.ShowAt(listView, e.GetPosition(listView));

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

        public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public static CancellationTokenSource CancellationTokenSource
        {
            get
            {
                return cancellationTokenSource;
            }
            set
            {

            }
        }

        public ItemViewModel(string ViewPath)
        {

            gottenPath = ViewPath;
            GetItemsAsync(ViewPath, CancellationTokenSource.Token);
            HistoryList.HistoryStore(ViewPath);
            
            
            
            if (HistoryList.HistoryListArr.Count == 1)
            {
                BS.isEnabled = false;
                Debug.WriteLine("Disabled Property");
                

            }
            else if (HistoryList.HistoryListArr.Count > 1)
            {
                BS.isEnabled = true;
                Debug.WriteLine("Enabled Property");
            }
            ArrayDiag.DumpArray();
        }

        private ListedItem li = new ListedItem();
        public ListedItem LI { get { return this.li; } }

        public async void GetItemsAsync(string path, CancellationToken ct)
        {
            

            folder = await StorageFolder.GetFolderFromPathAsync(path);          // Set location to the current directory specified in path
            folderList = await folder.GetFoldersAsync();                        // Create a read-only list of all folders in location
            IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync(); // Create a read-only list of all files in location
            int NumOfFolders = folderList.Count;                                // How many folders are in the list
            int NumOfFiles = fileList.Count;                                    // How many files are in the list
            int NumOfItems = NumOfFiles + NumOfFolders;
            int NumItemsRead = 0;
            if (NumOfItems == 0)
            {
                return;
            }
            PVIS.isVisible = Visibility.Visible;
            foreach (StorageFolder fol in folderList)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                else
                {  

                    int ProgressReported = (NumItemsRead * 100 / NumOfItems);
                    UpdateProgUI(ProgressReported);
                    gotFolName = fol.Name.ToString();
                    gotFolDate = fol.DateCreated.ToString();
                    gotFolPath = fol.Path.ToString();
                    gotFolType = "Folder";
                    gotFolImg = Visibility.Visible;
                    gotFileImgVis = Visibility.Collapsed;
                    this.filesAndFolders.Add(new ListedItem() { FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                    NumItemsRead++;
                }

            }

            foreach (StorageFile f in fileList)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                else
                {
                    int ProgressReported = (NumItemsRead * 100 / NumOfItems);
                    UpdateProgUI(ProgressReported);
                    gotName = f.Name.ToString();
                    gotDate = f.DateCreated.ToString(); // In the future, parse date to human readable format
                    gotType = f.FileType.ToString();
                    gotPath = f.Path.ToString();
                    gotFolImg = Visibility.Collapsed;
                    const uint requestedSize = 20;
                    const ThumbnailMode thumbnailMode = ThumbnailMode.PicturesView;
                    const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                    gotFileImg = await f.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                    BitmapImage icon = new BitmapImage();
                    if (gotFileImg != null)
                    {
                        icon.SetSource(gotFileImg.CloneStream());
                    }
                    gotFileImgVis = Visibility.Visible;
                    this.filesAndFolders.Add(new ListedItem() { FileImg = icon, FilePath = gotPath, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType });
                    NumItemsRead++;
                }
            }
            PVIS.isVisible = Visibility.Collapsed;

        }

        public static ProgressUI progressUI = new ProgressUI();
        public static ProgressUI PROGRESSUI
        {
            get
            {
                return progressUI;
            }
            set
            {

            }
        }

        public int UpdateProgUI(int level)
        {
             PROGRESSUI.prog = "Loading " + level + "% complete";
             //Debug.WriteLine("Status Updated For Folder Read Loop");
             return (int) level;
        }

        

    }

    public class HistoryList
    {
        public static List<string> HistoryListArr = new List<string>();
        public static int counter = 0;

        public static void HistoryStore(string ViewedPath)
        {
            if (counter <= 9 && !HistoryListArr.Contains(ViewedPath))
            {
                HistoryListArr.Add(ViewedPath);
                counter++;
            }
            else if (counter >= 10 && HistoryListArr.Count == 10)
            {
                for (int i = 0; i < (HistoryListArr.Count - 1); i++)
                {
                    HistoryListArr[i] = HistoryListArr[i + 1];
                }
                HistoryListArr[9] = ViewedPath;
                counter++;
            }

        }
    }

    public class ArrayDiag
    {

        public static void DumpArray()
        {
            foreach (string s in HistoryList.HistoryListArr)
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

    public class ProgressUI : INotifyPropertyChanged
    {
        public string _prog;
        public string prog
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
                    Debug.WriteLine("NotifyPropertyChanged was called successfully for ProgressUI Visibility");
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