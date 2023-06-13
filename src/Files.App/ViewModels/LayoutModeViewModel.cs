// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageEnumerators;
using Files.App.ServicesImplementation.Settings;
using Files.App.Shell;
using Files.App.Storage.FtpStorage;
using Files.App.Storage.NativeStorage;
using Files.App.Storage.WindowsStorage;
using Files.Backend.Helpers;
using Files.Backend.Services;
using Files.Backend.Services.SizeProvider;
using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;
using Files.Shared.Services;
using FluentFTP;
using Microsoft.Extensions.Logging;
using System.CodeDom;
using System.IO;
using System.Security.Authentication;
using Windows.Storage;
using FtpHelpers = Files.App.Helpers.FtpHelpers;

namespace Files.App.ViewModels
{
	public class LayoutModeViewModel : ObservableObject
	{
		private readonly SemaphoreSlim enumFolderSemaphore;
		private readonly IUserSettingsService userSettingsService;
		private readonly IJumpListService jumpListService;
		private readonly ITrashService trashService;
		private readonly ISizeProvider folderSizeProvider;
		private readonly IFileTagsSettingsService fileTagsSettingsService;
		private CancellationTokenSource addFilesCTS;
		private CancellationTokenSource semaphoreCTS;
		private List<StandardItemViewModel> items;
		private ILocatableStorable? currentFolder;
		private IFolderWatcher? folderWatcher;
		private EmptyTextType emptyTextType;
		private FolderSettingsViewModel folderSettings;

		public BulkConcurrentObservableCollection<StandardItemViewModel> Items { get; }
		public ObservableCollection<StandardItemViewModel> SelectedItems { get; }

		public ObservableCollection<SortOption> SortOptions { get; }
		
		public EmptyTextType EmptyTextType
		{
			get => emptyTextType;
			set => SetProperty(ref emptyTextType, value);
		}

		public ILocatableStorable? CurrentFolder
		{
			get => currentFolder;
			set => SetProperty(ref currentFolder, value);
		}

		public LayoutModeViewModel(IUserSettingsService userSettingsService, 
			IJumpListService jumpListService,
			ITrashService trashService,
			IFileTagsSettingsService fileTagsSettingsService,
			ISizeProvider folderSizeProvider,
			FolderSettingsViewModel folderSettings
			)
		{
			this.userSettingsService = userSettingsService;
			this.jumpListService = jumpListService;
			this.trashService = trashService;
			this.fileTagsSettingsService = fileTagsSettingsService;
			this.folderSizeProvider = folderSizeProvider;
			this.folderSettings = folderSettings;

			enumFolderSemaphore = new SemaphoreSlim(1, 1);
			addFilesCTS = new CancellationTokenSource();
			semaphoreCTS = new CancellationTokenSource();
			items = new List<StandardItemViewModel>();
			Items = new BulkConcurrentObservableCollection<StandardItemViewModel>(items);
			SelectedItems = new ObservableCollection<StandardItemViewModel>();
			fileTagsSettingsService.OnSettingImportedEvent += FileTagsSettingsService_OnSettingUpdated;
			fileTagsSettingsService.OnTagsUpdated += FileTagsSettingsService_OnSettingUpdated;
			userSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
		}

		public async Task SetCurrentFolderFromPathAsync(string path)
		{
			// Flag to use FindFirstFileExFromApp or StorageFolder enumeration - Use storage folder for Box Drive (#4629)
			var isBoxFolder = App.CloudDrivesManager.Drives.FirstOrDefault(x => x.Text == "Box")?.Path?.TrimEnd('\\') is string boxFolder && path.StartsWith(boxFolder);
			bool isWslDistro = App.WSLDistroManager.TryGetDistro(path, out _);
			bool isNetwork = path.StartsWith(@"\\", StringComparison.Ordinal) &&
				!path.StartsWith(@"\\?\", StringComparison.Ordinal) &&
				!path.StartsWith(@"\\SHELL\", StringComparison.Ordinal) &&
				!isWslDistro;
			bool enumFromStorageFolder = isBoxFolder;

			if (isNetwork)
			{
				var auth = await NetworkDrivesAPI.AuthenticateNetworkShare(path);
				if (!auth)
					throw new AuthenticationException();
			}

			if (!FolderHelpers.CheckFolderAccessWithWin32(path))
			{
				throw new UnauthorizedAccessException();
			}

			var pathRoot = Path.GetPathRoot(path);
			if (Path.IsPathRooted(path) && pathRoot == path)
			{
				if (await FolderHelpers.CheckBitlockerStatusAsync(path))
					await ContextMenu.InvokeVerb("unlock-bde", pathRoot);
			}

			if (enumFromStorageFolder)
			{
				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				CurrentFolder = new WindowsStorageFolder(folder);
			}
			else
			{
				CurrentFolder = await GetFolderOfTypeFromPathAsync(path);
			}
		}

		private async Task<ILocatableStorable> GetFolderOfTypeFromPathAsync(string path)
		{
			if (FtpHelpers.IsFtpPath(path) && FtpHelpers.VerifyFtpPath(path))
			{
				var service = Ioc.Default.GetRequiredService<IFtpStorageService>();

				if (!FtpHelpers.VerifyFtpPath(path))
					return await Task.FromException<ILocatableStorable>(new FtpException("The path failed verification for FTP"));

				using var client = new AsyncFtpClient();
				client.Host = FtpHelpers.GetFtpHost(path);
				client.Port = FtpHelpers.GetFtpPort(path);
				client.Credentials = FtpManager.Credentials.Get(client.Host, FtpManager.Anonymous);

				static async Task<FtpProfile?> WrappedAutoConnectFtpAsync(AsyncFtpClient client)
				{
					return await client.AutoConnect();
				}

				try
				{
					if (!client.IsConnected && await WrappedAutoConnectFtpAsync(client) is null)
						throw new InvalidOperationException();

					FtpManager.Credentials[client.Host] = client.Credentials;

					return await service.GetFolderFromPathAsync(path);
				}
				catch (FtpException)
				{
					// Network issue
					FtpManager.Credentials.Remove(client.Host);
					throw;
				}
			}
			else if (path.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal))
			{
				if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
				{
					return library;
				}

				throw new FileNotFoundException();
			}
			else
			{
				var service = Ioc.Default.GetRequiredService<NativeStorageService>();
				return await service.GetFolderFromPathAsync(path);
			}
		}

		private void ApplySingleFileChange(StandardItemViewModel item)
		{
			var newIndex = items.IndexOf(item);

			Items.Remove(item);
			if (newIndex != -1)
				Items.Insert(Math.Min(newIndex, Items.Count), item);

			if (folderSettings.DirectoryGroupOption != GroupOption.None)
			{
				var key = Items.ItemGroupKeySelector?.Invoke(item);
				var group = Items.GroupedCollection?.FirstOrDefault(x => x.Model.Key == key);
				group?.OrderOne(list => SortingHelper.OrderFileList(list, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles), item);
			}

			UpdateEmptyTextType();
		}

		private async void FolderSizeProvider_SizeChanged(object? sender, SizeChangedEventArgs e)
		{
			try
			{
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				var matchingItem = Items.FirstOrDefault(x => x.Storable.Id == e.Path);
				if (matchingItem is not null)
				{
					matchingItem.UpdateProperties();
				}
			}
			finally
			{
				enumFolderSemaphore.Release();
			}
		}

		private Task OrderItemsAsync()
		{
			// Sorting group contents is handled elsewhere
			if (folderSettings.DirectoryGroupOption != GroupOption.None)
				return Task.CompletedTask;

			void OrderEntries()
			{
				if (items.Count == 0)
					return;

				items = SortingHelper.OrderFileList(items, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles).ToList();
			}

			if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent)
				return Task.Run(OrderEntries);

			OrderEntries();

			return Task.CompletedTask;
		}

		private void OrderGroups(CancellationToken token = default)
		{
			var gps = Items.GroupedCollection?.Where(x => !x.IsSorted);
			if (gps is null)
				return;

			foreach (var gp in gps)
			{
				if (token.IsCancellationRequested)
					return;

				gp.Order(list => SortingHelper.OrderFileList(list, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles));
			}

			if (Items.GroupedCollection is null || Items.GroupedCollection.IsSorted)
				return;

			if (folderSettings.DirectoryGroupDirection == SortDirection.Ascending)
			{
				if (folderSettings.DirectoryGroupOption == GroupOption.Size)
					// Always show file sections below folders
					Items.GroupedCollection.Order(x => x.OrderBy(async y => y.First().Storable is not IFolder || await y.First().Properties.IsArchiveAsync())
						.ThenBy(y => y.Model.SortIndexOverride).ThenBy(y => y.Model.Text));
				else
					Items.GroupedCollection.Order(x => x.OrderBy(y => y.Model.SortIndexOverride).ThenBy(y => y.Model.Text));
			}
			else
			{
				if (folderSettings.DirectoryGroupOption == GroupOption.Size)
					// Always show file sections below folders
					Items.GroupedCollection.Order(x => x.OrderBy(async y => y.First().Storable is not IFolder || await y.First().Properties.IsArchiveAsync())
						.ThenByDescending(y => y.Model.SortIndexOverride).ThenByDescending(y => y.Model.Text));
				else
					Items.GroupedCollection.Order(x => x.OrderByDescending(y => y.Model.SortIndexOverride).ThenByDescending(y => y.Model.Text));
			}

			Items.GroupedCollection.IsSorted = true;
		}

		private async Task GroupOptionsUpdated(CancellationToken token)
		{
			try
			{
				// Conflicts will occur if re-grouping is run while items are still being enumerated,
				// so wait for enumeration to complete first
				await enumFolderSemaphore.WaitAsync(token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				Items.BeginBulkOperation();
				UpdateGroupOptions();

				if (Items.IsGrouped)
				{
					await Task.Run(() =>
					{
						Items.ResetGroups(token);
						if (token.IsCancellationRequested)
							return;

						OrderGroups();
					});
				}
				else
				{
					await OrderItemsAsync();
				}

				if (token.IsCancellationRequested)
					return;

				Items.EndBulkOperation();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
			finally
			{
				enumFolderSemaphore.Release();
			}
		}

		private void UpdateGroupOptions()
		{
			Items.ItemGroupKeySelector = GroupingHelper.GetItemGroupKeySelector(folderSettings.DirectoryGroupOption, folderSettings.DirectoryGroupByDateUnit);
			var groupInfoSelector = GroupingHelper.GetGroupInfoSelector(folderSettings.DirectoryGroupOption, folderSettings.DirectoryGroupByDateUnit);
			Items.GetGroupHeaderInfo = groupInfoSelector.Item1;
			Items.GetExtendedGroupHeaderInfo = groupInfoSelector.Item2;
		}

		private Task ApplyItemChangesAsync()
		{
			try
			{
				if (items is null || items.Count == 0)
				{
					void ClearDisplay()
					{
						Items.Clear();
					}

					if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent)
						ClearDisplay();

					return Task.CompletedTask;
				}

				// CollectionChanged will cause UI update, which may cause significant performance degradation,
				// so suppress CollectionChanged event here while loading items heavily.

				// Note that both DataGrid and GridView don't support multi-items changes notification, so here
				// we have to call BeginBulkOperation to suppress CollectionChanged and call EndBulkOperation
				// in the end to fire a CollectionChanged event with NotifyCollectionChangedAction.Reset
				Items.BeginBulkOperation();

				// After calling BeginBulkOperation, ObservableCollection.CollectionChanged is suppressed
				// so modifies to FilesAndFolders won't trigger UI updates, hence below operations can be
				// run safely without needs of dispatching to UI thread
				void ApplyChanges()
				{
					var startIndex = -1;
					var tempList = new List<StandardItemViewModel>();

					void ApplyBulkInsertEntries()
					{
						if (startIndex != -1)
						{
							Items.ReplaceRange(startIndex, tempList);
							startIndex = -1;
							tempList.Clear();
						}
					}

					for (var i = 0; i < items.Count; i++)
					{
						if (addFilesCTS.IsCancellationRequested)
							return;

						if (i < Items.Count)
						{
							if (Items[i] != items[i])
							{
								if (startIndex == -1)
									startIndex = i;

								tempList.Add(items[i]);
							}
							else
							{
								ApplyBulkInsertEntries();
							}
						}
						else
						{
							ApplyBulkInsertEntries();
							Items.InsertRange(i, items.Skip(i));

							break;
						}
					}

					ApplyBulkInsertEntries();

					if (Items.Count > items.Count)
						Items.RemoveRange(items.Count, items.Count - items.Count);

					if (folderSettings.DirectoryGroupOption != GroupOption.None)
						OrderGroups();

				}

				void UpdateUI()
				{
					// Trigger CollectionChanged with NotifyCollectionChangedAction.Reset
					// once loading is completed so that UI can be updated
					Items.EndBulkOperation();
					UpdateEmptyTextType();
				}

				return Task.Run(() =>
				{
					ApplyChanges();
					UpdateUI();
				});
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return Task.FromException(ex);
			}
		}

		private async void FileTagsSettingsService_OnSettingUpdated(object? sender, EventArgs e)
		{
			await GetItemsAsync();
		}

		public void UpdateEmptyTextType()
		{
			EmptyTextType = Items.Count == 0 ? 
				(IsSearchResults ? EmptyTextType.NoSearchResultsFound : EmptyTextType.FolderEmpty) 
				: EmptyTextType.None;
		}

		private async Task WatchForChangesAsync()
		{
			if (CurrentFolder is IMutableFolder mf)
			{
				if (folderWatcher is not null)
				{
					folderWatcher.ItemChanged -= Watcher_ItemChanged;
					folderWatcher.ItemRenamed -= Watcher_ItemRenamed;
					folderWatcher.ItemAdded -= Watcher_ItemAdded;
					folderWatcher.ItemRemoved -= Watcher_ItemRemoved;
					folderWatcher.Stop();
				}

				folderWatcher = await mf.GetFolderWatcherAsync();
				if (folderWatcher is not null)
				{
					folderWatcher.ItemChanged += Watcher_ItemChanged;
					folderWatcher.ItemRenamed += Watcher_ItemRenamed;
					folderWatcher.ItemAdded += Watcher_ItemAdded;
					folderWatcher.ItemRemoved += Watcher_ItemRemoved;
					folderWatcher.Start();
				}
			}
		}

		private async void Watcher_ItemRemoved(object? sender, FileSystemEventArgs e)
		{
			await RemoveItemAsync(e.FullPath);
		}

		private async void Watcher_ItemAdded(object? sender, FileSystemEventArgs e)
		{
			var item = await GetFolderOfTypeFromPathAsync(e.FullPath);
			var itemViewModel = new StandardItemViewModel(item);
			await AddItemAsync(itemViewModel);
			await HandleChangesOccurredAsync(item.Id);
		}

		private void Watcher_ItemRenamed(object? sender, RenamedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void Watcher_ItemChanged(object? sender, FileSystemEventArgs e)
		{
			throw new NotImplementedException();
		}

		private async Task HandleChangesOccurredAsync(string id)
		{
			await OrderItemsAsync();
			await ApplyItemChangesAsync();

			var item = items.FirstOrDefault(x => PathHelpers.FormatName(x.Storable.Id).Equals(PathHelpers.FormatName(id)));
			if (item is not null)
			{
				SelectedItems.Add(item);
			}
		}

		public async Task GetItemsAsync()
		{
			Cancel();

			try
			{
				// Only one instance at a time should access this function
				// Wait here until the previous one has ended
				// If we're waiting and a new update request comes through simply drop this instance
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				// Drop all the other waiting instances
				semaphoreCTS.Cancel();
				semaphoreCTS = new CancellationTokenSource();

				if (CurrentFolder is ILocatableFolder folder)
				{
					await foreach (IStorable i in folder.GetItemsAsync(Sdk.Storage.Enums.StorableKind.All, addFilesCTS.Token))
					{
						if (addFilesCTS.IsCancellationRequested)
						{
							return;
						}
						var storableViewModel = new StandardItemViewModel(i);
						Items.Add(storableViewModel);
					}
				}
				else if (CurrentFolder is LibraryLocationItem sli)
				{
					foreach (string path in sli.Folders)
					{
						var libraryFolder = new NativeFolder(path);
						await foreach (IStorable i in libraryFolder.GetItemsAsync(Sdk.Storage.Enums.StorableKind.All, addFilesCTS.Token))
						{
							if (addFilesCTS.IsCancellationRequested)
							{
								return;
							}
							var storableViewModel = new StandardItemViewModel(i);
							Items.Add(storableViewModel);
						}
					}
				}

				UpdateGroupOptions();
				await OrderItemsAsync();
				await ApplyItemChangesAsync();
				await WatchForChangesAsync();
			}
			finally
			{
				// Make sure item count is updated
				enumFolderSemaphore.Release();
			}
		}

		private async void RecycleBinRefreshRequested(object sender, FileSystemEventArgs e)
		{
			if (!Constants.UserEnvironmentPaths.RecycleBinPath.Equals(CurrentFolder?.Path, StringComparison.OrdinalIgnoreCase))
				return;

			await GetItemsAsync();
		}

		private async void RecycleBinItemDeleted(object sender, FileSystemEventArgs e)
		{
			if (!Constants.UserEnvironmentPaths.RecycleBinPath.Equals(CurrentFolder?.Path, StringComparison.OrdinalIgnoreCase))
				return;

			// Get the item that immediately follows matching item to be removed
			// If the matching item is the last item, try to get the previous item; otherwise, null
			// Case must be ignored since $Recycle.Bin != $RECYCLE.BIN
			var itemRemovedIndex = items.OfType<ILocatableStorable>().ToList().FindIndex(x => x.Path.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));
			var nextOfMatchingItem = items.ElementAtOrDefault(itemRemovedIndex + 1 < items.Count ? itemRemovedIndex + 1 : itemRemovedIndex - 1);
			await RemoveItemAsync(e.FullPath);

			if (nextOfMatchingItem is not null)
				await HandleChangesOccurredAsync(nextOfMatchingItem.Storable.Id);
		}

		private async void RecycleBinItemCreated(object sender, FileSystemEventArgs e)
		{
			if (!Constants.UserEnvironmentPaths.RecycleBinPath.Equals(CurrentFolder?.Path, StringComparison.OrdinalIgnoreCase))
				return;

			var item = items.OfType<ILocatableStorable>().FirstOrDefault(x => x.Path.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));

			if (item is null)
				return;

			var newListedItem = new StandardItemViewModel(item);

			await AddItemAsync(newListedItem);
			await OrderItemsAsync();
			ApplySingleFileChange(newListedItem);
		}

		private async void UserSettingsService_OnSettingChangedEvent(object? sender, Shared.EventArguments.SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(UserSettingsService.FoldersSettingsService.ShowFileExtensions):
				case nameof(UserSettingsService.FoldersSettingsService.ShowThumbnails):
				case nameof(UserSettingsService.FoldersSettingsService.ShowHiddenItems):
				case nameof(UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles):
				case nameof(UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible):
				case nameof(UserSettingsService.FoldersSettingsService.ShowDotFiles):
				case nameof(UserSettingsService.FoldersSettingsService.CalculateFolderSizes):
				case nameof(UserSettingsService.FoldersSettingsService.SelectFilesOnHover):
				case nameof(UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems):
					await GetItemsAsync();
					break;
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortOption):
				case nameof(UserSettingsService.FoldersSettingsService.DefaultGroupOption):
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles):
				case nameof(UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories):
				case nameof(UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit):
					folderSettings.OnDefaultPreferencesChanged(CurrentFolder.Path, e.SettingName);
					await OrderItemsAsync();
					await ApplyItemChangesAsync();
					break;
			}
		}

		public void Cancel()
		{
			folderWatcher?.Stop();
			addFilesCTS.Cancel();
			items.Clear();
			SelectedItems.Clear();
			Items.Clear();
		}

		private async Task AddItemAsync(StandardItemViewModel item)
		{
			try
			{
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (!items.Any(x => x.Storable.Id.Equals(item.Storable.Id, StringComparison.OrdinalIgnoreCase))) // Avoid adding duplicate items
			{
				items.Add(item);

				if (userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible
					&& item.Storable is ILocatableStorable ls)
				{
					// New file added, enumerate ADS
					foreach (var ads in NativeFileOperationsHelper.GetAlternateStreams(ls.Path))
					{
						var adsItem = Win32StorageEnumerator.GetAlternateStream(ads, item);
						items.Add(adsItem);
					}
				}
			}

			enumFolderSemaphore.Release();
		}

		private async Task RemoveItemAsync(string id)
		{
			try
			{
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				var matchingItem = items.FirstOrDefault(x => x.Storable.Id.Equals(id));
				if (matchingItem is not null)
				{
					var removedItem = items.Remove(matchingItem);
				}
			}
			finally
			{
				enumFolderSemaphore.Release();
			}
		}

	}
}
