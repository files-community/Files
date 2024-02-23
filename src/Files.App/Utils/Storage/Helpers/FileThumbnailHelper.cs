// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class FileThumbnailHelper
	{
		/// <summary>
		/// Returns icon or thumbnail for given file or folder
		/// </summary>
		public static async Task<(byte[] IconData, bool isIconCached)> GetIconAsync(string path, uint requestedSize, bool isFolder, bool getThumbnailOnly, IconOptions iconOptions)
		{
			var size = iconOptions.HasFlag(IconOptions.UseCurrentScale) ? requestedSize * App.AppModel.AppWindowDPI : requestedSize;
			var getIconOnly = iconOptions.HasFlag(IconOptions.ReturnIconOnly);

			return await Win32API.StartSTATask(() => Win32API.GetIcon(path, (int)size, isFolder, getThumbnailOnly, getIconOnly));
		}

		/// <summary>
		/// Returns overlay for given file or folder
		/// /// </summary>
		/// <param name="path"></param>
		/// <param name="isFolder"></param>
		/// <returns></returns>
		public static async Task<byte[]?> GetIconOverlayAsync(string path, bool isFolder)
			=> await Win32API.StartSTATask(() => Win32API.GetIconOverlay(path, isFolder));

		[Obsolete]
		public static async Task<byte[]> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions, bool isFolder = false)
		{
			var result = await GetIconAsync(filePath, thumbnailSize, isFolder, false, IconOptions.None);
			return result.IconData;
		}
	}
}