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
			var IID_DesktopWallpaper = typeof(IDesktopWallpaper).GUID;
			var CLSID_desktopWallpaper = typeof(DesktopWallpaper).GUID;

			HRESULT hr = PInvoke.CoCreateInstance(
				&CLSID_desktopWallpaper,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				&IID_DesktopWallpaper,
				(void**)pDesktopWallpaper.GetAddressOf())
			.ThrowOnFailure();

			// Get total count of all monitors available
			hr = pDesktopWallpaper.Get()->GetMonitorDevicePathCount(out var dwMonitorCount)
				.ThrowOnFailure();

			fixed (char* pszPath = szPath)
			{
				PWSTR pMonitorID = default;

				// Set the selected image file as wallpaper to all monitors available
				for (uint dwIndex = 0u; dwIndex < dwMonitorCount; dwIndex++)
				{
					hr = pDesktopWallpaper.Get()->GetMonitorDevicePathAt(dwIndex, &pMonitorID).ThrowOnFailure();
					hr = pDesktopWallpaper.Get()->SetWallpaper(pMonitorID, pszPath).ThrowOnFailure();

					pMonitorID = default;
				}
			}
		}

		/// <inheritdoc/>
		public unsafe void SetDesktopSlideshow(string[] aszPaths)
		{
			using ComPtr<IDesktopWallpaper> pDesktopWallpaper = default;
			var IID_DesktopWallpaper = typeof(IDesktopWallpaper).GUID;
			var CLSID_desktopWallpaper = typeof(DesktopWallpaper).GUID;

			HRESULT hr = PInvoke.CoCreateInstance(
				&CLSID_desktopWallpaper,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				&IID_DesktopWallpaper,
				(void**)pDesktopWallpaper.GetAddressOf())
			.ThrowOnFailure();

			var dwCount = (uint)aszPaths.Length;

			fixed (ITEMIDLIST** ppItemIdList = new ITEMIDLIST*[dwCount])
			{
				for (uint dwIndex = 0u; dwIndex < dwCount; dwIndex++)
				{
					var id = PInvoke.ILCreateFromPath(aszPaths[dwIndex]);
					ppItemIdList[dwIndex] = id;
				}

				// Get a shell array of the selected image files
				using ComPtr<IShellItemArray> pShellItemArray = default;
				PInvoke.SHCreateShellItemArrayFromIDLists(dwCount, ppItemIdList, pShellItemArray.GetAddressOf());

				// Set the slideshow
				hr = pDesktopWallpaper.Get()->SetSlideshow(pShellItemArray.Get());
			}

			// Set wallpaper position to fill the monitor.
			hr = pDesktopWallpaper.Get()->SetPosition(DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);
		}

		/// <inheritdoc/>
		public async Task SetLockScreenWallpaper(string szPath)
		{
			IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(szPath);
			await LockScreen.SetImageFileAsync(sourceFile);
		}
	}
}
