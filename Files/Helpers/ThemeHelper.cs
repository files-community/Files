using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Files.Helpers
{
    /// <summary>
    /// Class providing functionality around switching and restoring theme settings
    /// </summary>
    public static class ThemeHelper
    {
        private const string _SelectedAppThemeKey = "theme";
        private static Window _CurrentApplicationWindow;
        private static ApplicationViewTitleBar _TitleBar;

        // Keep reference so it does not get optimized/garbage collected
        public static UISettings UiSettings;

        /// <summary>
        /// Gets the current actual theme of the app based on the requested theme of the
        /// root element, or if that value is Default, the requested theme of the Application.
        /// </summary>
        public static ElementTheme ActualTheme
        {
            get
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    if (rootElement.RequestedTheme != ElementTheme.Default)
                    {
                        return rootElement.RequestedTheme;
                    }
                }

                return Interacts.Interaction.GetEnum<ElementTheme>(Application.Current.RequestedTheme.ToString());
            }
        }

        /// <summary>
        /// Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
        /// </summary>
        public static ElementTheme RootTheme
        {
            get
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    return rootElement.RequestedTheme;
                }

                return ElementTheme.Default;
            }
            set
            {
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }

                ApplicationData.Current.LocalSettings.Values[_SelectedAppThemeKey] = value.ToString();
                UpdateTheme();
            }
        }

        public static void Initialize()
        {
            App.AppSettings.AcrylicTheme = new AcrylicTheme();

            // Set TitleBar background color
            _TitleBar = ApplicationView.GetForCurrentView().TitleBar;
            _TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Save reference as this might be null when the user is in another app
            _CurrentApplicationWindow = Window.Current;
            string savedTheme = ApplicationData.Current.LocalSettings.Values[_SelectedAppThemeKey]?.ToString();

            if (!string.IsNullOrEmpty(savedTheme))
            {
                RootTheme = Interacts.Interaction.GetEnum<ElementTheme>(savedTheme);
            }
            else
            {
                RootTheme = ElementTheme.Default;
            }

            // Registering to color changes, thus we notice when user changes theme system wide
            UiSettings = new UISettings();
            UiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
        }

        private static void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            // Make sure we have a reference to our window so we dispatch a UI change
            if (_CurrentApplicationWindow != null)
            {
                // Dispatch on UI thread so that we have a current appbar to access and change
                _CurrentApplicationWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                {
                    UpdateTheme();
                });
            }
        }

        public static void UpdateTheme()
        {
            switch (RootTheme)
            {
                case ElementTheme.Default:
                    App.AppSettings.AcrylicTheme.SetDefaultTheme();
                    _TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                    _TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                    break;

                case ElementTheme.Light:
                    App.AppSettings.AcrylicTheme.SetLightTheme();
                    _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                    _TitleBar.ButtonForegroundColor = Colors.Black;
                    break;

                case ElementTheme.Dark:
                    App.AppSettings.AcrylicTheme.SetDarkTheme();
                    _TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                    _TitleBar.ButtonForegroundColor = Colors.White;
                    break;
            }
            App.AppSettings.UpdateThemeElements.Execute(null);
        }
    }
}