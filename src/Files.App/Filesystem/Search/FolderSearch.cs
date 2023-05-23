// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.Backend.Helpers;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.App.Filesystem.Search
{
	/// <summary>
	///  Provides an utility for storage folder search.
	/// </summary>
	public class FolderSearch
	{
		private const uint DEFAULT_STEP_SIZE = 500;

		private readonly IUserSettingsService _userSettingsService;

		private readonly DrivesViewModel _drivesViewModel;

		private readonly IFileTagsSettingsService _fileTagsSettingsService;

		public string? Query { get; set; }

		public string? Folder { get; set; }

		// 0: no limit
		public uint MaxItemCount { get; set; } = 0;

		public uint ThumbnailSize { get; set; } = 24;

		public bool SearchUnindexedItems { get; set; }

		private uint UsedMaxItemCount
			=> MaxItemCount > 0 ? MaxItemCount : uint.MaxValue;

		private bool IsAQSQuery
			=> Query is not null && (Query.StartsWith('$') || Query.Contains(':', StringComparison.Ordinal));

		private string QueryWithWildcard
		{
			get
			{
				// ".docx" -> "*.docx"
				if (!string.IsNullOrEmpty(Query) && Query.Contains('.'))
				{
					var split = Query.Split('.');
					var leading = string.Join('.', split.SkipLast(1));
					var query = $"{leading}*.{split.Last()}";

					return $"{query}*";
				}

				return $"{Query}*";
			}
		}

		public string AQSQuery
		{
			get
			{
				// If the query starts with a $, assume the query is in aqs format, otherwise assume the user is searching for the file name
				if (Query is not null && Query.StartsWith('$'))
					return Query.Substring(1);
				else if (Query is not null && Query.Contains(':', StringComparison.Ordinal))
					return Query;
				else
					return $"System.FileName:\"{QueryWithWildcard}\"";
			}
		}

		public EventHandler? SearchTick;

		public FolderSearch()
		{
			_userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			_drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
			_fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		}

		public Task SearchAsync(IList<ListedItem> results, CancellationToken token)
		{
			try
			{
				if (App.LibraryManager.TryGetLibrary(Folder, out var library))
					return AddItemsAsyncForLibrary(library, results, token);
				else if (Folder == "Home")
					return AddItemsAsyncForHome(results, token);
				else
					return AddItemsAsync(Folder, results, token);
			}
			catch (Exception e)
			{
				App.Logger.LogWarning(e, "Search failure");
			}

			return Task.CompletedTask;
		}

		private async Task AddItemsAsyncForHome(IList<ListedItem> results, CancellationToken token)
		{
			if (AQSQuery.StartsWith("tag:", StringComparison.Ordinal))
			{
				await SearchTagsAsync("", results, token); // Search tags everywhere, not only local drives
			}
			else
			{
				foreach (var drive in drivesViewModel.Drives.Cast<DriveItem>().Where(x => !x.IsNetwork))
					await AddItemsAsync(drive.Path, results, token);
			}
		}

		public async Task<ObservableCollection<ListedItem>> SearchAsync()
		{
			ObservableCollection<ListedItem> results = new();

			try
			{
				var token = CancellationToken.None;

				if (App.LibraryManager.TryGetLibrary(Folder, out var library))
					await AddItemsAsyncForLibrary(library, results, token);
				else if (Folder == "Home")
					await AddItemsAsyncForHome(results, token);
				else
					await AddItemsAsync(Folder, results, token);
			}
			catch (Exception e)
			{
				App.Logger.LogWarning(e, "Search failure");
			}

			return results;
		}

		private async Task SearchAsync(BaseStorageFolder folder, IList<ListedItem> results, CancellationToken token)
		{
			//var sampler = new IntervalSampler(500);

			uint index = 0;

			var stepSize = Math.Min(DEFAULT_STEP_SIZE, UsedMaxItemCount);

			var options = ToQueryOptions();

			var queryResult = folder.CreateItemQueryWithOptions(options);
			var items = await queryResult.GetItemsAsync(0, stepSize);

			while (items.Count > 0)
			{
				foreach (IStorageItem item in items)
				{
					if (token.IsCancellationRequested)
						return;

					try
					{
						if (!item.Name.StartsWith('.') || _userSettingsService.FoldersSettingsService.ShowDotFiles)
							results.Add(await GetListedItemAsync(item));
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, "Error creating ListedItem from StorageItem");
					}

					if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
						SearchTick?.Invoke(this, EventArgs.Empty);
				}

				index += (uint)items.Count;
				stepSize = Math.Min(DEFAULT_STEP_SIZE, UsedMaxItemCount - (uint)results.Count);
				items = await queryResult.GetItemsAsync(index, stepSize);
			}
		}

		private async Task AddItemsAsyncForLibrary(LibraryLocationItem library, IList<ListedItem> results, CancellationToken token)
		{
			foreach (var folder in library.Folders)
				await AddItemsAsync(folder, results, token);
		}

		private async Task SearchTagsAsync(string folder, IList<ListedItem> results, CancellationToken token)
		{
			//var sampler = new IntervalSampler(500);
			var tags = AQSQuery.Substring("tag:".Length)?
				.Split(',')
				.Where(t => !string.IsNullOrWhiteSpace(t))
				.SelectMany(_fileTagsSettingsService.GetTagsByName, (_, t) => t.Uid).ToHashSet();

			if (tags?.Any() != true)
				return;

			var dbInstance = FileTagsHelper.GetDbInstance();
			var matches = dbInstance.GetAllUnderPath(folder)
				.Where(x => tags.All(x.Tags.Contains));

			foreach (var match in matches)
			{
				(IntPtr hFile, NativeFindStorageItemHelper.WIN32_FIND_DATA findData) = await Task.Run(() =>
				{
					int additionalFlags = NativeFindStorageItemHelper.FIND_FIRST_EX_LARGE_FETCH;

					IntPtr hFileTsk = NativeFindStorageItemHelper.FindFirstFileExFromApp(
						match.FilePath,
						NativeFindStorageItemHelper.FINDEX_INFO_LEVELS.FindExInfoBasic,
						out NativeFindStorageItemHelper.WIN32_FIND_DATA findDataTsk,
						NativeFindStorageItemHelper.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
						IntPtr.Zero,
						additionalFlags);

					return (hFileTsk, findDataTsk);
				})
				.WithTimeoutAsync(TimeSpan.FromSeconds(5));

				if (hFile != IntPtr.Zero && hFile.ToInt64() != -1)
				{
					var isSystem = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.System) == SystemIO.FileAttributes.System;
					var isHidden = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Hidden) == SystemIO.FileAttributes.Hidden;
					var startWithDot = findData.cFileName.StartsWith('.');

					bool shouldBeListed =
						(!isHidden ||
						(_userSettingsService.FoldersSettingsService.ShowHiddenItems &&
						(!isSystem || _userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
						(!startWithDot || _userSettingsService.FoldersSettingsService.ShowDotFiles);

					if (shouldBeListed)
					{
						var item = GetListedItemAsync(match.FilePath, findData);
						if (item is not null)
							results.Add(item);
					}

					NativeFindStorageItemHelper.FindClose(hFile);
				}
				else
				{
					try
					{
						IStorageItem item = (BaseStorageFile)await GetStorageFileAsync(match.FilePath);
						item ??= (BaseStorageFolder)await GetStorageFolderAsync(match.FilePath);

						if (!item.Name.StartsWith('.') || _userSettingsService.FoldersSettingsService.ShowDotFiles)
							results.Add(await GetListedItemAsync(item));
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, "Error creating ListedItem from StorageItem");
					}
				}

				if (token.IsCancellationRequested)
					return;

				if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
					SearchTick?.Invoke(this, EventArgs.Empty);
			}
		}

		private async Task AddItemsAsync(string folder, IList<ListedItem> results, CancellationToken token)
		{
			if (AQSQuery.StartsWith("tag:", StringComparison.Ordinal))
			{
				await SearchTagsAsync(folder, results, token);
			}
			else
			{
				var workingFolder = await GetStorageFolderAsync(folder);

				var hiddenOnlyFromWin32 = false;
				if (workingFolder)
				{
					await SearchAsync(workingFolder, results, token);

					hiddenOnlyFromWin32 = (results.Count != 0);
				}

				if (!IsAQSQuery && (!hiddenOnlyFromWin32 || _userSettingsService.FoldersSettingsService.ShowHiddenItems))
					await SearchWithWin32Async(folder, hiddenOnlyFromWin32, UsedMaxItemCount - (uint)results.Count, results, token);
			}
		}

		private async Task SearchWithWin32Async(string folder, bool hiddenOnly, uint maxItemCount, IList<ListedItem> results, CancellationToken token)
		{
			//var sampler = new IntervalSampler(500);
			(IntPtr hFile, NativeFindStorageItemHelper.WIN32_FIND_DATA findData) = await Task.Run(() =>
			{
				int additionalFlags = NativeFindStorageItemHelper.FIND_FIRST_EX_LARGE_FETCH;

				IntPtr hFileTsk = NativeFindStorageItemHelper.FindFirstFileExFromApp(
					$"{folder}\\{QueryWithWildcard}",
					NativeFindStorageItemHelper.FINDEX_INFO_LEVELS.FindExInfoBasic,
					out NativeFindStorageItemHelper.WIN32_FIND_DATA findDataTsk,
					NativeFindStorageItemHelper.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
					IntPtr.Zero,
					additionalFlags);

				return (hFileTsk, findDataTsk);
			})
			.WithTimeoutAsync(TimeSpan.FromSeconds(5));

			if (hFile != IntPtr.Zero && hFile.ToInt64() != -1)
			{
				await Task.Run(() =>
				{
					var hasNextFile = false;
					do
					{
						if (results.Count >= maxItemCount)
							break;

						var itemPath = Path.Combine(folder, findData.cFileName);

						var isSystem = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.System) == SystemIO.FileAttributes.System;
						var isHidden = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Hidden) == SystemIO.FileAttributes.Hidden;
						var startWithDot = findData.cFileName.StartsWith('.');

						bool shouldBeListed =
							(hiddenOnly
								? isHidden && (!isSystem || !_userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles)
								: !isHidden ||(_userSettingsService.FoldersSettingsService.ShowHiddenItems && (!isSystem || _userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
							(!startWithDot || _userSettingsService.FoldersSettingsService.ShowDotFiles);

						if (shouldBeListed)
						{
							var item = GetListedItemAsync(itemPath, findData);
							if (item is not null)
								results.Add(item);
						}

						if (token.IsCancellationRequested)
							break;

						if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
							SearchTick?.Invoke(this, EventArgs.Empty);

						hasNextFile = NativeFindStorageItemHelper.FindNextFile(hFile, out findData);
					} while (hasNextFile);

					NativeFindStorageItemHelper.FindClose(hFile);
				});
			}
		}

		private ListedItem GetListedItemAsync(string itemPath, NativeFindStorageItemHelper.WIN32_FIND_DATA findData)
		{
			ListedItem listedItem = null;

			var isHidden = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Hidden) == SystemIO.FileAttributes.Hidden;
			var isFolder = ((SystemIO.FileAttributes)findData.dwFileAttributes & SystemIO.FileAttributes.Directory) == SystemIO.FileAttributes.Directory;

			if (!isFolder)
			{
				string itemFileExtension = null;
				string itemType = null;

				if (findData.cFileName.Contains('.', StringComparison.Ordinal))
				{
					itemFileExtension = Path.GetExtension(itemPath);
					itemType = itemFileExtension.Trim('.') + " " + itemType;
				}

				listedItem = new ListedItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					ItemNameRaw = findData.cFileName,
					ItemPath = itemPath,
					IsHiddenItem = isHidden,
					LoadFileIcon = false,
					FileExtension = itemFileExtension,
					ItemType = itemType,
					Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
				};
			}
			else
			{
				if (findData.cFileName != "." && findData.cFileName != "..")
				{
					listedItem = new ListedItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemNameRaw = findData.cFileName,
						ItemPath = itemPath,
						IsHiddenItem = isHidden,
						LoadFileIcon = false,
						Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
					};
				}
			}

			// Only load icon for search box suggestions
			if (listedItem is not null && MaxItemCount > 0) 
			{
				_ = FileThumbnailHelper.LoadIconFromPathAsync(listedItem.ItemPath, ThumbnailSize, ThumbnailMode.ListView, isFolder)
					.ContinueWith((t) =>
					{
						if (t.IsCompletedSuccessfully && t.Result is not null)
						{
							_ = FilesystemTasks.Wrap(() => App.Window.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								listedItem.FileImage = await t.Result.ToBitmapAsync();
							},
							Microsoft.UI.Dispatching.DispatcherQueuePriority.Low));
						}
					});
			}

			return listedItem;
		}

		private async Task<ListedItem> GetListedItemAsync(IStorageItem item)
		{
			ListedItem listedItem = null;

			if (item.IsOfType(StorageItemTypes.Folder))
			{
				var folder = item.AsBaseStorageFolder();
				var props = await folder.GetBasicPropertiesAsync();

				if (folder is BinStorageFolder binFolder)
				{
					listedItem = new RecycleBinItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemNameRaw = folder.DisplayName,
						ItemPath = folder.Path,
						ItemDateModifiedReal = props.DateModified,
						ItemDateCreatedReal = folder.DateCreated,
						NeedsPlaceholderGlyph = false,
						Opacity = 1,
						ItemDateDeletedReal = binFolder.DateDeleted,
						ItemOriginalPath = binFolder.OriginalPath
					};
				}
				else
				{
					listedItem = new ListedItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemNameRaw = folder.DisplayName,
						ItemPath = folder.Path,
						ItemDateModifiedReal = props.DateModified,
						ItemDateCreatedReal = folder.DateCreated,
						NeedsPlaceholderGlyph = false,
						Opacity = 1
					};
				}
			}
			else if (item.IsOfType(StorageItemTypes.File))
			{
				var file = item.AsBaseStorageFile();
				var props = await file.GetBasicPropertiesAsync();

				string itemFileExtension = null;
				string itemType = null;

				if (file.Name.Contains('.', StringComparison.Ordinal))
				{
					itemFileExtension = Path.GetExtension(file.Path);
					itemType = itemFileExtension.Trim('.') + " " + itemType;
				}

				var itemSize = props.Size.ToSizeString();

				if (file is BinStorageFile binFile)
				{
					listedItem = new RecycleBinItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						ItemNameRaw = file.Name,
						ItemPath = file.Path,
						LoadFileIcon = false,
						FileExtension = itemFileExtension,
						FileSizeBytes = (long)props.Size,
						FileSize = itemSize,
						ItemDateModifiedReal = props.DateModified,
						ItemDateCreatedReal = file.DateCreated,
						ItemType = itemType,
						NeedsPlaceholderGlyph = false,
						Opacity = 1,
						ItemDateDeletedReal = binFile.DateDeleted,
						ItemOriginalPath = binFile.OriginalPath
					};
				}
				else
				{
					listedItem = new ListedItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						ItemNameRaw = file.Name,
						ItemPath = file.Path,
						LoadFileIcon = false,
						FileExtension = itemFileExtension,
						FileSizeBytes = (long)props.Size,
						FileSize = itemSize,
						ItemDateModifiedReal = props.DateModified,
						ItemDateCreatedReal = file.DateCreated,
						ItemType = itemType,
						NeedsPlaceholderGlyph = false,
						Opacity = 1
					};
				}
			}

			// Only load icon for search box suggestions
			if (listedItem is not null && MaxItemCount > 0)
			{
				var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(item, ThumbnailSize, ThumbnailMode.ListView);

				if (iconData is not null)
					listedItem.FileImage = await iconData.ToBitmapAsync();
				else
					listedItem.NeedsPlaceholderGlyph = true;
			}

			return listedItem;
		}

		private QueryOptions ToQueryOptions()
		{
			var query = new QueryOptions
			{
				FolderDepth = FolderDepth.Deep,
				UserSearchFilter = AQSQuery ?? string.Empty,
				IndexerOption = SearchUnindexedItems
					? IndexerOption.DoNotUseIndexer
					: IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
			};

			query.SortOrder.Clear();
			query.SortOrder.Add(new SortEntry { PropertyName = "System.Search.Rank", AscendingOrder = false });
			query.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, null);
			query.SetThumbnailPrefetch(ThumbnailMode.ListView, 24, ThumbnailOptions.UseCurrentScale);

			return query;
		}

		private static Task<FilesystemResult<BaseStorageFolder>> GetStorageFolderAsync(string path)
		{
			return FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
		}

		private static Task<FilesystemResult<BaseStorageFile>> GetStorageFileAsync(string path)
		{
			return FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
		}
	}
}
