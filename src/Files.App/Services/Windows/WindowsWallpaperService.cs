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
			pDesktopWallpaper.CoCreateInstance<DesktopWallpaper>().ThrowOnFailure();

			pDesktopWallpaper.Get()->GetMonitorDevicePathCount(out var dwMonitorCount);

			fixed (char* pszPath = szPath)
			{
				PWSTR pMonitorID = default;

				for (uint dwIndex = 0; dwIndex < dwMonitorCount; dwIndex++)
				{
					pDesktopWallpaper.Get()->GetMonitorDevicePathAt(dwIndex, &pMonitorID);
					pDesktopWallpaper.Get()->SetWallpaper(pMonitorID, pszPath);
					pMonitorID = default;
				}
			}
		}

		/// <inheritdoc/>
		public unsafe void SetDesktopSlideshow(string[] aszPaths)
		{
			using ComPtr<IDesktopWallpaper> pDesktopWallpaper = default;
			pDesktopWallpaper.CoCreateInstance<DesktopWallpaper>().ThrowOnFailure();

			var dwCount = (uint)aszPaths.Length;

			fixed (ITEMIDLIST** idList = new ITEMIDLIST*[dwCount])
			{
				for (uint dwIndex = 0u; dwIndex < dwCount; dwIndex++)
				{
					var id = PInvoke.ILCreateFromPath(aszPaths[dwIndex]);
					idList[dwIndex] = id;
				}

				// Get shell item array from images to use for slideshow
				using ComPtr<IShellItemArray> pShellItemArray = default;
				PInvoke.SHCreateShellItemArrayFromIDLists(dwCount, idList, pShellItemArray.GetAddressOf());

				// Set slideshow
				pDesktopWallpaper.Get()->SetSlideshow(pShellItemArray.Get());
			}

			// Set wallpaper to fill desktop.
			pDesktopWallpaper.Get()->SetPosition(DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);
		}

		/// <inheritdoc/>
		public async Task SetLockScreenWallpaper(string szPath)
		{
			IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(szPath);
			await LockScreen.SetImageFileAsync(sourceFile);
		}
	}
}
