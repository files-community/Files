// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using static Files.Backend.Helpers.NativeFindStorageItemHelper;
using FileAttributes = System.IO.FileAttributes;
using WIN32_FIND_DATA = Files.Backend.Helpers.NativeFindStorageItemHelper.WIN32_FIND_DATA;

namespace Files.App.Filesystem.Search
{
	public class FolderSearch
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		private const uint defaultStepSize = 500;

		public string? Query { get; set; }
		public string? Folder { get; set; }

		public uint MaxItemCount { get; set; } = 0; // 0: no limit
		public uint ThumbnailSize { get; set; } = 24;
		public bool SearchUnindexedItems { get; set; } = false;

		private uint UsedMaxItemCount => MaxItemCount > 0 ? MaxItemCount : uint.MaxValue;

		public EventHandler? SearchTick;

		private bool IsAQSQuery => Query is not null && (Query.StartsWith('$') || Query.Contains(':', StringComparison.Ordinal));

		private string QueryWithWildcard
		{
			get
			{
				if (!string.IsNullOrEmpty(Query) && Query.Contains('.')) // ".docx" -> "*.docx"
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
				// if the query starts with a $, assume the query is in aqs format, otherwise assume the user is searching for the file name
				if (Query is not null && Query.StartsWith('$'))
				{
					return Query.Substring(1);
				}
				else if (Query is not null && Query.Contains(':', StringComparison.Ordinal))
				{
					return Query;
				}
				else
				{
					return $"System.FileName:\"{QueryWithWildcard}\"";
				}
			}
		}

		public Task SearchAsync(IList<StandardItemViewModel> results, CancellationToken token)
		{
			try
			{
				if (App.LibraryManager.TryGetLibrary(Folder, out var library))
				{
					return AddItemsAsyncForLibrary(library, results, token);
				}
				else if (Folder == "Home")
				{
					return AddItemsAsyncForHome(results, token);
				}
				else
				{
					return AddItemsAsync(Folder, results, token);
				}
			}
			catch (Exception e)
			{
				App.Logger.LogWarning(e, "Search failure");
			}

			return Task.CompletedTask;
		}

		private async Task AddItemsAsyncForHome(IList<StandardItemViewModel> results, CancellationToken token)
		{
			if (AQSQuery.StartsWith("tag:", StringComparison.Ordinal))
			{
				await SearchTagsAsync("", results, token); // Search tags everywhere, not only local drives
			}
			else
			{
				foreach (var drive in drivesViewModel.Drives.Cast<DriveItem>().Where(x => !x.IsNetwork))
				{
					await AddItemsAsync(drive.Path, results, token);
				}
			}
		}

		public async Task<ObservableCollection<StandardItemViewModel>> SearchAsync()
		{
			ObservableCollection<StandardItemViewModel> results = new ObservableCollection<StandardItemViewModel>();
			try
			{
				var token = CancellationToken.None;
				if (App.LibraryManager.TryGetLibrary(Folder, out var library))
				{
					await AddItemsAsyncForLibrary(library, results, token);
				}
				else if (Folder == "Home")
				{
					await AddItemsAsyncForHome(results, token);
				}
				else
				{
					await AddItemsAsync(Folder, results, token);
				}
			}
			catch (Exception e)
			{
				App.Logger.LogWarning(e, "Search failure");
			}

			return results;
		}

		private async Task AddItemsAsyncForLibrary(LibraryLocationItem library, IList<StandardItemViewModel> results, CancellationToken token)
		{
			foreach (var folder in library.Folders)
			{
				await AddItemsAsync(folder, results, token);
			}
		}

		private async Task SearchTagsAsync(string folder, IList<StandardItemViewModel> results, CancellationToken token)
		{
			//var sampler = new IntervalSampler(500);
			var tags = AQSQuery.Substring("tag:".Length)?.Split(',').Where(t => !string.IsNullOrWhiteSpace(t))
				.SelectMany(t => fileTagsSettingsService.GetTagsByName(t), (_, t) => t.Uid).ToHashSet();
			if (tags?.Any() != true)
			{
				return;
			}

			var dbInstance = FileTagsHelper.GetDbInstance();
			var matches = dbInstance.GetAllUnderPath(folder)
				.Where(x => tags.All(x.Tags.Contains));

			foreach (var match in matches)
			{
				(IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
				{
					int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
					IntPtr hFileTsk = FindFirstFileExFromApp(match.FilePath, FINDEX_INFO_LEVELS.FindExInfoBasic,
						out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
					return (hFileTsk, findDataTsk);
				}).WithTimeoutAsync(TimeSpan.FromSeconds(5));

				if (hFile != IntPtr.Zero && hFile.ToInt64() != -1)
				{
					var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
					var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
					var startWithDot = findData.cFileName.StartsWith('.');

					bool shouldBeListed = (!isHidden ||
						(UserSettingsService.FoldersSettingsService.ShowHiddenItems &&
						(!isSystem || UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
						(!startWithDot || UserSettingsService.FoldersSettingsService.ShowDotFiles);

					if (shouldBeListed)
					{
						var item = GetListedItemAsync(match.FilePath, findData);
						if (item is not null)
						{
							results.Add(item);
						}
					}

					FindClose(hFile);
				}
				else
				{
					try
					{
						IStorageItem item = (BaseStorageFile)await GetStorageFileAsync(match.FilePath);
						item ??= (BaseStorageFolder)await GetStorageFolderAsync(match.FilePath);
						if (!item.Name.StartsWith('.') || UserSettingsService.FoldersSettingsService.ShowDotFiles)
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
				{
					SearchTick?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		private async Task AddItemsAsync(string folder, IList<StandardItemViewModel> results, CancellationToken token)
		{
			if (AQSQuery.StartsWith("tag:", StringComparison.Ordinal))
			{
				await SearchTagsAsync(folder, results, token);
			}
			else
			{
				if (!IsAQSQuery && (!hiddenOnlyFromWin32 || UserSettingsService.FoldersSettingsService.ShowHiddenItems))
				{
					await SearchWithWin32Async(folder, hiddenOnlyFromWin32, UsedMaxItemCount - (uint)results.Count, results, token);
				}
			}
		}

		private async Task SearchWithWin32Async(string folder, bool hiddenOnly, uint maxItemCount, IList<StandardItemViewModel> results, CancellationToken token)
		{
			//var sampler = new IntervalSampler(500);
			(IntPtr hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
			{
				int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;
				IntPtr hFileTsk = FindFirstFileExFromApp($"{folder}\\{QueryWithWildcard}", FINDEX_INFO_LEVELS.FindExInfoBasic,
					out WIN32_FIND_DATA findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
				return (hFileTsk, findDataTsk);
			}).WithTimeoutAsync(TimeSpan.FromSeconds(5));

			if (hFile != IntPtr.Zero && hFile.ToInt64() != -1)
			{
				await Task.Run(() =>
				{
					var hasNextFile = false;
					do
					{
						if (results.Count >= maxItemCount)
						{
							break;
						}
						var itemPath = Path.Combine(folder, findData.cFileName);

						var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
						var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
						var startWithDot = findData.cFileName.StartsWith('.');

						bool shouldBeListed = (hiddenOnly ?
							isHidden && (!isSystem || !UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles) :
							!isHidden || (UserSettingsService.FoldersSettingsService.ShowHiddenItems && (!isSystem || UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
							(!startWithDot || UserSettingsService.FoldersSettingsService.ShowDotFiles);

						if (shouldBeListed)
						{
							var item = GetListedItemAsync(itemPath, findData);
							if (item is not null)
							{
								results.Add(item);
							}
						}

						if (token.IsCancellationRequested)
						{
							break;
						}

						if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
						{
							SearchTick?.Invoke(this, EventArgs.Empty);
						}

						hasNextFile = FindNextFile(hFile, out findData);
					} while (hasNextFile);

					FindClose(hFile);
				});
			}
		}

		private QueryOptions ToQueryOptions()
		{
			var query = new QueryOptions
			{
				FolderDepth = FolderDepth.Deep,
				UserSearchFilter = AQSQuery ?? string.Empty,
			};

			query.IndexerOption = SearchUnindexedItems
				? IndexerOption.DoNotUseIndexer
				: IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties;

			query.SortOrder.Clear();
			query.SortOrder.Add(new SortEntry { PropertyName = "System.Search.Rank", AscendingOrder = false });

			query.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, null);
			query.SetThumbnailPrefetch(ThumbnailMode.ListView, 24, ThumbnailOptions.UseCurrentScale);

			return query;
		}
	}
}
