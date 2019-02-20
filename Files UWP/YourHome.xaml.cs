using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Diagnostics;
using System.Drawing;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Filesystem;

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
            SizeChanged += YourHome_SizeChanged;
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(Convert.ToInt32(bounds.Width * scaleFactor), Convert.ToInt32(bounds.Height * scaleFactor));
            // If width is between 1 - 800
            if (bounds.Width >= 1 && bounds.Width <= 800)
            {

            }
            else if (bounds.Width > 800 && bounds.Width <= 1024)
            {

            }
        }

        private void YourHome_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(Convert.ToInt32(bounds.Width * scaleFactor), Convert.ToInt32(bounds.Height * scaleFactor));
            // If width is between 1 - 800
            if (bounds.Width >= 1 && bounds.Width <= 800)
            {

            }
            else if (bounds.Width > 800 && bounds.Width <= 1024)
            {

            }
        }

        private void CardPressed(object sender, ItemClickEventArgs e)
        {
            Debug.WriteLine(e.ClickedItem.GetType().ToString());
            string BelowCardText = ((Locations.LocationItem)e.ClickedItem).Text;
            Debug.WriteLine("Pressed Card Text: " + BelowCardText);
            if (BelowCardText == "Downloads")
            {
                foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                {
                    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
                    {
                        MainPage.Select.itemSelected = NavItemChoice;
                        break;
                    }
                }
               App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
            }
            else if (BelowCardText == "Documents")
            {
                foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                {
                    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
                    {
                        MainPage.Select.itemSelected = NavItemChoice;
                        break;
                    }
                }
               App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
            }
            else if (BelowCardText == "Pictures")
            {
                foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                {
                    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
                    {
                        MainPage.Select.itemSelected = NavItemChoice;
                        break;
                    }
                }
               App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
            }
            else if (BelowCardText == "Music")
            {
                foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                {
                    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
                    {
                        MainPage.Select.itemSelected = NavItemChoice;
                        break;
                    }
                }
               App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
            }
            else if (BelowCardText == "Videos")
            {
                foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                {
                    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
                    {
                        MainPage.Select.itemSelected = NavItemChoice;
                        break;
                    }
                }
               App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
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
    }
}
