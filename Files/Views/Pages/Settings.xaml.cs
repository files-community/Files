using Files.SettingsPages;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{

    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();

            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
                titleBar.BackgroundColor = Color.FromArgb(255, 25, 25, 25);
            }
            else if (App.Current.RequestedTheme == ApplicationTheme.Light)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 255, 255, 255);
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 155, 155, 155);
            }

            if (this.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
                titleBar.BackgroundColor = Color.FromArgb(255, 25, 25, 25);
            }
            else if (this.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 155, 155, 155);
                titleBar.BackgroundColor = Colors.Transparent;
            }
            SettingsContentFrame.Navigate(typeof(Appearance));
        }

        private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var item = args.InvokedItemContainer.Tag;
            _ = SettingsPane.MenuItems.IndexOf(SettingsPane.SelectedItem) switch
            {
                0 => SettingsContentFrame.Navigate(typeof(Appearance)),
                1 => SettingsContentFrame.Navigate(typeof(OnStartup)),
                2 => SettingsContentFrame.Navigate(typeof(Preferences)),
                4 => SettingsContentFrame.Navigate(typeof(FilesAndFolders)),
                3 => SettingsContentFrame.Navigate(typeof(StartPageWidgets)),
                5 => SettingsContentFrame.Navigate(typeof(About)),
                _ => SettingsContentFrame.Navigate(typeof(Appearance))
            };
        }
    }
}
