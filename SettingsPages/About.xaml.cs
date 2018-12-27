using Interacts;
using System;
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
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clicked = e.ClickedItem as ListViewBase;
            var trulyclicked = Interaction.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            if (trulyclicked.Name == "FeedbackForm")
            {
                await Launcher.LaunchUriAsync(new Uri(@"https://goo.gl/forms/Z4bfKhaJJ6lT3Tcu2"));
            }
        }
    }
}
