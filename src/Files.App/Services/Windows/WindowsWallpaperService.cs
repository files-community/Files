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
			PInvoke.CoCreateInstance(
				typeof(DesktopWallpaper).GUID,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				out IDesktopWallpaper* pDesktopWallpaper)
			.ThrowOnFailure();

			pDesktopWallpaper->GetMonitorDevicePathCount(out var dwMonitorCount);

			fixed (char* pszPath = szPath)
			{
				var pwszPath = new PWSTR(pszPath);

				for (uint dwIndex = 0; dwIndex < dwMonitorCount; dwIndex++)
				{
					pDesktopWallpaper->GetMonitorDevicePathAt(dwIndex, out var pMonitorID);
					pDesktopWallpaper->SetWallpaper(pMonitorID, pwszPath);
				}
			}
		}

		/// <inheritdoc/>
		public unsafe void SetDesktopSlideshow(string[] aszPaths)
		{
			PInvoke.CoCreateInstance(
				typeof(DesktopWallpaper).GUID,
				null,
				CLSCTX.CLSCTX_LOCAL_SERVER,
				out IDesktopWallpaper* pDesktopWallpaper)
			.ThrowOnFailure();

			var dwCount = (uint)aszPaths.Length;

			fixed (ITEMIDLIST** idList = new ITEMIDLIST*[dwCount])
			{
				for (uint dwIndex = 0u; dwIndex < dwCount; dwIndex++)
				{
					var id = PInvoke.ILCreateFromPath(aszPaths[dwIndex]);
					idList[dwIndex] = id;
				}

				IShellItemArray* pShellItemArray = default;
				// Get shell item array from images to use for slideshow
				PInvoke.SHCreateShellItemArrayFromIDLists(dwCount, idList, &pShellItemArray);

				// Set slideshow
				pDesktopWallpaper->SetSlideshow(pShellItemArray);
			}

			// Set wallpaper to fill desktop.
			pDesktopWallpaper->SetPosition(DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);
		}

		/// <inheritdoc/>
		public async Task SetLockScreenWallpaper(string szPath)
		{
			IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(szPath);
			await LockScreen.SetImageFileAsync(sourceFile);
		}
	}
}
