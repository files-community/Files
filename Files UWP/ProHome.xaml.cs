using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    /// <summary>
    /// This is not finished yet. This is the work that was started on having multiple Tabs
    /// </summary>
    public sealed partial class ProHome : Page
    {
        ObservableCollection<Tab> tabList = new ObservableCollection<Tab>();
        public static ContentDialog permissionBox;

        public static ObservableCollection<Tab> TabList { get; set; } = new ObservableCollection<Tab>();
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public ProHome()
        {
            this.InitializeComponent();
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = false;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            permissionBox = PermissionDialog;
            LocationsList.SelectedIndex = 0;
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(255, 0, 0, 0);
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
            }
            else if (App.Current.RequestedTheme == ApplicationTheme.Light)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(255, 255, 255, 255);
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
            }

            if (this.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
            }
            else if (this.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }
            TabList.Clear();
            TabList.Add(new Tab() { TabName = "Home", TabContent = "local:MainPage" });
        }

        private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = e.AddedItems as TabViewItem;

        }

        private async void VisiblePath_TextChanged(object sender, KeyRoutedEventArgs e)
        {

        }

        private void LocationsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if(DrivesList.SelectedItem != null)
            {
                DrivesList.SelectedItem = null;
            }
            var clickedItem = Interacts.Interaction.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            if(clickedItem.Tag.ToString() == "ThisPC")
            {
                ItemDisplayFrame.Navigate(typeof(YourHome));
            }
            else if(clickedItem.Tag.ToString() == "Desktop")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
            }
            else if(clickedItem.Tag.ToString() == "Downloads")
            {

            }

        }

        private void DrivesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if(LocationsList.SelectedItem != null)
            {
                LocationsList.SelectedItem = null;
            }
        }

        private async void PermissionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));

        }
    }

    public class Tab
    {
        public string TabName { get; set; }
        public string TabContent { get; set; }
    }
}
