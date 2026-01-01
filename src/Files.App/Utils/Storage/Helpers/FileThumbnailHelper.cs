// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class FileThumbnailHelper
	{
		private static IThumbnailService? _thumbnailService;

		public static void Initialize(IThumbnailService thumbnailService)
		{
			_thumbnailService = thumbnailService;
		}

		/// <summary>
		/// Returns icon or thumbnail for given file or folder
		/// </summary>
		public static async Task<byte[]?> GetIconAsync(string path, uint requestedSize, bool isFolder, IconOptions iconOptions)
		{
			var size = iconOptions.HasFlag(IconOptions.UseCurrentScale) ? requestedSize * App.AppModel.AppWindowDPI : requestedSize;
			// Ensure size is at least 1 to prevent layout errors
			size = Math.Max(1, size);

			if (_thumbnailService is not null)
			{
				return await _thumbnailService.GetThumbnailAsync(
					path,
					(int)size,
					isFolder,
					iconOptions);
			}

			return await STATask.Run(() => Win32Helper.GetIcon(path, (int)size, isFolder, iconOptions), App.Logger);
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