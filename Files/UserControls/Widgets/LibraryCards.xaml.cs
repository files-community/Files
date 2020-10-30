using Files.View_Models;
using Files.Views.Pages;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public sealed partial class LibraryCards : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public delegate void LibraryCardInvokedEventHandler(object sender, LibraryCardInvokedEventArgs e);

        public event LibraryCardInvokedEventHandler LibraryCardInvoked;
        public static List<FavoriteLocationItem> itemsAdded = new List<FavoriteLocationItem>();

        public LibraryCards()
        {
            InitializeComponent();
            itemsAdded.Clear();
            itemsAdded.Add(new FavoriteLocationItem() { Icon = "\xe91c", Text = ResourceController.GetTranslation("SidebarDownloads"), Tag = "Downloads", AutomationProperties = ResourceController.GetTranslation("SidebarDownloads") });
            itemsAdded.Add(new FavoriteLocationItem() { Icon = "\xea11", Text = ResourceController.GetTranslation("SidebarDocuments"), Tag = "Documents", AutomationProperties = ResourceController.GetTranslation("SidebarDocuments") });
            itemsAdded.Add(new FavoriteLocationItem() { Icon = "\xea83", Text = ResourceController.GetTranslation("SidebarPictures"), Tag = "Pictures", AutomationProperties = ResourceController.GetTranslation("SidebarPictures") });
            itemsAdded.Add(new FavoriteLocationItem() { Icon = "\xead4", Text = ResourceController.GetTranslation("SidebarMusic"), Tag = "Music", AutomationProperties = ResourceController.GetTranslation("SidebarMusic") });
            itemsAdded.Add(new FavoriteLocationItem() { Icon = "\xec0d", Text = ResourceController.GetTranslation("SidebarVideos"), Tag = "Videos", AutomationProperties = ResourceController.GetTranslation("SidebarVideos") });
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
            LibraryCardInvoked?.Invoke(this, new LibraryCardInvokedEventArgs() { Path = NavigationPath, LayoutType = AppSettings.GetLayoutType() });
        }
    }

    public class LibraryCardInvokedEventArgs : EventArgs
    {
        public Type LayoutType { get; set; }
        public string Path { get; set; }
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