using Files.Extensions;
using Files.Uwp.Helpers;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Files.Helpers
{
    /// <summary>
    /// Class providing functionality around switching and restoring theme settings
    /// </summary>
    public static class ThemeHelper
    {
        private const string selectedAppThemeKey = "theme";

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
                    return EnumExtensions.GetEnum<ElementTheme>(savedTheme);
                }
                else
                {
                    return ElementTheme.Default;
                }
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values[selectedAppThemeKey] = value.ToString();
                ApplyThemeForAllWindows();
            }
        }

        private async static void ApplyThemeForAllWindows()
        {
            if (Window.Current != null)
            {
                await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
                {
                    ApplyTheme(ApplicationView.GetForCurrentView());
                }, Windows.System.DispatcherQueuePriority.High);
            }

            // Make sure we have a reference to our window so we dispatch a UI change
            if (App.AppWindows.Count > 0)
            {
                foreach (AppWindow window in App.AppWindows.Values)
                {
                    // Dispatch on UI thread so that we have a current appbar to access and change
                    await window.DispatcherQueue.EnqueueAsync(() =>
                    {
                        ApplyTheme(window);
                    }, Windows.System.DispatcherQueuePriority.High);
                }
            }
        }

        public static void Initialize()
        {
            ApplyThemeForAllWindows();

            // Registering to color changes, thus we notice when user changes theme system wide
            UiSettings = new UISettings();
            UiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
        }

        private static async void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            // Make sure we have a reference to our window so we dispatch a UI change
            if (App.AppWindows.Count > 0)
            {
                foreach (AppWindow window in App.AppWindows.Values)
                {
                    // Dispatch on UI thread so that we have a current appbar to access and change
                    await window.DispatcherQueue.EnqueueAsync(() =>
                    {
                        ApplyTheme(window);
                    }, Windows.System.DispatcherQueuePriority.High);
                }
                
            }
        }

        private static void ApplyTheme(AppWindow window)
        {
            var rootTheme = RootTheme;

            if (WindowManagementHelpers.GetWindowContentFromAppWindow(window) is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = rootTheme;
            }

            window.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            window.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            switch (rootTheme)
            {
                case ElementTheme.Default:
                    window.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                    window.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                    break;

                case ElementTheme.Light:
                    window.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                    window.TitleBar.ButtonForegroundColor = Colors.Black;
                    break;

                case ElementTheme.Dark:
                    window.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                    window.TitleBar.ButtonForegroundColor = Colors.White;
                    break;
            }
            App.AppSettings.UpdateThemeElements.Execute(null);
        }

        private static void ApplyTheme(ApplicationView window)
        {
            var rootTheme = RootTheme;

            if (Window.Current.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = rootTheme;
            }

            window.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            window.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            switch (rootTheme)
            {
                case ElementTheme.Default:
                    window.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                    window.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                    break;

                case ElementTheme.Light:
                    window.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                    window.TitleBar.ButtonForegroundColor = Colors.Black;
                    break;

                case ElementTheme.Dark:
                    window.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                    window.TitleBar.ButtonForegroundColor = Colors.White;
                    break;
            }
            App.AppSettings.UpdateThemeElements.Execute(null);
        }
    }
}