using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Diagnostics;
using System.Drawing;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
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
            Locations.ItemLoader.itemsAdded.Clear();
            Locations.ItemLoader.DisplayItems();
            recentItemsCollection.Clear();
            PopulateRecentsList();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            ProHome.BackButton.IsEnabled = ProHome.accessibleContentFrame.CanGoBack;
            ProHome.ForwardButton.IsEnabled = ProHome.accessibleContentFrame.CanGoForward;
        }

        private void CardPressed(object sender, ItemClickEventArgs e)
        {
            string BelowCardText = ((Locations.LocationItem)e.ClickedItem).Text;
            if (BelowCardText == "Downloads")
            {
                
                ProHome.locationsList.SelectedIndex = 2;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
            }
            else if (BelowCardText == "Documents")
            {
                
                ProHome.locationsList.SelectedIndex = 3;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
            }
            else if (BelowCardText == "Pictures")
            {
                
                ProHome.locationsList.SelectedIndex = 4;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
            }
            else if (BelowCardText == "Music")
            {
                
                ProHome.locationsList.SelectedIndex = 5;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
            }
            else if (BelowCardText == "Videos")
            {
                
                ProHome.locationsList.SelectedIndex = 6;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
            }
        }

        private void DropShadowPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            (sender as DropShadowPanel).ShadowOpacity = 0.15;
        }

        private void DropShadowPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            (sender as DropShadowPanel).ShadowOpacity = 0.00;
        }

        private void Button_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton.Tag.ToString() == "\xE896") // Downloads
            {
                
                ProHome.locationsList.SelectedIndex = 2;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
            }
            else if (clickedButton.Tag.ToString() == "\xE8A5") // Documents
            {
                
                ProHome.locationsList.SelectedIndex = 3;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
            }
            else if (clickedButton.Tag.ToString() == "\xEB9F") // Pictures
            {
                
                ProHome.locationsList.SelectedIndex = 4;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
            }
            else if (clickedButton.Tag.ToString() == "\xEC4F") // Music
            {
                
                ProHome.locationsList.SelectedIndex = 5;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
            }
            else if (clickedButton.Tag.ToString() == "\xE8B2") // Videos
            {
                
                ProHome.locationsList.SelectedIndex = 6;
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
            }
        }
        public static StorageFile RecentsFile;
        public static StorageFolder dataFolder;
        public static ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();
        public static EmptyRecentsText Empty { get; set; } = new EmptyRecentsText();

        public async void PopulateRecentsList()
        {
            recentItemsCollection.Clear();
            dataFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            RecentsFile = await dataFolder.CreateFileAsync("recents.txt", CreationCollisionOption.OpenIfExists);
            BitmapImage ItemImage = new BitmapImage();
            string ItemPath = null;
            string ItemName;
            Visibility ItemFolderImgVis;
            Visibility ItemEmptyImgVis;
            Visibility ItemFileIconVis;
            IList<string> lines = new List<string>();
            lines = await FileIO.ReadLinesAsync(RecentsFile);
            if (lines.Count == 0)
            {
                Empty.Visibility = Visibility.Visible;
            }
            else if (lines.Count > 10)
            {
                try
                {
                    for (int LineNum = 0; LineNum < 10; LineNum++)
                    {
                        lines.RemoveAt(0);
                    }

                    await FileIO.WriteLinesAsync(RecentsFile, lines);
                    Empty.Visibility = Visibility.Collapsed;
                    lines = await FileIO.ReadLinesAsync(RecentsFile);
                    foreach (string s in lines)
                    {
                        try
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(s);
                            ItemName = item.DisplayName;
                            ItemPath = item.Path;
                            ItemFolderImgVis = Visibility.Visible;
                            ItemEmptyImgVis = Visibility.Collapsed;
                            ItemFileIconVis = Visibility.Collapsed;
                            if (!recentItemsCollection.Contains(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis }))
                            {
                                recentItemsCollection.Add(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
                            }

                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            IList<string> modifyLines = new List<string>();
                            modifyLines = lines;
                            modifyLines.Remove(s);
                            await FileIO.WriteLinesAsync(RecentsFile, modifyLines);
                            PopulateRecentsList();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Empty.Visibility = Visibility.Visible;
                        }
                        catch (System.ArgumentException)
                        {
                            var item = await StorageFile.GetFileFromPathAsync(s);
                            ItemName = item.DisplayName;
                            ItemPath = item.Path;
                            ItemImage = new BitmapImage();
                            var thumbnail = await item.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 30, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
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
                            if (!recentItemsCollection.Contains(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis }))
                            {
                                recentItemsCollection.Add(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
                            }
                        }
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    if (ItemPath != null)
                    {
                        RemoveDeletedItemFromList(ItemPath, lines);
                    }
                    else
                    {
                        Debug.WriteLine("Attempted to delete redundant RecentItem from file when ItemPath was never set.");
                    }
                }
            }
            else
            {
                try
                {
                    Empty.Visibility = Visibility.Collapsed;

                    foreach (string s in lines)
                    {
                        try
                        {
                            ItemPath = s;
                            var item = await StorageFolder.GetFolderFromPathAsync(s);
                            ItemName = item.DisplayName;
                            ItemPath = item.Path;
                            ItemFolderImgVis = Visibility.Visible;
                            ItemEmptyImgVis = Visibility.Collapsed;
                            ItemFileIconVis = Visibility.Collapsed;
                            if (!recentItemsCollection.Contains(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis }))
                            {
                                recentItemsCollection.Add(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
                            }

                        }
                        catch (UnauthorizedAccessException)
                        {
                            Empty.Visibility = Visibility.Visible;
                        }
                        catch (System.ArgumentException)
                        {
                            var item = await StorageFile.GetFileFromPathAsync(s);
                            ItemName = item.DisplayName;
                            ItemPath = item.Path;
                            ItemImage = new BitmapImage();
                            var thumbnail = await item.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 30, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
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
                            if (!recentItemsCollection.Contains(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis }))
                            {
                                recentItemsCollection.Add(new RecentItem() { path = ItemPath, name = ItemName, FolderImg = ItemFolderImgVis, EmptyImgVis = ItemEmptyImgVis, FileImg = ItemImage, FileIconVis = ItemFileIconVis });
                            }
                        }
                    }
                }
                catch (System.IO.FileNotFoundException)
                {
                    if(ItemPath != null)
                    {
                        RemoveDeletedItemFromList(ItemPath, lines);
                    }
                    else
                    {
                        Debug.WriteLine("Attempted to delete redundant RecentItem from file when ItemPath was never set.");
                    }
                }
                
            }
        }

        private async void RemoveDeletedItemFromList(string s, IList<string> lines)
        {
            IList<string> modifyLines = new List<string>();
            modifyLines = lines;
            modifyLines.Remove(s);
            await FileIO.WriteLinesAsync(RecentsFile, modifyLines);
            PopulateRecentsList();
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
            catch (System.ArgumentException)
            {
                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), path);
            }
        }

        private void RecentsView_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {

        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            recentItemsCollection.Clear();
            RecentsView.ItemsSource = null;
            await RecentsFile.DeleteAsync();
            ProHome.accessibleContentFrame.Navigate(typeof(YourHome), null, new Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());

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
