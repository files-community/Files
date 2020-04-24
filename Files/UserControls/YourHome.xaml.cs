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
using Files.View_Models;
using Files.Controls;
using Files.Views.Pages;

namespace Files
{
    public sealed partial class YourHome : Page
    {
        public YourHome()
        {
            InitializeComponent();
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = sender as MenuFlyoutItem;
            var clickedOnItem = flyoutItem.DataContext as RecentItem;
            if (clickedOnItem.IsFile)
            {
                var filePath = clickedOnItem.RecentPath;
                var folderPath = filePath.Substring(0, filePath.Length - clickedOnItem.Name.Length);
                App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), folderPath);
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
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;
            App.PS.IsEnabled = false;
            //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.AlwaysPresentCommands.isEnabled = false;
            //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = false;
            App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
            App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;
            App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;
            // Clear the path UI and replace with Favorites
            App.CurrentInstance.NavigationToolbar.PathComponents.Clear();

            string componentLabel = parameters;
            string tag = parameters;
            PathBoxItem item = new PathBoxItem()
            {
                Title = componentLabel,
                Path = tag,
            };
            App.CurrentInstance.NavigationToolbar.PathComponents.Add(item);

            //SetPageContentVisibility(parameters);
        }

        public bool FavoritesCardsVis { get; set; } = true;
        public bool RecentsListVis { get; set; } = true;
        public bool DrivesListVis { get; set; } = false;

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
            string NavigationPath = ""; // path to navigate
            string ClickedCard = (sender as Button).Tag.ToString();

            switch (ClickedCard)
            {
                case "Downloads":
                    NavigationPath = App.AppSettings.DownloadsPath;
                    break;

                case "Documents":
                    NavigationPath = App.AppSettings.DocumentsPath;
                    break;

                case "Pictures":
                    NavigationPath = App.AppSettings.PicturesPath;
                    break;

                case "Music":
                    NavigationPath = App.AppSettings.MusicPath;
                    break;

                case ("Videos"):
                    NavigationPath = App.AppSettings.VideosPath;
                    break;
            }

            switch (App.AppSettings.LayoutMode)
            {
                case 0:
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), NavigationPath); // List View
                    break;

                case 1:
                    App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), NavigationPath); // Grid View
                    break;
            }

            App.InteractionViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
        }

        public static StorageFile RecentsFile;
        public static StorageFolder dataFolder;
        public static ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();
        public static EmptyRecentsText Empty { get; set; } = new EmptyRecentsText();

        public async void PopulateRecentsList()
        {
            var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
            BitmapImage ItemImage;
            string ItemPath;
            string ItemName;
            StorageItemTypes ItemType;
            Visibility ItemFolderImgVis;
            Visibility ItemEmptyImgVis;
            Visibility ItemFileIconVis;
            bool IsRecentsListEmpty = true;
            foreach (var entry in mostRecentlyUsed.Entries)
            {
                try
                {
                    var item = await mostRecentlyUsed.GetItemAsync(entry.Token);
                    if (item.IsOfType(StorageItemTypes.File))
                    {
                        IsRecentsListEmpty = false;
                    }
                }
                catch (Exception) { }
            }

            if (IsRecentsListEmpty)
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
                    if (item.IsOfType(StorageItemTypes.File))
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
                        recentItemsCollection.Add(new RecentItem() { RecentPath = ItemPath, Name = ItemName, Type = ItemType, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
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

            if (recentItemsCollection.Count == 0)
            {
                Empty.Visibility = Visibility.Visible;
            }
        }

        private async void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var path = (e.ClickedItem as RecentItem).RecentPath;
            try
            {
                await Interaction.InvokeWin32Component(path);
            }
            catch (UnauthorizedAccessException)
            {
                await App.ConsentDialogDisplay.ShowAsync();
            }
            catch (System.ArgumentException)
            {
                if (new DirectoryInfo(path).Root.ToString().Contains(@"C:\"))
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), path);
                }
                else
                {
                    foreach (DriveItem drive in App.AppSettings.DrivesManager.Drives)
                    {
                        if (drive.Tag.ToString() == new DirectoryInfo(path).Root.ToString())
                        {
                            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), path);
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

        private async void RemoveOneFrequentItem(object sender, RoutedEventArgs e)
        {
            //Get the sender frameworkelement

            if (sender is MenuFlyoutItem fe)
            {
                //Grab it's datacontext ViewModel and remove it from the list.

                if (fe.DataContext is RecentItem vm)
                {
                    if (await ShowDialog("Remove item from Recents List", "Do you wish to remove " + vm.Name + " from the list?", "Yes", "No"))
                    {
                        //remove it from the visible collection
                        recentItemsCollection.Remove(vm);

                        //Now clear it also from the recent list cache permanently.
                        //No token stored in the viewmodel, so need to find it the old fashioned way.
                        var mru = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                        foreach (var element in mru.Entries)
                        {
                            var f = await mru.GetItemAsync(element.Token);
                            if (f.Path.Equals(vm.RecentPath))
                            {
                                mru.Remove(element.Token);
                                if (mru.Entries.Count == 0)
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
                if (Window.Current.Content is Frame rootFrame)
                {
                    var d = new ContentDialog
                    {
                        Title = title,
                        Content = message,
                        PrimaryButtonText = primaryText
                    };

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
        public string RecentPath { get; set; }
        public string Name { get; set; }
        public bool IsFile { get => Type == StorageItemTypes.File; }
        public StorageItemTypes Type { get; set; }
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