using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class About : Page
    {
        public About()
        {
            InitializeComponent();
            var version = Package.Current.Id.Version;
            VersionNumber.Text = string.Format($"Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
        }
        
        private void OpenLogLocationButton_Click(object sender, RoutedEventArgs e) => View_Models.SettingsViewModel.OpenLogLocation();

        private async void FeedbackListView_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (FeedbackListView.SelectedIndex == 0)
            {
                View_Models.SettingsViewModel.ReportIssueOnGitHub()
            }
            else if (FeedbackListView.SelectedIndex == 1)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/releases"));
            }
            else if (FeedbackListView.SelectedIndex == 2)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/graphs/contributors"));
            }
        }
    }
}