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
        private static AppWindowTitleBar TitleBar;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public AppWindow propWindow;
        public SettingsViewModel AppSettings => App.AppSettings;
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
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            base.OnNavigatedTo(e);
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {   
                propWindow = Interaction.AppWindows[UIContext]; // Collect AppWindow-specific info
                
                TitleBar = propWindow.TitleBar; // Set properties window titleBar style
                TitleBar.ButtonBackgroundColor = Colors.Transparent;
                TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                AppSettings.UpdateThemeElements.Execute(null);
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
                        TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                        TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                        break;

                    case ElementTheme.Light:
                        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                        TitleBar.ButtonForegroundColor = Colors.Black;
                        break;

                    case ElementTheme.Dark:
                        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                        TitleBar.ButtonForegroundColor = Colors.White;
                        break;
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
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