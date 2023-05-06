// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Vanara.PInvoke;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Files.App.Helpers
{
	public static class WallpaperHelpers
	{
		public static async Task SetAsBackground(WallpaperType type, string filePath)
		{
			if (type == WallpaperType.Desktop)
			{
				// Set the desktop background
				var wallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();
				wallpaper.GetMonitorDevicePathAt(0, out var monitorId);
				wallpaper.SetWallpaper(monitorId, filePath);
			}
			else if (type == WallpaperType.LockScreen)
			{
				// Set the lockscreen background
				IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(filePath);
				await LockScreen.SetImageFileAsync(sourceFile);
			}
		}

		public static void SetSlideshow(string[] filePaths)
		{
			if (filePaths is null || !filePaths.Any())
				return;

			var idList = filePaths.Select(Shell32.IntILCreateFromPath).ToArray();
			Shell32.SHCreateShellItemArrayFromIDLists((uint)idList.Length, idList.ToArray(), out var shellItemArray);

			// Set SlideShow
			var wallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();
			wallpaper.SetSlideshow(shellItemArray);

			// Set wallpaper to fill desktop.
			wallpaper.SetPosition(Shell32.DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);
		}
	}
}
