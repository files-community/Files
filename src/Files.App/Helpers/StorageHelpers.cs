// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.Backend.Helpers;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper to handle <see cref="IStorageItem"/>.
	/// </summary>
	public static class StorageHelpers
	{
		public static async Task<IStorageItem> ToStorageItem(this IStorageItemWithPath item)
		{
			return (await item.ToStorageItemResult()).Result;
		}

		public static async Task<TRequested> ToStorageItem<TRequested>(string path) where TRequested : IStorageItem
		{
			FilesystemResult<BaseStorageFile> file = null;
			FilesystemResult<BaseStorageFolder> folder = null;

			if (FileExtensionHelpers.IsShortcutOrUrlFile(path))
			{
				// TODO:
				//  In the future, when IStorageItemWithPath will inherit from IStorageItem,
				//  we could implement this code here for getting .lnk files.
				//  For now, we can't.
				return default;
			}

			// Fast get attributes
			bool exists = NativeFileOperationsHelper.GetFileAttributesExFromApp(path, NativeFileOperationsHelper.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out NativeFileOperationsHelper.WIN32_FILE_ATTRIBUTE_DATA itemAttributes);

			// Exists on local storage
			if (exists)
			{
				// Directory
				if (itemAttributes.dwFileAttributes.HasFlag(System.IO.FileAttributes.Directory))
				{
					if (typeof(IStorageFile).IsAssignableFrom(typeof(TRequested))) // Wanted file
					{
						// NotAFile
						return default;
					}
					// Just get the directory
					else
					{
						await GetFolder();
					}
				}
				// File
				else
				{
					// Wanted directory
					if (typeof(IStorageFolder).IsAssignableFrom(typeof(TRequested)))
					{
						// NotAFile
						return default;
					}
					// Just get the file
					else
					{
						await GetFile();
					}
				}
			}
			// Does not exist or is not present on local storage
			else
			{
				Debug.WriteLine($"Path does not exist. Trying to find storage item manually (HRESULT: {Marshal.GetLastWin32Error()})");

				if (typeof(IStorageFile).IsAssignableFrom(typeof(TRequested)))
				{
					await GetFile();
				}
				else if (typeof(IStorageFolder).IsAssignableFrom(typeof(TRequested)))
				{
					await GetFolder();
				}
				else if (typeof(IStorageItem).IsAssignableFrom(typeof(TRequested)))
				{
					// Possibly a file
					if (SystemIO.Path.HasExtension(path))
					{
						await GetFile();
					}

					// Possibly a folder
					if (!file || file.Result is null)
					{
						await GetFolder();

						if (file is null && (!folder || folder.Result is null))
						{
							// Try file because it wasn't checked
							await GetFile();
						}
					}
				}
			}

			if (file is not null && file)
				return (TRequested)(IStorageItem)file.Result;
			else if (folder is not null && folder)
				return (TRequested)(IStorageItem)folder.Result;

			return default;

			// Extensions

			async Task GetFile()
			{
				var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(path));
				file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path, rootItem));
			}

			async Task GetFolder()
			{
				var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(path));
				folder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, rootItem));
			}
		}

		public static async Task<long> GetFileSize(this IStorageFile file)
		{
			BasicProperties properties = await file.GetBasicPropertiesAsync();
			return (long)properties.Size;
		}

		public static async Task<FilesystemResult<IStorageItem>> ToStorageItemResult(this IStorageItemWithPath item)
		{
			var returnedItem = new FilesystemResult<IStorageItem>(null, FileSystemStatusCode.Generic);
			var rootItem = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(item.Path));
			if (!string.IsNullOrEmpty(item.Path))
			{
				returnedItem =
					(item.ItemType == FilesystemItemType.File)
						? ToType<IStorageItem, BaseStorageFile>(
							await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path, rootItem)))
						: ToType<IStorageItem, BaseStorageFolder>(
							await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(item.Path, rootItem)));
			}

			if (returnedItem.Result is null && item.Item is not null)
				returnedItem = new FilesystemResult<IStorageItem>(item.Item, FileSystemStatusCode.Success);

			return returnedItem;
		}

		public static IStorageItemWithPath FromPathAndType(string customPath, FilesystemItemType? itemType)
		{
			return
				(itemType == FilesystemItemType.File)
					? new StorageFileWithPath(null, customPath)
					: new StorageFolderWithPath(null, customPath);
		}

		public static async Task<FilesystemItemType> GetTypeFromPath(string path)
		{
			IStorageItem item = await ToStorageItem<IStorageItem>(path);

			return item is null ? FilesystemItemType.File : (item.IsOfType(StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File);
		}

		public static bool Exists(string path)
			=> NativeFileOperationsHelper.GetFileAttributesExFromApp(path, NativeFileOperationsHelper.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out _);

		public static IStorageItemWithPath FromStorageItem(this IStorageItem item, string customPath = null, FilesystemItemType? itemType = null)
		{
			if (item is null)
				return FromPathAndType(customPath, itemType);
			else if (item.IsOfType(StorageItemTypes.File))
				return new StorageFileWithPath(item.AsBaseStorageFile(), string.IsNullOrEmpty(item.Path) ? customPath : item.Path);
			else if (item.IsOfType(StorageItemTypes.Folder))
				return new StorageFolderWithPath(item.AsBaseStorageFolder(), string.IsNullOrEmpty(item.Path) ? customPath : item.Path);

			return null;
		}

		public static FilesystemResult<T> ToType<T, V>(FilesystemResult<V> result) where T : class
			=> new FilesystemResult<T>(result.Result as T, result.ErrorCode);
	}
}
