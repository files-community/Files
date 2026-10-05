// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System.IO;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using FileAttributes = System.IO.FileAttributes;

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

		public DispatcherQueue DispatcherQueue { get; set; } = MainWindow.Instance.DispatcherQueue;

		/// <summary>
		/// Raised on a throttle with the results found since the previous tick, on a background thread during Win32 walks.
		/// </summary>
		public event EventHandler<IReadOnlyList<ListedItem>>? SearchTick;

		private readonly IntervalSampler tickSampler = new(500);
		private readonly HashSet<string> indexedResultPaths = new(StringComparer.OrdinalIgnoreCase);
		private readonly List<ShortcutItem> shortcutResults = [];
		private List<ListedItem> pendingResults = [];
		private bool hasRaisedTick;

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

			try
			{
				if (MaxItemCount > 0)
					await LoadSuggestionIconsAsync(results, token);
				else
					await ResolveShortcutTargetsAsync(token);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception e)
			{
				App.Logger.LogWarning(e, "Failed to finalize search results");
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

		private void AddResult(IList<ListedItem> results, ListedItem item, CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;

			results.Add(item);
			pendingResults.Add(item);
			if (item is ShortcutItem shortcutItem)
				shortcutResults.Add(shortcutItem);

			RaiseSearchTickIfDue(token);
		}

		private void RaiseSearchTickIfDue(CancellationToken token)
		{
			if (pendingResults.Count == 0 || token.IsCancellationRequested || (hasRaisedTick && !tickSampler.CheckNow()))
				return;

			var batch = pendingResults;
			pendingResults = [];
			hasRaisedTick = true;

			SearchTick?.Invoke(this, batch);
		}

		private uint GetRemainingItemCount(IList<ListedItem> results)
			=> results.Count >= UsedMaxItemCount ? 0 : UsedMaxItemCount - (uint)results.Count;

		private async Task SearchAsync(BaseStorageFolder folder, IList<ListedItem> results, CancellationToken token)
		{
			uint index = 0;
			var stepSize = Math.Min(defaultStepSize, GetRemainingItemCount(results));
			if (stepSize == 0)
				return;

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
						{
							var listedItem = await GetListedItemAsync(item);
							AddResult(results, listedItem, token);
							if (listedItem.ItemPath is not null)
								indexedResultPaths.Add(listedItem.ItemPath);
						}
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, "Error creating ListedItem from StorageItem");
					}
				}

				index += (uint)items.Count;
				stepSize = Math.Min(defaultStepSize, GetRemainingItemCount(results));
				if (stepSize == 0)
					return;

				items = await queryResult.GetItemsAsync(index, stepSize).AsTask(token);
			}
		}

		// Awaited so the final sort treats folder shortcuts as folders
		private async Task ResolveShortcutTargetsAsync(CancellationToken token)
		{
			if (shortcutResults.Count == 0)
				return;

			var links = new ShellLinkItem?[shortcutResults.Count];
			await Parallel.ForEachAsync(
				Enumerable.Range(0, links.Length),
				new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = 4 },
				async (i, _) => links[i] = await FileOperationsHelpers.ParseLinkAsync(shortcutResults[i].GetRequiredPath(), resolveTarget: false));

			await DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				for (var i = 0; i < links.Length; i++)
				{
					if (links[i] is not { } link)
						continue;

					var shortcutItem = shortcutResults[i];
					shortcutItem.TargetPath = link.TargetPath;
					shortcutItem.Arguments = link.Arguments;
					shortcutItem.WorkingDirectory = link.WorkingDirectory;
					shortcutItem.RunAsAdmin = link.RunAsAdmin;
					shortcutItem.ShowWindowCommand = link.ShowWindowCommand;
					shortcutItem.PrimaryItemAttribute = link.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File;
				}
			});
		}

		private Task LoadSuggestionIconsAsync(IList<ListedItem> results, CancellationToken token)
		{
			return Task.WhenAll(results.Where(x => x.FileImage is null).Select(async item =>
			{
				var iconResult = await FileThumbnailHelper.GetIconAsync(
					item.GetRequiredPath(),
					Constants.ShellIconSizes.Small,
					item.PrimaryItemAttribute == StorageItemTypes.Folder,
					IconOptions.ReturnIconOnly);

				if (iconResult is null || token.IsCancellationRequested)
					return;

				await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					if (await iconResult.ToBitmapAsync() is { } bitmapImage)
						item.FileImage = bitmapImage;
				});
			}));
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

				(FindCloseSafeHandle? hFile, WIN32_FIND_DATAW findData) = await Task.Run(() =>
				{
					WIN32_FIND_DATAW findDataTsk = default;
					FindCloseSafeHandle hFileTsk;
					unsafe
					{
						hFileTsk = PInvoke.FindFirstFileEx(match.FilePath, FINDEX_INFO_LEVELS.FindExInfoBasic,
							&findDataTsk, FINDEX_SEARCH_OPS.FindExSearchNameMatch, FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
					}
					return (hFileTsk, findDataTsk);
				}).WithTimeoutAsync(TimeSpan.FromSeconds(5));
				if (token.IsCancellationRequested)
				{
					hFile?.Dispose();
					return;
				}

				if (hFile is { IsInvalid: false } tagSearchHandle)
				{
					using (tagSearchHandle)
					{
						string fileName = findData.cFileName.ToString();
						var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
						var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
						var startWithDot = fileName.StartsWith('.');

						bool shouldBeListed = (!isHidden ||
							(UserSettingsService.FoldersSettingsService.ShowHiddenItems &&
							(!isSystem || UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
							(!startWithDot || UserSettingsService.FoldersSettingsService.ShowDotFiles);

						if (shouldBeListed)
						{
							var item = GetListedItemAsync(match.FilePath, findData);
							if (item is not null)
								AddResult(results, item, token);
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
							AddResult(results, await GetListedItemAsync(item), token);
						}
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, "Error creating ListedItem from StorageItem");
					}
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
				var storageFolder = workingFolder ? workingFolder.Result : null;

				if (IsAQSQuery)
				{
					if (storageFolder is not null)
						await SearchAsync(storageFolder, results, token);

					return;
				}

				if (storageFolder is SystemStorageFolder && !DriveHelpers.IsMtpPath(folder) && !await IsFullyIndexedAsync(storageFolder))
				{
					await SearchWithWin32Async(folder, false, results, token);
					return;
				}

				var hiddenOnlyFromWin32 = false;
				if (storageFolder is not null)
				{
					var countBefore = results.Count;
					await SearchAsync(storageFolder, results, token);
					hiddenOnlyFromWin32 = results.Count != countBefore;
				}

				await SearchWithWin32Async(folder, hiddenOnlyFromWin32, results, token);
			}
		}

		private static async Task<bool> IsFullyIndexedAsync(BaseStorageFolder folder)
		{
			try
			{
				return await folder.GetIndexedStateAsync() == IndexedState.FullyIndexed;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private Task SearchWithWin32Async(string folder, bool hiddenOnly, IList<ListedItem> results, CancellationToken token)
		{
			return Task.Factory.StartNew(
				() => SearchWithWin32(folder, hiddenOnly, results, token),
				token,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Default)
				.WaitAsync(token);
		}

		private void SearchWithWin32(string root, bool hiddenOnly, IList<ListedItem> results, CancellationToken token)
		{
			var expression = FileSystemName.TranslateWin32Expression($"*{QueryWithWildcard}");
			var showHiddenItems = UserSettingsService.FoldersSettingsService.ShowHiddenItems;
			var showProtectedSystemFiles = UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles;
			var showDotFiles = UserSettingsService.FoldersSettingsService.ShowDotFiles;

			var pendingFolders = new Queue<(string Path, bool UseFindTimeout)>();
			pendingFolders.Enqueue((root, RequiresFindTimeout(root)));

			while (pendingFolders.TryDequeue(out var pendingFolder))
			{
				var (folder, useFindTimeout) = pendingFolder;
				if (token.IsCancellationRequested || results.Count >= UsedMaxItemCount)
					return;

				using (var findHandle = FindFirstFile(folder, useFindTimeout, out var findData, token))
				{
					if (findHandle is null || findHandle.IsInvalid)
						continue;

					do
					{
						var fileName = findData.cFileName.ToString();
						if (fileName is "." or "..")
							continue;

						var attributes = (FileAttributes)findData.dwFileAttributes;
						var isDirectory = attributes.HasFlag(FileAttributes.Directory);
						var reparseTag = attributes.HasFlag(FileAttributes.ReparsePoint) ? findData.dwReserved0 : 0;

						if (isDirectory && !IsNameSurrogateReparseTag(reparseTag))
							pendingFolders.Enqueue((Path.Combine(folder, fileName), useFindTimeout || IsCloudFilesReparseTag(reparseTag)));

						if (!FileSystemName.MatchesWin32Expression(expression, fileName))
							continue;

						var isSystem = attributes.HasFlag(FileAttributes.System);
						var isHidden = attributes.HasFlag(FileAttributes.Hidden);
						var startWithDot = fileName.StartsWith('.');
						var isShortcut = !isDirectory && FileExtensionHelpers.IsShortcutOrUrlFile(fileName);

						bool shouldBeListed = (hiddenOnly ?
							(!isHidden && isShortcut) || (isHidden && showHiddenItems && (!isSystem || showProtectedSystemFiles)) :
							!isHidden || (showHiddenItems && (!isSystem || showProtectedSystemFiles))) &&
							(!startWithDot || showDotFiles);

						var itemPath = Path.Combine(folder, fileName);
						if (!shouldBeListed || (hiddenOnly && indexedResultPaths.Contains(itemPath)))
							continue;

						try
						{
							if ((isShortcut ? GetShortcutItem(itemPath, findData) : GetListedItemAsync(itemPath, findData)) is { } item)
								AddResult(results, item, token);
						}
						catch (Exception ex)
						{
							App.Logger.LogWarning(ex, "Error creating ListedItem from Win32 find data");
						}
					}
					while (!token.IsCancellationRequested && results.Count < UsedMaxItemCount && PInvoke.FindNextFile(findHandle, out findData));
				}

				RaiseSearchTickIfDue(token);
			}
		}

		private static FindCloseSafeHandle? FindFirstFile(string folder, bool useTimeout, out WIN32_FIND_DATAW findData, CancellationToken token)
		{
			if (!useTimeout)
				return FindFirstFile(folder, out findData);

			var findTask = Task.Run(() => (Handle: FindFirstFile(folder, out var data), Data: data));
			try
			{
				if (findTask.Wait(TimeSpan.FromSeconds(5), token))
				{
					(var handle, findData) = findTask.Result;
					return handle;
				}
			}
			catch (OperationCanceledException)
			{
			}

			_ = findTask.ContinueWith(t => t.Result.Handle.Dispose(), TaskContinuationOptions.OnlyOnRanToCompletion);
			findData = default;
			return null;
		}

		private static unsafe FindCloseSafeHandle FindFirstFile(string folder, out WIN32_FIND_DATAW findData)
		{
			WIN32_FIND_DATAW data = default;
			var handle = PInvoke.FindFirstFileEx(Path.Join(folder, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic,
				&data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, FIND_FIRST_EX_FLAGS.FIND_FIRST_EX_LARGE_FETCH);
			findData = data;
			return handle;
		}

		private static bool RequiresFindTimeout(string root)
		{
			if (DriveHelpers.IsNetworkPath(root))
				return true;

			try
			{
				// Virtual cloud drives such as Google Drive report Fixed but aren't NTFS
				var drive = new DriveInfo(root);
				return drive.DriveType is not System.IO.DriveType.Fixed
					|| drive.DriveFormat is not ("NTFS" or "ReFS")
					|| File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint);
			}
			catch (Exception)
			{
				return true;
			}
		}

		// Junctions, symlinks and mount points
		private static bool IsNameSurrogateReparseTag(uint reparseTag)
			=> (reparseTag & 0x20000000) != 0;

		// IO_REPARSE_TAG_CLOUD and IO_REPARSE_TAG_CLOUD_1 to _F
		private static bool IsCloudFilesReparseTag(uint reparseTag)
			=> (reparseTag & 0xFFFF0FFF) == 0x9000001A;

		private static ShortcutItem GetShortcutItem(string itemPath, WIN32_FIND_DATAW findData)
		{
			string fileName = findData.cFileName.ToString();
			var isUrl = FileExtensionHelpers.IsWebLinkFile(fileName);
			var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
			PInvoke.FileTimeToSystemTime(findData.ftLastWriteTime, out SYSTEMTIME modifiedTime);
			PInvoke.FileTimeToSystemTime(findData.ftCreationTime, out SYSTEMTIME createdTime);
			var fileSize = Win32FindDataExtensions.GetSize(findData);

			return new ShortcutItem(null)
			{
				PrimaryItemAttribute = StorageItemTypes.File,
				FileExtension = fileName.Contains('.', StringComparison.Ordinal) ? Path.GetExtension(itemPath)! : string.Empty,
				IsHiddenItem = isHidden,
				Opacity = isHidden ? Constants.UI.DimItemOpacity : 1,
				FileImage = null,
				LoadFileIcon = false,
				ItemNameRaw = fileName,
				ItemDateModifiedReal = modifiedTime.ToDateTime(),
				ItemDateCreatedReal = createdTime.ToDateTime(),
				ItemType = isUrl ? Strings.ShortcutWebLinkFileType.GetLocalizedResource() : Strings.Shortcut.GetLocalizedResource(),
				ItemPath = itemPath,
				FileSize = fileSize.ToSizeString(),
				FileSizeBytes = fileSize,
				IsUrl = isUrl,
			};
		}

		private ListedItem? GetListedItemAsync(string itemPath, WIN32_FIND_DATAW findData)
		{
			string fileName = findData.cFileName.ToString();
			ListedItem? listedItem = null;
			var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
			var isFolder = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory;
			PInvoke.FileTimeToSystemTime(findData.ftLastWriteTime, out SYSTEMTIME systemModifiedTimeOutput);
			PInvoke.FileTimeToSystemTime(findData.ftCreationTime, out SYSTEMTIME systemCreatedTimeOutput);

			if (!isFolder)
			{
				string? itemFileExtension = null;
				string? itemType = null;
				long fileSize = Win32FindDataExtensions.GetSize(findData);
				if (fileName.Contains('.', StringComparison.Ordinal))
				{
					itemFileExtension = Path.GetExtension(itemPath);
					itemType = itemFileExtension!.Trim('.') + " " + itemType;
				}

				listedItem = new ListedItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					ItemNameRaw = fileName,
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
				if (fileName != "." && fileName != "..")
				{
					listedItem = new ListedItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemNameRaw = fileName,
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

			return query;
		}

		private static Task<FilesystemResult<BaseStorageFolder>> GetStorageFolderAsync(string path)
			=> FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));

		private static Task<FilesystemResult<BaseStorageFile>> GetStorageFileAsync(string path)
			=> FilesystemTasks.WrapNullable(() => StorageFileExtensions.DangerousGetFileFromPathAsync(path));
	}
}
