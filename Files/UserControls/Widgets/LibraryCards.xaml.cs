using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public sealed partial class LibraryCards : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public static List<FavoriteLocationItem> itemsAdded = new List<FavoriteLocationItem>();

        public LibraryCards()
        {
            InitializeComponent();
            itemsAdded.Clear();
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Blue.png", Icon = "\xe91c", Text = "SidebarDownloads".GetLocalized(), Tag = "Downloads" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Green.png", Icon = "\xea11", Text = "SidebarDocuments".GetLocalized(), Tag = "Documents" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Orange.png", Icon = "\xea83", Text = "SidebarPictures".GetLocalized(), Tag = "Pictures" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Pink.png", Icon = "\xead4", Text = "SidebarMusic".GetLocalized(), Tag = "Music" });
            itemsAdded.Add(new FavoriteLocationItem() { ImageSource = "Assets/Cards/Gradients/Red.png", Icon = "\xec0d", Text = "SidebarVideos".GetLocalized(), Tag = "Videos" });
            foreach (var item in itemsAdded) { item.AutomationProperties = item.Text; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string NavigationPath = ""; // path to navigate
            string ClickedCard = (sender as Button).Tag.ToString();

            switch (ClickedCard)
            {
                case "Downloads":
                    NavigationPath = AppSettings.DownloadsPath;
                    break;

                case "Documents":
                    NavigationPath = AppSettings.DocumentsPath;
                    break;

                case "Pictures":
                    NavigationPath = AppSettings.PicturesPath;
                    break;

                case "Music":
                    NavigationPath = AppSettings.MusicPath;
                    break;

                case "Videos":
                    NavigationPath = AppSettings.VideosPath;
                    break;

                case "RecycleBin":
                    NavigationPath = AppSettings.RecycleBinPath;
                    break;
            }

            App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), NavigationPath);

            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
        }
    }

    public class FavoriteLocationItem
    {
        public string ImageSource { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Tag { get; set; }
        public string AutomationProperties { get; set; }
    }
}