// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class FileThumbnailHelper
	{
		/// <summary>
		/// Returns icon or thumbnail for given file or folder
		/// </summary>
		public static async Task<byte[]?> GetIconAsync(string path, uint requestedSize, bool isFolder, IconOptions iconOptions)
		{
			var size = iconOptions.HasFlag(IconOptions.UseCurrentScale) ? requestedSize * App.AppModel.AppWindowDPI : requestedSize;

			return await Win32Helper.StartSTATask(() => Win32Helper.GetIcon(path, (int)size, isFolder, iconOptions));
		}

		/// <summary>
		/// Returns thumbnail for given file or folder using Storage API
		/// </summary>
		public static async Task<byte[]?> GetIconAsync(IStorageItem item, uint requestedSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions)
		{
			using StorageItemThumbnail thumbnail = item switch
			{
				BaseStorageFile file => await FilesystemTasks.Wrap(() => file.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions).AsTask()),
				BaseStorageFolder folder => await FilesystemTasks.Wrap(() => folder.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions).AsTask()),
				_ => new(null!, FileSystemStatusCode.Generic)
			};
			if (thumbnail is not null && thumbnail.Size != 0 && thumbnail.OriginalHeight != 0 && thumbnail.OriginalWidth != 0)
				return await thumbnail.ToByteArrayAsync();
			return null;
		}

		/// <summary>
		/// Returns overlay for given file or folder
		/// /// </summary>
		/// <param name="path"></param>
		/// <param name="isFolder"></param>
		/// <returns></returns>
		public static async Task<byte[]?> GetIconOverlayAsync(string path, bool isFolder)
			=> await Win32Helper.StartSTATask(() => Win32Helper.GetIconOverlay(path, isFolder));

		[Obsolete]
		public static async Task<byte[]?> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions, bool isFolder = false)
		{
			var result = await GetIconAsync(filePath, thumbnailSize, isFolder, IconOptions.None);
			return result;
		}
	}
}