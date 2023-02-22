using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using Files.Core.Services.Settings;
using Microsoft.UI.Xaml;
using System;
using Windows.UI;

namespace Files.App.Helpers
{
	public sealed class AppThemeResourcesHelper
	{
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		/// <summary>
		/// Applies updated resource styles
		/// </summary>
		public void ApplyResources()
		{
			// Get the index of the current theme
			var selTheme = ThemeHelper.RootTheme;

			// Toggle between the themes to force reload the resource styles
			ThemeHelper.RootTheme = ElementTheme.Dark;
			ThemeHelper.RootTheme = ElementTheme.Light;

			// Restore the theme to the correct theme
			ThemeHelper.RootTheme = selTheme;
		}

		/// <summary>
		/// Overrides the xaml resource for App.Theme.BackgroundBrush
		/// </summary>
		/// <param name="appThemeBackgroundColor"></param>
		public void SetAppThemeBackgroundColor(Color appThemeBackgroundColor)
		{
			Application.Current.Resources["App.Theme.BackgroundBrush"] = appThemeBackgroundColor;
		}

		/// <summary>
		/// Overrides the xaml resource for App.Theme.AddressBar.BackgroundBrush
		/// </summary>
		/// <param name="appThemeAddressBarBackgroundColor"></param>
		public void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.AddressBar.BackgroundBrush"] = appThemeAddressBarBackgroundColor;

			// Overrides the selected tab background to match the address bar
			Application.Current.Resources["TabViewItemHeaderBackgroundSelected"] = appThemeAddressBarBackgroundColor;
		}

		/// <summary>
		/// Overrides the xaml resource for App.Theme.Sidebar.BackgroundBrush
		/// </summary>
		/// <param name="appThemeSidebarBackgroundColor"></param>
		public void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.Sidebar.BackgroundBrush"] = appThemeSidebarBackgroundColor;
		}

		/// <summary>
		/// Overrides the xaml resource for App.Theme.FileArea.BackgroundBrush
		/// </summary>
		/// <param name="appThemeFileAreaBackgroundColor"></param>
		public void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor)
		{
			Application.Current.Resources["App.Theme.FileArea.BackgroundBrush"] = appThemeFileAreaBackgroundColor;
		}

		/// <summary>
		/// Overrides the xaml resource for ContentControlThemeFontFamily
		/// </summary>
		/// <param name="contentControlThemeFontFamily"></param>
		public void SetAppThemeFontFamily(string contentControlThemeFontFamily)
		{
			Application.Current.Resources["ContentControlThemeFontFamily"] = contentControlThemeFontFamily;
		}

		/// <summary>
		/// Overrides the xaml resource for the list view item height
		/// </summary>
		/// <param name="useCompactSpacing"></param>
		public void SetCompactSpacing(bool useCompactSpacing)
		{
			var listItemHeight = useCompactSpacing ? 24 : 36;
			var navigationViewItemOnLeftMinHeight = useCompactSpacing ? 20 : 32;

			Application.Current.Resources["ListItemHeight"] = listItemHeight;
			Application.Current.Resources["NavigationViewItemOnLeftMinHeight"] = navigationViewItemOnLeftMinHeight;
		}

		/// <summary>
		/// Loads the resource styles from settings
		/// </summary>
		public void LoadAppResources()
		{
			var useCompactStyles = UserSettingsService.AppearanceSettingsService.UseCompactStyles;
			var appThemeBackgroundColor = ColorHelper.ToColor(UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor);
			var appThemeAddressBarBackgroundColor = UserSettingsService.AppearanceSettingsService.AppThemeAddressBarBackgroundColor;
			var appThemeSidebarBackgroundColor = UserSettingsService.AppearanceSettingsService.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = UserSettingsService.AppearanceSettingsService.AppThemeFileAreaBackgroundColor;
			var appThemeFontFamily = UserSettingsService.AppearanceSettingsService.AppThemeFontFamily;

			SetCompactSpacing(useCompactStyles);
			SetAppThemeBackgroundColor(appThemeBackgroundColor);

			if (!String.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				SetAppThemeAddressBarBackgroundColor(ColorHelper.ToColor(appThemeAddressBarBackgroundColor));
			else
				UserSettingsService.AppearanceSettingsService.AppThemeAddressBarBackgroundColor = ""; //migrate to new default

			if (!String.IsNullOrWhiteSpace(appThemeSidebarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				SetAppThemeSidebarBackgroundColor(ColorHelper.ToColor(appThemeSidebarBackgroundColor));
			else
				UserSettingsService.AppearanceSettingsService.AppThemeSidebarBackgroundColor = ""; //migrate to new default

			if (!String.IsNullOrWhiteSpace(appThemeFileAreaBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				SetAppThemeFileAreaBackgroundColor(ColorHelper.ToColor(appThemeFileAreaBackgroundColor));
			else
				UserSettingsService.AppearanceSettingsService.AppThemeFileAreaBackgroundColor = ""; //migrate to new default

			if (appThemeFontFamily != "Segoe UI Variable")
				SetAppThemeFontFamily(appThemeFontFamily);

			ApplyResources();
		}
	}
}
