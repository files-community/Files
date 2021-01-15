using System;
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
        private const string selectedAppThemeKey = "theme";
        private static Window currentApplicationWindow;
        private static ApplicationViewTitleBar titleBar;

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

                ApplicationData.Current.LocalSettings.Values[selectedAppThemeKey] = value.ToString();
                UpdateTheme();
            }
        }

        public static void Initialize()
        {
            App.AppSettings.AcrylicTheme = new AcrylicTheme();

            // Set TitleBar background color
            titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Save reference as this might be null when the user is in another app
            currentApplicationWindow = Window.Current;
            string savedTheme = ApplicationData.Current.LocalSettings.Values[selectedAppThemeKey]?.ToString();

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

        private static async void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            // Make sure we have a reference to our window so we dispatch a UI change
            if (currentApplicationWindow != null)
            {
                // Dispatch on UI thread so that we have a current appbar to access and change
                await currentApplicationWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
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
                    titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                    titleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                    break;

                case ElementTheme.Light:
                    App.AppSettings.AcrylicTheme.SetLightTheme();
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                    titleBar.ButtonForegroundColor = Colors.Black;
                    break;

                case ElementTheme.Dark:
                    App.AppSettings.AcrylicTheme.SetDarkTheme();
                    titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                    titleBar.ButtonForegroundColor = Colors.White;
                    break;
            }
            App.AppSettings.UpdateThemeElements.Execute(null);
        }
    }
}