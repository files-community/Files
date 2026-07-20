// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;
using Windows.System.UserProfile;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsWallpaperService"/>
	public sealed class WindowsWallpaperService : IWindowsWallpaperService
	{
		/// <inheritdoc/>
		public unsafe void SetDesktopWallpaper(string szPath)
		{
			// Instantiate IDesktopWallpaper
			HRESULT hr = PInvoke.CoCreateInstance(typeof(DesktopWallpaper).GUID, null, CLSCTX.CLSCTX_LOCAL_SERVER, out IDesktopWallpaper? pDesktopWallpaper);
			if (hr.Failed || pDesktopWallpaper is null)
				return;

			// Get total count of all available monitors
			hr = pDesktopWallpaper.GetMonitorDevicePathCount(out var dwMonitorCount);

			fixed (char* pszPath = szPath)
			{
				PWSTR pMonitorId = default;

				// Set the selected image file as wallpaper for all available monitors
				for (uint dwIndex = 0u; dwIndex < dwMonitorCount; dwIndex++)
				{
					// Set the wallpaper
					hr = pDesktopWallpaper.GetMonitorDevicePathAt(dwIndex, out pMonitorId);
					hr = pDesktopWallpaper.SetWallpaper(pMonitorId.ToString(), szPath);
					PInvoke.CoTaskMemFree(pMonitorId);

					pMonitorId = default;
				}
			}
		}

		/// <inheritdoc/>
		public unsafe void SetDesktopSlideshow(string[] aszPaths)
		{
			// Instantiate IDesktopWallpaper
			HRESULT hr = PInvoke.CoCreateInstance(typeof(DesktopWallpaper).GUID, null, CLSCTX.CLSCTX_LOCAL_SERVER, out IDesktopWallpaper? pDesktopWallpaper);
			if (hr.Failed || pDesktopWallpaper is null)
				return;

			uint dwCount = (uint)aszPaths.Length;
			ITEMIDLIST** ppItemIdList = stackalloc ITEMIDLIST*[aszPaths.Length];

			// Get an array of PIDL from the selected image files
			for (uint dwIndex = 0u; dwIndex < dwCount; dwIndex++)
				ppItemIdList[dwIndex] = PInvoke.ILCreateFromPath(aszPaths[dwIndex]);

			// Get an IShellItemArray from the array of the PIDL
			hr = PInvoke.SHCreateShellItemArrayFromIDLists(dwCount, ppItemIdList, out IShellItemArray pShellItemArray);

			// Release the allocated PIDL
			for (uint dwIndex = 0u; dwIndex < dwCount; dwIndex++)
				PInvoke.CoTaskMemFree((void*)ppItemIdList[dwIndex]);

			// Set the slideshow and its position
			hr = pDesktopWallpaper.SetSlideshow(pShellItemArray);
			hr = pDesktopWallpaper.SetPosition(DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);
		}

		/// <inheritdoc/>
		public async Task SetLockScreenWallpaper(string szPath)
		{
			IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(szPath);
			await LockScreen.SetImageFileAsync(sourceFile);
		}
	}
}
