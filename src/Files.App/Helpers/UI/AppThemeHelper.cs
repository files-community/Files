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

		private static Window? _currentApplicationWindow;

		private static AppWindowTitleBar? _titleBar;

		private static bool _isInitialized = false;

		// Keep reference so it does not get optimized/garbage collected
		private static UISettings? UISettings;

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

			// Save reference as this might be null when the user is in another app
			_currentApplicationWindow = MainWindow.Instance;

			// Set TitleBar background color
			if (_currentApplicationWindow is not null)
				_titleBar = MainWindow.Instance.AppWindow.TitleBar;

			// Apply the desired theme based on what is set in the application settings
			ApplyTheme();

			// Registering to color changes, thus we notice when user changes theme system wide
			UISettings = new UISettings();
			UISettings.ColorValuesChanged += UISettings_ColorValuesChanged;

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

		private static async void UISettings_ColorValuesChanged(UISettings sender, object args)
		{
			// Make sure we have a reference to our window so we dispatch a UI change
			if (_currentApplicationWindow is null)
			{
				_currentApplicationWindow = MainWindow.Instance;

				if (_currentApplicationWindow is null)
					return;
			}

			_titleBar ??= MainWindow.Instance.AppWindow.TitleBar;

			// Dispatch on UI thread so that we have a current app bar to access and change
			await _currentApplicationWindow.DispatcherQueue.EnqueueOrInvokeAsync(ApplyTheme);
		}

		private static void ApplyTheme()
		{
			ApplyTheme(RootTheme);
		}

		private static void ApplyTheme(ElementTheme rootTheme)
		{
			if (MainWindow.Instance.Content is FrameworkElement rootElement)
				rootElement.RequestedTheme = rootTheme;

			if (_titleBar is not null)
			{
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
			}

			UpdateThemeElements?.Execute(null);
		}
	}
}
