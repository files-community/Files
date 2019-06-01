using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Interacts;
using Files.Filesystem;

namespace Files.SettingsPages
{

    public sealed partial class About : Page
    {
        public About()
        {
            this.InitializeComponent();
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clicked = e.ClickedItem as ListViewBase;
            var trulyclicked = Interaction<About>.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            if (trulyclicked.Name == "FeedbackForm")
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://github.com/duke7553/files-uwp/issues/new"));
            }
        }
    }
}
