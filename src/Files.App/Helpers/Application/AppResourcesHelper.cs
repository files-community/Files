// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.Helpers
{
	internal static class AppResourcesHelper
	{
		private static IEnumerable<IconFileInfo> SidebarIconResources = LoadSidebarIconResources();
		private static IconFileInfo ShieldIconResource = LoadShieldIconResource();

		/// <summary>
		/// Loads the resource styles from settings
		/// </summary>
		public static void LoadAppResources(this IResourcesService service, IAppearanceSettingsService appearance)
		{
			var useCompactStyles = appearance.UseCompactStyles;
			var appThemeBackgroundColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(appearance.AppThemeBackgroundColor);
			var appThemeAddressBarBackgroundColor = appearance.AppThemeAddressBarBackgroundColor;
			var appThemeSidebarBackgroundColor = appearance.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = appearance.AppThemeFileAreaBackgroundColor;
			var appThemeFontFamily = appearance.AppThemeFontFamily;

			service.SetCompactSpacing(useCompactStyles);
			service.SetAppThemeBackgroundColor(appThemeBackgroundColor.FromWindowsColor());

			if (!string.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeAddressBarBackgroundColor(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(appThemeAddressBarBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeAddressBarBackgroundColor = ""; //migrate to new default

			if (!string.IsNullOrWhiteSpace(appThemeSidebarBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeSidebarBackgroundColor(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(appThemeSidebarBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeSidebarBackgroundColor = ""; //migrate to new default

			if (!string.IsNullOrWhiteSpace(appThemeFileAreaBackgroundColor) && appThemeAddressBarBackgroundColor != "#00000000")
				service.SetAppThemeFileAreaBackgroundColor(CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor(appThemeFileAreaBackgroundColor).FromWindowsColor());
			else
				appearance.AppThemeFileAreaBackgroundColor = ""; //migrate to new default

			if (appThemeFontFamily != Constants.Appearance.StandardFont)
				service.SetAppThemeFontFamily(appThemeFontFamily);

			service.ApplyResources();
		}

		public static IconFileInfo GetSidebarIconResourceInfo(int index)
		{
			var icons = SidebarIconResources;

			return icons?.FirstOrDefault(x => x.Index == index);
		}

		public static async Task<BitmapImage?> GetSidebarIconResource(int index)
		{
			var iconInfo = GetSidebarIconResourceInfo(index);

			return iconInfo is not null
				? await iconInfo.IconData.ToBitmapAsync()
				: null;
		}

		public static async Task<BitmapImage?> GetShieldIconResource()
		{
			return ShieldIconResource is not null
				? await ShieldIconResource.IconData.ToBitmapAsync()
				: null;
		}

		private static IEnumerable<IconFileInfo> LoadSidebarIconResources()
		{
			string imageRes = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");

			var imageResList = Win32API.ExtractSelectedIconsFromDLL(
				imageRes,
				new List<int>()
				{
					Constants.ImageRes.RecycleBin,
					Constants.ImageRes.NetworkDrives,
					Constants.ImageRes.Libraries,
					Constants.ImageRes.ThisPC,
					Constants.ImageRes.CloudDrives,
					Constants.ImageRes.Folder,
					Constants.ImageRes.OneDrive
				},
				32);

			return imageResList;
		}

		private static IconFileInfo LoadShieldIconResource()
		{
			string imageRes = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");

			var imageResList = Win32API.ExtractSelectedIconsFromDLL(
				imageRes,
				new List<int>()
				{
					Constants.ImageRes.ShieldIcon
				},
				16);

			return imageResList.First();
		}
	}
}
