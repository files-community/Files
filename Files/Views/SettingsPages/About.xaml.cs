using System;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
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

        private async void OpenLogLocationButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
        }

        private async void FeedbackListView_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (FeedbackListView.SelectedIndex == 0)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/issues/new/choose"));
            }
            else if (FeedbackListView.SelectedIndex == 1)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/releases"));
            }
            else if (FeedbackListView.SelectedIndex == 2)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/graphs/contributors"));
            }

            (FeedbackListView.Items[FeedbackListView.SelectedIndex] as ListViewItem).IsSelected = false;
        }
    }
}