// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Shell;
using Files.Backend.Helpers;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using static Vanara.PInvoke.Gdi32;

namespace Files.App.Helpers
{
	public static class FileThumbnailHelper
	{
		public static Task<IRandomAccessStream> GetThumbnailAsync(this ShellItem item, uint size)
		{
			return Task.Run(() =>
			{
				using MemoryStream stream = new();
				var hBitmap = item.GetImage(new SIZE((int)size, (int)size), ShellItemGetImageOptions.ResizeToFit);
				hBitmap.ToBitmap().Save(stream, ImageFormat.MemoryBmp);
				return stream.AsRandomAccessStream();
			});
		}

		public static Task<IRandomAccessStream?> GetOverlayIconAsync(this ShellItem item)
		{
			return Task.Run(() =>
			{
				if (Shell32.SHGetImageList(0, typeof(ComCtl32.IImageList).GUID, out var imageListOut).Succeeded)
				{
					ShellFileInfo shfi = new ShellFileInfo(item.PIDL);
					var imageList = (ComCtl32.IImageList)imageListOut;

					var overlayImage = imageList.GetOverlayImage(shfi.IconOverlayIndex);
					using var hOverlay = imageList.GetIcon(overlayImage, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
					if (!hOverlay.IsNull && !hOverlay.IsInvalid)
					{
						using MemoryStream stream = new();
						using var icon = hOverlay.ToIcon();
						using var image = icon.ToBitmap();
						image.Save(stream, ImageFormat.MemoryBmp);
						return stream.AsRandomAccessStream();
					}
				}
				return null;
			});
		}

		public static Task<(byte[] IconData, byte[] OverlayData)> LoadIconAndOverlayAsync(string filePath, uint thumbnailSize, bool isFolder = false)
			=> Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, true, false));

		public static async Task<byte[]> LoadOverlayAsync(string filePath, uint thumbnailSize)
		{
			return (await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, false, true, true))).overlay;
		}

		public static async Task<byte[]> LoadIconWithoutOverlayAsync(string filePath, uint thumbnailSize, bool isFolder = false)
		{
			return (await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, false))).icon;
		}

		public static async Task<byte[]> LoadIconFromStorageItemAsync(IStorageItem item, uint thumbnailSize, ThumbnailMode thumbnailMode)
		{
			if (item.IsOfType(StorageItemTypes.File))
			{
				using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(
					() => item.AsBaseStorageFile().GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ResizeThumbnail).AsTask());
				if (thumbnail is not null)
				{
					return await thumbnail.ToByteArrayAsync();
				}
			}
			else if (item.IsOfType(StorageItemTypes.Folder))
			{
				using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(
					() => item.AsBaseStorageFolder().GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ResizeThumbnail).AsTask());
				if (thumbnail is not null)
				{
					return await thumbnail.ToByteArrayAsync();
				}
			}
			return null;
		}

		public static async Task<byte[]> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode, bool isFolder = false)
		{
			if (!FileExtensionHelpers.IsShortcutOrUrlFile(filePath))
			{
				var item = await StorageHelpers.ToStorageItem<IStorageItem>(filePath);
				if (item is not null)
				{
					var iconData = await LoadIconFromStorageItemAsync(item, thumbnailSize, thumbnailMode);
					if (iconData is not null)
					{
						return iconData;
					}
				}
			}
			return await LoadIconWithoutOverlayAsync(filePath, thumbnailSize, isFolder);
		}
	}
}