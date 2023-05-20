// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.App.Helpers.FileListCache;
using Files.Backend.Extensions;
using Files.Backend.Helpers;
using Files.Backend.Services.SizeProvider;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Vanara.PInvoke;
using Windows.Storage;

namespace Files.App.Filesystem.StorageEnumerators
{
	/// <summary>
	/// Provides static helper for Win32 storage enumeration.
	/// </summary>
	public static class Win32StorageEnumerator
	{
		private static readonly ISizeProvider _folderSizeProvider = Ioc.Default.GetService<ISizeProvider>();

		private static readonly string _folderTypeTextLocalized = "Folder".GetLocalizedResource();

		private static readonly IFileListCache _fileListCache = FileListCacheController.GetInstance();

		public static async Task<List<ListedItem>> ListEntries(string path, IntPtr hFile, NativeFindStorageItemHelper.WIN32_FIND_DATA findData, CancellationToken cancellationToken, int countLimit, Func<List<ListedItem>, Task> intermediateAction, Dictionary<string, BitmapImage> defaultIconPairs = null)
		{
			var sampler = new IntervalSampler(500);
			var tempList = new List<ListedItem>();

			var count = 0;

			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			bool CalculateFolderSizes = userSettingsService.FoldersSettingsService.CalculateFolderSizes;

			do
			{
				var isSystem = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.System) == SystemIO.FileAttributes.System;
				var isHidden = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Hidden) == SystemIO.FileAttributes.Hidden;
				var startWithDot = findData.cFileName.StartsWith('.');
				if ((!isHidden ||
					(userSettingsService.FoldersSettingsService.ShowHiddenItems &&
					(!isSystem || userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
					(!startWithDot || userSettingsService.FoldersSettingsService.ShowDotFiles))
				{
					if (((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Directory) != SystemIO.FileAttributes.Directory)
					{
						var file = await GetFile(findData, path, cancellationToken);
						if (file is not null)
						{
							if (defaultIconPairs is not null)
							{
								if (!string.IsNullOrEmpty(file.FileExtension))
								{
									var lowercaseExtension = file.FileExtension.ToLowerInvariant();

									if (defaultIconPairs.ContainsKey(lowercaseExtension))
										file.SetDefaultIcon(defaultIconPairs[lowercaseExtension]);
								}
							}

							tempList.Add(file);
							++count;

							if (userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
								tempList.AddRange(EnumAdsForPath(file.ItemPath, file));
						}
					}
					else if (((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Directory) == SystemIO.FileAttributes.Directory)
					{
						if (findData.cFileName != "." && findData.cFileName != "..")
						{
							var folder = await GetFolder(findData, path, cancellationToken);
							if (folder is not null)
							{
								if (defaultIconPairs?.ContainsKey(string.Empty) ?? false)
								{
									// Set folder icon (found by empty extension string)
									folder.SetDefaultIcon(defaultIconPairs[string.Empty]);
								}

								tempList.Add(folder);
								++count;

								if (userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
									tempList.AddRange(EnumAdsForPath(folder.ItemPath, folder));

								if (CalculateFolderSizes)
								{
									if (_folderSizeProvider.TryGetSize(folder.ItemPath, out var size))
									{
										folder.FileSizeBytes = (long)size;
										folder.FileSize = size.ToSizeString();
									}

									_ = _folderSizeProvider.UpdateAsync(folder.ItemPath, cancellationToken);
								}
							}
						}
					}
				}

				if (cancellationToken.IsCancellationRequested || count == countLimit)
					break;

				if (intermediateAction is not null && (count == 32 || sampler.CheckNow()))
				{
					await intermediateAction(tempList);

					// Clear the temporary list every time we do an intermediate action
					tempList.Clear();
				}
			} while (NativeFindStorageItemHelper.FindNextFile(hFile, out findData));

			NativeFindStorageItemHelper.FindClose(hFile);
			return tempList;
		}

		private static IEnumerable<ListedItem> EnumAdsForPath(string itemPath, ListedItem main)
		{
			foreach (var ads in NativeFileOperationsHelper.GetAlternateStreams(itemPath))
				yield return GetAlternateStream(ads, main);
		}

		public static ListedItem GetAlternateStream((string Name, long Size) ads, ListedItem main)
		{
			string itemType = "File".GetLocalizedResource();
			string itemFileExtension = null;

			if (ads.Name.Contains('.'))
			{
				itemFileExtension = Path.GetExtension(ads.Name);
				itemType = itemFileExtension.Trim('.') + " " + itemType;
			}

			// Remove ":" and ":$DATA"
			string adsName = ads.Name.Substring(1, ads.Name.Length - 7);

			return new AlternateStreamItem()
			{
				PrimaryItemAttribute = StorageItemTypes.File,
				FileExtension = itemFileExtension,
				FileImage = null,
				LoadFileIcon = false,
				ItemNameRaw = adsName,
				IsHiddenItem = false,
				Opacity = Constants.UI.DimItemOpacity,
				ItemDateModifiedReal = main.ItemDateModifiedReal,
				ItemDateAccessedReal = main.ItemDateAccessedReal,
				ItemDateCreatedReal = main.ItemDateCreatedReal,
				ItemType = itemType,
				ItemPath = $"{main.ItemPath}:{adsName}",
				FileSize = ads.Size.ToSizeString(),
				FileSizeBytes = ads.Size
			};
		}

		public static async Task<ListedItem> GetFolder(NativeFindStorageItemHelper.WIN32_FIND_DATA findData, string pathRoot, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return null;

			DateTime itemModifiedDate;
			DateTime itemCreatedDate;

			try
			{
				NativeFindStorageItemHelper.FileTimeToSystemTime(ref findData.ftLastWriteTime, out NativeFindStorageItemHelper.SYSTEMTIME systemModifiedTimeOutput);
				itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

				NativeFindStorageItemHelper.FileTimeToSystemTime(ref findData.ftCreationTime, out NativeFindStorageItemHelper.SYSTEMTIME systemCreatedTimeOutput);
				itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
			}
			catch (ArgumentException)
			{
				// Invalid date means invalid findData, do not add to list
				return null;
			}

			var itemPath = Path.Combine(pathRoot, findData.cFileName);
			string itemName = await _fileListCache.ReadFileDisplayNameFromCache(itemPath, cancellationToken);

			if (string.IsNullOrEmpty(itemName))
				itemName = findData.cFileName;

			bool isHidden = (((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Hidden) == SystemIO.FileAttributes.Hidden);

			double opacity = 1;

			if (isHidden)
				opacity = Constants.UI.DimItemOpacity;

			return new ListedItem(null)
			{
				PrimaryItemAttribute = StorageItemTypes.Folder,
				ItemNameRaw = itemName,
				ItemDateModifiedReal = itemModifiedDate,
				ItemDateCreatedReal = itemCreatedDate,
				ItemType = _folderTypeTextLocalized,
				FileImage = null,
				IsHiddenItem = isHidden,
				Opacity = opacity,
				LoadFileIcon = false,
				ItemPath = itemPath,
				FileSize = null,
				FileSizeBytes = 0,
			};
		}

		public static async Task<ListedItem> GetFile(NativeFindStorageItemHelper.WIN32_FIND_DATA findData, string pathRoot, CancellationToken cancellationToken)
		{
			var itemPath = Path.Combine(pathRoot, findData.cFileName);
			var itemName = findData.cFileName;

			DateTime itemModifiedDate, itemCreatedDate, itemLastAccessDate;
			try
			{
				NativeFindStorageItemHelper.FileTimeToSystemTime(ref findData.ftLastWriteTime, out NativeFindStorageItemHelper.SYSTEMTIME systemModifiedDateOutput);
				itemModifiedDate = systemModifiedDateOutput.ToDateTime();

				NativeFindStorageItemHelper.FileTimeToSystemTime(ref findData.ftCreationTime, out NativeFindStorageItemHelper.SYSTEMTIME systemCreatedDateOutput);
				itemCreatedDate = systemCreatedDateOutput.ToDateTime();

				NativeFindStorageItemHelper.FileTimeToSystemTime(ref findData.ftLastAccessTime, out NativeFindStorageItemHelper.SYSTEMTIME systemLastAccessOutput);
				itemLastAccessDate = systemLastAccessOutput.ToDateTime();
			}
			catch (ArgumentException)
			{
				// Invalid date means invalid findData, do not add to list
				return null;
			}

			long itemSizeBytes = findData.GetSize();
			var itemSize = itemSizeBytes.ToSizeString();
			string itemType = "File".GetLocalizedResource();
			string itemFileExtension = null;

			if (findData.cFileName.Contains('.'))
			{
				itemFileExtension = Path.GetExtension(itemPath);
				itemType = itemFileExtension.Trim('.') + " " + itemType;
			}

			bool itemThumbnailImgVis = false;
			bool itemEmptyImgVis = true;

			if (cancellationToken.IsCancellationRequested)
				return null;

			bool isHidden = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Hidden) == SystemIO.FileAttributes.Hidden;
			double opacity = isHidden ? Constants.UI.DimItemOpacity : 1;

			// https://learn.microsoft.com/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
			bool isReparsePoint = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.ReparsePoint) == SystemIO.FileAttributes.ReparsePoint;
			bool isSymlink = isReparsePoint && findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK;

			if (isSymlink)
			{
				var targetPath = NativeFileOperationsHelper.ParseSymLink(itemPath);
				return new ShortcutItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					FileExtension = itemFileExtension,
					IsHiddenItem = isHidden,
					Opacity = opacity,
					FileImage = null,
					LoadFileIcon = itemThumbnailImgVis,
					LoadWebShortcutGlyph = false,
					ItemNameRaw = itemName,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateAccessedReal = itemLastAccessDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = "Shortcut".GetLocalizedResource(),
					ItemPath = itemPath,
					FileSize = itemSize,
					FileSizeBytes = itemSizeBytes,
					TargetPath = targetPath,
					IsSymLink = true
				};
			}
			else if (FileExtensionHelpers.IsShortcutOrUrlFile(findData.cFileName))
			{
				var isUrl = FileExtensionHelpers.IsWebLinkFile(findData.cFileName);

				var shInfo = await FileOperationsHelpers.ParseLinkAsync(itemPath);
				if (shInfo is null)
					return null;

				return new ShortcutItem(null)
				{
					PrimaryItemAttribute = shInfo.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
					FileExtension = itemFileExtension,
					IsHiddenItem = isHidden,
					Opacity = opacity,
					FileImage = null,
					LoadFileIcon = !shInfo.IsFolder && itemThumbnailImgVis,
					LoadWebShortcutGlyph = !shInfo.IsFolder && isUrl && itemEmptyImgVis,
					ItemNameRaw = itemName,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateAccessedReal = itemLastAccessDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = isUrl ? "ShortcutWebLinkFileType".GetLocalizedResource() : "Shortcut".GetLocalizedResource(),
					ItemPath = itemPath,
					FileSize = itemSize,
					FileSizeBytes = itemSizeBytes,
					TargetPath = shInfo.TargetPath,
					Arguments = shInfo.Arguments,
					WorkingDirectory = shInfo.WorkingDirectory,
					RunAsAdmin = shInfo.RunAsAdmin,
					IsUrl = isUrl,
				};
			}
			else if (App.LibraryManager.TryGetLibrary(itemPath, out LibraryLocationItem library))
			{
				return new LibraryItem(library)
				{
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
				};
			}
			else
			{
				if (ZipStorageFolder.IsZipPath(itemPath) && await ZipStorageFolder.CheckDefaultZipApp(itemPath))
				{
					return new ZipItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder, // Treat zip files as folders
						FileExtension = itemFileExtension,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes
					};
				}
				else
				{
					return new ListedItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes
					};
				}
			}
		}
	}
}
