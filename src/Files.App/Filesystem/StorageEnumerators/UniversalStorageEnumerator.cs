using Files.App.Filesystem.StorageItems;
using Files.Backend.Helpers;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace Files.App.Filesystem.StorageEnumerators
{
	public static class UniversalStorageEnumerator
	{
		public static async Task<List<ListedItem>> ListEntries(
			BaseStorageFolder rootFolder,
			StorageFolderWithPath currentStorageFolder,
			CancellationToken cancellationToken,
			int countLimit,
			Func<List<ListedItem>, Task> intermediateAction,
			Dictionary<string, BitmapImage> defaultIconPairs = null
		)
		{
			var sampler = new IntervalSampler(500);
			var tempList = new List<ListedItem>();
			uint count = 0;
			var firstRound = true;

			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			while (true)
			{
				IReadOnlyList<IStorageItem> items;
				uint maxItemsToRetrieve = 300;

				if (intermediateAction is null)
				{
					// without intermediate action increase batches significantly
					maxItemsToRetrieve = 1000;
				}
				else if (firstRound)
				{
					maxItemsToRetrieve = 32;
					firstRound = false;
				}
				try
				{
					items = await rootFolder.GetItemsAsync(count, maxItemsToRetrieve);
					if (items is null || items.Count == 0)
					{
						break;
					}
				}
				catch (NotImplementedException)
				{
					break;
				}
				catch (Exception ex) when (
					ex is UnauthorizedAccessException
					|| ex is FileNotFoundException
					|| (uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
				{
					// If some unexpected exception is thrown - enumerate this folder file by file - just to be sure
					items = await EnumerateFileByFile(rootFolder, count, maxItemsToRetrieve);
				}
				foreach (var item in items)
				{
					var startWithDot = item.Name.StartsWith('.');
					if (!startWithDot || userSettingsService.FoldersSettingsService.ShowDotFiles)
					{
						if (item.IsOfType(StorageItemTypes.Folder))
						{
							var folder = await AddFolderAsync(item.AsBaseStorageFolder(), currentStorageFolder, cancellationToken);
							if (folder is not null)
							{
								if (defaultIconPairs?.ContainsKey(string.Empty) ?? false)
								{
									folder.SetDefaultIcon(defaultIconPairs[string.Empty]);
								}
								tempList.Add(folder);
							}
						}
						else
						{
							var fileEntry = await AddFileAsync(item.AsBaseStorageFile(), currentStorageFolder, cancellationToken);
							if (fileEntry is not null)
							{
								if (defaultIconPairs is not null)
								{
									if (!string.IsNullOrEmpty(fileEntry.FileExtension))
									{
										var lowercaseExtension = fileEntry.FileExtension.ToLowerInvariant();
										if (defaultIconPairs.ContainsKey(lowercaseExtension))
										{
											fileEntry.SetDefaultIcon(defaultIconPairs[lowercaseExtension]);
										}
									}
								}
								tempList.Add(fileEntry);
							}
						}
					}
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
				}
				count += maxItemsToRetrieve;

				if (countLimit > -1 && count >= countLimit)
				{
					break;
				}

				if (intermediateAction is not null && (items.Count == maxItemsToRetrieve || sampler.CheckNow()))
				{
					await intermediateAction(tempList);
					// clear the temporary list every time we do an intermediate action
					tempList.Clear();
				}
			}
			return tempList;
		}

		private static async Task<IReadOnlyList<IStorageItem>> EnumerateFileByFile(BaseStorageFolder rootFolder, uint startFrom, uint itemsToIterate)
		{
			var tempList = new List<IStorageItem>();
			for (var i = startFrom; i < startFrom + itemsToIterate; i++)
			{
				IStorageItem item;
				try
				{
					var results = await rootFolder.GetItemsAsync(i, 1);
					item = results?.FirstOrDefault();
					if (item is null)
					{
						break;
					}
				}
				catch (NotImplementedException)
				{
					break;
				}
				catch (Exception ex) when (
					ex is UnauthorizedAccessException
					|| ex is FileNotFoundException
					|| (uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
				{
					continue;
				}
				tempList.Add(item);
			}
			return tempList;
		}

		public static async Task<ListedItem> AddFolderAsync(BaseStorageFolder folder, StorageFolderWithPath currentStorageFolder, CancellationToken cancellationToken)
		{
			var basicProperties = await folder.GetBasicPropertiesAsync();
			if (!cancellationToken.IsCancellationRequested)
			{
				if (folder is ShortcutStorageFolder linkFolder)
				{
					return new ShortcutItem(folder.FolderRelativeId)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder,
						IsHiddenItem = false,
						Opacity = 1,
						FileImage = null,
						LoadFileIcon = false,
						ItemNameRaw = folder.DisplayName,
						ItemDateModifiedReal = basicProperties.DateModified,
						ItemDateCreatedReal = folder.DateCreated,
						ItemType = folder.DisplayType,
						ItemPath = folder.Path,
						FileSize = null,
						FileSizeBytes = 0,
						TargetPath = linkFolder.TargetPath,
						Arguments = linkFolder.Arguments,
						WorkingDirectory = linkFolder.WorkingDirectory,
						RunAsAdmin = linkFolder.RunAsAdmin
					};
				}
				else if (folder is BinStorageFolder binFolder)
				{
					return new RecycleBinItem(folder.FolderRelativeId)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemNameRaw = folder.DisplayName,
						ItemDateModifiedReal = basicProperties.DateModified,
						ItemDateCreatedReal = folder.DateCreated,
						ItemType = folder.DisplayType,
						IsHiddenItem = false,
						Opacity = 1,
						FileImage = null,
						LoadFileIcon = false,
						ItemPath = string.IsNullOrEmpty(folder.Path) ? PathNormalization.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
						FileSize = null,
						FileSizeBytes = 0,
						ItemDateDeletedReal = binFolder.DateDeleted,
						ItemOriginalPath = binFolder.OriginalPath,
					};
				}
				else
				{
					return new ListedItem(folder.FolderRelativeId)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemNameRaw = folder.DisplayName,
						ItemDateModifiedReal = basicProperties.DateModified,
						ItemDateCreatedReal = folder.DateCreated,
						ItemType = folder.DisplayType,
						IsHiddenItem = false,
						Opacity = 1,
						FileImage = null,
						LoadFileIcon = false,
						ItemPath = string.IsNullOrEmpty(folder.Path) ? PathNormalization.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
						FileSize = null,
						FileSizeBytes = 0
					};
				}
			}
			return null;
		}

		public static async Task<ListedItem> AddFileAsync(
			BaseStorageFile file,
			StorageFolderWithPath currentStorageFolder,
			CancellationToken cancellationToken
		)
		{
			var basicProperties = await file.GetBasicPropertiesAsync();
			// Display name does not include extension
			var itemName = file.Name;
			var itemModifiedDate = basicProperties.DateModified;
			var itemCreatedDate = file.DateCreated;
			var itemPath = string.IsNullOrEmpty(file.Path) ? PathNormalization.Combine(currentStorageFolder.Path, file.Name) : file.Path;
			var itemSize = basicProperties.Size.ToSizeString();
			var itemSizeBytes = basicProperties.Size;
			var itemType = file.DisplayType;
			var itemFileExtension = file.FileType;
			var itemThumbnailImgVis = false;

			if (cancellationToken.IsCancellationRequested)
			{
				return null;
			}

			// TODO: is this needed to be handled here?
			if (App.LibraryManager.TryGetLibrary(file.Path, out LibraryLocationItem library))
			{
				return new LibraryItem(library)
				{
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
				};
			}
			else
			{
				if (file is ShortcutStorageFile linkFile)
				{
					var isUrl = FileExtensionHelpers.IsWebLinkFile(linkFile.Name);
					return new ShortcutItem(file.FolderRelativeId)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = false,
						Opacity = 1,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						LoadWebShortcutGlyph = isUrl,
						ItemNameRaw = itemName,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = (long)itemSizeBytes,
						TargetPath = linkFile.TargetPath,
						Arguments = linkFile.Arguments,
						WorkingDirectory = linkFile.WorkingDirectory,
						RunAsAdmin = linkFile.RunAsAdmin,
						IsUrl = isUrl,
					};
				}
				else if (file is BinStorageFile binFile)
				{
					return new RecycleBinItem(file.FolderRelativeId)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = false,
						Opacity = 1,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = (long)itemSizeBytes,
						ItemDateDeletedReal = binFile.DateDeleted,
						ItemOriginalPath = binFile.OriginalPath
					};
				}
				else
				{
					return new ListedItem(file.FolderRelativeId)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = false,
						Opacity = 1,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = (long)itemSizeBytes,
					};
				}
			}
			return null;
		}
	}
}
