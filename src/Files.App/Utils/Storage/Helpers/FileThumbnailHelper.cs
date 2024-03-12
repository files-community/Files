// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class FileThumbnailHelper
	{
		private static Task<byte[]?>? longLoadingTask;

		/// <summary>
		/// Returns icon or thumbnail for given file or folder
		/// </summary>
		public static async Task<byte[]?> GetIconAsync(string path, uint requestedSize, bool isFolder, IconOptions iconOptions)
		{
			var size = iconOptions.HasFlag(IconOptions.UseCurrentScale) ? requestedSize * App.AppModel.AppWindowDPI : requestedSize;
			var returnIconOnly = iconOptions.HasFlag(IconOptions.ReturnIconOnly);

			return await Win32API.StartSTATask(async () => {
				if (longLoadingTask is not null && !longLoadingTask.IsCompleted)
					// Return cached thumbnail if any non-cached thumbnail is loading
					return await Win32API.GetIcon(path, (int)size, isFolder, returnIconOnly, true);

				Task<byte[]?> getIconTask = Win32API.GetIcon(path, (int)size, isFolder, returnIconOnly, false);
				await Task.WhenAny(getIconTask, Task.Delay(100));
				if (getIconTask.IsCompleted)
					return await getIconTask;
				else
				{
					// Save the loading task to be checked later
					longLoadingTask = getIconTask;

					// Return cached thumbnail
					return await Win32API.GetIcon(path, (int)size, isFolder, returnIconOnly, true);
				}
			});
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
		public static async Task<byte[]?> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode, ThumbnailOptions thumbnailOptions, bool isFolder = false)
		{
			var result = await GetIconAsync(filePath, thumbnailSize, isFolder, IconOptions.None);
			return result;
		}
	}
}