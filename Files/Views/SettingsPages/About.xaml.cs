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

        private void ListView_ItemClick(object sender, ItemClickEventArgs e) => View_Models.SettingsViewModel.ReportIssueOnGitHub();

        private void OpenLogLocationButton_Click(object sender, RoutedEventArgs e) => View_Models.SettingsViewModel.OpenLogLocation();
    }
}