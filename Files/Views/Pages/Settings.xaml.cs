using Files.SettingsPages;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
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
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(DragArea);

            SettingsPane.SelectedItem = SettingsPane.MenuItems[0];
        }

        private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            //ToDo waiting for WinUI bug to be fixed before using item invoked

            //_ = SettingsPane.MenuItems.IndexOf(SettingsPane.SelectedItem) switch
            //{
            //    0 => SettingsContentFrame.Navigate(typeof(Appearance)),
            //    1 => SettingsContentFrame.Navigate(typeof(OnStartup)),
            //    //2 => SettingsContentFrame.Navigate(typeof(StartPageWidgets)),
            //    3 => SettingsContentFrame.Navigate(typeof(Preferences)),
            //    4 => SettingsContentFrame.Navigate(typeof(FilesAndFolders)),
            //    5 => SettingsContentFrame.Navigate(typeof(About)),
            //    _ => SettingsContentFrame.Navigate(typeof(Appearance))
            //};
        }

        private void SettingsPane_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            _ = SettingsPane.MenuItems.IndexOf(SettingsPane.SelectedItem) switch
            {
                0 => SettingsContentFrame.Navigate(typeof(Appearance)),
                1 => SettingsContentFrame.Navigate(typeof(OnStartup)),
                //2 => SettingsContentFrame.Navigate(typeof(StartPageWidgets)),
                3 => SettingsContentFrame.Navigate(typeof(Preferences)),
                4 => SettingsContentFrame.Navigate(typeof(FilesAndFolders)),
                5 => SettingsContentFrame.Navigate(typeof(Experimental)),
                6 => SettingsContentFrame.Navigate(typeof(About)),
                _ => SettingsContentFrame.Navigate(typeof(Appearance))
            };
        }

        private void SettingsPane_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(InstanceTabsView));
        }
    }
}