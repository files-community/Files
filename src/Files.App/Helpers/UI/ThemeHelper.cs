// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Files.App.Helpers
{
	/// <summary>
	/// Class providing functionality around switching and restoring theme settings
	/// </summary>
	public static class ThemeHelper
	{
		private const string selectedAppThemeKey = "theme";
		private static Window? currentApplicationWindow;
		private static AppWindowTitleBar? titleBar;
		private static bool isInitialized = false;

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

				return !string.IsNullOrEmpty(savedTheme)
					? EnumExtensions.GetEnum<ElementTheme>(savedTheme)
					: ElementTheme.Default;
			}
			set
			{
				ApplicationData.Current.LocalSettings.Values[selectedAppThemeKey] = value.ToString();
				ApplyTheme();
			}
		}

		public static bool Initialize()
		{
			if (isInitialized)
				return false;

			isInitialized = true;

			// Save reference as this might be null when the user is in another app
			currentApplicationWindow = MainWindow.Instance;

			// Set TitleBar background color
			if (currentApplicationWindow is not null)
				titleBar = MainWindow.Instance.AppWindow.TitleBar;

			// Apply the desired theme based on what is set in the application settings
			ApplyTheme();

			// Registering to color changes, thus we notice when user changes theme system wide
			UiSettings = new UISettings();
			UiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;

			return true;
		}

		public static void ApplyResources()
		{
			// Toggle between the themes to force reload the resource styles
			ApplyTheme(ElementTheme.Dark);
			ApplyTheme(ElementTheme.Light);

			// Restore the theme to the correct theme
			ApplyTheme();
		}

		private static async void UiSettings_ColorValuesChanged(UISettings sender, object args)
		{
			// Make sure we have a reference to our window so we dispatch a UI change
			if (currentApplicationWindow is null)
			{
				currentApplicationWindow = MainWindow.Instance;

				if (currentApplicationWindow is null)
					return;
			}

			titleBar ??= MainWindow.Instance.AppWindow.TitleBar;

			// Dispatch on UI thread so that we have a current appbar to access and change
			await currentApplicationWindow.DispatcherQueue.EnqueueOrInvokeAsync(ApplyTheme);
		}

		private static void ApplyTheme()
		{
			ApplyTheme(RootTheme);
		}

		private static void ApplyTheme(ElementTheme rootTheme)
		{
			if (MainWindow.Instance.Content is FrameworkElement rootElement)
				rootElement.RequestedTheme = rootTheme;

			if (titleBar is not null)
			{
				titleBar.ButtonBackgroundColor = Colors.Transparent;
				titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

				switch (rootTheme)
				{
					case ElementTheme.Default:
						titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
						titleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
						break;

					case ElementTheme.Light:
						titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
						titleBar.ButtonForegroundColor = Colors.Black;
						break;

					case ElementTheme.Dark:
						titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
						titleBar.ButtonForegroundColor = Colors.White;
						break;
				}
			}
			Ioc.Default.GetRequiredService<SettingsViewModel>().UpdateThemeElements.Execute(null);
		}
	}
}