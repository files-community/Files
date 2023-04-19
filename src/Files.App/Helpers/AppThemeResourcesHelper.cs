using CommunityToolkit.WinUI.Helpers;
using Files.Backend.Services;
using Files.Backend.Services.Settings;

namespace Files.App.Helpers
{
	public static class AppThemeResourcesHelper
	{
		/// <summary>
		/// Loads the resource styles from settings
		/// </summary>
		public static void LoadAppResources(this IResourcesService service, IAppearanceSettingsService appearance)
		{
			var useCompactStyles = appearance.UseCompactStyles;
			var appThemeBackgroundColor = ColorHelper.ToColor(appearance.AppThemeBackgroundColor);
			var appThemeAddressBarBackgroundColor = appearance.AppThemeAddressBarBackgroundColor;
			var appThemeSidebarBackgroundColor = appearance.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = appearance.AppThemeFileAreaBackgroundColor;
			var appThemeFontFamily = appearance.AppThemeFontFamily;

			service.SetCompactSpacing(useCompactStyles);
			service.SetAppThemeBackgroundColor(appThemeBackgroundColor.FromWindowsColor());

			if (!String.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeAddressBarBackgroundColor(ColorHelper.ToColor(appThemeAddressBarBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeAddressBarBackgroundColor = ""; //migrate to new default

			if (!String.IsNullOrWhiteSpace(appThemeSidebarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeSidebarBackgroundColor(ColorHelper.ToColor(appThemeSidebarBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeSidebarBackgroundColor = ""; //migrate to new default

			if (!String.IsNullOrWhiteSpace(appThemeFileAreaBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeFileAreaBackgroundColor(ColorHelper.ToColor(appThemeFileAreaBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeFileAreaBackgroundColor = ""; //migrate to new default

			if (appThemeFontFamily != "Segoe UI Variable")
				service.SetAppThemeFontFamily(appThemeFontFamily);

			service.ApplyResources();
		}
	}
}
