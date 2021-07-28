using System.Net;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class About : Page
    {
        public About()
        {
            InitializeComponent();
        }

        private async void SettingsBlockControl_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            // get's the privacy policy from the repo and sets the md text block to it's content
            var url = "https://raw.githubusercontent.com/files-community/Files/main/Privacy.md";
            PrivacyTextBlock.Text = await new WebClient().DownloadStringTaskAsync(url);
        }
    }
}