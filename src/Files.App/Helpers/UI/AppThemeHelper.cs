// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for switching and restoring app theme settings.
	/// </summary>
	public static class AppThemeHelper
	{
		private static readonly IUserSettingsService _userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private static bool _isInitialized = false;

		private static UISettings? uiSettings;

		/// <summary>
		/// Gets the UI theme mode that is used by the root window content for resource determination.
		/// </summary>
		/// <remarks>
		/// The UI theme mode specified with RequestedTheme can override the app-level RequestedTheme.
		/// </remarks>
		public static ElementTheme RootTheme =>
			!string.IsNullOrEmpty(_userSettingsService.AppearanceSettingsService.AppThemeMode)
				? EnumExtensions.GetEnum<ElementTheme>(_userSettingsService.AppearanceSettingsService.AppThemeMode)
				: ElementTheme.Default;

		public static event EventHandler? ThemeModeChanged;

		/// <summary>
		/// Initializes a new <see cref="AppThemeHelper"/> instance.
		/// </summary>
		/// <remarks>
		/// Once this class was initialized, this class doesn't need to be initialized again in somewhere else.
		/// </remarks>
		/// <returns>Returns true if initialization succeed; otherwise false.</returns>
		public static bool Initialize()
		{
			if (_isInitialized)
				return false;
			else
				_isInitialized = true;

			// Apply the desired theme based on what is set in the application settings
			ApplyTheme();

			// Registering to color changes, thus we notice when user changes theme system wide
			uiSettings = new UISettings();
			uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;

			return true;
		}

		/// <summary>
		/// Refreshes theme mode.
		/// </summary>
		/// <remarks>
		/// To reload the app resources, the theme will be toggled between dark and light, and the correct theme will be applied.
		/// </remarks>
		public static void RefreshThemeMode()
		{
			// Toggle between the themes to force reload the resource styles
			ApplyTheme(MainWindow.Instance, ElementTheme.Dark);
			ApplyTheme(MainWindow.Instance, ElementTheme.Light);

			// Restore the theme to the correct theme
			ApplyTheme();
		}

		/// <summary>
		/// Applies theme mode to the requested theme of a specific window.
		/// </summary>
		/// <param name="window">A window whose requested theme will be changed.</param>
		/// <param name="rootTheme">Requested theme mode</param>
		/// <param name="executeChangedEvent">Determines if the event should be triggered. if the main window's requested theme will be changed, the event should be called</param>
		public static void ApplyTheme(Window? window = null, ElementTheme? rootTheme = null, bool executeChangedEvent = true)
		{
			// Validate variables
			window ??= MainWindow.Instance;
			rootTheme ??= RootTheme;

			// Set theme mode on the window
			if (window.Content is FrameworkElement rootElement)
				rootElement.RequestedTheme = (ElementTheme)rootTheme;

			// Store theme mode setting
			_userSettingsService.AppearanceSettingsService.AppThemeMode = rootTheme.ToString()!;

			// Update title bar buttons color
			var titleBar = window.AppWindow.TitleBar;
			if (titleBar is not null)
			{
				// Remove normal state background
				titleBar.ButtonBackgroundColor = Colors.Transparent;
				titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

				// Pointer over state background
				titleBar.ButtonHoverBackgroundColor = rootTheme switch
				{
					ElementTheme.Light => titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x33, 0x00, 0x00, 0x00),
					ElementTheme.Dark => titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF),
					_ => titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"],
				};

				// Pointer over state foreground
				titleBar.ButtonForegroundColor = rootTheme switch
				{
					ElementTheme.Light => titleBar.ButtonHoverBackgroundColor = Colors.Black,
					ElementTheme.Dark => titleBar.ButtonHoverBackgroundColor = Colors.White,
					_ => titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"],
				};
			}

			// Trigger theme mode changed event
			if (executeChangedEvent)
				ThemeModeChanged?.Invoke(null, EventArgs.Empty);
		}

		private static async void UISettings_ColorValuesChanged(UISettings sender, object args)
		{
			// Dispatch on UI thread so that we have a current app bar to access and change
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => ApplyTheme());
		}
	}
}
