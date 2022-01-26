using Files.Extensions;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Uwp;

#nullable enable

namespace Files.Helpers
{
    /// <summary>
    /// Class providing functionality around switching and restoring theme settings
    /// </summary>
    internal sealed class ThemeHelper
    {
        private readonly Window _appWindow;

        private readonly ApplicationViewTitleBar _titleBar;

        private readonly UISettings _uiSettings;

        private readonly DispatcherQueue _dispatcherQueue;

        public ElementTheme RootTheme
        {
            get
            {
                var savedTheme = ApplicationData.Current.LocalSettings.Values["theme"]?.ToString();

                return !string.IsNullOrEmpty(savedTheme) ? EnumExtensions.GetEnum<ElementTheme>(savedTheme) : ElementTheme.Default;
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["theme"] = value.ToString();
                UpdateTheme();
            }
        }

        private static Dictionary<Window, ThemeHelper> _ThemeHelpers { get; } = new();
        public static IReadOnlyDictionary<Window, ThemeHelper> ThemeHelpers
        {
            get => _ThemeHelpers;
        }

        private ThemeHelper(Window appWindow, ApplicationViewTitleBar titleBar)
        {
            this._appWindow = appWindow;
            this._titleBar = titleBar;
            this._uiSettings = new();
            this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            this._uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
        }

        private async void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            await _dispatcherQueue.EnqueueAsync(UpdateTheme);
        }

        public void UpdateTheme()
        {
            var rootTheme = RootTheme;

            if (Window.Current.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = rootTheme;
            }

            _titleBar.ButtonBackgroundColor = Colors.Transparent;
            _titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            switch (rootTheme)
            {
                case ElementTheme.Default:
                    _titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
                    _titleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
                    break;

                case ElementTheme.Light:
                    _titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
                    _titleBar.ButtonForegroundColor = Colors.Black;
                    break;

                case ElementTheme.Dark:
                    _titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
                    _titleBar.ButtonForegroundColor = Colors.White;
                    break;
            }

            App.AppSettings.UpdateThemeElements.Execute(null);
        }

        public static ThemeHelper? RegisterWindowInstance(Window appWindow, ApplicationViewTitleBar titleBar)
        {
            var themeHelper = new ThemeHelper(appWindow, titleBar);

            return _ThemeHelpers.TryAdd(appWindow, themeHelper) ? themeHelper : null;
        }

        public static bool UnregisterWindowInstance(Window appWindow)
        {
            if (_ThemeHelpers.Remove(appWindow, out var themeHelper))
            {
                themeHelper._uiSettings.ColorValuesChanged -= themeHelper.UiSettings_ColorValuesChanged;
            }

            return false;
        }
    }
}