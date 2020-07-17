using Files.View_Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public sealed partial class LibraryCards : UserControl
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public LibraryCards()
        {
            InitializeComponent();

            Locations.ItemLoader.itemsAdded.Clear();
            Locations.ItemLoader.DisplayItems();
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

            App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(NavigationPath), NavigationPath);

            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
        }
    }
}