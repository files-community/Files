// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Windows.Input;
using Windows.Storage;
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

		// Keep reference so it does not get optimized/garbage collected
		private static UISettings? uiSettings;

		public static event EventHandler? ThemeModeChanged;

		public static ICommand? UpdateThemeElements;

		/// <summary>
		/// Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
		/// </summary>
		public static ElementTheme RootTheme
		{
			get
			{
				var savedTheme = _userSettingsService.AppearanceSettingsService.AppThemeMode;

				return !string.IsNullOrEmpty(savedTheme)
					? EnumExtensions.GetEnum<ElementTheme>(savedTheme)
					: ElementTheme.Default;
			}
			set
			{
				_userSettingsService.AppearanceSettingsService.AppThemeMode = value.ToString();

				ApplyTheme();
			}
		}

		public static bool Initialize()
		{
			if (_isInitialized)
				return false;

			_isInitialized = true;

			UpdateThemeElements = new RelayCommand(() => ThemeModeChanged?.Invoke(null, EventArgs.Empty));

			// Apply the desired theme based on what is set in the application settings
			ApplyTheme();

			// Registering to color changes, thus we notice when user changes theme system wide
			uiSettings = new UISettings();
			uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;

			return true;
		}

		public static void RefreshThemeMode()
		{
			// Toggle between the themes to force reload the resource styles
			ApplyTheme(MainWindow.Instance, ElementTheme.Dark);
			ApplyTheme(MainWindow.Instance, ElementTheme.Light);

			// Restore the theme to the correct theme
			ApplyTheme();
		}

		private static async void UISettings_ColorValuesChanged(UISettings sender, object args)
		{
			// Dispatch on UI thread so that we have a current app bar to access and change
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => ApplyTheme());
		}

		public static void ApplyTheme(Window? window = null, ElementTheme? rootTheme = null, bool executeChangedEvent = true)
		{
			window ??= MainWindow.Instance;

			rootTheme ??= RootTheme;

			if (window.Content is FrameworkElement rootElement)
				rootElement.RequestedTheme = (ElementTheme)rootTheme;

			var titleBar = window.AppWindow.TitleBar;

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

			if (executeChangedEvent)
				UpdateThemeElements?.Execute(null);
		}
	}
}
