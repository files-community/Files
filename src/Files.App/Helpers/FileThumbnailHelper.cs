using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Shell;
using Files.Core.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Helpers
{
	public static class FileThumbnailHelper
	{
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