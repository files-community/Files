using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Microsoft.UI.Xaml.Media;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class Properties : Page
    {
        private static ApplicationViewTitleBar TitleBar;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        private object navParameter;

        public SettingsViewModel AppSettings => App.AppSettings;

        public Properties()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navParameter = e.Parameter;
            this.TabShorcut.Visibility = e.Parameter is ShortcutItem ? Visibility.Visible : Visibility.Collapsed;
            base.OnNavigatedTo(e);
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                // Set window size in the loaded event to prevent flickering
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(400, 550));
                ApplicationView.GetForCurrentView().Consolidated += Properties_Consolidated;
                TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                TitleBar.ButtonBackgroundColor = Colors.Transparent;
                TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    AppSettings.UpdateThemeElements.Execute(null);
                });
            }
        }

        private void Properties_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            ApplicationView.GetForCurrentView().Consolidated -= Properties_Consolidated;
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        private void Properties_Unloaded(object sender, RoutedEventArgs e)
        {
            // Why is this not called? Are we cleaning up properly?
        }

        private async void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RequestedTheme = ThemeHelper.RootTheme;
                Background = new AcrylicBrush()
                {
                    TintLuminosityOpacity = 0.9,
                    AlwaysUseFallback = AppSettings.AcrylicEnabled,
                    BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                    FallbackColor = AppSettings.AcrylicTheme.FallbackColor,
                    TintColor = AppSettings.AcrylicTheme.TintColor,
                    TintOpacity = AppSettings.AcrylicTheme.TintOpacity,
                };
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    switch (RequestedTheme)
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
            });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                var propertiesDialog = new PropertiesDialog();
                propertiesDialog.Hide();
            }
        }

        private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape) && ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
        }

        private void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            var navParam = new PropertyNavParam() { tokenSource = tokenSource, navParameter = navParameter };

            switch (args.SelectedItemContainer.Tag)
            {
                case "General":
                    contentFrame.Navigate(typeof(PropertiesGeneral), navParam, args.RecommendedNavigationTransitionInfo);
                    break;

                case "Shortcut":
                    contentFrame.Navigate(typeof(PropertiesShortcut), navParam, args.RecommendedNavigationTransitionInfo);
                    break;
            }
        }

        public class PropertyNavParam
        {
            public CancellationTokenSource tokenSource;
            public object navParameter;
        }
    }
}