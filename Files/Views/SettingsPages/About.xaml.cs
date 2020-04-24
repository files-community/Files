using Files.Interacts;
using System;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class About : Page
    {
        public About()
        {
            this.InitializeComponent();

            VersionNumber.Text = string.Format("Version: {0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clicked = e.ClickedItem as ListViewBase;
            var trulyclicked = Interaction.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            if (trulyclicked.Name == "FeedbackForm")
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/duke7553/files-uwp/issues/new/choose"));
            }
        }
    }
}