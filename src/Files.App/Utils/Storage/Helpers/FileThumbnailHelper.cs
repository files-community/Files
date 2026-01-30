// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class FileThumbnailHelper
	{
		private static readonly IThumbnailService thumbnailService = Ioc.Default.GetRequiredService<IThumbnailService>();

		/// <summary>
		/// Returns icon or thumbnail for given file or folder
		/// </summary>
		public static async Task<byte[]?> GetIconAsync(string path, uint requestedSize, bool isFolder, IconOptions iconOptions)
		{
			var size = iconOptions.HasFlag(IconOptions.UseCurrentScale) ? requestedSize * App.AppModel.AppWindowDPI : requestedSize;
			// Ensure size is at least 1 to prevent layout errors
			size = Math.Max(1, size);

			return await thumbnailService.GetThumbnailAsync(
				path,
				(int)size,
				isFolder,
				iconOptions);
		}

		/// <summary>
		/// Returns cached thumbnail without calling the Shell API.
		/// Returns null if not found in the cache.
		/// </summary>
		public static async Task<byte[]?> GetCachedIconAsync(string path, uint requestedSize, bool isFolder, IconOptions iconOptions)
		{
			var size = iconOptions.HasFlag(IconOptions.UseCurrentScale) ? requestedSize * App.AppModel.AppWindowDPI : requestedSize;
			size = Math.Max(1, size);

			return await thumbnailService.GetCachedThumbnailAsync(
				path,
				(int)size,
				isFolder,
				iconOptions);
		}

		/// <summary>
		/// Returns overlay for given file or folder
		/// /// </summary>
		/// <param name="path"></param>
		/// <param name="isFolder"></param>
		/// <returns></returns>
		public static async Task<byte[]?> GetIconOverlayAsync(string path, bool isFolder)
			=> await STATask.Run(() => Win32Helper.GetIconOverlay(path, isFolder), App.Logger);

		[Obsolete]
		public static async Task<byte[]?> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions, bool isFolder = false)
		{
			var result = await GetIconAsync(filePath, thumbnailSize, isFolder, IconOptions.None);
			return result;
		}
	}
}