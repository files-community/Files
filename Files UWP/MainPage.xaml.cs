using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Files.Filesystem;
using Windows.System;

namespace Files
{

    public sealed partial class MainPage : Page
    {
        public static Microsoft.UI.Xaml.Controls.NavigationView nv;
        public static Frame accessibleContentFrame;
        public static AutoSuggestBox accessibleAutoSuggestBox;
        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
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

        private void navView_ItemSelected(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem item = args.SelectedItem as Microsoft.UI.Xaml.Controls.NavigationViewItem;


            if (item.Content.Equals("Settings"))
            {
                //ContentFrame.Navigate(typeof(Settings));
            }
        }

        
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
                if(ItemViewModel.tokenSource != null)
                {
                    ItemViewModel.tokenSource.Cancel();
                    ItemViewModel.FilesAndFolders.Clear();
                }
                
                if (item.ToString() == "Home")
                {
                    ContentFrame.Navigate(typeof(YourHome));
                    auto_suggest.PlaceholderText = "Search Recents";
                }
                else if (item.ToString() == "Desktop")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                    auto_suggest.PlaceholderText = "Search Desktop";
                }
                else if (item.ToString() == "Documents")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                    auto_suggest.PlaceholderText = "Search Documents";
                }
                else if (item.ToString() == "Downloads")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                    auto_suggest.PlaceholderText = "Search Downloads";
                }
                else if (item.ToString() == "Pictures")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                    auto_suggest.PlaceholderText = "Search Pictures";
                }
                else if (item.ToString() == "Music")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                    auto_suggest.PlaceholderText = "Search Music";
                }
                else if (item.ToString() == "Videos")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                    auto_suggest.PlaceholderText = "Search Videos";
                }
                else if (item.ToString() == "Local Disk (C:\\)")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                    auto_suggest.PlaceholderText = "Search";
                }
                else if (item.ToString() == "OneDrive")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    ContentFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                    auto_suggest.PlaceholderText = "Search OneDrive";
                }
                else
                {
                    var tagOfInvokedItem = (nv.MenuItems[nv.MenuItems.IndexOf(itemContainer)] as Microsoft.UI.Xaml.Controls.NavigationViewItem).Tag;

                    if (StorageFolder.GetFolderFromPathAsync(tagOfInvokedItem.ToString()) != null)
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        ContentFrame.Navigate(typeof(GenericFileBrowser), tagOfInvokedItem);
                        auto_suggest.PlaceholderText = "Search " + tagOfInvokedItem;
                    }
                    else
                    {
                        
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
                    Debug.WriteLine("NotifyPropertyChanged was called successfully for NavView Selection");
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