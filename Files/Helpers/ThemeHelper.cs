using System;
using Windows.Storage;
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

        // Keep reference so it does not get optimized/garbage collected
        public static UISettings UiSettings;

        /// <summary>
        /// Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
        /// </summary>
        public static ElementTheme RootTheme
        {
            get
            {
                var savedTheme = ApplicationData.Current.LocalSettings.Values[selectedAppThemeKey]?.ToString();

                if (!string.IsNullOrEmpty(savedTheme))
                {
                    return Interacts.Interaction.GetEnum<ElementTheme>(savedTheme);
                }
                else
                {
                    return ElementTheme.Default;
                }
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values[selectedAppThemeKey] = value.ToString();
                ApplyTheme();
            }
        }

        public static void Initialize()
        {
            // Save reference as this might be null when the user is in another app
            currentApplicationWindow = Window.Current;

            //Apply the desired theme based on what is set in the application settings
            ApplyTheme();

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
                    ApplyTheme();
                });
            }
        }

        private static void ApplyTheme()
        {
            var rootTheme = RootTheme;

            if (Window.Current.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = rootTheme;
            }

            switch (rootTheme)
            {
                case ElementTheme.Default:
                    App.AppSettings.AcrylicTheme.SetDefaultTheme();
                    break;

                case ElementTheme.Light:
                    App.AppSettings.AcrylicTheme.SetLightTheme();
                    break;

                case ElementTheme.Dark:
                    App.AppSettings.AcrylicTheme.SetDarkTheme();
                    break;
            }
            App.AppSettings.UpdateThemeElements.Execute(null);
        }
    }
}