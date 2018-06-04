using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;



namespace Files
{


    public sealed partial class YourHome : Page
    {

        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        public YourHome()
        {
            this.InitializeComponent();
        }

        private void b0_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GenericFileBrowser), DesktopPath);
        }

        private void b1_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GenericFileBrowser), DownloadsPath);

        }

        private void b2_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
        }

        private void b3_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GenericFileBrowser), PicturesPath);
        }

        private void b4_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GenericFileBrowser), MusicPath);
        }

        private void b5_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GenericFileBrowser), VideosPath);
        }
    }
}
