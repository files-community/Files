// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.Cloud;
using Files.App.Filesystem.Search;
using Files.App.Filesystem.StorageEnumerators;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers.FileListCache;
using Files.App.Shell;
using Files.App.Storage.FtpStorage;
using Files.App.UserControls;
using Files.App.ViewModels.Previews;
using Files.Backend.Services;
using Files.Backend.Services.SizeProvider;
using Files.Backend.ViewModels.Dialogs;
using Files.Shared.Cloud;
using Files.Shared.EventArguments;
using Files.Shared.Services;
using FluentFTP;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Vanara.Windows.Shell;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using static Files.App.Helpers.NativeDirectoryChangesHelper;
using static Files.Backend.Helpers.NativeFindStorageItemHelper;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.ViewModels
{
	public sealed class ItemViewModel : ObservableObject, IDisposable
	{
		private readonly SemaphoreSlim enumFolderSemaphore;
		private readonly ConcurrentQueue<(uint Action, string FileName)> operationQueue;
		private readonly ConcurrentQueue<uint> gitChangesQueue;
		private readonly ConcurrentDictionary<string, bool> itemLoadQueue;
		private readonly AsyncManualResetEvent operationEvent;
		private readonly AsyncManualResetEvent gitChangedEvent;
		private readonly DispatcherQueue dispatcherQueue;
		private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");
		private readonly IFileListCache fileListCache = FileListCacheController.GetInstance();
		private readonly string folderTypeTextLocalized = "Folder".GetLocalizedResource();

		private Task? aProcessQueueAction;
		private Task? gitProcessQueueAction;

		// Files and folders list for manipulating
		private List<ListedItem> filesAndFolders;
		private readonly IJumpListService jumpListService = Ioc.Default.GetRequiredService<IJumpListService>();
		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		private readonly ISizeProvider folderSizeProvider = Ioc.Default.GetRequiredService<ISizeProvider>();

		// Only used for Binding and ApplyFilesAndFoldersChangesAsync, don't manipulate on this!
		public BulkConcurrentObservableCollection<ListedItem> FilesAndFolders { get; }

		private FolderSettingsViewModel folderSettings = null;

		private ListedItem? currentFolder;
		public ListedItem? CurrentFolder
		{
			get => currentFolder;
			private set => SetProperty(ref currentFolder, value);
		}

		public CollectionViewSource viewSource;

		private FileSystemWatcher watcher;

		private static BitmapImage shieldIcon;

		private CancellationTokenSource addFilesCTS;
		private CancellationTokenSource semaphoreCTS;
		private CancellationTokenSource loadPropsCTS;
		private CancellationTokenSource watcherCTS;
		private CancellationTokenSource searchCTS;

		public event EventHandler DirectoryInfoUpdated;

		public event EventHandler GitDirectoryUpdated;

		public event EventHandler<List<ListedItem>> OnSelectionRequestedEvent;

		public string WorkingDirectory { get; private set; }

		public string? GitDirectory { get; private set; }

		private StorageFolderWithPath? currentStorageFolder;
		private StorageFolderWithPath workingRoot;

		public delegate void WorkingDirectoryModifiedEventHandler(object sender, WorkingDirectoryModifiedEventArgs e);

		public event WorkingDirectoryModifiedEventHandler WorkingDirectoryModified;

		public delegate void PageTypeUpdatedEventHandler(object sender, PageTypeUpdatedEventArgs e);

		public event PageTypeUpdatedEventHandler PageTypeUpdated;

		public delegate void ItemLoadStatusChangedEventHandler(object sender, ItemLoadStatusChangedEventArgs e);

		public event ItemLoadStatusChangedEventHandler ItemLoadStatusChanged;

		public async Task SetWorkingDirectoryAsync(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return;

			var isLibrary = false;
			string? name = null;
			if (App.LibraryManager.TryGetLibrary(value, out LibraryLocationItem library))
			{
				isLibrary = true;
				name = library.Text;
			}

			WorkingDirectoryModified?.Invoke(this, new WorkingDirectoryModifiedEventArgs { Path = value, IsLibrary = isLibrary, Name = name });

			if (isLibrary || !Path.IsPathRooted(value))
				workingRoot = currentStorageFolder = null;
			else if (!Path.IsPathRooted(WorkingDirectory) || Path.GetPathRoot(WorkingDirectory) != Path.GetPathRoot(value))
				workingRoot = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(value));

			if (value == "Home")
				currentStorageFolder = null;
			else
				_ = Task.Run(() => jumpListService.AddFolderAsync(value));

			WorkingDirectory = value;

			string? pathRoot;
			if (FtpHelpers.IsFtpPath(WorkingDirectory))
			{
				var rootIndex = FtpHelpers.GetRootIndex(WorkingDirectory);
				pathRoot = rootIndex is -1
					? WorkingDirectory
					: WorkingDirectory.Substring(0, rootIndex);
			}
			else
			{
				pathRoot = Path.GetPathRoot(WorkingDirectory);
			}

			GitDirectory = pathRoot is null ? null : GitHelpers.GetGitRepositoryPath(WorkingDirectory, pathRoot);
			OnPropertyChanged(nameof(WorkingDirectory));
		}

		public Task<FilesystemResult<BaseStorageFolder>> GetFolderFromPathAsync(string value)
			=> FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(value, workingRoot, currentStorageFolder));

		public Task<FilesystemResult<BaseStorageFile>> GetFileFromPathAsync(string value)
			=> FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(value, workingRoot, currentStorageFolder));

		public Task<FilesystemResult<StorageFolderWithPath>> GetFolderWithPathFromPathAsync(string value)
			=> FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(value, workingRoot, currentStorageFolder));

		public Task<FilesystemResult<StorageFileWithPath>> GetFileWithPathFromPathAsync(string value)
			=> FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(value, workingRoot, currentStorageFolder));

		private EmptyTextType emptyTextType;
		public EmptyTextType EmptyTextType
		{
			get => emptyTextType;
			set => SetProperty(ref emptyTextType, value);
		}

		public async Task UpdateSortOptionStatus()
		{
			OnPropertyChanged(nameof(IsSortedByName));
			OnPropertyChanged(nameof(IsSortedByDate));
			OnPropertyChanged(nameof(IsSortedByType));
			OnPropertyChanged(nameof(IsSortedBySize));
			OnPropertyChanged(nameof(IsSortedByOriginalPath));
			OnPropertyChanged(nameof(IsSortedByDateDeleted));
			OnPropertyChanged(nameof(IsSortedByDateCreated));
			OnPropertyChanged(nameof(IsSortedBySyncStatus));
			OnPropertyChanged(nameof(IsSortedByFileTag));

			await OrderFilesAndFoldersAsync();
			await ApplyFilesAndFoldersChangesAsync();
		}

		public async Task UpdateSortDirectionStatus()
		{
			OnPropertyChanged(nameof(IsSortedAscending));
			OnPropertyChanged(nameof(IsSortedDescending));

			await OrderFilesAndFoldersAsync();
			await ApplyFilesAndFoldersChangesAsync();
		}

		public async Task UpdateSortDirectoriesAlongsideFiles()
		{
			OnPropertyChanged(nameof(AreDirectoriesSortedAlongsideFiles));

			await OrderFilesAndFoldersAsync();
			await ApplyFilesAndFoldersChangesAsync();
		}

		private void UpdateSortAndGroupOptions()
		{
			OnPropertyChanged(nameof(IsSortedByName));
			OnPropertyChanged(nameof(IsSortedByDate));
			OnPropertyChanged(nameof(IsSortedByType));
			OnPropertyChanged(nameof(IsSortedBySize));
			OnPropertyChanged(nameof(IsSortedByOriginalPath));
			OnPropertyChanged(nameof(IsSortedByDateDeleted));
			OnPropertyChanged(nameof(IsSortedByDateCreated));
			OnPropertyChanged(nameof(IsSortedBySyncStatus));
			OnPropertyChanged(nameof(IsSortedByFileTag));
			OnPropertyChanged(nameof(IsSortedAscending));
			OnPropertyChanged(nameof(IsSortedDescending));
			OnPropertyChanged(nameof(AreDirectoriesSortedAlongsideFiles));
		}

		public bool IsSortedByName
		{
			get => folderSettings.DirectorySortOption == SortOption.Name;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.Name;
					OnPropertyChanged(nameof(IsSortedByName));
				}
			}
		}

		public bool IsSortedByOriginalPath
		{
			get => folderSettings.DirectorySortOption == SortOption.OriginalFolder;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.OriginalFolder;

					OnPropertyChanged(nameof(IsSortedByOriginalPath));
				}
			}
		}

		public bool IsSortedByDateDeleted
		{
			get => folderSettings.DirectorySortOption == SortOption.DateDeleted;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.DateDeleted;

					OnPropertyChanged(nameof(IsSortedByDateDeleted));
				}
			}
		}

		public bool IsSortedByDate
		{
			get => folderSettings.DirectorySortOption == SortOption.DateModified;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.DateModified;

					OnPropertyChanged(nameof(IsSortedByDate));
				}
			}
		}

		public bool IsSortedByDateCreated
		{
			get => folderSettings.DirectorySortOption == SortOption.DateCreated;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.DateCreated;

					OnPropertyChanged(nameof(IsSortedByDateCreated));
				}
			}
		}

		public bool IsSortedByType
		{
			get => folderSettings.DirectorySortOption == SortOption.FileType;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.FileType;

					OnPropertyChanged(nameof(IsSortedByType));
				}
			}
		}

		public bool IsSortedBySize
		{
			get => folderSettings.DirectorySortOption == SortOption.Size;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.Size;
					OnPropertyChanged(nameof(IsSortedBySize));
				}
			}
		}

		public bool IsSortedBySyncStatus
		{
			get => folderSettings.DirectorySortOption == SortOption.SyncStatus;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.SyncStatus;
					OnPropertyChanged(nameof(IsSortedBySyncStatus));
				}
			}
		}

		public bool IsSortedByFileTag
		{
			get => folderSettings.DirectorySortOption == SortOption.FileTag;
			set
			{
				if (value)
				{
					folderSettings.DirectorySortOption = SortOption.FileTag;
					OnPropertyChanged(nameof(IsSortedByFileTag));
				}
			}
		}

		public bool IsSortedAscending
		{
			get => folderSettings.DirectorySortDirection == SortDirection.Ascending;
			set
			{
				folderSettings.DirectorySortDirection = value ? SortDirection.Ascending : SortDirection.Descending;
				OnPropertyChanged(nameof(IsSortedAscending));
				OnPropertyChanged(nameof(IsSortedDescending));
			}
		}

		public bool IsSortedDescending
		{
			get => !IsSortedAscending;
			set
			{
				folderSettings.DirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
				OnPropertyChanged(nameof(IsSortedAscending));
				OnPropertyChanged(nameof(IsSortedDescending));
			}
		}

		public bool AreDirectoriesSortedAlongsideFiles
		{
			get => folderSettings.SortDirectoriesAlongsideFiles;
			set
			{
				folderSettings.SortDirectoriesAlongsideFiles = value;
				OnPropertyChanged(nameof(AreDirectoriesSortedAlongsideFiles));
			}
		}

		public ItemViewModel(FolderSettingsViewModel folderSettingsViewModel)
		{
			folderSettings = folderSettingsViewModel;
			filesAndFolders = new List<ListedItem>();
			FilesAndFolders = new BulkConcurrentObservableCollection<ListedItem>();
			operationQueue = new ConcurrentQueue<(uint Action, string FileName)>();
			gitChangesQueue = new ConcurrentQueue<uint>();
			itemLoadQueue = new ConcurrentDictionary<string, bool>();
			addFilesCTS = new CancellationTokenSource();
			semaphoreCTS = new CancellationTokenSource();
			loadPropsCTS = new CancellationTokenSource();
			watcherCTS = new CancellationTokenSource();
			operationEvent = new AsyncManualResetEvent();
			gitChangedEvent = new AsyncManualResetEvent();
			enumFolderSemaphore = new SemaphoreSlim(1, 1);
			dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			fileTagsSettingsService.OnSettingImportedEvent += FileTagsSettingsService_OnSettingUpdated;
			fileTagsSettingsService.OnTagsUpdated += FileTagsSettingsService_OnSettingUpdated;
			folderSizeProvider.SizeChanged += FolderSizeProvider_SizeChanged;
			RecycleBinManager.Default.RecycleBinItemCreated += RecycleBinItemCreated;
			RecycleBinManager.Default.RecycleBinItemDeleted += RecycleBinItemDeleted;
			RecycleBinManager.Default.RecycleBinRefreshRequested += RecycleBinRefreshRequested;
		}

		private async void RecycleBinRefreshRequested(object sender, FileSystemEventArgs e)
		{
			if (!Constants.UserEnvironmentPaths.RecycleBinPath.Equals(CurrentFolder?.ItemPath, StringComparison.OrdinalIgnoreCase))
				return;

			await dispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				RefreshItems(null);
			});
		}

		private async void RecycleBinItemDeleted(object sender, FileSystemEventArgs e)
		{
			if (!Constants.UserEnvironmentPaths.RecycleBinPath.Equals(CurrentFolder?.ItemPath, StringComparison.OrdinalIgnoreCase))
				return;

			// Get the item that immediately follows matching item to be removed
			// If the matching item is the last item, try to get the previous item; otherwise, null
			// Case must be ignored since $Recycle.Bin != $RECYCLE.BIN
			var itemRemovedIndex = filesAndFolders.FindIndex(x => x.ItemPath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));
			var nextOfMatchingItem = filesAndFolders.ElementAtOrDefault(itemRemovedIndex + 1 < filesAndFolders.Count ? itemRemovedIndex + 1 : itemRemovedIndex - 1);
			var removedItem = await RemoveFileOrFolderAsync(e.FullPath);

			if (removedItem is not null)
				await ApplySingleFileChangeAsync(removedItem);

			if (nextOfMatchingItem is not null)
				await RequestSelectionAsync(new List<ListedItem>() { nextOfMatchingItem });
		}

		private async void RecycleBinItemCreated(object sender, FileSystemEventArgs e)
		{
			if (!Constants.UserEnvironmentPaths.RecycleBinPath.Equals(CurrentFolder?.ItemPath, StringComparison.OrdinalIgnoreCase))
				return;

			using var folderItem = SafetyExtensions.IgnoreExceptions(() => new ShellItem(e.FullPath));
			if (folderItem is null)
				return;

			var shellFileItem = ShellFolderExtensions.GetShellFileItem(folderItem);

			var newListedItem = await AddFileOrFolderFromShellFile(shellFileItem);
			if (newListedItem is null)
				return;

			await AddFileOrFolderAsync(newListedItem);
			await OrderFilesAndFoldersAsync();
			await ApplySingleFileChangeAsync(newListedItem);
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
				var matchingItem = filesAndFolders.FirstOrDefault(x => x.ItemPath == e.Path);
				if (matchingItem is not null)
				{
					await dispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						if (e.ValueState is SizeChangedValueState.None)
						{
							matchingItem.FileSizeBytes = 0;
							matchingItem.FileSize = "ItemSizeNotCalculated".GetLocalizedResource();
						}
						else if (e.ValueState is SizeChangedValueState.Final || (long)e.NewSize > matchingItem.FileSizeBytes)
						{
							matchingItem.FileSizeBytes = (long)e.NewSize;
							matchingItem.FileSize = e.NewSize.ToSizeString();
						}

						DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
					},
					Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
				}
			}
			finally
			{
				enumFolderSemaphore.Release();
			}
		}

		private async void FileTagsSettingsService_OnSettingUpdated(object? sender, EventArgs e)
		{
			await dispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				if (WorkingDirectory != "Home")
					RefreshItems(null);
			});
		}

		private async void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
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
					await dispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						if (WorkingDirectory != "Home")
							RefreshItems(null);
					});
					break;
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortOption):
				case nameof(UserSettingsService.FoldersSettingsService.DefaultGroupOption):
				case nameof(UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles):
				case nameof(UserSettingsService.FoldersSettingsService.SyncFolderPreferencesAcrossDirectories):
				case nameof(UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit):
					await dispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						folderSettings.OnDefaultPreferencesChanged(WorkingDirectory, e.SettingName);
						UpdateSortAndGroupOptions();
					});
					await OrderFilesAndFoldersAsync();
					await ApplyFilesAndFoldersChangesAsync();
					break;
			}
		}

		public void CancelLoadAndClearFiles()
		{
			Debug.WriteLine("CancelLoadAndClearFiles");
			CloseWatcher();
			if (IsLoadingItems)
				addFilesCTS.Cancel();
			CancelExtendedPropertiesLoading();
			filesAndFolders.Clear();
			FilesAndFolders.Clear();
			CancelSearch();
		}

		public void CancelExtendedPropertiesLoading()
		{
			loadPropsCTS.Cancel();
			loadPropsCTS = new CancellationTokenSource();
		}

		public void CancelExtendedPropertiesLoadingForItem(ListedItem item)
		{
			itemLoadQueue.TryUpdate(item.ItemPath, true, false);
		}

		public async Task ApplySingleFileChangeAsync(ListedItem item)
		{
			var newIndex = filesAndFolders.IndexOf(item);
			await dispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				FilesAndFolders.Remove(item);
				if (newIndex != -1)
					FilesAndFolders.Insert(Math.Min(newIndex, FilesAndFolders.Count), item);

				if (folderSettings.DirectoryGroupOption != GroupOption.None)
				{
					var key = FilesAndFolders.ItemGroupKeySelector?.Invoke(item);
					var group = FilesAndFolders.GroupedCollection?.FirstOrDefault(x => x.Model.Key == key);
					group?.OrderOne(list => SortingHelper.OrderFileList(list, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles), item);
				}

				UpdateEmptyTextType();
				DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
			});
		}

		private bool IsSearchResults { get; set; }

		public void UpdateEmptyTextType()
		{
			EmptyTextType = FilesAndFolders.Count == 0 ? (IsSearchResults ? EmptyTextType.NoSearchResultsFound : EmptyTextType.FolderEmpty) : EmptyTextType.None;
		}

		// Apply changes immediately after manipulating on filesAndFolders completed
		public async Task ApplyFilesAndFoldersChangesAsync()
		{
			try
			{
				if (filesAndFolders is null || filesAndFolders.Count == 0)
				{
					void ClearDisplay()
					{
						FilesAndFolders.Clear();
						UpdateEmptyTextType();
						DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
					}

					if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && dispatcherQueue.HasThreadAccess)
						ClearDisplay();
					else
						await dispatcherQueue.EnqueueOrInvokeAsync(ClearDisplay);

					return;
				}

				// CollectionChanged will cause UI update, which may cause significant performance degradation,
				// so suppress CollectionChanged event here while loading items heavily.

				// Note that both DataGrid and GridView don't support multi-items changes notification, so here
				// we have to call BeginBulkOperation to suppress CollectionChanged and call EndBulkOperation
				// in the end to fire a CollectionChanged event with NotifyCollectionChangedAction.Reset
				FilesAndFolders.BeginBulkOperation();

				// After calling BeginBulkOperation, ObservableCollection.CollectionChanged is suppressed
				// so modifies to FilesAndFolders won't trigger UI updates, hence below operations can be
				// run safely without needs of dispatching to UI thread
				void ApplyChanges()
				{
					var startIndex = -1;
					var tempList = new List<ListedItem>();

					void ApplyBulkInsertEntries()
					{
						if (startIndex != -1)
						{
							FilesAndFolders.ReplaceRange(startIndex, tempList);
							startIndex = -1;
							tempList.Clear();
						}
					}

					for (var i = 0; i < filesAndFolders.Count; i++)
					{
						if (addFilesCTS.IsCancellationRequested)
							return;

						if (i < FilesAndFolders.Count)
						{
							if (FilesAndFolders[i] != filesAndFolders[i])
							{
								if (startIndex == -1)
									startIndex = i;

								tempList.Add(filesAndFolders[i]);
							}
							else
							{
								ApplyBulkInsertEntries();
							}
						}
						else
						{
							ApplyBulkInsertEntries();
							FilesAndFolders.InsertRange(i, filesAndFolders.Skip(i));

							break;
						}
					}

					ApplyBulkInsertEntries();

					if (FilesAndFolders.Count > filesAndFolders.Count)
						FilesAndFolders.RemoveRange(filesAndFolders.Count, FilesAndFolders.Count - filesAndFolders.Count);

					if (folderSettings.DirectoryGroupOption != GroupOption.None)
						OrderGroups();

				}

				void UpdateUI()
				{
					// Trigger CollectionChanged with NotifyCollectionChangedAction.Reset
					// once loading is completed so that UI can be updated
					FilesAndFolders.EndBulkOperation();
					UpdateEmptyTextType();
					DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
				}

				if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && dispatcherQueue.HasThreadAccess)
				{
					await Task.Run(ApplyChanges);
					UpdateUI();
				}
				else
				{
					ApplyChanges();
					await dispatcherQueue.EnqueueOrInvokeAsync(UpdateUI);
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private Task RequestSelectionAsync(List<ListedItem> itemsToSelect)
		{
			// Don't notify if there weren't listed items
			if (itemsToSelect is null || itemsToSelect.IsEmpty())
				return Task.CompletedTask;

			return dispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				OnSelectionRequestedEvent?.Invoke(this, itemsToSelect);
			});
		}

		private Task OrderFilesAndFoldersAsync()
		{
			// Sorting group contents is handled elsewhere
			if (folderSettings.DirectoryGroupOption != GroupOption.None)
				return Task.CompletedTask;

			void OrderEntries()
			{
				if (filesAndFolders.Count == 0)
					return;

				filesAndFolders = SortingHelper.OrderFileList(filesAndFolders, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles).ToList();
			}

			if (NativeWinApiHelper.IsHasThreadAccessPropertyPresent && dispatcherQueue.HasThreadAccess)
				return Task.Run(OrderEntries);

			OrderEntries();

			return Task.CompletedTask;
		}

		private void OrderGroups(CancellationToken token = default)
		{
			var gps = FilesAndFolders.GroupedCollection?.Where(x => !x.IsSorted);
			if (gps is null)
				return;

			foreach (var gp in gps)
			{
				if (token.IsCancellationRequested)
					return;

				gp.Order(list => SortingHelper.OrderFileList(list, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles));
			}

			if (FilesAndFolders.GroupedCollection is null || FilesAndFolders.GroupedCollection.IsSorted)
				return;

			if (folderSettings.DirectoryGroupDirection == SortDirection.Ascending)
			{
				if (folderSettings.DirectoryGroupOption == GroupOption.Size)
					// Always show file sections below folders
					FilesAndFolders.GroupedCollection.Order(x => x.OrderBy(y => y.First().PrimaryItemAttribute != StorageItemTypes.Folder || y.First().IsArchive)
						.ThenBy(y => y.Model.SortIndexOverride).ThenBy(y => y.Model.Text));
				else
					FilesAndFolders.GroupedCollection.Order(x => x.OrderBy(y => y.Model.SortIndexOverride).ThenBy(y => y.Model.Text));
			}
			else
			{
				if (folderSettings.DirectoryGroupOption == GroupOption.Size)
					// Always show file sections below folders
					FilesAndFolders.GroupedCollection.Order(x => x.OrderBy(y => y.First().PrimaryItemAttribute != StorageItemTypes.Folder || y.First().IsArchive)
						.ThenByDescending(y => y.Model.SortIndexOverride).ThenByDescending(y => y.Model.Text));
				else
					FilesAndFolders.GroupedCollection.Order(x => x.OrderByDescending(y => y.Model.SortIndexOverride).ThenByDescending(y => y.Model.Text));
			}

			FilesAndFolders.GroupedCollection.IsSorted = true;
		}

		public async Task GroupOptionsUpdated(CancellationToken token)
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
				FilesAndFolders.BeginBulkOperation();
				UpdateGroupOptions();

				if (FilesAndFolders.IsGrouped)
				{
					await Task.Run(() =>
					{
						FilesAndFolders.ResetGroups(token);
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

				await dispatcherQueue.EnqueueOrInvokeAsync(
					FilesAndFolders.EndBulkOperation);
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

		public Task ReloadItemGroupHeaderImagesAsync()
		{
			// This is needed to update the group icons for file type groups
			if (folderSettings.DirectoryGroupOption != GroupOption.FileType || FilesAndFolders.GroupedCollection is null)
				return Task.CompletedTask;

			return Task.Run(async () =>
			{
				foreach (var gp in FilesAndFolders.GroupedCollection.ToList())
				{
					var img = await GetItemTypeGroupIcon(gp.FirstOrDefault());
					await dispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						gp.Model.ImageSource = img;
					}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
				}
			});
		}

		public void UpdateGroupOptions()
		{
			FilesAndFolders.ItemGroupKeySelector = GroupingHelper.GetItemGroupKeySelector(folderSettings.DirectoryGroupOption, folderSettings.DirectoryGroupByDateUnit);
			var groupInfoSelector = GroupingHelper.GetGroupInfoSelector(folderSettings.DirectoryGroupOption, folderSettings.DirectoryGroupByDateUnit);
			FilesAndFolders.GetGroupHeaderInfo = groupInfoSelector.Item1;
			FilesAndFolders.GetExtendedGroupHeaderInfo = groupInfoSelector.Item2;
		}

		public Dictionary<string, BitmapImage> DefaultIcons = new ();

		private uint currentDefaultIconSize = 0;

		public async Task GetDefaultItemIcons(uint size)
		{
			if (currentDefaultIconSize == size)
				return;

			// TODO: Add more than just the folder icon
			DefaultIcons.Clear();

			using StorageItemThumbnail icon = await FilesystemTasks.Wrap(() => StorageItemIconHelpers.GetIconForItemType(size, IconPersistenceOptions.Persist));
			if (icon is not null)
			{
				var img = new BitmapImage();
				await img.SetSourceAsync(icon);
				DefaultIcons.Add(string.Empty, img);
			}

			currentDefaultIconSize = size;
		}

		private bool isLoadingItems = false;
		public bool IsLoadingItems
		{
			get => isLoadingItems;
			set => isLoadingItems = value;
		}

		private async Task<BitmapImage> GetShieldIcon()
		{
			shieldIcon ??= await UIHelpers.GetShieldIconResource();

			return shieldIcon;
		}

		// ThumbnailSize is set to 96 so that unless we override it, mode is in turn set to SingleItem
		private async Task LoadItemThumbnail(ListedItem item, uint thumbnailSize = 96, IStorageItem? matchingStorageItem = null)
		{
			var wasIconLoaded = false;
			if (item.IsLibrary || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
			{
				if (UserSettingsService.FoldersSettingsService.ShowThumbnails &&
					!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
				{
					var matchingStorageFile = matchingStorageItem?.AsBaseStorageFile() ?? await GetFileFromPathAsync(item.ItemPath);

					if (matchingStorageFile is not null)
					{
						// SingleItem returns image thumbnails in the correct aspect ratio for the grid layouts
						// ListView is used for the details and columns layout
						var thumbnailMode = thumbnailSize < 96 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

						using StorageItemThumbnail Thumbnail = await FilesystemTasks.Wrap(() => matchingStorageFile.GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ResizeThumbnail).AsTask());

						if (!(Thumbnail is null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.FileImage ??= new BitmapImage();
								item.FileImage.DecodePixelType = DecodePixelType.Logical;
								item.FileImage.DecodePixelWidth = (int)thumbnailSize;
								await item.FileImage.SetSourceAsync(Thumbnail);
								if (!string.IsNullOrEmpty(item.FileExtension) &&
									!item.IsShortcut && !item.IsExecutable &&
									!ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant()))
								{
									DefaultIcons.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
								}
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
							wasIconLoaded = true;
						}

						var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath, thumbnailSize);
						if (overlayInfo is not null)
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.IconOverlay = await overlayInfo.ToBitmapAsync();
								item.ShieldIcon = await GetShieldIcon();
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
						}
					}
				}

				if (!wasIconLoaded)
				{
					var iconInfo = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize, false);
					if (iconInfo.IconData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.FileImage = await iconInfo.IconData.ToBitmapAsync();
							if (!string.IsNullOrEmpty(item.FileExtension) &&
								!item.IsShortcut && !item.IsExecutable &&
								!ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant()))
							{
								DefaultIcons.AddIfNotPresent(item.FileExtension.ToLowerInvariant(), item.FileImage);
							}
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}

					if (iconInfo.OverlayData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
							item.ShieldIcon = await GetShieldIcon();
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}
				}
			}
			else
			{
				if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
				{
					var matchingStorageFolder = matchingStorageItem?.AsBaseStorageFolder() ?? await GetFolderFromPathAsync(item.ItemPath);
					if (matchingStorageFolder is not null)
					{
						// SingleItem returns image thumbnails in the correct aspect ratio for the grid layouts
						// ListView is used for the details and columns layout
						var thumbnailMode = thumbnailSize < 96 ? ThumbnailMode.ListView : ThumbnailMode.SingleItem;

						// We use ReturnOnlyIfCached because otherwise folders thumbnails have a black background, this has the downside the folder previews don't work
						using StorageItemThumbnail Thumbnail = await FilesystemTasks.Wrap(() => matchingStorageFolder.GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ReturnOnlyIfCached).AsTask());
						if (!(Thumbnail is null || Thumbnail.Size == 0 || Thumbnail.OriginalHeight == 0 || Thumbnail.OriginalWidth == 0))
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.FileImage ??= new BitmapImage();
								item.FileImage.DecodePixelType = DecodePixelType.Logical;
								item.FileImage.DecodePixelWidth = (int)thumbnailSize;
								await item.FileImage.SetSourceAsync(Thumbnail);
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
							wasIconLoaded = true;
						}

						var overlayInfo = await FileThumbnailHelper.LoadOverlayAsync(item.ItemPath, thumbnailSize);
						if (overlayInfo is not null)
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								item.IconOverlay = await overlayInfo.ToBitmapAsync();
								item.ShieldIcon = await GetShieldIcon();
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
						}
					}
				}

				if (!wasIconLoaded)
				{
					var iconInfo = await FileThumbnailHelper.LoadIconAndOverlayAsync(item.ItemPath, thumbnailSize, true);
					if (iconInfo.IconData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.FileImage = await iconInfo.IconData.ToBitmapAsync();
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}

					if (iconInfo.OverlayData is not null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							item.IconOverlay = await iconInfo.OverlayData.ToBitmapAsync();
							item.ShieldIcon = await GetShieldIcon();
						}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
					}
				}
			}
		}

		private static void SetFileTag(ListedItem item)
		{
			var dbInstance = FileTagsHelper.GetDbInstance();
			dbInstance.SetTags(item.ItemPath, item.FileFRN, item.FileTags);
		}

		// This works for recycle bin as well as GetFileFromPathAsync/GetFolderFromPathAsync work
		// for file inside the recycle bin (but not on the recycle bin folder itself)
		public async Task LoadExtendedItemProperties(ListedItem item, uint thumbnailSize = 20)
		{
			if (item is null)
				return;

			itemLoadQueue[item.ItemPath] = false;

			var cts = loadPropsCTS;

			try
			{
				await Task.Run(async () =>
				{
					if (itemLoadQueue.TryGetValue(item.ItemPath, out var canceled) && canceled)
						return;

					item.ItemPropertiesInitialized = true;
					var wasSyncStatusLoaded = false;
					var loadGroupHeaderInfo = false;
					ImageSource? groupImage = null;
					GroupedCollection<ListedItem>? gp = null;
					try
					{
						var isFileTypeGroupMode = folderSettings.DirectoryGroupOption == GroupOption.FileType;
						BaseStorageFile? matchingStorageFile = null;
						if (item.Key is not null && FilesAndFolders.IsGrouped && FilesAndFolders.GetExtendedGroupHeaderInfo is not null)
						{
							gp = FilesAndFolders.GroupedCollection?.Where(x => x.Model.Key == item.Key).FirstOrDefault();
							loadGroupHeaderInfo = gp is not null && !gp.Model.Initialized && gp.GetExtendedGroupHeaderInfo is not null;
						}

						if (item.IsLibrary || item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
						{
							if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
							{
								cts.Token.ThrowIfCancellationRequested();
								matchingStorageFile = await GetFileFromPathAsync(item.ItemPath);
								if (matchingStorageFile is not null)
								{
									cts.Token.ThrowIfCancellationRequested();
									await LoadItemThumbnail(item, thumbnailSize, matchingStorageFile);

									var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFile);
									var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFile);
									var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);
									var itemType = (item.ItemType == "Folder".GetLocalizedResource()) ? item.ItemType : matchingStorageFile.DisplayType;
									cts.Token.ThrowIfCancellationRequested();

									await dispatcherQueue.EnqueueOrInvokeAsync(() =>
									{
										item.FolderRelativeId = matchingStorageFile.FolderRelativeId;
										item.ItemType = itemType;
										item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
										item.FileFRN = fileFRN;
										item.FileTags = fileTag;
									},
									Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

									SetFileTag(item);
									wasSyncStatusLoaded = true;
								}
							}

							if (!wasSyncStatusLoaded)
								await LoadItemThumbnail(item, thumbnailSize, null);
						}
						else
						{
							if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath))
							{
								cts.Token.ThrowIfCancellationRequested();
								BaseStorageFolder matchingStorageFolder = await GetFolderFromPathAsync(item.ItemPath);
								if (matchingStorageFolder is not null)
								{
									cts.Token.ThrowIfCancellationRequested();
									await LoadItemThumbnail(item, thumbnailSize, matchingStorageFolder);
									if (matchingStorageFolder.DisplayName != item.Name && !matchingStorageFolder.DisplayName.StartsWith("$R", StringComparison.Ordinal))
									{
										cts.Token.ThrowIfCancellationRequested();
										await dispatcherQueue.EnqueueOrInvokeAsync(() =>
										{
											item.ItemNameRaw = matchingStorageFolder.DisplayName;
										});
										await fileListCache.SaveFileDisplayNameToCache(item.ItemPath, matchingStorageFolder.DisplayName);
										if (folderSettings.DirectorySortOption == SortOption.Name && !isLoadingItems)
										{
											await OrderFilesAndFoldersAsync();
											await ApplySingleFileChangeAsync(item);
										}
									}

									cts.Token.ThrowIfCancellationRequested();
									var syncStatus = await CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
									var fileFRN = await FileTagsHelper.GetFileFRN(matchingStorageFolder);
									var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);
									var itemType = (item.ItemType == "Folder".GetLocalizedResource()) ? item.ItemType : matchingStorageFolder.DisplayType;
									cts.Token.ThrowIfCancellationRequested();

									await dispatcherQueue.EnqueueOrInvokeAsync(() =>
									{
										item.FolderRelativeId = matchingStorageFolder.FolderRelativeId;
										item.ItemType = itemType;
										item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
										item.FileFRN = fileFRN;
										item.FileTags = fileTag;
									},
									Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

									SetFileTag(item);
									wasSyncStatusLoaded = true;
								}
							}
							if (!wasSyncStatusLoaded)
							{
								cts.Token.ThrowIfCancellationRequested();
								await LoadItemThumbnail(item, thumbnailSize, null);
							}
						}

						if (loadGroupHeaderInfo && isFileTypeGroupMode)
						{
							cts.Token.ThrowIfCancellationRequested();
							groupImage = await GetItemTypeGroupIcon(item, matchingStorageFile);
						}
					}
					catch (Exception)
					{
					}
					finally
					{
						if (!wasSyncStatusLoaded)
						{
							cts.Token.ThrowIfCancellationRequested();
							await FilesystemTasks.Wrap(async () =>
							{
								var fileTag = FileTagsHelper.ReadFileTag(item.ItemPath);

								await dispatcherQueue.EnqueueOrInvokeAsync(() =>
								{
									// Reset cloud sync status icon
									item.SyncStatusUI = new CloudDriveSyncStatusUI();

									item.FileTags = fileTag;
								},
								Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

								SetFileTag(item);
							});
						}

						if (loadGroupHeaderInfo)
						{
							cts.Token.ThrowIfCancellationRequested();
							await SafetyExtensions.IgnoreExceptions(() =>
								dispatcherQueue.EnqueueOrInvokeAsync(() =>
								{
									gp.Model.ImageSource = groupImage;
									gp.InitializeExtendedGroupHeaderInfoAsync();
								}));
						}
					}
				}, cts.Token);
			}
			catch (OperationCanceledException)
			{
				// Ignored
			}
			finally
			{
				itemLoadQueue.TryRemove(item.ItemPath, out _);
			}
		}

		private async Task<ImageSource?> GetItemTypeGroupIcon(ListedItem item, BaseStorageFile? matchingStorageItem = null)
		{
			ImageSource? groupImage = null;
			if (item.PrimaryItemAttribute != StorageItemTypes.Folder || item.IsArchive)
			{
				var headerIconInfo = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(item.ItemPath, 64u, false);

				if (headerIconInfo is not null && !item.IsShortcut)
					groupImage = await dispatcherQueue.EnqueueOrInvokeAsync(() => headerIconInfo.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

				// The groupImage is null if loading icon from fulltrust process failed
				if (!item.IsShortcut && !item.IsHiddenItem && !FtpHelpers.IsFtpPath(item.ItemPath) && groupImage is null)
				{
					matchingStorageItem ??= await GetFileFromPathAsync(item.ItemPath);

					if (matchingStorageItem is not null)
					{
						using StorageItemThumbnail headerThumbnail = await FilesystemTasks.Wrap(() => matchingStorageItem.GetThumbnailAsync(ThumbnailMode.DocumentsView, 36, ThumbnailOptions.UseCurrentScale).AsTask());
						if (headerThumbnail is not null)
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								var bmp = new BitmapImage();
								await bmp.SetSourceAsync(headerThumbnail);
								groupImage = bmp;
							});
						}

					}
				}
			}
			// This prevents both the shortcut glyph and folder icon being shown
			else if (!item.IsShortcut)
			{
				await dispatcherQueue.EnqueueOrInvokeAsync(() => groupImage = new SvgImageSource(new Uri("ms-appx:///Assets/FolderIcon2.svg"))
				{
					RasterizePixelHeight = 128,
					RasterizePixelWidth = 128,
				}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}

			return groupImage;
		}

		public void RefreshItems(string? previousDir, Action postLoadCallback = null)
		{
			RapidAddItemsToCollectionAsync(WorkingDirectory, previousDir, postLoadCallback);
		}

		private async Task RapidAddItemsToCollectionAsync(string path, string? previousDir, Action postLoadCallback)
		{
			IsSearchResults = false;
			ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting });

			CancelLoadAndClearFiles();

			if (string.IsNullOrEmpty(path))
				return;

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

				IsLoadingItems = true;

				filesAndFolders.Clear();
				FilesAndFolders.Clear();

				ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress });

				if (path.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal))
				{
					if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library) && !library.IsEmpty)
					{
						var libItem = new LibraryItem(library);
						foreach (var folder in library.Folders)
							await RapidAddItemsToCollection(folder, libItem);
					}
				}
				else
				{
					await RapidAddItemsToCollection(path);
				}

				ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete, PreviousDirectory = previousDir, Path = path });
				IsLoadingItems = false;

				AdaptiveLayoutHelpers.ApplyAdaptativeLayout(folderSettings, WorkingDirectory, filesAndFolders);

				if (Ioc.Default.GetRequiredService<PreviewPaneViewModel>().IsEnabled)
				{
					// Find and select README file
					foreach (var item in filesAndFolders)
					{
						if (item.PrimaryItemAttribute == StorageItemTypes.File && item.Name.Contains("readme", StringComparison.OrdinalIgnoreCase))
						{
							OnSelectionRequestedEvent?.Invoke(this, new List<ListedItem>() { item });
							break;
						}
					}
				}
			}
			finally
			{
				// Make sure item count is updated
				DirectoryInfoUpdated?.Invoke(this, EventArgs.Empty);
				enumFolderSemaphore.Release();
			}

			postLoadCallback?.Invoke();
		}

		private async Task RapidAddItemsToCollection(string? path, LibraryItem? library = null)
		{
			if (string.IsNullOrEmpty(path))
				return;

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			await GetDefaultItemIcons(folderSettings.GetIconSize());

			if (FtpHelpers.IsFtpPath(path))
			{
				// Recycle bin and network are enumerated by the fulltrust process
				PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false });
				await EnumerateItemsFromSpecialFolderAsync(path);
			}
			else
			{
				var isRecycleBin = path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				var enumerated = await EnumerateItemsFromStandardFolderAsync(path, addFilesCTS.Token, library);

				// Hide progressbar after enumeration
				IsLoadingItems = false;

				switch (enumerated)
				{
					// Enumerated with FindFirstFileExFromApp
					// Is folder synced to cloud storage?
					case 0:
						currentStorageFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
						var syncStatus = await CheckCloudDriveSyncStatusAsync(currentStorageFolder?.Item);
						PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown });
						WatchForDirectoryChanges(path, syncStatus);
						break;

					// Enumerated with StorageFolder
					case 1:
						PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false, IsTypeRecycleBin = isRecycleBin });
						currentStorageFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
						WatchForStorageFolderChanges(currentStorageFolder?.Item);
						break;

					// Watch for changes using FTP in Box Drive folder (#7428) and network drives (#5869)
					case 2:
						PageTypeUpdated?.Invoke(this, new PageTypeUpdatedEventArgs() { IsTypeCloudDrive = false });
						WatchForWin32FolderChanges(path);
						break;

					// Enumeration failed
					case -1:
					default:
						break;
				}
			}

			if (addFilesCTS.IsCancellationRequested)
			{
				addFilesCTS = new CancellationTokenSource();
				IsLoadingItems = false;
				return;
			}

			stopwatch.Stop();
			Debug.WriteLine($"Loading of items in {path} completed in {stopwatch.ElapsedMilliseconds} milliseconds.\n");
		}

		public void CloseWatcher()
		{
			watcher?.Dispose();
			watcher = null;

			aProcessQueueAction = null;
			gitProcessQueueAction = null;
			watcherCTS?.Cancel();
			watcherCTS = new CancellationTokenSource();
		}

		public async Task EnumerateItemsFromSpecialFolderAsync(string path)
		{
			var isFtp = FtpHelpers.IsFtpPath(path);

			CurrentFolder = new ListedItem(null!)
			{
				PrimaryItemAttribute = StorageItemTypes.Folder,
				ItemPropertiesInitialized = true,
				ItemNameRaw =
							path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase) ? "RecycleBin".GetLocalizedResource() :
							path.StartsWith(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase) ? "Network".GetLocalizedResource() :
							path.StartsWith(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase) ? "ThisPC".GetLocalizedResource() :
							isFtp ? "FTP" : "Unknown",
				ItemDateModifiedReal = DateTimeOffset.Now, // Fake for now
				ItemDateCreatedReal = DateTimeOffset.Now,  // Fake for now
				ItemType = "Folder".GetLocalizedResource(),
				FileImage = null,
				LoadFileIcon = false,
				ItemPath = path,
				FileSize = null,
				FileSizeBytes = 0
			};

			if (!isFtp || !FtpHelpers.VerifyFtpPath(path))
				return;

			// TODO: Show invalid path dialog

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

			await Task.Run(async () =>
			{
				try
				{
					if (!client.IsConnected && await WrappedAutoConnectFtpAsync(client) is null)
					{
						await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
						{
							var credentialDialogViewModel = new CredentialDialogViewModel();

							if (await dialogService.ShowDialogAsync(credentialDialogViewModel) != DialogResult.Primary)
								return;

							// Can't do more than that to mitigate immutability of strings. Perhaps convert DisposableArray to SecureString immediately?
							if (!credentialDialogViewModel.IsAnonymous)
								client.Credentials = new NetworkCredential(credentialDialogViewModel.UserName, Encoding.UTF8.GetString(credentialDialogViewModel.Password));
						});
					}

					if (!client.IsConnected && await WrappedAutoConnectFtpAsync(client) is null)
						throw new InvalidOperationException();

					FtpManager.Credentials[client.Host] = client.Credentials;

					var sampler = new IntervalSampler(500);
					var list = await client.GetListing(FtpHelpers.GetFtpPath(path));

					for (var i = 0; i < list.Length; i++)
					{
						filesAndFolders.Add(new FtpItem(list[i], path));

						if (i == list.Length - 1 || sampler.CheckNow())
						{
							await OrderFilesAndFoldersAsync();
							await ApplyFilesAndFoldersChangesAsync();
						}
					}
				}
				catch
				{
					// Network issue
					FtpManager.Credentials.Remove(client.Host);
				}
			});
		}

		public async Task<int> EnumerateItemsFromStandardFolderAsync(string path, CancellationToken cancellationToken, LibraryItem? library = null)
		{
			// Flag to use FindFirstFileExFromApp or StorageFolder enumeration - Use storage folder for Box Drive (#4629)
			var isBoxFolder = App.CloudDrivesManager.Drives.FirstOrDefault(x => x.Text == "Box")?.Path?.TrimEnd('\\') is string boxFolder && path.StartsWith(boxFolder);
			bool isWslDistro = App.WSLDistroManager.TryGetDistro(path, out _);
			bool isNetwork = path.StartsWith(@"\\", StringComparison.Ordinal) &&
				!path.StartsWith(@"\\?\", StringComparison.Ordinal) &&
				!path.StartsWith(@"\\SHELL\", StringComparison.Ordinal) &&
				!isWslDistro;
			bool enumFromStorageFolder = isBoxFolder;

			BaseStorageFolder? rootFolder = null;

			if (isNetwork)
			{
				var auth = await NetworkDrivesAPI.AuthenticateNetworkShare(path);
				if (!auth)
					return -1;
			}

			if (!enumFromStorageFolder && FolderHelpers.CheckFolderAccessWithWin32(path))
			{
				// Will enumerate with FindFirstFileExFromApp, rootFolder only used for Bitlocker
				currentStorageFolder = null;
			}
			else if (workingRoot is not null)
			{
				var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path, workingRoot, currentStorageFolder));
				if (!res)
					return -1;

				currentStorageFolder = res.Result;
				rootFolder = currentStorageFolder.Item;
				enumFromStorageFolder = true;
			}
			else
			{
				var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(path));
				if (res)
				{
					currentStorageFolder = res.Result;
					rootFolder = currentStorageFolder.Item;
				}
				else if (res == FileSystemStatusCode.Unauthorized)
				{
					await DialogDisplayHelper.ShowDialogAsync(
						"AccessDenied".GetLocalizedResource(),
						"AccessDeniedToFolder".GetLocalizedResource());

					return -1;
				}
				else if (res == FileSystemStatusCode.NotFound)
				{
					await DialogDisplayHelper.ShowDialogAsync(
						"FolderNotFoundDialog/Title".GetLocalizedResource(),
						"FolderNotFoundDialog/Text".GetLocalizedResource());

					return -1;
				}
				else
				{
					await DialogDisplayHelper.ShowDialogAsync(
						"DriveUnpluggedDialog/Title".GetLocalizedResource(),
						res.ErrorCode.ToString());

					return -1;
				}
			}

			var pathRoot = Path.GetPathRoot(path);
			if (Path.IsPathRooted(path) && pathRoot == path)
			{
				rootFolder ??= await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path));
				if (await FolderHelpers.CheckBitlockerStatusAsync(rootFolder, WorkingDirectory))
					await ContextMenu.InvokeVerb("unlock-bde", pathRoot);
			}

			if (enumFromStorageFolder)
			{
				var basicProps = await rootFolder?.GetBasicPropertiesAsync();
				var currentFolder = library ?? new ListedItem(rootFolder?.FolderRelativeId ?? string.Empty)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemPropertiesInitialized = true,
					ItemNameRaw = rootFolder?.DisplayName ?? string.Empty,
					ItemDateModifiedReal = basicProps.DateModified,
					ItemType = rootFolder?.DisplayType ?? string.Empty,
					FileImage = null,
					LoadFileIcon = false,
					ItemPath = string.IsNullOrEmpty(rootFolder?.Path) ? currentStorageFolder?.Path ?? string.Empty : rootFolder.Path,
					FileSize = null,
					FileSizeBytes = 0,
				};

				if (library is null)
					currentFolder.ItemDateCreatedReal = rootFolder?.DateCreated ?? DateTimeOffset.Now;

				CurrentFolder = currentFolder;
				await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder, cancellationToken);

				// Workaround for #7428
				return isBoxFolder ? 2 : 1;
			}
			else
			{
				(IntPtr hFile, WIN32_FIND_DATA findData, int errorCode) = await Task.Run(() =>
				{
					var findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
					var additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

					IntPtr hFileTsk = FindFirstFileExFromApp(
						path + "\\*.*",
						findInfoLevel,
						out WIN32_FIND_DATA findDataTsk,
						FINDEX_SEARCH_OPS.FindExSearchNameMatch,
						IntPtr.Zero,
						additionalFlags);

					return (hFileTsk, findDataTsk, hFileTsk.ToInt64() == -1 ? Marshal.GetLastWin32Error() : 0);
				})
				.WithTimeoutAsync(TimeSpan.FromSeconds(5));

				var itemModifiedDate = DateTime.Now;
				var itemCreatedDate = DateTime.Now;

				try
				{
					FileTimeToSystemTime(ref findData.ftLastWriteTime, out var systemModifiedTimeOutput);
					itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

					FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedTimeOutput);
					itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
				}
				catch (ArgumentException)
				{
				}

				var isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
				var opacity = isHidden ? Constants.UI.DimItemOpacity : 1d;

				var currentFolder = library ?? new ListedItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemPropertiesInitialized = true,
					ItemNameRaw = Path.GetFileName(path.TrimEnd('\\')),
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = folderTypeTextLocalized,
					FileImage = null,
					IsHiddenItem = isHidden,
					Opacity = opacity,
					LoadFileIcon = false,
					ItemPath = path,
					FileSize = null,
					FileSizeBytes = 0,
				};

				CurrentFolder = currentFolder;

				if (hFile == IntPtr.Zero)
				{
					await DialogDisplayHelper.ShowDialogAsync("DriveUnpluggedDialog/Title".GetLocalizedResource(), "");

					return -1;
				}
				else if (hFile.ToInt64() == -1)
				{
					await EnumFromStorageFolderAsync(path, rootFolder, currentStorageFolder, cancellationToken);

					// errorCode == ERROR_ACCESS_DENIED
					if (!filesAndFolders.Any() && errorCode == 0x5)
					{
						await DialogDisplayHelper.ShowDialogAsync(
							"AccessDenied".GetLocalizedResource(),
							"AccessDeniedToFolder".GetLocalizedResource());

						return -1;
					}

					return 1;
				}
				else
				{
					await Task.Run(async () =>
					{
						List<ListedItem> fileList = await Win32StorageEnumerator.ListEntries(path, hFile, findData, cancellationToken, -1, intermediateAction: async (intermediateList) =>
						{
							filesAndFolders.AddRange(intermediateList);
							await OrderFilesAndFoldersAsync();
							await ApplyFilesAndFoldersChangesAsync();
						}, defaultIconPairs: DefaultIcons);

						filesAndFolders.AddRange(fileList);
						await OrderFilesAndFoldersAsync();
						await ApplyFilesAndFoldersChangesAsync();
					});

					return 0;
				}
			}
		}

		private Task EnumFromStorageFolderAsync(string path, BaseStorageFolder? rootFolder, StorageFolderWithPath currentStorageFolder, CancellationToken cancellationToken)
		{
			if (rootFolder is null)
				return Task.CompletedTask;

			return Task.Run(async () =>
			{
				List<ListedItem> finalList = await UniversalStorageEnumerator.ListEntries(
					rootFolder,
					currentStorageFolder,
					cancellationToken,
					-1,
					async (intermediateList) =>
					{
						filesAndFolders.AddRange(intermediateList);

						await OrderFilesAndFoldersAsync();
						await ApplyFilesAndFoldersChangesAsync();
					},
					defaultIconPairs: DefaultIcons);

				filesAndFolders.AddRange(finalList);

				await OrderFilesAndFoldersAsync();
				await ApplyFilesAndFoldersChangesAsync();
			}, cancellationToken);
		}

		private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatusAsync(IStorageItem item)
		{
			int? syncStatus = null;
			if (item is BaseStorageFile file && file.Properties is not null)
			{
				var extraProperties = await FilesystemTasks.Wrap(() => file.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus" }).AsTask());
				if (extraProperties)
					syncStatus = (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
			}
			else if (item is BaseStorageFolder folder && folder.Properties is not null)
			{
				var extraProperties = await FilesystemTasks.Wrap(() => folder.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus", "System.FileOfflineAvailabilityStatus" }).AsTask());
				if (extraProperties)
				{
					syncStatus = (int?)(uint?)extraProperties.Result["System.FileOfflineAvailabilityStatus"];

					// If no FileOfflineAvailabilityStatus, check FilePlaceholderStatus
					syncStatus ??= (int?)(uint?)extraProperties.Result["System.FilePlaceholderStatus"];
				}
			}

			if (syncStatus is null || !Enum.IsDefined(typeof(CloudDriveSyncStatus), syncStatus))
				return CloudDriveSyncStatus.Unknown;

			return (CloudDriveSyncStatus)syncStatus;
		}

		private async Task WatchForStorageFolderChanges(BaseStorageFolder? rootFolder)
		{
			if (rootFolder is null)
				return;

			await Task.Factory.StartNew(() =>
			{
				var options = new QueryOptions()
				{
					FolderDepth = FolderDepth.Shallow,
					IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
				};

				options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
				options.SetThumbnailPrefetch(ThumbnailMode.ListView, 0, ThumbnailOptions.ReturnOnlyIfCached);

				if (rootFolder.AreQueryOptionsSupported(options))
				{
					var itemQueryResult = rootFolder.CreateItemQueryWithOptions(options).ToStorageItemQueryResult();
					itemQueryResult.ContentsChanged += ItemQueryResult_ContentsChanged;

					// Just get one item to start getting notifications
					var watchedItemsOperation = itemQueryResult.GetItemsAsync(0, 1);

					watcherCTS.Token.Register(() =>
					{
						itemQueryResult.ContentsChanged -= ItemQueryResult_ContentsChanged;
						watchedItemsOperation?.Cancel();
					});
				}
			},
			default,
			TaskCreationOptions.LongRunning,
			TaskScheduler.Default);
		}

		private void WatchForWin32FolderChanges(string? folderPath)
		{
			if (Directory.Exists(folderPath))
			{
				watcher = new FileSystemWatcher
				{
					Path = folderPath,
					Filter = "*.*",
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
				};

				watcher.Created += DirectoryWatcher_Changed;
				watcher.Deleted += DirectoryWatcher_Changed;
				watcher.Renamed += DirectoryWatcher_Changed;
				watcher.EnableRaisingEvents = true;
			}
		}

		private async void DirectoryWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			Debug.WriteLine($"Directory watcher event: {e.ChangeType}, {e.FullPath}");

			await dispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				RefreshItems(null);
			});
		}

		private async void ItemQueryResult_ContentsChanged(IStorageQueryResultBase sender, object args)
		{
			// Query options have to be reapplied otherwise old results are returned
			var options = new QueryOptions()
			{
				FolderDepth = FolderDepth.Shallow,
				IndexerOption = IndexerOption.OnlyUseIndexerAndOptimizeForIndexedProperties
			};

			options.SetPropertyPrefetch(PropertyPrefetchOptions.None, null);
			options.SetThumbnailPrefetch(ThumbnailMode.ListView, 0, ThumbnailOptions.ReturnOnlyIfCached);

			sender.ApplyNewQueryOptions(options);

			await dispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				RefreshItems(null);
			});
		}

		private void WatchForDirectoryChanges(string path, CloudDriveSyncStatus syncStatus)
		{
			Debug.WriteLine($"WatchForDirectoryChanges: {path}");
			var hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(path, 1, 1 | 2 | 4,
				IntPtr.Zero, 3, (uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped, IntPtr.Zero);
			if (hWatchDir.ToInt64() == -1)
				return;

			var hasSyncStatus = syncStatus != CloudDriveSyncStatus.NotSynced && syncStatus != CloudDriveSyncStatus.Unknown;

			aProcessQueueAction ??= Task.Factory.StartNew(() => ProcessOperationQueue(watcherCTS.Token, hasSyncStatus), default,
				TaskCreationOptions.LongRunning, TaskScheduler.Default);

			var aWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
			{
				var buff = new byte[4096];
				var rand = Guid.NewGuid();
				var notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE;

				if (hasSyncStatus)
					notifyFilters |= FILE_NOTIFY_CHANGE_ATTRIBUTES;

				var overlapped = new OVERLAPPED();
				overlapped.hEvent = CreateEvent(IntPtr.Zero, false, false, null);
				const uint INFINITE = 0xFFFFFFFF;

				while (x.Status != AsyncStatus.Canceled)
				{
					unsafe
					{
						fixed (byte* pBuff = buff)
						{
							ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
							if (x.Status != AsyncStatus.Canceled)
							{
								ReadDirectoryChangesW(hWatchDir, pBuff,
								4096, false,
								notifyFilters, null,
								ref overlapped, null);
							}
							else
							{
								break;
							}

							Debug.WriteLine("waiting: {0}", rand);
							if (x.Status == AsyncStatus.Canceled)
								break;

							var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);
							Debug.WriteLine("wait done: {0}", rand);

							uint offset = 0;
							ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
							if (x.Status == AsyncStatus.Canceled)
								break;

							do
							{
								notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
								string? FileName = null;
								unsafe
								{
									fixed (char* name = notifyInfo.FileName)
									{
										FileName = Path.Combine(path, new string(name, 0, (int)notifyInfo.FileNameLength / 2));
									}
								}

								uint action = notifyInfo.Action;

								Debug.WriteLine("action: {0}", action);

								operationQueue.Enqueue((action, FileName));

								offset += notifyInfo.NextEntryOffset;
							}
							while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

							operationEvent.Set();

							//ResetEvent(overlapped.hEvent);
							Debug.WriteLine("Task running...");
						}
					}
				}

				CloseHandle(overlapped.hEvent);
				operationQueue.Clear();

				Debug.WriteLine("aWatcherAction done: {0}", rand);
			});

			if (GitDirectory is not null)
				WatchForGitChanges(hasSyncStatus);

			watcherCTS.Token.Register(() =>
			{
				if (aWatcherAction is not null)
				{
					aWatcherAction?.Cancel();

					// Prevent duplicate execution of this block
					aWatcherAction = null;

					Debug.WriteLine("watcher canceled");
				}

				CancelIoEx(hWatchDir, IntPtr.Zero);
				CloseHandle(hWatchDir);
			});
		}

		private void WatchForGitChanges(bool hasSyncStatus)
		{
			var hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(
				GitDirectory!,
				1,
				1 | 2 | 4,
				IntPtr.Zero,
				3,
				(uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped,
				IntPtr.Zero);

			if (hWatchDir.ToInt64() == -1)
				return;

			gitProcessQueueAction ??= Task.Factory.StartNew(() => ProcessGitChangesQueue(watcherCTS.Token), default,
				TaskCreationOptions.LongRunning, TaskScheduler.Default);

			var gitWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
			{
				var buff = new byte[4096];
				var rand = Guid.NewGuid();
				var notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE | FILE_NOTIFY_CHANGE_CREATION;

				if (hasSyncStatus)
					notifyFilters |= FILE_NOTIFY_CHANGE_ATTRIBUTES;

				var overlapped = new OVERLAPPED();
				overlapped.hEvent = CreateEvent(IntPtr.Zero, false, false, null);
				const uint INFINITE = 0xFFFFFFFF;

				while (x.Status != AsyncStatus.Canceled)
				{
					unsafe
					{
						fixed (byte* pBuff = buff)
						{
							ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
							if (x.Status == AsyncStatus.Canceled)
								break;

							ReadDirectoryChangesW(hWatchDir, pBuff,
								4096, true,
								notifyFilters, null,
								ref overlapped, null);

							if (x.Status == AsyncStatus.Canceled)
								break;

							var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);

							uint offset = 0;
							ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
							if (x.Status == AsyncStatus.Canceled)
								break;

							do
							{
								notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);

								uint action = notifyInfo.Action;

								gitChangesQueue.Enqueue(action);

								offset += notifyInfo.NextEntryOffset;
							}
							while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

							gitChangedEvent.Set();
						}
					}
				}

				CloseHandle(overlapped.hEvent);
				gitChangesQueue.Clear();
			});

			watcherCTS.Token.Register(() =>
			{
				if (gitWatcherAction is not null)
				{
					gitWatcherAction?.Cancel();

					// Prevent duplicate execution of this block
					gitWatcherAction = null;
				}

				CancelIoEx(hWatchDir, IntPtr.Zero);
				CloseHandle(hWatchDir);
			});
		}

		private async Task ProcessGitChangesQueue(CancellationToken cancellationToken)
		{
			const int DELAY = 200;
			var sampler = new IntervalSampler(100);
			int changes = 0;

			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					if (await gitChangedEvent.WaitAsync(DELAY, cancellationToken))
					{
						gitChangedEvent.Reset();
						while (gitChangesQueue.TryDequeue(out var _))
							++changes;

						if (changes != 0 && sampler.CheckNow())
						{
							await dispatcherQueue.EnqueueOrInvokeAsync(() => GitDirectoryUpdated?.Invoke(null, null!));
							changes = 0;
						}
					}
				}
			}
			catch { }
		}

		private async Task ProcessOperationQueue(CancellationToken cancellationToken, bool hasSyncStatus)
		{
			const uint FILE_ACTION_ADDED = 0x00000001;
			const uint FILE_ACTION_REMOVED = 0x00000002;
			const uint FILE_ACTION_MODIFIED = 0x00000003;
			const uint FILE_ACTION_RENAMED_OLD_NAME = 0x00000004;
			const uint FILE_ACTION_RENAMED_NEW_NAME = 0x00000005;

			const int UPDATE_BATCH_SIZE = 32;
			var sampler = new IntervalSampler(200);
			var updateQueue = new Queue<string>();

			var anyEdits = false;
			ListedItem? lastItemAdded = null;
			var rand = Guid.NewGuid();

			// Call when any edits have occurred
			async Task HandleChangesOccurredAsync()
			{
				await OrderFilesAndFoldersAsync();
				await ApplyFilesAndFoldersChangesAsync();

				if (lastItemAdded is not null)
				{
					await RequestSelectionAsync(new List<ListedItem>() { lastItemAdded });
					lastItemAdded = null;
				}

				anyEdits = false;
			}

			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					if (await operationEvent.WaitAsync(200, cancellationToken))
					{
						operationEvent.Reset();

						while (operationQueue.TryDequeue(out var operation))
						{
							if (cancellationToken.IsCancellationRequested)
								break;

							try
							{
								switch (operation.Action)
								{
									case FILE_ACTION_ADDED:
									case FILE_ACTION_RENAMED_NEW_NAME:
										lastItemAdded = await AddFileOrFolderAsync(operation.FileName);
										if (lastItemAdded is not null)
											anyEdits = true;
										break;

									case FILE_ACTION_MODIFIED:
										if (!updateQueue.Contains(operation.FileName))
											updateQueue.Enqueue(operation.FileName);
										break;

									case FILE_ACTION_REMOVED:
										var itemRemoved = await RemoveFileOrFolderAsync(operation.FileName);
										if (itemRemoved is not null)
											anyEdits = true;
										break;

									case FILE_ACTION_RENAMED_OLD_NAME:
										var itemRenamedOld = await RemoveFileOrFolderAsync(operation.FileName);
										if (itemRenamedOld is not null)
											anyEdits = true;
										break;
								}
							}
							catch (Exception ex)
							{
								App.Logger.LogWarning(ex, ex.Message);
							}

							if (anyEdits && sampler.CheckNow())
								await HandleChangesOccurredAsync();
						}

						var itemsToUpdate = new List<string>();
						for (var i = 0; i < UPDATE_BATCH_SIZE && updateQueue.Count > 0; i++)
							itemsToUpdate.Add(updateQueue.Dequeue());

						await UpdateFilesOrFoldersAsync(itemsToUpdate, hasSyncStatus);
					}

					if (updateQueue.Count > 0)
					{
						var itemsToUpdate = new List<string>();
						for (var i = 0; i < UPDATE_BATCH_SIZE && updateQueue.Count > 0; i++)
							itemsToUpdate.Add(updateQueue.Dequeue());

						await UpdateFilesOrFoldersAsync(itemsToUpdate, hasSyncStatus);
					}

					if (anyEdits && sampler.CheckNow())
						await HandleChangesOccurredAsync();
				}
			}
			catch
			{
				// Prevent disposed cancellation token
			}

			Debug.WriteLine("aProcessQueueAction done: {0}", rand);
		}

		public Task<ListedItem> AddFileOrFolderFromShellFile(ShellFileItem item)
		{
			return
				item.IsFolder ?
				UniversalStorageEnumerator.AddFolderAsync(ShellStorageFolder.FromShellItem(item), currentStorageFolder, addFilesCTS.Token) :
				UniversalStorageEnumerator.AddFileAsync(ShellStorageFile.FromShellItem(item), currentStorageFolder, addFilesCTS.Token);
		}

		private async Task AddFileOrFolderAsync(ListedItem? item)
		{
			if (item is null)
				return;

			try
			{
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (!filesAndFolders.Any(x => x.ItemPath.Equals(item.ItemPath, StringComparison.OrdinalIgnoreCase))) // Avoid adding duplicate items
			{
				filesAndFolders.Add(item);

				if (UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
				{
					// New file added, enumerate ADS
					foreach (var ads in NativeFileOperationsHelper.GetAlternateStreams(item.ItemPath))
					{
						var adsItem = Win32StorageEnumerator.GetAlternateStream(ads, item);
						filesAndFolders.Add(adsItem);
					}
				}
			}

			enumFolderSemaphore.Release();
		}

		private async Task<ListedItem?> AddFileOrFolderAsync(string fileOrFolderPath)
		{
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
			var additionalFlags = FIND_FIRST_EX_CASE_SENSITIVE;

			IntPtr hFile = FindFirstFileExFromApp(fileOrFolderPath, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
												  additionalFlags);
			if (hFile.ToInt64() == -1)
			{
				// If we cannot find the file (probably since it doesn't exist anymore) simply exit without adding it
				return null;
			}

			FindClose(hFile);

			var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
			var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
			var startWithDot = findData.cFileName.StartsWith('.');
			if ((isHidden &&
			   (!UserSettingsService.FoldersSettingsService.ShowHiddenItems ||
			   (isSystem && !UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) ||
			   (startWithDot && !UserSettingsService.FoldersSettingsService.ShowDotFiles))
			{
				// Do not add to file list if hidden/system attribute is set and system/hidden file are not to be shown
				return null;
			}

			ListedItem listedItem;

			// FILE_ATTRIBUTE_DIRECTORY
			if ((findData.dwFileAttributes & 0x10) > 0)
				listedItem = await Win32StorageEnumerator.GetFolder(findData, Directory.GetParent(fileOrFolderPath).FullName, addFilesCTS.Token);
			else
				listedItem = await Win32StorageEnumerator.GetFile(findData, Directory.GetParent(fileOrFolderPath).FullName, addFilesCTS.Token);

			await AddFileOrFolderAsync(listedItem);

			return listedItem;
		}

		private async Task<(ListedItem Item, CloudDriveSyncStatus? SyncStatus, long? Size, DateTimeOffset Created, DateTimeOffset Modified)?> GetFileOrFolderUpdateInfoAsync(ListedItem item, bool hasSyncStatus)
		{
			IStorageItem? storageItem = null;
			if (item.PrimaryItemAttribute == StorageItemTypes.File)
				storageItem = (await GetFileFromPathAsync(item.ItemPath)).Result;
			else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
				storageItem = (await GetFolderFromPathAsync(item.ItemPath)).Result;

			if (storageItem is not null)
			{
				CloudDriveSyncStatus? syncStatus = hasSyncStatus ? await CheckCloudDriveSyncStatusAsync(storageItem) : null;
				long? size = null;
				DateTimeOffset created = default, modified = default;

				if (storageItem.IsOfType(StorageItemTypes.File))
				{
					var properties = await storageItem.AsBaseStorageFile().GetBasicPropertiesAsync();
					size = (long)properties.Size;
					modified = properties.DateModified;
					created = properties.ItemDate;
				}
				else if (storageItem.IsOfType(StorageItemTypes.Folder))
				{
					var properties = await storageItem.AsBaseStorageFolder().GetBasicPropertiesAsync();
					modified = properties.DateModified;
					created = properties.ItemDate;
				}

				return (item, syncStatus, size, created, modified);
			}

			return null;
		}

		private async Task UpdateFilesOrFoldersAsync(IEnumerable<string> paths, bool hasSyncStatus)
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
				var matchingItems = filesAndFolders.Where(x => paths.Any(p => p.Equals(x.ItemPath, StringComparison.OrdinalIgnoreCase)));
				var results = await Task.WhenAll(matchingItems.Select(x => GetFileOrFolderUpdateInfoAsync(x, hasSyncStatus)));

				await dispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					foreach (var result in results)
					{
						if (result is not null)
						{
							var item = result.Value.Item;
							item.ItemDateModifiedReal = result.Value.Modified;
							item.ItemDateCreatedReal = result.Value.Created;

							if (result.Value.SyncStatus is not null)
								item.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(result.Value.SyncStatus.Value);

							if (result.Value.Size is not null)
							{
								item.FileSizeBytes = result.Value.Size.Value;
								item.FileSize = item.FileSizeBytes.ToSizeString();
							}
						}
					}
				},
				Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
			finally
			{
				enumFolderSemaphore.Release();
			}
		}

		public async Task<ListedItem?> RemoveFileOrFolderAsync(string path)
		{
			try
			{
				await enumFolderSemaphore.WaitAsync(semaphoreCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return null;
			}

			try
			{
				var matchingItem = filesAndFolders.FirstOrDefault(x => x.ItemPath.Equals(path, StringComparison.OrdinalIgnoreCase));

				if (matchingItem is not null)
				{
					filesAndFolders.Remove(matchingItem);

					if (UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
					{
						// Main file is removed, remove connected ADS
						foreach (var adsItem in filesAndFolders.Where(x => x is AlternateStreamItem ads && ads.MainStreamPath == matchingItem.ItemPath).ToList())
							filesAndFolders.Remove(adsItem);
					}

					return matchingItem;
				}
			}
			finally
			{
				enumFolderSemaphore.Release();
			}

			return null;
		}

		public async Task AddSearchResultsToCollection(ObservableCollection<ListedItem> searchItems, string currentSearchPath)
		{
			filesAndFolders.Clear();
			filesAndFolders.AddRange(searchItems);

			await OrderFilesAndFoldersAsync();
			await ApplyFilesAndFoldersChangesAsync();
		}

		public async Task SearchAsync(FolderSearch search)
		{
			ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting });

			CancelSearch();
			searchCTS = new CancellationTokenSource();
			filesAndFolders.Clear();
			IsLoadingItems = true;
			IsSearchResults = true;
			await ApplyFilesAndFoldersChangesAsync();
			EmptyTextType = EmptyTextType.None;

			ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress });

			var results = new List<ListedItem>();
			search.SearchTick += async (s, e) =>
			{
				filesAndFolders = new List<ListedItem>(results);
				await OrderFilesAndFoldersAsync();
				await ApplyFilesAndFoldersChangesAsync();
			};

			await search.SearchAsync(results, searchCTS.Token);

			filesAndFolders = new List<ListedItem>(results);

			await OrderFilesAndFoldersAsync();
			await ApplyFilesAndFoldersChangesAsync();

			ItemLoadStatusChanged?.Invoke(this, new ItemLoadStatusChangedEventArgs() { Status = ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete });
			IsLoadingItems = false;
		}

		public void CancelSearch()
		{
			searchCTS?.Cancel();
		}

		public void Dispose()
		{
			CancelLoadAndClearFiles();
			RecycleBinManager.Default.RecycleBinItemCreated -= RecycleBinItemCreated;
			RecycleBinManager.Default.RecycleBinItemDeleted -= RecycleBinItemDeleted;
			RecycleBinManager.Default.RecycleBinRefreshRequested -= RecycleBinRefreshRequested;
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
			fileTagsSettingsService.OnSettingImportedEvent -= FileTagsSettingsService_OnSettingUpdated;
			fileTagsSettingsService.OnTagsUpdated -= FileTagsSettingsService_OnSettingUpdated;
			folderSizeProvider.SizeChanged -= FolderSizeProvider_SizeChanged;
			DefaultIcons.Clear();
		}
	}

	public class PageTypeUpdatedEventArgs
	{
		public bool IsTypeCloudDrive { get; set; }

		public bool IsTypeRecycleBin { get; set; }
	}

	public class WorkingDirectoryModifiedEventArgs : EventArgs
	{
		public string? Path { get; set; }

		public string? Name { get; set; }

		public bool IsLibrary { get; set; }
	}

	public class ItemLoadStatusChangedEventArgs : EventArgs
	{
		public enum ItemLoadStatus
		{
			Starting,
			InProgress,
			Complete
		}

		public ItemLoadStatus Status { get; set; }

		/// <summary>
		/// This property may not be provided consistently if Status is not Complete
		/// </summary>
		public string? PreviousDirectory { get; set; }

		/// <summary>
		/// This property may not be provided consistently if Status is not Complete
		/// </summary>
		public string? Path { get; set; }
	}
}
