// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;
using Windows.System.UserProfile;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsWallpaperService"/>
	public sealed class WindowsWallpaperService : IWindowsWallpaperService
	{
		/// <inheritdoc/>
		public unsafe void SetDesktopWallpaper(string path)
		{
			// Instantiate IDesktopWallpaper
			HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_DesktopWallpaper, null, Windows.Win32.System.Com.CLSCTX.CLSCTX_ALL, IID.IID_IDesktopWallpaper, out var desktopWallpaperObj);
			var desktopWallpaper = (IDesktopWallpaper)desktopWallpaperObj;

			// Get total count of all available monitors
			hr = desktopWallpaper.GetMonitorDevicePathCount(out var monitorCount);

			fixed (char* pathPtr = path)
			{
				PWSTR monitorId = default;

				// Set the selected image file as wallpaper for all available monitors
				for (uint index = 0u; index < monitorCount; index++)
				{
					// Set the wallpaper
					hr = desktopWallpaper.GetMonitorDevicePathAt(index, &monitorId);
					hr = desktopWallpaper.SetWallpaper(monitorId, pathPtr);

					monitorId = default;
				}
			}
		}

		/// <inheritdoc/>
		public unsafe void SetDesktopSlideshow(string[] paths)
		{
			// Instantiate IDesktopWallpaper
			HRESULT hr = PInvoke.CoCreateInstance(CLSID.CLSID_DesktopWallpaper, null, Windows.Win32.System.Com.CLSCTX.CLSCTX_ALL, IID.IID_IDesktopWallpaper, out var desktopWallpaperObj);
			var desktopWallpaper = (IDesktopWallpaper)desktopWallpaperObj;

			uint count = (uint)paths.Length;
			ITEMIDLIST** itemIdList = stackalloc ITEMIDLIST*[paths.Length];

			// Get an array of PIDL from the selected image files
			for (uint index = 0u; index < count; index++)
				itemIdList[index] = PInvoke.ILCreateFromPath(paths[index]);

			// Get an IShellItemArray from the array of the PIDL
			hr = PInvoke.SHCreateShellItemArrayFromIDLists(count, itemIdList, out var shellItemArray);

			// Release the allocated PIDL
			for (uint index = 0u; index < count; index++)
				PInvoke.CoTaskMemFree((void*)itemIdList[index]);

			// Set the slideshow and its position
			hr = desktopWallpaper.SetSlideshow(shellItemArray);
			hr = desktopWallpaper.SetPosition(DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);
		}

		/// <inheritdoc/>
		public async Task SetLockScreenWallpaper(string path)
		{
			IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(path);
			await LockScreen.SetImageFileAsync(sourceFile);
		}
	}
}
