#nullable enable
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

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(@"https://github.com/duke7553/files-uwp/issues/new/choose"));
        }

        private async void OpenLogLocationButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
        }
    }
}