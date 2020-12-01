using Files.DataModels;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.Services.Store;
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
            VersionNumber.Text = string.Format($"{"SettingsAboutVersionTitle".GetLocalized()} {version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
        }

        private void OpenLogLocationButton_Click(object sender, RoutedEventArgs e) => View_Models.SettingsViewModel.OpenLogLocation();

        private async void FeedbackListView_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (FeedbackListView.SelectedIndex == 0)
            {
                View_Models.SettingsViewModel.ReportIssueOnGitHub();
            }
            else if (FeedbackListView.SelectedIndex == 1)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/releases"));
            }
            else if (FeedbackListView.SelectedIndex == 2)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/graphs/contributors"));
            }
            else if (FeedbackListView.SelectedIndex == 3)
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://paypal.me/yaichenbaum"));
            }

            (FeedbackListView.Items[FeedbackListView.SelectedIndex] as ListViewItem).IsSelected = false;
        }

        private async void UpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            AppUpdater updater = new AppUpdater();
            //int updates = await updater.CheckForUpdatesAsync();

            bool dialogResult = await updater.DownloadUpdatesConsent();
            if (dialogResult)
            {
                IAsyncResult result = updater.DownloadUpdates();
                while (!result.IsCompleted)
                {
                    UpdateProgress.Visibility = Visibility.Visible;
                    //UpdateProgress.
                }

                if (result.IsCompleted)
                {
                    UpdateProgress.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}