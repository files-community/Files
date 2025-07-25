// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Files.App.Utils.Storage
{
	public static class FileThumbnailHelper
	{
		/// <summary>
		/// Returns icon or thumbnail for given file or folder
		/// </summary>
		public static async Task<ImageSource?> GetIconAsync(string path, uint requestedSize, bool isFolder, IconOptions iconOptions)
		{
			var size = iconOptions.HasFlag(IconOptions.UseCurrentScale) ? requestedSize * App.AppModel.AppWindowDPI : requestedSize;

			var iconBytes = await Win32Helper.StartSTATask(() => Win32Helper.GetIcon(path, (int)size, isFolder, iconOptions));
			
			if (iconBytes is null)
				return null;

			// Convert byte[] to BitmapImage
			using var ms = new MemoryStream(iconBytes);
			var bitmapImage = new BitmapImage();
			await bitmapImage.SetSourceAsync(ms.AsRandomAccessStream());
			return bitmapImage;
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