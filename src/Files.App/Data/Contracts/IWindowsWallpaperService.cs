// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service for manipulating shell wallpapers on Windows.
	/// </summary>
	public interface IWindowsWallpaperService
	{
		/// <summary>
		/// Sets desktop wallpaper using the specified image path.
		/// </summary>
		/// <param name="szPath">The image path to assign as the desktop wallpaper.</param>
		void SetDesktopWallpaper(string szPath);

		/// <summary>
		/// Sets desktop slideshow using the specified image paths.
		/// </summary>
		/// <param name="aszPaths">The image paths to use to set as slideshow.</param>
		void SetDesktopSlideshow(string[] aszPaths);

		/// <summary>
		/// Gets lock screen wallpaper using the specified image path.
		/// </summary>
		/// <param name="szPath">The image path to use to set as lock screen wallpaper.</param>
		Task SetLockScreenWallpaper(string szPath);
	}
}
