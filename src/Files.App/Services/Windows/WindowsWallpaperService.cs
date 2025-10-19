// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
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
		private static readonly Ole32.PROPERTYKEY PKEY_FilePlaceholderStatus = new Ole32.PROPERTYKEY(new Guid("B2F9B9D6-FEC4-4DD5-94D7-8957488C807B"), 2);
		private const uint PS_CLOUDFILE_PLACEHOLDER = 8;
		/// <inheritdoc/>
		public unsafe void SetDesktopWallpaper(string szPath)
		{
			// Instantiate IDesktopWallpaper
			using ComPtr<IDesktopWallpaper> pDesktopWallpaper = default;
			HRESULT hr = pDesktopWallpaper.CoCreateInstance(CLSID.CLSID_DesktopWallpaper).ThrowOnFailure();

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
			// Instantiate IDesktopWallpaper
			using ComPtr<IDesktopWallpaper> pDesktopWallpaper = default;
			HRESULT hr = pDesktopWallpaper.CoCreateInstance(CLSID.CLSID_DesktopWallpaper).ThrowOnFailure();

			uint dwCount = (uint)aszPaths.Length;
			ITEMIDLIST** ppItemIdList = stackalloc ITEMIDLIST*[aszPaths.Length];

			// Get an array of PIDL from the selected image files
			for (uint dwIndex = 0u; dwIndex < dwCount; dwIndex++)
				ppItemIdList[dwIndex] = PInvoke.ILCreateFromPath(aszPaths[dwIndex]);

			// Get an IShellItemArray from the array of the PIDL
			using ComPtr<IShellItemArray> pShellItemArray = default;
			hr = PInvoke.SHCreateShellItemArrayFromIDLists(dwCount, ppItemIdList, pShellItemArray.GetAddressOf()).ThrowOnFailure();

			// Release the allocated PIDL
			for (uint dwIndex = 0u; dwIndex < dwCount; dwIndex++)
				PInvoke.CoTaskMemFree((void*)ppItemIdList[dwIndex]);

			// Set the slideshow and its position
			hr = pDesktopWallpaper.Get()->SetSlideshow(pShellItemArray.Get()).ThrowOnFailure();
			hr = pDesktopWallpaper.Get()->SetPosition(DESKTOP_WALLPAPER_POSITION.DWPOS_FILL).ThrowOnFailure();
		}

		/// <inheritdoc/>
		public async Task SetLockScreenWallpaper(string szPath)
		{
			// Verify the file exists on disk
			if (!File.Exists(szPath))
				throw new FileNotFoundException("The specified file does not exist.", szPath);

			// Check if the file is a cloud placeholder (online-only file)
			if (IsCloudPlaceholder(szPath))
				throw new InvalidOperationException("The file is stored in the cloud and is not available offline. Please download the file before setting it as a wallpaper.");

			IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(szPath);
			await LockScreen.SetImageFileAsync(sourceFile);
		}

		/// <summary>
		/// Checks if the file is a cloud placeholder (online-only file).
		/// </summary>
		/// <param name="path">The path to the file.</param>
		/// <returns>True if the file is a cloud placeholder; otherwise, false.</returns>
		private static bool IsCloudPlaceholder(string path)
		{
			try
			{
				using var shi = new ShellItem(path);
				if (shi.Properties.TryGetValue<uint>(PKEY_FilePlaceholderStatus, out var value) && value == PS_CLOUDFILE_PLACEHOLDER)
					return true;
			}
			catch
			{
				// If we can't determine the placeholder status, assume it's not a placeholder
				// and let the subsequent StorageFile.GetFileFromPathAsync call handle any errors
			}

			return false;
		}
	}
}
