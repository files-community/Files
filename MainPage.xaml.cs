using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Graphics;
using Microsoft.Toolkit.Uwp;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml.Media.Animation;

namespace Files
{

    public sealed partial class MainPage : Page
    {
        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public MainPage()
        {
            this.InitializeComponent();
            this.IsTextScaleFactorEnabled = true;
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 500));

            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            //DragArea.Height = CoreTitleBar.Height;
            Window.Current.SetTitleBar(DragArea);

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(100, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            


            WelcomeFileCheck();
            ContentFrame.Navigate(typeof(YourHome));
            auto_suggest.IsEnabled = true;
        }

        private void navView_ItemInvoked(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem item = args.SelectedItem as NavigationViewItem;
            if (item.Name == "homeIc")
            {
                ContentFrame.Navigate(typeof(YourHome));
                auto_suggest.PlaceholderText = "Search Recents";
            }
            else if (item.Name == "DesktopIC")
            {
                ContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                auto_suggest.PlaceholderText = "Search Desktop";
            }
            else if (item.Name == "DocumentsIC")
            {
                ContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                auto_suggest.PlaceholderText = "Search Documents";
            }
            else if (item.Name == "DownloadsIC")
            {
                ContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                auto_suggest.PlaceholderText = "Search Downloads";
            }
            else if (item.Name == "PicturesIC")
            {
                ContentFrame.Navigate(typeof(GenericFileBrowser), PicturesPath);
                auto_suggest.PlaceholderText = "Search Pictures";
            }
            else if (item.Name == "MusicIC")
            {
                ContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                auto_suggest.PlaceholderText = "Search Music";
            }
            else if (item.Name == "VideosIC")
            {
                ContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                auto_suggest.PlaceholderText = "Search Videos";
            }
            else if (item.Name == "LocD_IC")
            {
                ContentFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                auto_suggest.PlaceholderText = "Search";
            }
        }

        public async void WelcomeFileCheck()
        {
            string env = Environment.ExpandEnvironmentVariables("%userprofile%");
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            string cachePath = storageFolder.Path + @"\welcome.txt";
            diagText.Text = cachePath;
            FileInfo fInfo = new FileInfo(cachePath);
            if (await storageFolder.TryGetItemAsync("welcome.txt") == null)
            {
                WelcomeGrid.Visibility = Visibility.Visible;
            }

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                WelcomeGrid.Visibility = Visibility.Collapsed;

                var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
                fal.Clear();
                fal.AddOrReplace("CDriveToken", folder);

                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                Windows.Storage.StorageFile sampleFile = await storageFolder.CreateFileAsync("welcome.txt", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            }
        }

        private void auto_suggest_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            
        }
    }
}