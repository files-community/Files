// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Files.App.Services
{
	public class AppThemeModeService : IAppThemeModeService
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields & Properties

		private readonly UISettings UISettings = new();

		public AppThemeMode ThemeMode
		{
			get
			{
				var theme = UserSettingsService.AppearanceSettingsService.AppThemeMode;

				return EnumExtensions.GetEnum<AppThemeMode>(theme);
			}
			set
			{
				UserSettingsService.AppearanceSettingsService.AppThemeMode = value.ToString();

				SetThemeMode();
			}
		}

		// Events

		public event EventHandler? ThemeModeChanged;

		// Constructor

		public AppThemeModeService()
		{
			// Set the desired theme based on what is set in the application settings
			SetThemeMode();

			// Registering to color changes, so that we can notice when changed system theme mode
			UISettings.ColorValuesChanged += UISettings_ColorValuesChanged;
		}

		// Methods

		public void RefreshThemeMode()
		{
			// Toggle between the themes to force reload the resource styles
			SetThemeMode(null, null, ElementTheme.Dark);
			SetThemeMode(null, null, ElementTheme.Light);

			// Restore the theme to the correct theme
			SetThemeMode();
		}

		private void SetThemeMode(Window? window = null, AppWindowTitleBar? titleBar = null, ElementTheme? rootTheme = null, bool callThemeModeChangedEvent = true)
		{
			window ??= MainWindow.Instance;
			titleBar ??= MainWindow.Instance.AppWindow.TitleBar;
			rootTheme ??= (ElementTheme)ThemeMode;

			if (window.Content is FrameworkElement rootElement)
				rootElement.RequestedTheme = (ElementTheme)rootTheme;

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

			if (callThemeModeChangedEvent)
				ThemeModeChanged?.Invoke(null, EventArgs.Empty);
		}

		private async void UISettings_ColorValuesChanged(UISettings sender, object args)
		{
			// Dispatch on UI thread so that we have a current app bar to access and change
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				SetThemeMode();
			});
		}
	}
}
