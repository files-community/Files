// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Files.Core.Services;
using Files.Core.Services.Settings;
using System;

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
			var appThemeBackgroundColor = appearance.AppThemeBackgroundColor;
			var appThemeAddressBarBackgroundColor = appearance.AppThemeAddressBarBackgroundColor;
			var appThemeSidebarBackgroundColor = appearance.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = appearance.AppThemeFileAreaBackgroundColor;
			var appThemeFontFamily = appearance.AppThemeFontFamily;

			service.SetCompactSpacing(useCompactStyles);
			try
			{
				service.SetAppThemeBackgroundColor(ColorHelper.ToColor(appThemeBackgroundColor).FromWindowsColor());
			}
			catch
			{
				appearance.AppThemeBackgroundColor = "#00000000"; //migrate to new default
				service.SetAppThemeBackgroundColor(ColorHelper.ToColor("#00000000").FromWindowsColor());
			}

			if (!string.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
			{
				try
				{
					service.SetAppThemeAddressBarBackgroundColor(ColorHelper.ToColor(appThemeAddressBarBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeAddressBarBackgroundColor = ""; //migrate to new default
				}
			}
			else
				appearance.AppThemeAddressBarBackgroundColor = ""; //migrate to new default

			if (!string.IsNullOrWhiteSpace(appThemeSidebarBackgroundColor) && appThemeSidebarBackgroundColor != "#00000000")
			{
				try
				{
					service.SetAppThemeSidebarBackgroundColor(ColorHelper.ToColor(appThemeSidebarBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeSidebarBackgroundColor = ""; //migrate to new default
				}
			}
			else
				appearance.AppThemeSidebarBackgroundColor = ""; //migrate to new default

			if (!string.IsNullOrWhiteSpace(appThemeFileAreaBackgroundColor) && appThemeFileAreaBackgroundColor != "#00000000")
			{
				try
				{
					service.SetAppThemeFileAreaBackgroundColor(ColorHelper.ToColor(appThemeFileAreaBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeFileAreaBackgroundColor = ""; //migrate to new default
				}
			}
			else
				appearance.AppThemeFileAreaBackgroundColor = ""; //migrate to new default

			if (appThemeFontFamily != Constants.Appearance.StandardFont)
				service.SetAppThemeFontFamily(appThemeFontFamily);

			service.ApplyResources();
		}
	}
}
