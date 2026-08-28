// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using FileAttributes = System.IO.FileAttributes;
using WIN32_FIND_DATA = Files.App.Helpers.Win32PInvoke.WIN32_FIND_DATA;

namespace Files.App.Utils.Storage
{
	public sealed class FolderSearch
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		private readonly ILogger logger = Ioc.Default.GetRequiredService<ILogger<FolderSearch>>();

		private static readonly string folderTypeTextLocalized = Strings.Folder.GetLocalizedResource();

		private const uint defaultStepSize = 500;

		public string? Query { get; set; }

		public string? Folder { get; set; }

		public uint MaxItemCount { get; set; } = 0; // 0: no limit

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
					var escaped = QueryWithWildcard.Replace("\"", "\\\"");
					return QueryWithWildcard.Contains(' ') ? $"System.FileName:\"{escaped}\"" : $"System.FileName:{QueryWithWildcard}";
				}
			}
		}

		public async Task SearchAsync(IList<ListedItem> results, CancellationToken token)
		{
			try
			{
				if (App.LibraryManager.TryGetLibrary(Folder, out var library))
				{
					await AddItemsForLibraryAsync(library, results, token);
				}
				else if (Folder == "Home")
				{
					await AddItemsForHomeAsync(results, token);
				}
				else
				{
					await AddItemsAsync(Folder ?? throw new InvalidOperationException("The search folder has not been set."), results, token);
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception e)
			{
				App.Logger.LogWarning(e, "Search failure");
			}
		}

		private async Task AddItemsForHomeAsync(IList<ListedItem> results, CancellationToken token)
		{
			if (IsTagQuery(AQSQuery))
			{
				await SearchTagsAsync("", results, token); // Search tags everywhere, not only local drives
			}
			else
			{
				foreach (var drive in drivesViewModel.Drives.ToList().Cast<DriveItem>().Where(x => !x.IsNetwork))
				{
					await AddItemsAsync(drive.Path!, results, token);
				}
			}
		}

		public async Task<ObservableCollection<ListedItem>> SearchAsync()
		{
			ObservableCollection<ListedItem> results = [];
			try
			{
				var token = CancellationToken.None;
				if (App.LibraryManager.TryGetLibrary(Folder, out var library))
				{
					await AddItemsForLibraryAsync(library, results, token);
				}
				else if (Folder == "Home")
				{
					await AddItemsForHomeAsync(results, token);
				}
				else
				{
					await AddItemsAsync(Folder ?? throw new InvalidOperationException("The search folder has not been set."), results, token);
				}
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
			var stepSize = Math.Min(defaultStepSize, UsedMaxItemCount);
			var options = ToQueryOptions();

			var queryResult = folder.CreateItemQueryWithOptions(options);
			var items = await queryResult.GetItemsAsync(0, stepSize).AsTask(token);

			while (items.Count > 0)
			{
				foreach (IStorageItem item in items)
				{
					if (token.IsCancellationRequested)
					{
						return;
					}

					try
					{
						if (!item.Name.StartsWith('.') || UserSettingsService.FoldersSettingsService.ShowDotFiles)
							results.Add(await GetListedItemAsync(item));
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, "Error creating ListedItem from StorageItem");
					}

					if (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/)
					{
						SearchTick?.Invoke(this, EventArgs.Empty);
					}
				}

				index += (uint)items.Count;
				stepSize = Math.Min(defaultStepSize, UsedMaxItemCount - (uint)results.Count);
				items = await queryResult.GetItemsAsync(index, stepSize).AsTask(token);
			}
		}

		private async Task AddItemsForLibraryAsync(LibraryLocationItem library, IList<ListedItem> results, CancellationToken token)
		{
			foreach (var folder in library.Folders)
			{
				await AddItemsAsync(folder, results, token);
			}
		}

		private bool IsTagQuery(string query)
		{
			return query?.Contains("tag:", StringComparison.OrdinalIgnoreCase) == true;
		}

		public static string FormatTagQuery(string tagName)
		{
			if (tagName.Contains(' ') || tagName.Contains('"') || tagName.Contains(','))
			{
				return $"tag:\"{tagName.Replace("\"", "\"\"")}\"";
			}
			return $"tag:{tagName}";
		}

		private TagQueryExpression ParseTagQuery(string query)
		{
			var expression = new TagQueryExpression();
			var orParts = Regex.Split(query, @"\s+OR\s+", RegexOptions.IgnoreCase);

			foreach (var orPart in orParts)
			{
				var andGroup = new List<TagTerm>();
				var andParts = Regex.Split(orPart, @"\s+AND\s+", RegexOptions.IgnoreCase);

				foreach (var andPart in andParts)
				{
					var matches = Regex.Matches(andPart.Trim(), @"(NOT\s+)?tag:(?:""([^""]+)""|([^\s""]+))", RegexOptions.IgnoreCase);
					foreach (Match match in matches)
					{
						var isExclude = !string.IsNullOrEmpty(match.Groups[1].Value);
						var tagValue = match.Groups[2].Value;
						if (string.IsNullOrEmpty(tagValue))
							tagValue = match.Groups[3].Value;

						if (string.IsNullOrEmpty(tagValue))
						{
							logger.LogWarning("Failed to parse tag query.");
							continue;
						}

						var tagValues = tagValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
						var tagUids = new HashSet<string>();

						foreach (var tagName in tagValues)
						{
							var uids = fileTagsSettingsService.GetTagsByName(tagName).Select(t => t.Uid);
							foreach (var uid in uids)
							{
								tagUids.Add(uid);
							}
						}

						andGroup.Add(new TagTerm { TagUids = tagUids, IsExclude = isExclude });
					}
				}

				if (andGroup.Count > 0)
				{
					expression.OrGroups.Add(andGroup);
				}
			}

			return expression;
		}

		private bool MatchesTagExpression(IEnumerable<string>? fileTags, TagQueryExpression expression)
		{
			// Imported/synced tag entries can deserialize with a null Tags array, which would NRE on fileTags.Contains below.
			fileTags ??= [];

			foreach (var orGroup in expression.OrGroups)
			{
				bool groupMatches = true;
				foreach (var term in orGroup)
				{
					if (term.IsExclude)
					{
						if (term.TagUids.Count > 0 && term.TagUids.Any(fileTags.Contains))
						{
							groupMatches = false;
							break;
						}
					}
					else
					{
						if (term.TagUids.Count == 0 || !term.TagUids.Any(fileTags.Contains))
						{
							groupMatches = false;
							break;
						}
					}
				}

				if (groupMatches)
				{
					return true;
				}
			}

			return false;
		}

		private async Task SearchTagsAsync(string folder, IList<ListedItem> results, CancellationToken token)
		{
			//var sampler = new IntervalSampler(500);
			var expression = ParseTagQuery(AQSQuery);

			if (expression.OrGroups.Count == 0)
			{
				return;
			}

			var dbInstance = FileTagsHelper.GetDbInstance();
			var matches = dbInstance.GetAllUnderPath(folder)
				.Where(x => MatchesTagExpression(x.Tags, expression));
			if (string.IsNullOrEmpty(folder))
				matches = matches.Where(x => !StorageTrashBinService.IsUnderTrashBin(x.FilePath));

			foreach (var match in matches)
			{
				if (token.IsCancellationRequested)
					return;

				(Win32PInvoke.SafeFindHandle? hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
				{
					int additionalFlags = Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH;
					var hFileTsk = Win32PInvoke.FindFirstFileExFromAppSafe(match.FilePath, Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
						out WIN32_FIND_DATA findDataTsk, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
					return (hFileTsk, findDataTsk);
				}).WithTimeoutAsync(TimeSpan.FromSeconds(5));
				if (token.IsCancellationRequested)
				{
					hFile?.Dispose();
					return;
				}

				if (hFile is { IsInvalid: false })
				{
					using (hFile)
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
							if (item is not null && !token.IsCancellationRequested)
								results.Add(item);
						}
					}
				}
				else
				{
					hFile?.Dispose();
					try
					{
						IStorageItem? item = (await GetStorageFileAsync(match.FilePath)).Result;
						item ??= (await GetStorageFolderAsync(match.FilePath)).Result;
						item = item
							?? throw new InvalidOperationException($"The search item '{match.FilePath}' could not be opened.");
						if (!item.Name.StartsWith('.') || UserSettingsService.FoldersSettingsService.ShowDotFiles)
						{
							var listedItem = await GetListedItemAsync(item);
							if (!token.IsCancellationRequested)
								results.Add(listedItem);
						}
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

		private async Task AddItemsAsync(string folder, IList<ListedItem> results, CancellationToken token)
		{
			if (IsTagQuery(AQSQuery))
			{
				await SearchTagsAsync(folder, results, token);
			}
			else
			{
				var workingFolder = await GetStorageFolderAsync(folder);

				var hiddenOnlyFromWin32 = false;
				if (workingFolder)
				{
					var storageFolder = workingFolder.Result
						?? throw new InvalidOperationException($"The search folder '{folder}' could not be opened.");
					await SearchAsync(storageFolder, results, token);
					hiddenOnlyFromWin32 = (results.Count != 0);
				}

				if (!IsAQSQuery)
				{
					await SearchWithWin32Async(folder, hiddenOnlyFromWin32, UsedMaxItemCount - (uint)results.Count, results, token);
				}
			}
		}

		private async Task SearchWithWin32Async(string folder, bool hiddenOnly, uint maxItemCount, IList<ListedItem> results, CancellationToken token)
		{
			//var sampler = new IntervalSampler(500);
			if (token.IsCancellationRequested)
				return;

			(Win32PInvoke.SafeFindHandle? hFile, WIN32_FIND_DATA findData) = await Task.Run(() =>
			{
				int additionalFlags = Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH;
				var hFileTsk = Win32PInvoke.FindFirstFileExFromAppSafe($"{folder}\\*{QueryWithWildcard}", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
					out WIN32_FIND_DATA findDataTsk, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
				return (hFileTsk, findDataTsk);
			}).WithTimeoutAsync(TimeSpan.FromSeconds(5));
			if (token.IsCancellationRequested)
			{
				hFile?.Dispose();
				return;
			}

			var pendingShortcuts = new List<(string Path, WIN32_FIND_DATA FindData)>();

			if (hFile is { IsInvalid: false } findHandle)
			{
				// Always enter the delegate so the find handle is disposed; cancellation is checked before mutations.
				await Task.Run(() =>
				{
					using (findHandle)
					{
						var rawHandle = findHandle.DangerousGetHandle();
						var hasNextFile = false;
						do
						{
							if (token.IsCancellationRequested)
								break;

							if (results.Count >= maxItemCount)
								break;
							var itemPath = Path.Combine(folder, findData.cFileName);

							var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
							var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
							var startWithDot = findData.cFileName.StartsWith('.');
							var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(findData.cFileName);

							bool shouldBeListed = (hiddenOnly ?
								(!isHidden && isShortcut) || (isHidden && UserSettingsService.FoldersSettingsService.ShowHiddenItems && (!isSystem || UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles)) :
								!isHidden || (UserSettingsService.FoldersSettingsService.ShowHiddenItems && (!isSystem || UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
								(!startWithDot || UserSettingsService.FoldersSettingsService.ShowDotFiles);

							if (shouldBeListed)
							{
								if (isShortcut)
								{
									pendingShortcuts.Add((itemPath, findData));
								}
								else
								{
									var item = GetListedItemAsync(itemPath, findData);
									if (item is not null && !token.IsCancellationRequested)
										results.Add(item);
								}
							}

							if (!token.IsCancellationRequested && (results.Count == 32 || results.Count % 300 == 0 /*|| sampler.CheckNow()*/))
								SearchTick?.Invoke(this, EventArgs.Empty);

							hasNextFile = Win32PInvoke.FindNextFile(rawHandle, out findData);
						} while (hasNextFile);
					}
				});
			}
			else
			{
				hFile?.Dispose();
			}

			foreach (var (itemPath, itemFindData) in pendingShortcuts)
			{
				if (results.Count >= maxItemCount || token.IsCancellationRequested)
					break;

				var isUrl = FileExtensionHelpers.IsWebLinkFile(itemFindData.cFileName);
				var shortcutFindData = itemFindData;
				var isHidden = ((FileAttributes)shortcutFindData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
				Win32PInvoke.FileTimeToSystemTime(in shortcutFindData.ftLastWriteTime, out Win32PInvoke.SYSTEMTIME modifiedTime);
				Win32PInvoke.FileTimeToSystemTime(in shortcutFindData.ftCreationTime, out Win32PInvoke.SYSTEMTIME createdTime);
				var fileSize = Win32FindDataExtensions.GetSize(shortcutFindData);
				var itemFileExtension = shortcutFindData.cFileName.Contains('.', StringComparison.Ordinal) ? Path.GetExtension(itemPath)! : string.Empty;

				var shortcutItem = new ShortcutItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					FileExtension = itemFileExtension,
					IsHiddenItem = isHidden,
					Opacity = isHidden ? Constants.UI.DimItemOpacity : 1,
					FileImage = null,
					LoadFileIcon = false,
					ItemNameRaw = shortcutFindData.cFileName,
					ItemDateModifiedReal = modifiedTime.ToDateTime(),
					ItemDateCreatedReal = createdTime.ToDateTime(),
					ItemType = isUrl ? Strings.ShortcutWebLinkFileType.GetLocalizedResource() : Strings.Shortcut.GetLocalizedResource(),
					ItemPath = itemPath,
					FileSize = fileSize.ToSizeString(),
					FileSizeBytes = fileSize,
					IsUrl = isUrl,
				};

				if (results.Any(r => string.Equals(r.ItemPath, itemPath, StringComparison.OrdinalIgnoreCase)))
					continue;

				if (MaxItemCount == 0)
				{
					_ = FileOperationsHelpers.ParseLinkAsync(itemPath).ContinueWith((t) =>
					{
						if (t.IsCompletedSuccessfully && t.Result is not null)
						{
							_ = FilesystemTasks.Wrap(() => MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							{
								shortcutItem.TargetPath = t.Result.TargetPath;
								shortcutItem.Arguments = t.Result.Arguments;
								shortcutItem.WorkingDirectory = t.Result.WorkingDirectory;
								shortcutItem.RunAsAdmin = t.Result.RunAsAdmin;
								shortcutItem.ShowWindowCommand = t.Result.ShowWindowCommand;
								shortcutItem.PrimaryItemAttribute = t.Result.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File;
							}));
						}
					});
				}
				else
				{
					var iconResult = await FileThumbnailHelper.GetIconAsync(
						itemPath,
						Constants.ShellIconSizes.Small,
						false,
						IconOptions.ReturnIconOnly);
					if (iconResult is not null)
						shortcutItem.FileImage = await iconResult.ToBitmapAsync();
				}

				if (token.IsCancellationRequested)
					break;

				results.Add(shortcutItem);

				if (!token.IsCancellationRequested && (results.Count == 32 || results.Count % 300 == 0))
				{
					SearchTick?.Invoke(this, EventArgs.Empty);
				}
			}

			if (token.IsCancellationRequested)
				return;

			(Win32PInvoke.SafeFindHandle? hSubDir, WIN32_FIND_DATA subDirData) = await Task.Run(() =>
			{
				int additionalFlags = Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH;
				var hSubDirTsk = Win32PInvoke.FindFirstFileExFromAppSafe($"{folder}\\*", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
					out WIN32_FIND_DATA subDirDataTsk, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
				return (hSubDirTsk, subDirDataTsk);
			}).WithTimeoutAsync(TimeSpan.FromSeconds(5));
			if (token.IsCancellationRequested)
			{
				hSubDir?.Dispose();
				return;
			}

			if (hSubDir is { IsInvalid: false } subDirectoryHandle)
			{
				var subDirectories = new List<string>();

				// Always enter the delegate so the find handle is disposed; cancellation is checked before mutations.
				await Task.Run(() =>
				{
					using (subDirectoryHandle)
					{
						var rawHandle = subDirectoryHandle.DangerousGetHandle();
						var hasNextDir = false;
						do
						{
							if (token.IsCancellationRequested)
								break;

							var isDirectory = ((FileAttributes)subDirData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory;
							if (isDirectory && subDirData.cFileName != "." && subDirData.cFileName != "..")
								subDirectories.Add(Path.Combine(folder, subDirData.cFileName));

							hasNextDir = Win32PInvoke.FindNextFile(rawHandle, out subDirData);
						} while (hasNextDir);
					}
				});

				foreach (var subDir in subDirectories)
				{
					if (results.Count >= maxItemCount || token.IsCancellationRequested)
						break;

					await SearchWithWin32Async(subDir, hiddenOnly, maxItemCount - (uint)results.Count, results, token);
				}
			}
			else
			{
				hSubDir?.Dispose();
			}
		}

		private ListedItem? GetListedItemAsync(string itemPath, WIN32_FIND_DATA findData)
		{
			ListedItem? listedItem = null;
			var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
			var isFolder = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory;
			Win32PInvoke.FileTimeToSystemTime(in findData.ftLastWriteTime, out Win32PInvoke.SYSTEMTIME systemModifiedTimeOutput);
			Win32PInvoke.FileTimeToSystemTime(in findData.ftCreationTime, out Win32PInvoke.SYSTEMTIME systemCreatedTimeOutput);

			if (!isFolder)
			{
				string? itemFileExtension = null;
				string? itemType = null;
				long fileSize = Win32FindDataExtensions.GetSize(findData);
				if (findData.cFileName.Contains('.', StringComparison.Ordinal))
				{
					itemFileExtension = Path.GetExtension(itemPath);
					itemType = itemFileExtension!.Trim('.') + " " + itemType;
				}

				listedItem = new ListedItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					ItemNameRaw = findData.cFileName,
					ItemPath = itemPath,
					ItemDateModifiedReal = systemModifiedTimeOutput.ToDateTime(),
					ItemDateCreatedReal = systemCreatedTimeOutput.ToDateTime(),
					IsHiddenItem = isHidden,
					LoadFileIcon = false,
					FileExtension = itemFileExtension,
					ItemType = itemType,
					Opacity = isHidden ? Constants.UI.DimItemOpacity : 1,
					FileSize = fileSize.ToSizeString(),
					FileSizeBytes = fileSize,
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
						ItemDateModifiedReal = systemModifiedTimeOutput.ToDateTime(),
						ItemDateCreatedReal = systemCreatedTimeOutput.ToDateTime(),
						IsHiddenItem = isHidden,
						LoadFileIcon = false,
						ItemType = folderTypeTextLocalized,
						Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
					};
				}
			}

			if (listedItem is not null && MaxItemCount > 0) // Only load icon for searchbox suggestions
			{
				_ = FileThumbnailHelper.GetIconAsync(
					listedItem.ItemPath,
					Constants.ShellIconSizes.Small,
					isFolder,
					IconOptions.ReturnIconOnly)
					.ContinueWith((t) =>
					{
						if (t.IsCompletedSuccessfully && t.Result is not null)
						{
							_ = FilesystemTasks.Wrap(() => MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								var bitmapImage = await t.Result.ToBitmapAsync();
								if (bitmapImage is not null)
									listedItem.FileImage = bitmapImage;
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low));
						}
					});
			}

			return listedItem;
		}

		private async Task<ListedItem> GetListedItemAsync(IStorageItem item)
		{
			ListedItem? listedItem = null;
			if (item.IsOfType(StorageItemTypes.Folder))
			{
				var folder = item.AsBaseStorageFolder()
					?? throw new InvalidOperationException($"The search result '{item.Path}' could not be opened as a folder.");

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
						ItemType = folderTypeTextLocalized,
						Opacity = 1,
						FileSize = props.Size.ToSizeString(),
						FileSizeBytes = (long)props.Size,
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
						ItemType = folderTypeTextLocalized,
						Opacity = 1
					};
				}
			}
			else if (item.IsOfType(StorageItemTypes.File))
			{
				var file = item.AsBaseStorageFile()
					?? throw new InvalidOperationException($"The search result '{item.Path}' could not be opened as a file.");

				var props = await file.GetBasicPropertiesAsync();
				string? itemFileExtension = null;
				string? itemType = null;
				if (file.Name.Contains('.', StringComparison.Ordinal))
				{
					itemFileExtension = Path.GetExtension(file.Path);
					itemType = itemFileExtension!.Trim('.') + " " + itemType;
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
						Opacity = 1,
						ItemDateDeletedReal = binFile.DateDeleted,
						ItemOriginalPath = binFile.OriginalPath
					};
				}
				else if (FileExtensionHelpers.IsShortcutOrUrlFile(file.Path))
				{
					var isUrl = FileExtensionHelpers.IsWebLinkFile(file.Path);
					var shortcutItem = new ShortcutItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = false,
						Opacity = 1,
						FileImage = null,
						LoadFileIcon = false,
						ItemNameRaw = file.Name,
						ItemDateModifiedReal = props.DateModified,
						ItemDateCreatedReal = file.DateCreated,
						ItemType = isUrl ? Strings.ShortcutWebLinkFileType.GetLocalizedResource() : Strings.Shortcut.GetLocalizedResource(),
						ItemPath = file.Path,
						FileSize = itemSize,
						FileSizeBytes = (long)props.Size,
						IsUrl = isUrl,
					};
					if (MaxItemCount == 0)
					{
						_ = FileOperationsHelpers.ParseLinkAsync(file.Path).ContinueWith((t) =>
						{
							if (t.IsCompletedSuccessfully && t.Result is not null)
							{
								_ = FilesystemTasks.Wrap(() => MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
								{
									shortcutItem.TargetPath = t.Result.TargetPath;
									shortcutItem.Arguments = t.Result.Arguments;
									shortcutItem.WorkingDirectory = t.Result.WorkingDirectory;
									shortcutItem.RunAsAdmin = t.Result.RunAsAdmin;
									shortcutItem.ShowWindowCommand = t.Result.ShowWindowCommand;
									shortcutItem.PrimaryItemAttribute = t.Result.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File;
								}));
							}
						});
					}
					listedItem = shortcutItem;
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
						Opacity = 1
					};
				}
			}
			if (listedItem is not null && MaxItemCount > 0) // Only load icon for searchbox suggestions
			{
				var iconResult = await FileThumbnailHelper.GetIconAsync(
					item.Path,
					Constants.ShellIconSizes.Small,
					item.IsOfType(StorageItemTypes.Folder),
					IconOptions.ReturnIconOnly);

				if (iconResult is not null)
					listedItem.FileImage = await iconResult.ToBitmapAsync();
			}
			return listedItem
				?? throw new InvalidOperationException($"The search result '{item.Path}' is neither a file nor a folder.");
		}

		private QueryOptions ToQueryOptions()
		{
			var query = new QueryOptions
			{
				FolderDepth = FolderDepth.Deep,
				UserSearchFilter = AQSQuery ?? string.Empty,
			};

			query.IndexerOption = IndexerOption.UseIndexerWhenAvailable;

			query.SortOrder.Clear();
			query.SortOrder.Add(new SortEntry { PropertyName = "System.Search.Rank", AscendingOrder = false });

			query.SetPropertyPrefetch(PropertyPrefetchOptions.BasicProperties, null);
			query.SetThumbnailPrefetch(ThumbnailMode.ListView, 24, ThumbnailOptions.UseCurrentScale);

			return query;
		}

		private static Task<FilesystemResult<BaseStorageFolder>> GetStorageFolderAsync(string path)
			=> FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

		private static Task<FilesystemResult<BaseStorageFile>> GetStorageFileAsync(string path)
			=> FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
	}
}
