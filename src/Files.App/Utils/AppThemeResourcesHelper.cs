// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Files.Backend.Services;

namespace Files.App.Helpers
{
	public static class AppThemeResourcesHelper
	{
		/// <summary>
		/// Loads the app theme resource styles from settings with <see cref="IResourcesService"/> and <see cref="IAppearanceSettingsService"/>.
		/// </summary>
		public static void LoadAppResources(this IResourcesService service, IAppearanceSettingsService appearance)
		{
			// Get required information
			var useCompactStyles = appearance.UseCompactStyles;
			var appThemeBackgroundColor = ColorHelper.ToColor(appearance.AppThemeBackgroundColor);
			var appThemeAddressBarBackgroundColor = appearance.AppThemeAddressBarBackgroundColor;
			var appThemeSidebarBackgroundColor = appearance.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = appearance.AppThemeFileAreaBackgroundColor;
			var appThemeFontFamily = appearance.AppThemeFontFamily;

			// Set settings
			service.SetCompactSpacing(useCompactStyles);
			service.SetAppThemeBackgroundColor(appThemeBackgroundColor.FromWindowsColor());

			// Address bar background
			if (!string.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeAddressBarBackgroundColor(ColorHelper.ToColor(appThemeAddressBarBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeAddressBarBackgroundColor = ""; // Migrate to new default

			// Sidebar background
			if (!string.IsNullOrWhiteSpace(appThemeSidebarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeSidebarBackgroundColor(ColorHelper.ToColor(appThemeSidebarBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeSidebarBackgroundColor = ""; // Migrate to new default

			// File area background
			if (!string.IsNullOrWhiteSpace(appThemeFileAreaBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeFileAreaBackgroundColor(ColorHelper.ToColor(appThemeFileAreaBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeFileAreaBackgroundColor = ""; // Migrate to new default

			// App font family
			if (appThemeFontFamily != Constants.Common.SegoeUIVariable)
				service.SetAppThemeFontFamily(appThemeFontFamily);

			service.ApplyResources();
		}
	}
}
