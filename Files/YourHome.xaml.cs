using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using Files.Interacts;
using Windows.System;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;
using Windows.UI.Popups;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Linq;
using Files.Filesystem;

namespace Files
{


    public sealed partial class YourHome : Page
    {

        public YourHome()
        {
            InitializeComponent();

            // Overwrite paths for common locations if Custom Locations setting is enabled
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["customLocationsSetting"] != null)
            {
                if (localSettings.Values["customLocationsSetting"].Equals(true))
                {
                    App.DesktopPath = localSettings.Values["DesktopLocation"].ToString();
                    App.DownloadsPath = localSettings.Values["DownloadsLocation"].ToString();
                    App.DocumentsPath = localSettings.Values["DocumentsLocation"].ToString();
                    App.PicturesPath = localSettings.Values["PicturesLocation"].ToString();
                    App.MusicPath = localSettings.Values["MusicLocation"].ToString();
                    App.VideosPath = localSettings.Values["VideosLocation"].ToString();
                    App.OneDrivePath = localSettings.Values["OneDriveLocation"].ToString();
                }
            }
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {           
            var flyoutItem = sender as MenuFlyoutItem;
            var clickedOnItem = flyoutItem.DataContext as RecentItem;
            if (clickedOnItem.isFile)
            {
                var filePath = clickedOnItem.path;
                var folderPath = filePath.Substring(0, filePath.Length - clickedOnItem.name.Length);
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), folderPath);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter.ToString();
            Locations.ItemLoader.itemsAdded.Clear();
            Locations.ItemLoader.DisplayItems();
            recentItemsCollection.Clear();
            PopulateRecentsList();
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabInfo(parameters, null);
            instanceTabsView.TabStrip_SelectionChanged(null, null);
            App.OccupiedInstance.RibbonArea.Refresh.IsEnabled = false;
            App.PS.isEnabled = false;
            App.OccupiedInstance.AlwaysPresentCommands.isEnabled = false;
            App.OccupiedInstance.LayoutItems.isEnabled = false;
            App.OccupiedInstance.RibbonArea.Up.IsEnabled = false;
            // Clear the path UI and replace with Favorites
            App.OccupiedInstance.pathBoxItems.Clear();
            //Style tabStyleFixed = App.selectedTabInstance.accessiblePathTabView.Resources["PathSectionTabStyle"] as Style;

            string componentLabel = parameters;
            string tag = parameters;
            PathBoxItem item = new PathBoxItem()
            {
                Title = componentLabel,
                Path = tag,
            };
            App.OccupiedInstance.pathBoxItems.Add(item);


            //SetPageContentVisibility(parameters);
            
        }

        private void SetPageContentVisibility(string parameters)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (parameters == "Start")
            {
                App.OccupiedInstance.PathText.Text = "Start";
                if (localSettings.Values["FavoritesDisplayed_Start"] != null || localSettings.Values["RecentsDisplayed_Start"] != null || localSettings.Values["DrivesDisplayed_Start"] != null)
                {
                    switch ((bool)localSettings.Values["FavoritesDisplayed_Start"])
                    {
                        case true:
                            favoritesCardsVis = true;
                            break;
                        case false:
                            favoritesCardsVis = false;
                            break;
                    }

                    switch ((bool)localSettings.Values["RecentsDisplayed_Start"])
                    {
                        case true:
                            recentsListVis = true;
                            break;
                        case false:
                            recentsListVis = false;
                            break;
                    }

                    switch ((bool)localSettings.Values["DrivesDisplayed_Start"])
                    {
                        case true:
                            drivesListVis = true;
                            break;
                        case false:
                            drivesListVis = false;
                            break;
                    }
                }
            }
            else if (parameters == "New tab")
            {
                App.OccupiedInstance.PathText.Text = "New tab";
                if (localSettings.Values["FavoritesDisplayed_NewTab"] != null || localSettings.Values["RecentsDisplayed_NewTab"] != null || localSettings.Values["DrivesDisplayed_NewTab"] != null)
                {
                    switch ((bool)localSettings.Values["FavoritesDisplayed_NewTab"])
                    {
                        case true:
                            favoritesCardsVis = true;
                            break;
                        case false:
                            favoritesCardsVis = false;
                            break;
                    }

                    switch ((bool)localSettings.Values["RecentsDisplayed_NewTab"])
                    {
                        case true:
                            recentsListVis = true;
                            break;
                        case false:
                            recentsListVis = false;
                            break;
                    }

                    switch ((bool)localSettings.Values["DrivesDisplayed_NewTab"])
                    {
                        case true:
                            drivesListVis = true;
                            break;
                        case false:
                            drivesListVis = false;
                            break;
                    }
                }
            }
        }

        public bool favoritesCardsVis { get; set; } = true;
        public bool recentsListVis { get; set; } = true;
        public bool drivesListVis { get; set; } = false;

        private void CardPressed(object sender, ItemClickEventArgs e)
        {
            string BelowCardText = ((Locations.LocationItem)e.ClickedItem).Text;
            if (BelowCardText == "Downloads")
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DownloadsPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Documents")
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DocumentsPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Pictures")
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.PicturesPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.PicturesPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Music")
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.MusicPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.MusicPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Videos")
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.VideosPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.VideosPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
        }

        private void DropShadowPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            (sender as DropShadowPanel).ShadowOpacity = 0.25;
        }

        private void DropShadowPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            (sender as DropShadowPanel).ShadowOpacity = 0.05;
        }

        private void Button_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton.Tag.ToString() == "\xE896") // Downloads
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DownloadsPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xE8A5") // Documents
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DocumentsPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xEB9F") // Pictures
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.PicturesPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.PicturesPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xEC4F") // Music
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.MusicPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.MusicPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xE8B2") // Videos
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.VideosPath, StringComparison.OrdinalIgnoreCase));
                App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.VideosPath);
                App.OccupiedInstance.LayoutItems.isEnabled = true;
            }
        }
        public static StorageFile RecentsFile;
        public static StorageFolder dataFolder;
        public static ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();
        public static EmptyRecentsText Empty { get; set; } = new EmptyRecentsText();

        public async void PopulateRecentsList()
        {
            var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
            BitmapImage ItemImage = new BitmapImage();
            string ItemPath = null;
            string ItemName;
            StorageItemTypes ItemType;
            Visibility ItemFolderImgVis;
            Visibility ItemEmptyImgVis;
            Visibility ItemFileIconVis;
            if (mostRecentlyUsed.Entries.Count == 0)
            {
                Empty.Visibility = Visibility.Visible;
            }
            else
            {
                Empty.Visibility = Visibility.Collapsed;
            }
            foreach (Windows.Storage.AccessCache.AccessListEntry entry in mostRecentlyUsed.Entries)
            {
                string mruToken = entry.Token;
                try
                {
                    Windows.Storage.IStorageItem item = await mostRecentlyUsed.GetItemAsync(mruToken);
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        ItemName = item.Name;
                        ItemPath = item.Path;
                        ItemType = StorageItemTypes.Folder;
                        ItemFolderImgVis = Visibility.Visible;
                        ItemEmptyImgVis = Visibility.Collapsed;
                        ItemFileIconVis = Visibility.Collapsed;
                        recentItemsCollection.Add(new RecentItem() { name = ItemName, path = ItemPath, type = ItemType, EmptyImgVis = ItemEmptyImgVis, FolderImg = ItemFolderImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
                    }
                    else if (item.IsOfType(StorageItemTypes.File))
                    {
                        ItemName = item.Name;
                        ItemPath = item.Path;
                        ItemType = StorageItemTypes.File;
                        ItemImage = new BitmapImage();
                        StorageFile file = await StorageFile.GetFileFromPathAsync(ItemPath);
                        var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 30, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
                        if (thumbnail == null)
                        {
                            ItemEmptyImgVis = Visibility.Visible;
                        }
                        else
                        {
                            await ItemImage.SetSourceAsync(thumbnail.CloneStream());
                            ItemEmptyImgVis = Visibility.Collapsed;
                        }
                        ItemFolderImgVis = Visibility.Collapsed;
                        ItemFileIconVis = Visibility.Visible;
                        recentItemsCollection.Add(new RecentItem() { path = ItemPath, name = ItemName, type = ItemType, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    mostRecentlyUsed.Remove(mruToken);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip item until consent is provided
                }
            }
        }

        private async void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var path = (e.ClickedItem as RecentItem).path;
            try
            {
                var file = (await StorageFile.GetFileFromPathAsync(path));
                if (file.DisplayType == "Application")
                {
                    await Interaction.LaunchExe(path);

                }
                else
                {
                    var options = new LauncherOptions
                    {
                        DisplayApplicationPicker = false
                    };
                    await Launcher.LaunchFileAsync(file, options);
                }
            }
            catch (UnauthorizedAccessException)
            {
                await App.consentDialog.ShowAsync();
            }
            catch (System.ArgumentException)
            {
                if (new DirectoryInfo(path).Root.ToString().Contains(@"C:\"))
                {
                    App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem) == App.sideBarItems[0]);
                    App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), path);
                }
                else
                {
                    foreach(DriveItem drive in App.foundDrives)
                    {
                        if (drive.tag.ToString() == new DirectoryInfo(path).Root.ToString())
                        {
                            App.OccupiedInstance.DrivesList.SelectedItem = drive;
                            App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), path);
                            return;
                        }
                    }
                }
            }
            catch (COMException)
            {
                MessageDialog dialog = new MessageDialog("Please insert the necessary drive to access this item.", "Drive Unplugged");
                await dialog.ShowAsync();
            }
        }

        private async void mfi_RemoveOneItem_Click(object sender, RoutedEventArgs e)
        {
            //Get the sender frameworkelement
            var fe = sender as MenuFlyoutItem;

            if (fe != null)
            {
                //Grab it's datacontext ViewModel and remove it from the list.
                var vm = fe.DataContext as RecentItem;
            
                if (vm != null)
                {
                    if (await ShowDialog("Remove item from Recents List", "Do you wish to remove " + vm.name + " from the list?", "Yes", "No"))
                    {
                        //remove it from the visible collection
                        recentItemsCollection.Remove(vm);

                        //Now clear it also from the recent list cache permanently.  
                        //No token stored in the viewmodel, so need to find it the old fashioned way.
                        var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                        foreach (var element in mru.Entries)
                        {
                            var f = await mru.GetItemAsync(element.Token);
                            if (f.Path.Equals(vm.path))
                            {
                                mru.Remove(element.Token);
                                if(mru.Entries.Count == 0)
                                {
                                    Empty.Visibility = Visibility.Visible;
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Standard dialog, to keep consistency.
        /// Long term, better to put this into base viewmodel class, along with MVVM stuff (NotifyProperyCHanged, etc) and inherrit it.
        /// Note that the Secondarytext can be un-assigned, then the secondary button won't be presented.
        /// Result is true if the user presses primary text button
        /// </summary>
        /// <param name="title">
        /// The title of the message dialog
        /// </param>
        /// <param name="message">
        /// THe main body message displayed within the dialog
        /// </param>
        /// <param name="primaryText">
        /// Text to be displayed on the primary button (which returns true when pressed).
        /// If not set, defaults to 'OK'
        /// </param>
        /// <param name="secondaryText">
        /// The (optional) secondary button text.
        /// If not set, it won't be presented to the user at all.
        /// </param>
        public async Task<bool> ShowDialog(string title, string message, string primaryText = "OK", string secondaryText = null)
        {
            bool result = false;

            try
            {
                var rootFrame = Window.Current.Content as Frame;

                if (rootFrame != null)
                {
                    var d = new ContentDialog();

                    d.Title = title;
                    d.Content = message;
                    d.PrimaryButtonText = primaryText;

                    if (!string.IsNullOrEmpty(secondaryText))
                    {
                        d.SecondaryButtonText = secondaryText;
                    }
                    var dr = await d.ShowAsync();

                    result = (dr == ContentDialogResult.Primary);
                }
            }
            catch (Exception)
            {

            }
            finally
            {

            }

            return result;

        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            recentItemsCollection.Clear();
            RecentsView.ItemsSource = null;
            var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
            mru.Clear();
            Empty.Visibility = Visibility.Visible;
        }

        private void DropShadowPanel_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            (sender as DropShadowPanel).ShadowOpacity = 0.025;
        }

        private void DropShadowPanel_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            (sender as DropShadowPanel).ShadowOpacity = 0.25;
        }

        private void RecentsView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
           

        }
    }

    public class RecentItem
    {
        public BitmapImage FileImg { get; set; }
        public string path { get; set; }
        public string name { get; set; }
        public bool isFile { get => type == StorageItemTypes.File; }
        public StorageItemTypes type { get; set; }
        public Visibility FolderImg { get; set; }
        public Visibility EmptyImgVis { get; set; }
        public Visibility FileIconVis { get; set; }
    }

    public class EmptyRecentsText : INotifyPropertyChanged
    {
        private Visibility visibility;
        public Visibility Visibility
        {
            get
            {
                return visibility;
            }
            set
            {
                if (value != visibility)
                {
                    visibility = value;
                    NotifyPropertyChanged("Visibility");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
