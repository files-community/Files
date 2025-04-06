// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Files.App.Services;
using Files.App.Services.Settings;
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
			var appThemeToolbarBackgroundColor = appearance.AppThemeToolbarBackgroundColor;
			var appThemeSidebarBackgroundColor = appearance.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = appearance.AppThemeFileAreaBackgroundColor;
			var appThemeFileAreaSecondaryBackgroundColor = appearance.AppThemeFileAreaSecondaryBackgroundColor;
			var appThemeInfoPaneBackgroundColor = appearance.AppThemeInfoPaneBackgroundColor;
			var appThemeFontFamily = appearance.AppThemeFontFamily;

			try
			{
				service.SetAppThemeBackgroundColor(appThemeBackgroundColor.ToColor());
			}
			catch
			{
				appearance.AppThemeBackgroundColor = "#00000000"; // reset to default
				service.SetAppThemeBackgroundColor("#00000000".ToColor());
			}

			if (!string.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor))
			{
				try
				{
					service.SetAppThemeAddressBarBackgroundColor(appThemeAddressBarBackgroundColor.ToColor());
				}
				catch
				{
					appearance.AppThemeAddressBarBackgroundColor = ""; // reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeToolbarBackgroundColor))
			{
				try
				{
					service.SetAppThemeToolbarBackgroundColor(appThemeToolbarBackgroundColor.ToColor());
				}
				catch
				{
					appearance.AppThemeAddressBarBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeSidebarBackgroundColor))
			{
				try
				{
					service.SetAppThemeSidebarBackgroundColor(appThemeSidebarBackgroundColor.ToColor());
				}
				catch
				{
					appearance.AppThemeSidebarBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeFileAreaBackgroundColor))
			{
				try
				{
					service.SetAppThemeFileAreaBackgroundColor(appThemeFileAreaBackgroundColor.ToColor());
				}
				catch
				{
					appearance.AppThemeFileAreaBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeFileAreaSecondaryBackgroundColor))
			{
				try
				{
					service.SetAppThemeFileAreaSecondaryBackgroundColor(appThemeFileAreaSecondaryBackgroundColor.ToColor());
				}
				catch
				{
					appearance.AppThemeFileAreaSecondaryBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeInfoPaneBackgroundColor))
			{
				try
				{
					service.SetAppThemeInfoPaneBackgroundColor(appThemeInfoPaneBackgroundColor.ToColor());
				}
				catch
				{
					appearance.AppThemeInfoPaneBackgroundColor = ""; //reset to default
				}
			}

			if (appThemeFontFamily != Constants.Appearance.StandardFont)
				service.SetAppThemeFontFamily(appThemeFontFamily);

			service.ApplyResources();
		}
	}
}
