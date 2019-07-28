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
        public ItemViewModel<YourHome> instanceViewModel;
        public Interaction<YourHome> instanceInteraction;

        public YourHome()
        {
            InitializeComponent();
            instanceViewModel = new ItemViewModel<YourHome>();
            instanceInteraction = new Interaction<YourHome>();
            GetCurrentSelectedTabInstance<ProHome>().PathText.Text = "Favorites";

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
            var selectedTabContent = ((instanceTabsView.tabView.SelectedItem as TabViewItem).Content as Grid);
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
            Locations.ItemLoader.itemsAdded.Clear();
            Locations.ItemLoader.DisplayItems();
            recentItemsCollection.Clear();
            PopulateRecentsList();
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.SetSelectedTabHeader("Favorites");
            GetCurrentSelectedTabInstance<ProHome>().BackButton.IsEnabled = GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.CanGoBack;
            GetCurrentSelectedTabInstance<ProHome>().ForwardButton.IsEnabled = GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.CanGoForward;
            GetCurrentSelectedTabInstance<ProHome>().RefreshButton.IsEnabled = false;
            GetCurrentSelectedTabInstance<ProHome>().accessiblePasteButton.IsEnabled = false;
            GetCurrentSelectedTabInstance<ProHome>().AlwaysPresentCommands.isEnabled = false;
            GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = false;

            // Clear the path UI and replace with Favorites
            GetCurrentSelectedTabInstance<ProHome>().accessiblePathTabView.Items.Clear();
            Style tabStyleFixed = GetCurrentSelectedTabInstance<ProHome>().accessiblePathTabView.Resources["PathSectionTabStyle"] as Style;
            FontWeight weight = new FontWeight()
            {
                Weight = FontWeights.SemiBold.Weight
            };
            string componentLabel = "Favorites";
            string tag = "Favorites";
            Microsoft.UI.Xaml.Controls.TabViewItem item = new Microsoft.UI.Xaml.Controls.TabViewItem()
            {
                Header = componentLabel + " ›",
                Tag = tag,
                CornerRadius = new CornerRadius(0),
                Style = tabStyleFixed,
                FontWeight = weight,
                FontSize = 14
            };
            GetCurrentSelectedTabInstance<ProHome>().accessiblePathTabView.Items.Add(item);

        }

        private void CardPressed(object sender, ItemClickEventArgs e)
        {
            string BelowCardText = ((Locations.LocationItem)e.ClickedItem).Text;
            if (BelowCardText == "Downloads")
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 2;
                //instanceViewModel.TextState.isVisible = Visibility.Collapsed;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Documents")
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 3;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Pictures")
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 4;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;

            }
            else if (BelowCardText == "Music")
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 5;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
            }
            else if (BelowCardText == "Videos")
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 6;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
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
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 2;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xE8A5") // Documents
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 3;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xEB9F") // Pictures
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 4;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xEC4F") // Music
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 5;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
            }
            else if (clickedButton.Tag.ToString() == "\xE8B2") // Videos
            {
                
                GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 6;
                GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                GetCurrentSelectedTabInstance<ProHome>().LayoutItems.isEnabled = true;
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
                    await instanceInteraction.LaunchExe(path);

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
                await GetCurrentSelectedTabInstance<ProHome>().consentDialog.ShowAsync();
            }
            catch (System.ArgumentException)
            {
                if (new DirectoryInfo(path).Root.ToString().Contains(@"C:\"))
                {
                    GetCurrentSelectedTabInstance<ProHome>().drivesList.SelectedIndex = 0;
                    GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), path);
                }
                else
                {
                    foreach(ListViewItem drive in GetCurrentSelectedTabInstance<ProHome>().drivesList.Items)
                    {
                        if (drive.Tag.ToString() == new DirectoryInfo(path).Root.ToString())
                        {
                            GetCurrentSelectedTabInstance<ProHome>().drivesList.SelectedItem = null;
                            drive.IsSelected = true;
                            GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Navigate(typeof(GenericFileBrowser), path);
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

        private void RecentsView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {

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
                    if (await ShowDialog("Remove File or Folder from recents", "Do you wish to remove " + vm.name + " from recent list?", "Yes", "No"))
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
        /// Note that the Secondarytext can be un-assigned, then the econdary button won't be presented.
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

                    Windows.UI.Xaml.Media.AcrylicBrush myBrush = new Windows.UI.Xaml.Media.AcrylicBrush();
                    myBrush.BackgroundSource = Windows.UI.Xaml.Media.AcrylicBackgroundSource.Backdrop;
                    myBrush.TintColor = Colors.Transparent;
                    myBrush.FallbackColor = Colors.Gray;
                    myBrush.TintOpacity = 0.6;
                    d.Background = myBrush;

                    if (!string.IsNullOrEmpty(secondaryText))
                    {
                        d.SecondaryButtonText = secondaryText;
                    }
                    var dr = await d.ShowAsync();

                    result = (dr == ContentDialogResult.Primary);
                }
            }
            catch (Exception ex)
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
