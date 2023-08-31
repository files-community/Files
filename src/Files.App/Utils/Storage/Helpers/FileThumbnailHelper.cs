// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
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

		public static async Task<byte[]> LoadIconFromStorageItemAsync(IStorageItem item, uint thumbnailSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions)
		{
			if (item.IsOfType(StorageItemTypes.File))
			{
				using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(
					() => item.AsBaseStorageFile().GetThumbnailAsync(thumbnailMode, thumbnailSize, thumbnailOptions).AsTask());
				if (thumbnail is not null)
				{
					return await thumbnail.ToByteArrayAsync();
				}
			}
			else if (item.IsOfType(StorageItemTypes.Folder))
			{
				using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(
					() => item.AsBaseStorageFolder().GetThumbnailAsync(thumbnailMode, thumbnailSize, thumbnailOptions).AsTask());
				if (thumbnail is not null)
				{
					return await thumbnail.ToByteArrayAsync();
				}
			}
			return null;
		}

		public static async Task<byte[]> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions, bool isFolder = false)
		{
			if (!FileExtensionHelpers.IsShortcutOrUrlFile(filePath))
			{
				var item = await StorageHelpers.ToStorageItem<IStorageItem>(filePath);
				if (item is not null)
				{
					var iconData = await LoadIconFromStorageItemAsync(item, thumbnailSize, thumbnailMode, thumbnailOptions);
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