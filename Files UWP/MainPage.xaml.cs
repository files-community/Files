using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Filesystem;
using Windows.System;

namespace Files
{

    public sealed partial class MainPage : Page
    {
        public static Microsoft.UI.Xaml.Controls.NavigationView nv;
        public static Frame accessibleContentFrame;
        public static AutoSuggestBox accessibleAutoSuggestBox;
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public static ContentDialog permissionBox;

        public MainPage()
        {
            this.InitializeComponent();
            accessibleContentFrame = ContentFrame;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(DragArea);
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(0, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            nv = navView;
            accessibleAutoSuggestBox = auto_suggest;
            PopulateNavViewWithExternalDrives();
            permissionBox = PermissionDialog;
            

            //make the minimize, maximize and close button visible in light theme
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
            }
            else if (App.Current.RequestedTheme == ApplicationTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }

            if (this.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
            }
            else if (this.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }
        }

        public async void PopulateNavViewWithExternalDrives()
        {   
            var knownRemDevices = new ObservableCollection<string>();
            foreach (var f in await KnownFolders.RemovableDevices.GetFoldersAsync())
            {
                var path = f.Path;
                knownRemDevices.Add(path);
            }

            var driveLetters = DriveInfo.GetDrives().Select(x => x.RootDirectory.Root).ToList();

            if (!driveLetters.Any()) return;

            driveLetters.ToList().ForEach(roots =>
            {
                try
                {
                    if (roots.Name == @"C:\") return;
                    var content = string.Empty;
                    SymbolIcon icon;
                    if (knownRemDevices.Contains(roots.Name))
                    {
                        content = $"Removable Drive ({roots.Name})";
                        icon = new SymbolIcon((Symbol)0xE88E);
                    }
                    else
                    {
                        content = $"Local Disk ({roots.Name})";
                        icon = new SymbolIcon((Symbol)0xEDA2);
                    }
                    nv.MenuItems.Add(new Microsoft.UI.Xaml.Controls.NavigationViewItem()
                    {
                        Content = content,
                        Icon = icon,
                        Tag = roots.Name
                    });
                }
                catch (UnauthorizedAccessException e)
                {
                    Debug.WriteLine(e.Message);
                }
            });
        }

        private static SelectItem select = new SelectItem();
        public static SelectItem Select { get { return MainPage.select; } }
        
        private void auto_suggest_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {

        }

        private void navView_Loaded(object sender, RoutedEventArgs e)
        {
            
            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in nv.MenuItems)
            {
                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "homeIc")
                {
                    Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ContentFrame.Navigate(typeof(YourHome));
            auto_suggest.IsEnabled = true;
            auto_suggest.PlaceholderText = "Search Recents";
        }

        private void NavView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {

            var item = args.InvokedItem;
            var itemContainer = args.InvokedItemContainer;
            //var item = Interaction.FindParent<NavigationViewItemBase>(args.InvokedItem as DependencyObject);
            if (args.IsSettingsInvoked == true)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(Settings));
            }
            else
            {
                if(App.ViewModel.tokenSource != null)
                {
                    App.ViewModel.tokenSource.Cancel();
                    App.ViewModel.FilesAndFolders.Clear();
                }
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                if (item.ToString() == "Home")
                {
                    ContentFrame.Navigate(typeof(YourHome));
                    auto_suggest.PlaceholderText = "Search Recents";
                }
                else if (item.ToString() == "Desktop")
                {
                    ContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                    auto_suggest.PlaceholderText = "Search Desktop";
                }
                else if (item.ToString() == "Documents")
                {
                    ContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                    auto_suggest.PlaceholderText = "Search Documents";
                }
                else if (item.ToString() == "Downloads")
                {
                    ContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                    auto_suggest.PlaceholderText = "Search Downloads";
                }
                else if (item.ToString() == "Pictures")
                {
                    ContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                    auto_suggest.PlaceholderText = "Search Pictures";
                }
                else if (item.ToString() == "Music")
                {
                    ContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                    auto_suggest.PlaceholderText = "Search Music";
                }
                else if (item.ToString() == "Videos")
                {
                    ContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                    auto_suggest.PlaceholderText = "Search Videos";
                }
                else if (item.ToString() == "Local Disk (C:\\)")
                {
                    ContentFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                    auto_suggest.PlaceholderText = "Search";
                }
                else if (item.ToString() == "OneDrive")
                {
                    ContentFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                    auto_suggest.PlaceholderText = "Search OneDrive";
                }
                else
                {
                    var tagOfInvokedItem = (nv.MenuItems[nv.MenuItems.IndexOf(itemContainer)] as Microsoft.UI.Xaml.Controls.NavigationViewItem).Tag;

                    if (StorageFolder.GetFolderFromPathAsync(tagOfInvokedItem.ToString()) != null)
                    {
                        ContentFrame.Navigate(typeof(GenericFileBrowser), tagOfInvokedItem);
                        auto_suggest.PlaceholderText = "Search " + tagOfInvokedItem;
                    } 
                }
            }
        }

        private async void PermissionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }
    }
    public class SelectItem : INotifyPropertyChanged
    {


        public Microsoft.UI.Xaml.Controls.NavigationViewItemBase _itemSelected;
        public Microsoft.UI.Xaml.Controls.NavigationViewItemBase itemSelected
        {
            get
            {
                return _itemSelected;
            }

            set
            {
                if (value != _itemSelected)
                {
                    _itemSelected = value;
                    NotifyPropertyChanged("itemSelected");
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