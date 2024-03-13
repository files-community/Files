// Copyright (c) 2024 Files Community
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
			var appThemeBackgroundColor = appearance.AppThemeBackgroundColor;
			var appThemeAddressBarBackgroundColor = appearance.AppThemeAddressBarBackgroundColor;
			var appThemeSidebarBackgroundColor = appearance.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = appearance.AppThemeFileAreaBackgroundColor;
			var appThemeFontFamily = appearance.AppThemeFontFamily;

			try
			{
				service.SetAppThemeBackgroundColor(appThemeBackgroundColor.ToColor().FromWindowsColor());
			}
			catch
			{
				appearance.AppThemeBackgroundColor = "#00000000"; //migrate to new default
				service.SetAppThemeBackgroundColor("#00000000".ToColor().FromWindowsColor());
			}

			if (!string.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
			{
				try
				{
					service.SetAppThemeAddressBarBackgroundColor(appThemeAddressBarBackgroundColor.ToColor().FromWindowsColor());
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
					service.SetAppThemeSidebarBackgroundColor(appThemeSidebarBackgroundColor.ToColor().FromWindowsColor());
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
					service.SetAppThemeFileAreaBackgroundColor(appThemeFileAreaBackgroundColor.ToColor().FromWindowsColor());
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
