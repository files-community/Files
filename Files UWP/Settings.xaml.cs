using Files.SettingsPages;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;



namespace Files
{

    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = false;
            //Window.Current.SetTitleBar(DragArea);
            SettingsContentFrame.Navigate(typeof(Personalization));
        }

        private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItem;
            if (item.ToString() == "Personalization")
            {
                SettingsContentFrame.Navigate(typeof(Personalization));

            }
            else if (item.ToString() == "Preferences")
            {
                SettingsContentFrame.Navigate(typeof(Preferences));
            }
            else if (item.ToString() == "About")
            {
                SettingsContentFrame.Navigate(typeof(About));
            }

        }

        private void SettingsPane_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.GoBack();
            

        }
    }
}
