// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using System.IO;
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
			// Ensure size is at least 1 to prevent layout errors
			size = Math.Max(1, size);

			if (!isFolder && !iconOptions.HasFlag(IconOptions.ReturnIconOnly))
			{
				var extension = Path.GetExtension(path);
				if (FileExtensionHelpers.IsFontFile(extension))
				{
					var winrtThumbnail = await FontFileHelper.GetWinRTThumbnailAsync(path, (uint)size);
					if (winrtThumbnail is not null)
						return winrtThumbnail;

					if (!extension.Equals(".fon", StringComparison.OrdinalIgnoreCase))
					{
						var fontThumbnail = await Win32Helper.StartSTATask(() => FontFileHelper.GenerateFontThumbnail(path, (int)size));
						if (fontThumbnail is not null)
							return fontThumbnail;
					}
				}
			}

			return await Win32Helper.StartSTATask(() => Win32Helper.GetIcon(path, (int)size, isFolder, iconOptions));
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