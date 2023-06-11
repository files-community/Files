// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageEnumerators;
using Files.App.ServicesImplementation.Settings;
using Files.App.Storage.FtpStorage;
using Files.App.Storage.NativeStorage;
using Files.App.Storage.WindowsStorage;
using Files.Backend.Helpers;
using Files.Backend.Services;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;
using Files.Shared.Services;
using FluentFTP;
using Microsoft.Extensions.Logging;
using System.IO;
using FtpHelpers = Files.App.Helpers.FtpHelpers;

namespace Files.App.ViewModels
{
	public class LayoutModeViewModel : ObservableObject
	{
		private readonly SemaphoreSlim enumFolderSemaphore;
		private readonly IUserSettingsService userSettingsService;
		private readonly IJumpListService jumpListService;
		private readonly ITrashService trashService;
		private readonly IFileTagsSettingsService fileTagsSettingsService;
		private List<StandardItemViewModel> items;
		private ILocatableFolder? currentFolder;
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

		public ILocatableFolder? CurrentFolder
		{
			get => currentFolder;
			set => SetProperty(ref currentFolder, value);
		}

		public LayoutModeViewModel(IUserSettingsService userSettingsService, 
			IJumpListService jumpListService,
			ITrashService trashService,
			IFileTagsSettingsService fileTagsSettingsService,
			FolderSettingsViewModel folderSettings
			)
		{
			this.userSettingsService = userSettingsService;
			this.jumpListService = jumpListService;
			this.trashService = trashService;
			this.fileTagsSettingsService = fileTagsSettingsService;
			this.folderSettings = folderSettings;

			items = new List<StandardItemViewModel>();
			Items = new BulkConcurrentObservableCollection<StandardItemViewModel>(items);
			SelectedItems = new ObservableCollection<StandardItemViewModel>();
			fileTagsSettingsService.OnSettingImportedEvent += FileTagsSettingsService_OnSettingUpdated;
			fileTagsSettingsService.OnTagsUpdated += FileTagsSettingsService_OnSettingUpdated;
			userSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
		}

		public async Task SetCurrentFolderFromPathAsync(string path)
		{
			if (await GetFolderOfTypeFromPathAsync(path) is ILocatableFolder folder)
			{
				CurrentFolder = folder;
			}
			else
			{
				if (FtpHelpers.IsFtpPath(path) && FtpHelpers.VerifyFtpPath(path))
				{
					throw new FtpException("The valid path failed authentication for FTP");
				}
			}
		}

		private async Task<ILocatableFolder> GetFolderOfTypeFromPathAsync(string path)
		{
			if (FtpHelpers.IsFtpPath(path))
			{
				var service = Ioc.Default.GetRequiredService<IFtpStorageService>();

				if (!FtpHelpers.VerifyFtpPath(path))
					return await Task.FromException<ILocatableFolder>(new FtpException("The path failed verification for FTP"));

				using var client = new AsyncFtpClient();
				client.Host = FtpHelpers.GetFtpHost(path);
				client.Port = FtpHelpers.GetFtpPort(path);
				client.Credentials = FtpManager.Credentials.Get(client.Host, FtpManager.Anonymous);

				static async Task<FtpProfile?> WrappedAutoConnectFtpAsync(AsyncFtpClient client)
				{
					try
					{
						return await client.AutoConnect();
					}
					catch (FtpAuthenticationException)
					{
						return null;
					}

					throw new InvalidOperationException();
				}

				try
				{
					if (!client.IsConnected && await WrappedAutoConnectFtpAsync(client) is null)
						throw new InvalidOperationException();

					FtpManager.Credentials[client.Host] = client.Credentials;

					return await service.GetFolderFromPathAsync(path);
				}
				catch
				{
					// Network issue
					FtpManager.Credentials.Remove(client.Host);
					return null;
				}
			}
			else
			{
				var service = Ioc.Default.GetRequiredService<NativeStorageService>();
				return await service.GetFolderFromPathAsync(path);
			}
			// TODO: Add case for Windows Storage
		}

		private async Task ApplySingleFileChangeAsync(StandardItemViewModel item)
		{
			var newIndex = items.IndexOf(item);
			await dispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
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
			});
		}

		private Task OrderFilesAndFoldersAsync()
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
					Items.GroupedCollection.Order(x => x.OrderBy(y => y.First().Storable is not IFolder || y.First().IsArchive)
						.ThenBy(y => y.Model.SortIndexOverride).ThenBy(y => y.Model.Text));
				else
					Items.GroupedCollection.Order(x => x.OrderBy(y => y.Model.SortIndexOverride).ThenBy(y => y.Model.Text));
			}
			else
			{
				if (folderSettings.DirectoryGroupOption == GroupOption.Size)
					// Always show file sections below folders
					Items.GroupedCollection.Order(x => x.OrderBy(y => y.First().Storable is not IFolder || y.First().IsArchive)
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
					await OrderFilesAndFoldersAsync();
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

		private async Task ApplyItemChangesAsync()
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

					return;
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

				if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent)
				{
					await Task.Run(ApplyChanges);
					UpdateUI();
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private async void FileTagsSettingsService_OnSettingUpdated(object? sender, EventArgs e)
		{
			await GetItemsAsync();
		}

		public void UpdateEmptyTextType()
		{
			EmptyTextType = Items.Count == 0 ? (IsSearchResults ? EmptyTextType.NoSearchResultsFound : EmptyTextType.FolderEmpty) : EmptyTextType.None;
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
			await OrderFilesAndFoldersAsync();
			await ApplyItemChangesAsync();

			var item = items.FirstOrDefault(x => PathHelpers.FormatName(x.Storable.Id).Equals(PathHelpers.FormatName(id)));
			if (item is not null)
			{
				SelectedItems.Add(item);
			}
		}

		public async Task GetItemsAsync(CancellationToken cancellationToken = default)
		{
			Cancel();

			await foreach (IStorable i in CurrentFolder.GetItemsAsync(Sdk.Storage.Enums.StorableKind.All, cancellationToken))
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}
				var storableViewModel = new StandardItemViewModel(i);
				
			}

			UpdateGroupOptions();
			await WatchForChangesAsync();
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
			await OrderFilesAndFoldersAsync();
			await ApplySingleFileChangeAsync(newListedItem);
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
					await dispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						folderSettings.OnDefaultPreferencesChanged(CurrentFolder.Path, e.SettingName);
						UpdateSortAndGroupOptions();
					});
					await OrderFilesAndFoldersAsync();
					await ApplyItemChangesAsync();
					break;
			}
		}

		public void Cancel()
		{
			folderWatcher.Stop();
			if (IsLoadingItems)
				addFilesCTS.Cancel();
			CancelExtendedPropertiesLoading();
			items.Clear();
			SelectedItems.Clear();
			Items.Clear();
			CancelSearch();
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
