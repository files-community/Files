using CommunityToolkit.WinUI;
using Files.Backend.Extensions;
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
		private static Window currentApplicationWindow;
		private static AppWindowTitleBar titleBar;

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

		public static void Initialize()
		{
			// Save reference as this might be null when the user is in another app
			currentApplicationWindow = App.Window;

			// Set TitleBar background color
			titleBar = App.GetAppWindow(currentApplicationWindow).TitleBar;

			// Apply the desired theme based on what is set in the application settings
			ApplyTheme();

			// Registering to color changes, thus we notice when user changes theme system wide
			UiSettings = new UISettings();
			UiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
		}

		private static async void UiSettings_ColorValuesChanged(UISettings sender, object args)
		{
			// Make sure we have a reference to our window so we dispatch a UI change
			if (currentApplicationWindow is not null)
			{
				// Dispatch on UI thread so that we have a current appbar to access and change
				await currentApplicationWindow.DispatcherQueue.EnqueueAsync(() =>
				{
					ApplyTheme();
				});
			}
		}

		private static void ApplyTheme()
		{
			var rootTheme = RootTheme;

			if (App.Window.Content is FrameworkElement rootElement)
			{
				rootElement.RequestedTheme = rootTheme;
			}

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
			App.AppSettings.UpdateThemeElements.Execute(null);
		}
	}
}