using Files.SettingsPages;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;



namespace Files
{

    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
            SecondaryPane.SelectedIndex = 1;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(ListViewItem lvi in SecondaryPane.Items)
            {
                if((e.AddedItems[0] as ListViewItem).Name == "Personalization" && lvi.Name == "Personalization")
                {
                    SettingsContentFrame.Navigate(typeof(Personalization));
                }
                else if((e.AddedItems[0] as ListViewItem).Name == "Preferences" && lvi.Name == "Preferences")
                {
                    SettingsContentFrame.Navigate(typeof(Preferences));
                }
                else if ((e.AddedItems[0] as ListViewItem).Name == "About" && lvi.Name == "About")
                {
                    SettingsContentFrame.Navigate(typeof(About));
                }
            }
        }
    }
}
