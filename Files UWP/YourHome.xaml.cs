using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Filesystem;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using Files.Interacts;
using Windows.System;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using Windows.UI.Popups;
using Windows.UI.Text;
using System.Threading.Tasks;
using Windows.UI;
using System.Windows.Input;

namespace Files
{


    public sealed partial class YourHome : Page
    {
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        public YourHome()
        {
            InitializeComponent();

            // Overwrite paths for common locations if Custom Locations setting is enabled
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["customLocationsSetting"] != null)
            {
                if (localSettings.Values["customLocationsSetting"].Equals(true))
                {
                    DesktopPath = localSettings.Values["DesktopLocation"].ToString();
                    DownloadsPath = localSettings.Values["DownloadsLocation"].ToString();
                    DocumentsPath = localSettings.Values["DocumentsLocation"].ToString();
                    PicturesPath = localSettings.Values["PicturesLocation"].ToString();
                    MusicPath = localSettings.Values["MusicLocation"].ToString();
                    VideosPath = localSettings.Values["VideosLocation"].ToString();
                }
            }

              
        }

        public T GetCurrentSelectedTabInstance<T>()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            var selectedTabContent = ((InstanceTabsView.tabView.SelectedItem as Microsoft.UI.Xaml.Controls.TabViewItem).Content as Grid);
            foreach (UIElement uiElement in selectedTabContent.Children)
            {
                if (uiElement.GetType() == typeof(Frame))
                {
                    return (T)((uiElement as Frame).Content);
                }
            }
            return default;
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
            App.selectedTabInstance.BackButton.IsEnabled = App.selectedTabInstance.accessibleContentFrame.CanGoBack;
            App.selectedTabInstance.ForwardButton.IsEnabled = App.selectedTabInstance.accessibleContentFrame.CanGoForward;
            App.selectedTabInstance.RefreshButton.IsEnabled = false;
            App.selectedTabInstance.accessiblePasteButton.IsEnabled = false;
            App.selectedTabInstance.AlwaysPresentCommands.isEnabled = false;
            App.selectedTabInstance.LayoutItems.isEnabled = false;

            // Clear the path UI and replace with Favorites
            App.selectedTabInstance.pathBoxItems.Clear();
            //Style tabStyleFixed = App.selectedTabInstance.accessiblePathTabView.Resources["PathSectionTabStyle"] as Style;

            string componentLabel = parameters;
            string tag = parameters;
            PathBoxItem item = new PathBoxItem()
            {
                Title = componentLabel,
                Path = tag,
            };
            App.selectedTabInstance.pathBoxItems.Add(item);


            //SetPageContentVisibility(parameters);
            
        }

        private void SetPageContentVisibility(string parameters)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (parameters == "Start")
            {
                App.selectedTabInstance.PathText.Text = "Start";
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
                App.selectedTabInstance.PathText.Text = "New tab";
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
                
                App.selectedTabInstance.locationsList.SelectedIndex = 2;
                //instanceViewModel.TextState.isVisible = Visibility.Collapsed;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Documents")
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 3;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Pictures")
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 4;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;

            }
            else if (BelowCardText == "Music")
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 5;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Videos")
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 6;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
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
                
                App.selectedTabInstance.locationsList.SelectedIndex = 2;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xE8A5") // Documents
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 3;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xEB9F") // Pictures
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 4;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xEC4F") // Music
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 5;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xE8B2") // Videos
            {
                
                App.selectedTabInstance.locationsList.SelectedIndex = 6;
                App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                App.selectedTabInstance.LayoutItems.isEnabled = true;
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
                        ItemFolderImgVis = Visibility.Visible;
                        ItemEmptyImgVis = Visibility.Collapsed;
                        ItemFileIconVis = Visibility.Collapsed;
                        recentItemsCollection.Add(new RecentItem() { name = ItemName, path = ItemPath, EmptyImgVis = ItemEmptyImgVis, FolderImg = ItemFolderImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
                    }
                    else if (item.IsOfType(StorageItemTypes.File))
                    {
                        ItemName = item.Name;
                        ItemPath = item.Path;
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
                        recentItemsCollection.Add(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
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
                if (file.FileType == "Application")
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
                await App.selectedTabInstance.consentDialog.ShowAsync();
            }
            catch (System.ArgumentException)
            {
                if (new DirectoryInfo(path).Root.ToString().Contains(@"C:\"))
                {
                    App.selectedTabInstance.drivesList.SelectedIndex = 0;
                    App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), path);
                }
                else
                {
                    foreach(ListViewItem drive in App.selectedTabInstance.drivesList.Items)
                    {
                        if (drive.Tag.ToString() == new DirectoryInfo(path).Root.ToString())
                        {
                            App.selectedTabInstance.drivesList.SelectedItem = null;
                            drive.IsSelected = true;
                            App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), path);
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
        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
