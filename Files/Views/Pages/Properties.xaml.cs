using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using System;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class Properties : Page
    {
        private static AppWindowTitleBar _TitleBar;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public AppWindow propWindow;

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        public Properties()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = new SelectedItemsPropertiesViewModel(e.Parameter as ListedItem);
            ViewModel.ItemMD5HashProgress = ItemMD5HashProgress;
            ViewModel.Dispatcher = Dispatcher;
            App.AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            base.OnNavigatedTo(e);
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                // Collect AppWindow-specific info
                propWindow = Interaction.AppWindows[UIContext];
                // Set properties window titleBar style
                _TitleBar = propWindow.TitleBar;
                _TitleBar.ButtonBackgroundColor = Colors.Transparent;
                _TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                App.AppSettings.UpdateThemeElements.Execute(null);
            }
            await ViewModel.GetPropertiesAsync(_tokenSource);
        }

        private void Properties_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_tokenSource != null && !_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                switch (ThemeHelper.RootTheme)
                {
                    case ElementTheme.Default:
                        _TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                        _TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                        break;

                    case ElementTheme.Light:
                        _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                        _TitleBar.ButtonForegroundColor = Colors.Black;
                        break;

                    case ElementTheme.Dark:
                        _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                        _TitleBar.ButtonForegroundColor = Colors.White;
                        break;
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            App.AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await propWindow.CloseAsync();
            }
            else
            {
                App.PropertiesDialogDisplay.Hide();
            }
        }
    }
}