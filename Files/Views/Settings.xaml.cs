using Files.SettingsPages;
using Files.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.Views
{
    public sealed partial class Settings : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public Settings()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);

            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(DragArea);

            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested += OnBackRequested;

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }

            SettingsPane.SelectedItem = SettingsPane.MenuItems[0];
        }

        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested -= OnBackRequested;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                GoBack();
                e.Handled = true;
            }
        }

        private void SettingsPane_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            GoBack();
        }

        private void GoBack()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void SettingsPane_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            _ = SettingsPane.MenuItems.IndexOf(SettingsPane.SelectedItem) switch
            {
                0 => SettingsContentFrame.Navigate(typeof(Appearance)),
                1 => SettingsContentFrame.Navigate(typeof(OnStartup)),
                2 => SettingsContentFrame.Navigate(typeof(Preferences)),
                3 => SettingsContentFrame.Navigate(typeof(Widgets)),
                4 => SettingsContentFrame.Navigate(typeof(Multitasking)),
                5 => SettingsContentFrame.Navigate(typeof(FilesAndFolders)),
                6 => SettingsContentFrame.Navigate(typeof(Experimental)),
                7 => SettingsContentFrame.Navigate(typeof(About)),
                _ => SettingsContentFrame.Navigate(typeof(Appearance))
            };
        }
    }
}
