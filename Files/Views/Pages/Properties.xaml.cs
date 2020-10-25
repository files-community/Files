using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class Properties : Page
    {
        private static ApplicationViewTitleBar TitleBar;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private ContentDialog propertiesDialog;

        private object navParameter;

        private ListedItem listedItem;

        public SettingsViewModel AppSettings => App.AppSettings;

        public Properties()
        {
            InitializeComponent();
            propertiesDialog = Interaction.FindParent<ContentDialog>(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navParameter = e.Parameter;
            this.TabShorcut.Visibility = e.Parameter is ShortcutItem ? Visibility.Visible : Visibility.Collapsed;
            this.listedItem = e.Parameter as ListedItem;
            this.TabDetails.Visibility = listedItem != null && listedItem.FileExtension != null && !listedItem.IsShortcutItem ? Visibility.Visible : Visibility.Collapsed;
            this.SetBackground();
            base.OnNavigatedTo(e);
        }

        private async void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            AppSettings.PropertyChanged += AppSettings_PropertyChanged;
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                // Set window size in the loaded event to prevent flickering
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(400, 550));
                ApplicationView.GetForCurrentView().Consolidated += Properties_Consolidated;
                TitleBar = ApplicationView.GetForCurrentView().TitleBar;
                TitleBar.ButtonBackgroundColor = Colors.Transparent;
                TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => AppSettings.UpdateThemeElements.Execute(null));
            }
            else
            {
                propertiesDialog.Closed += PropertiesDialog_Closed;
            }
        }

        private void AppSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AcrylicEnabled":
                case "FallbackColor":
                case "TintColor":
                case "TintOpacity":
                    SetBackground();
                    break;
            }
        }

        private async void SetBackground()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var backgroundBrush = new AcrylicBrush()
                {
                    AlwaysUseFallback = AppSettings.AcrylicEnabled,
                    BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                    FallbackColor = AppSettings.AcrylicTheme.FallbackColor,
                    TintColor = AppSettings.AcrylicTheme.TintColor,
                    TintOpacity = AppSettings.AcrylicTheme.TintOpacity,
                };
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 9))
                {
                    backgroundBrush.TintLuminosityOpacity = 0.9;
                }

                if (!(new AccessibilitySettings()).HighContrast)
                {
                    Background = backgroundBrush;
                }
                else
                {
                    Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
                }
            });
        }

        private void Properties_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
            ApplicationView.GetForCurrentView().Consolidated -= Properties_Consolidated;
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        private void PropertiesDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
            sender.Closed -= PropertiesDialog_Closed;
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
                tokenSource = null;
            }
            propertiesDialog.Hide();
        }

        private void Properties_Unloaded(object sender, RoutedEventArgs e)
        {
            // Why is this not called? Are we cleaning up properly?
        }

        private async void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            var selectedTheme = ThemeHelper.RootTheme;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                RequestedTheme = selectedTheme;
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
                SetBackground();
            });
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (contentFrame.Content is PropertiesGeneral)
            {
                await (contentFrame.Content as PropertiesGeneral).SaveChanges(listedItem);
            }
            else if (contentFrame.Content is PropertiesDetails)
            {
                await (contentFrame.Content as PropertiesDetails).SaveChanges(listedItem);
            }

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                propertiesDialog.Hide();
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                propertiesDialog.Hide();
            }
        }

        private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                }
                else
                {
                    propertiesDialog.Hide();
                }
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

                case "Details":
                    contentFrame.Navigate(typeof(PropertiesDetails), navParam, args.RecommendedNavigationTransitionInfo);
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