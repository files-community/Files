// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
			using ComPtr<IDesktopWallpaper> pDesktopWallpaper = default;
			var CLSID_DesktopWallpaper = typeof(DesktopWallpaper).GUID;
			var IID_DesktopWallpaper = typeof(IDesktopWallpaper).GUID;

			HRESULT hr = PInvoke.CoCreateInstance(
				&CLSID_DesktopWallpaper,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				&IID_DesktopWallpaper,
				(void**)pDesktopWallpaper.GetAddressOf())
			.ThrowOnFailure();

			// Get total count of all available monitors
			hr = pDesktopWallpaper.Get()->GetMonitorDevicePathCount(out var dwMonitorCount).ThrowOnFailure();

			fixed (char* pszPath = szPath)
			{
				PWSTR pMonitorId = default;

				// Set the selected image file as wallpaper for all available monitors
				for (uint dwIndex = 0u; dwIndex < dwMonitorCount; dwIndex++)
				{
					// Set the wallpaper
					hr = pDesktopWallpaper.Get()->GetMonitorDevicePathAt(dwIndex, &pMonitorId).ThrowOnFailure();
					hr = pDesktopWallpaper.Get()->SetWallpaper(pMonitorId, pszPath).ThrowOnFailure();

					pMonitorId = default;
				}
			}
		}

		/// <inheritdoc/>
		public unsafe void SetDesktopSlideshow(string[] aszPaths)
		{
			using ComPtr<IDesktopWallpaper> pDesktopWallpaper = default;
			var CLSID_DesktopWallpaper = typeof(DesktopWallpaper).GUID;
			var IID_DesktopWallpaper = typeof(IDesktopWallpaper).GUID;

			HRESULT hr = PInvoke.CoCreateInstance(
				&CLSID_DesktopWallpaper,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				&IID_DesktopWallpaper,
				(void**)pDesktopWallpaper.GetAddressOf())
			.ThrowOnFailure();

			ITEMIDLIST** ppItemIdList = stackalloc ITEMIDLIST*[aszPaths.Length];

			// Get array of PIDL from the selected image files
			for (uint dwIndex = 0u; dwIndex < (uint)aszPaths.Length; dwIndex++)
				ppItemIdList[dwIndex] = PInvoke.ILCreateFromPath(aszPaths[dwIndex]);

			// Get a shell array of the array of the PIDL
			using ComPtr<IShellItemArray> pShellItemArray = default;
			hr = PInvoke.SHCreateShellItemArrayFromIDLists(dwCount, ppItemIdList, pShellItemArray.GetAddressOf()).ThrowOnFailure();

			// Set the slideshow
			hr = pDesktopWallpaper.Get()->SetSlideshow(pShellItemArray.Get()).ThrowOnFailure();

			// Free the allocated PIDL
			for (uint dwIndex = 0u; dwIndex < (uint)aszPaths.Length; dwIndex++)
				PInvoke.ILFree(ppItemIdList[dwIndex]);

			// Set wallpaper position to fill the monitor.
			hr = pDesktopWallpaper.Get()->SetPosition(DESKTOP_WALLPAPER_POSITION.DWPOS_FILL).ThrowOnFailure();
		}

		/// <inheritdoc/>
		public async Task SetLockScreenWallpaper(string szPath)
		{
			IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(szPath);
			await LockScreen.SetImageFileAsync(sourceFile);
		}
	}
}
