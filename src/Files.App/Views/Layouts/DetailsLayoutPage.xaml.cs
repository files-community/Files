// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.UserControls.Selection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using SortDirection = Files.App.Data.Enums.SortDirection;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the browser page of Details View
	/// </summary>
	public sealed partial class DetailsLayoutPage : BaseGroupableLayoutPage
	{
		// Constants

		private const int TAG_TEXT_BLOCK = 1;

		// Enums
		
		private enum ThumbnailPriority
		{
			Immediate,  // Visible items
			Soon,       // Items about to be visible
			Later       // Items far from viewport
		}

		// Fields

		private ListedItem? _nextItemToSelect;

		/// <summary>
		/// This reference is used to prevent unnecessary icon reloading by only reloading icons when their
		/// size changes, even if the layout size changes (since some layout sizes share the same icon size).
		/// </summary>
		private uint currentIconSize;

		// Thumbnail loading infrastructure
		// Removed global _thumbnailLoadingCts - now using local cancellation tokens for each operation
		private readonly Dictionary<string, WeakReference<BitmapImage>> _thumbnailCache = new();
		private readonly Queue<ListedItem> _thumbnailQueue = new();
		private readonly SemaphoreSlim _thumbnailBatchSemaphore = new(1, 1);
		private readonly SemaphoreSlim _thumbnailLoadSemaphore = new(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2); // Dynamic concurrency
		private readonly ConcurrentDictionary<string, Task<BitmapImage?>> _thumbnailLoadTasks = new(); // Prevent duplicate loads
		private bool _isBatchProcessing;
		private Microsoft.UI.Dispatching.DispatcherQueueTimer? _scrollEndTimer;
		private bool _isScrolling;
		private readonly Timer _cacheCleanupTimer;

		// Helper methods for thumbnail loading
		private ThumbnailPriority GetItemPriority(ListedItem item, Rect viewport)
		{
			if (FileList.ContainerFromItem(item) is not ListViewItem container)
				return ThumbnailPriority.Later;

			var transform = container.TransformToVisual(FileList);
			var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
			var itemBounds = new Rect(position.X, position.Y, container.ActualWidth, container.ActualHeight);

			// Check if item is visible
			if (viewport.X <= itemBounds.X && itemBounds.X <= viewport.X + viewport.Width &&
				viewport.Y <= itemBounds.Y && itemBounds.Y <= viewport.Y + viewport.Height)
				return ThumbnailPriority.Immediate;

			// Check if item is near viewport (within 500 pixels)
			var expandedViewport = new Rect(
				viewport.X - 500,
				viewport.Y - 500,
				viewport.Width + 1000,
				viewport.Height + 1000);

			if (expandedViewport.X <= itemBounds.X && itemBounds.X <= expandedViewport.X + expandedViewport.Width &&
				expandedViewport.Y <= itemBounds.Y && itemBounds.Y <= expandedViewport.Y + expandedViewport.Height)
				return ThumbnailPriority.Soon;

			return ThumbnailPriority.Later;
		}

		private async Task LoadThumbnailWithCacheAsync(ListedItem item, CancellationToken cancellationToken)
		{
			try
			{
				// Check cache first
				if (_thumbnailCache.TryGetValue(item.ItemPath, out var weakRef) && 
					weakRef.TryGetTarget(out var cachedThumbnail))
				{
					item.FileImage = cachedThumbnail;
					return;
				}

				// Prevent duplicate loads for the same item
				if (_thumbnailLoadTasks.TryGetValue(item.ItemPath, out var existingTask))
				{
					var result = await existingTask;
					if (result != null)
					{
						item.FileImage = result;
						_thumbnailCache[item.ItemPath] = new WeakReference<BitmapImage>(result);
					}
					return;
				}

				// Create new load task
				var loadTask = Task.Run(async () =>
				{
					await _thumbnailLoadSemaphore.WaitAsync(cancellationToken);
					try
					{
						// Load thumbnail
						await ParentShellPageInstance.ShellViewModel.LoadExtendedItemPropertiesAsync(item);

						// Cache the thumbnail if loaded
						if (item.FileImage is BitmapImage bitmapImage)
						{
							_thumbnailCache[item.ItemPath] = new WeakReference<BitmapImage>(bitmapImage);
							return bitmapImage;
						}
						return null;
					}
					finally
					{
						_thumbnailLoadSemaphore.Release();
					}
				}, cancellationToken);

				// Store the task to prevent duplicates
				_thumbnailLoadTasks[item.ItemPath] = loadTask;

				var loadResult = await loadTask;
				if (loadResult != null)
				{
					item.FileImage = loadResult;
				}

				// Clean up the task reference
				_thumbnailLoadTasks.TryRemove(item.ItemPath, out _);
			}
			catch (OperationCanceledException)
			{
				// Cancelled, do nothing
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Failed to load thumbnail for {item.ItemPath}: {ex.Message}");
			}
		}

		private void InitializeScrollThrottling()
		{
			_scrollEndTimer = DispatcherQueue.CreateTimer();
			_scrollEndTimer.Interval = TimeSpan.FromMilliseconds(150);
			_scrollEndTimer.Tick += async (s, e) =>
			{
				_scrollEndTimer.Stop();
				_isScrolling = false;
				await ProcessThumbnailQueueAsync();
			};
		}

		private async Task ProcessThumbnailQueueAsync()
		{
			if (_isBatchProcessing || _thumbnailQueue.Count == 0)
				return;

			await _thumbnailBatchSemaphore.WaitAsync();
			try
			{
				_isBatchProcessing = true;

				// Create a new cancellation token source for this operation
				using var operationCts = new CancellationTokenSource();
				var cancellationToken = operationCts.Token;

				// Use Parallel.ForEach for better performance
				var itemsToProcess = new List<ListedItem>();
				while (_thumbnailQueue.Count > 0 && itemsToProcess.Count < 50)
				{
					itemsToProcess.Add(_thumbnailQueue.Dequeue());
				}

				await Parallel.ForEachAsync(
					itemsToProcess,
					new ParallelOptions 
					{ 
						MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
						CancellationToken = cancellationToken 
					},
					async (item, token) =>
					{
						try
						{
							await LoadThumbnailWithCacheAsync(item, token);
						}
						catch (OperationCanceledException)
						{
							// Gracefully handle cancellation
							System.Diagnostics.Debug.WriteLine("Thumbnail loading was canceled");
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine($"Error loading thumbnail: {ex.Message}");
						}
					});
			}
			catch (OperationCanceledException)
			{
				// Gracefully handle cancellation
				System.Diagnostics.Debug.WriteLine("ProcessThumbnailQueueAsync was canceled");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error in ProcessThumbnailQueueAsync: {ex.Message}");
			}
			finally
			{
				_isBatchProcessing = false;
				_thumbnailBatchSemaphore.Release();
			}
		}

		// Updated thumbnail loading method with priority-based loading
		private async Task ShowIconsAndThumbnailsAsync()
		{
			if (ContentScroller is null || ParentShellPageInstance is null)
				return;

			try
			{
				// Create a new cancellation token source for this operation
				using var operationCts = new CancellationTokenSource();
				var cancellationToken = operationCts.Token;

			// Clear the queue
			_thumbnailQueue.Clear();

			// Get viewport info
			var scrollViewer = ContentScroller;
			var viewport = new Rect(0, scrollViewer.VerticalOffset, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);

			// Categorize items by priority
			var itemsByPriority = new Dictionary<ThumbnailPriority, List<ListedItem>>
			{
				[ThumbnailPriority.Immediate] = new(),
				[ThumbnailPriority.Soon] = new(),
				[ThumbnailPriority.Later] = new()
			};

			foreach (var item in ParentShellPageInstance.ShellViewModel.FilesAndFolders)
			{
				if (item.FileImage != null)
					continue;

				var priority = GetItemPriority(item, viewport);
				itemsByPriority[priority].Add(item);
			}

			// Load immediate priority items with high concurrency
			var immediateTasks = itemsByPriority[ThumbnailPriority.Immediate]
				.Take(Environment.ProcessorCount * 2) // Dynamic limit based on CPU cores
				.Select(item => LoadThumbnailWithCacheAsync(item, cancellationToken))
				.ToList();

			// Queue soon and later priority items
			foreach (var item in itemsByPriority[ThumbnailPriority.Soon])
				_thumbnailQueue.Enqueue(item);
			foreach (var item in itemsByPriority[ThumbnailPriority.Later])
				_thumbnailQueue.Enqueue(item);

			// Start loading immediate items
			if (immediateTasks.Any())
			{
				try
				{
					await Task.WhenAll(immediateTasks);
				}
				catch (OperationCanceledException)
				{
					// Cancelled, expected
				}
			}

			// Process the queue if not scrolling
			if (!_isScrolling)
				await ProcessThumbnailQueueAsync();
		}
		catch (OperationCanceledException)
		{
			// Gracefully handle cancellation
			System.Diagnostics.Debug.WriteLine("ShowIconsAndThumbnailsAsync was canceled");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error in ShowIconsAndThumbnailsAsync: {ex.Message}");
		}
		}

		// Optimized ForceLoadMissingThumbnailsAsync
		private async Task ForceLoadMissingThumbnailsAsync()
		{
			if (ParentShellPageInstance is null)
				return;

			try
			{
				// Create a new cancellation token source for this operation
				using var operationCts = new CancellationTokenSource();
				var cancellationToken = operationCts.Token;

				// Get all items without thumbnails
				var itemsWithoutThumbnails = ParentShellPageInstance.ShellViewModel.FilesAndFolders
					.Where(item => item.FileImage == null)
					.ToList();

				if (itemsWithoutThumbnails.Count == 0)
					return;

				System.Diagnostics.Debug.WriteLine($"Force loading thumbnails for {itemsWithoutThumbnails.Count} items");

				// Process in batches with enhanced concurrency
				const int batchSize = 20;
				const int maxConcurrency = 8;

				await Parallel.ForEachAsync(
					itemsWithoutThumbnails.Chunk(batchSize),
					new ParallelOptions 
					{ 
						MaxDegreeOfParallelism = maxConcurrency,
						CancellationToken = cancellationToken 
					},
					async (batch, token) =>
					{
						try
						{
							var tasks = batch.Select(item => LoadThumbnailWithCacheAsync(item, token));
							await Task.WhenAll(tasks);
							await Task.Delay(50, token); // Small delay between batches
						}
						catch (OperationCanceledException)
						{
							// Gracefully handle cancellation
							System.Diagnostics.Debug.WriteLine("Batch processing was canceled");
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine($"Error in batch processing: {ex.Message}");
						}
					});
			}
			catch (OperationCanceledException)
			{
				// Gracefully handle cancellation
				System.Diagnostics.Debug.WriteLine("ForceLoadMissingThumbnailsAsync was canceled");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error in ForceLoadMissingThumbnailsAsync: {ex.Message}");
			}
		}

		// Debug helper methods
		private void LogThumbnailStatus()
		{
			if (ParentShellPageInstance is null)
				return;

			var items = ParentShellPageInstance.ShellViewModel.FilesAndFolders;
			var withThumbnails = items.Count(i => i.FileImage != null);
			var total = items.Count();

			// Debug output
			System.Diagnostics.Debug.WriteLine($"Thumbnail Status: {withThumbnails}/{total} loaded, {_thumbnailQueue.Count} queued");
		}

		private string GetThumbnailCacheStats()
		{
			var activeCount = 0;
			var deadCount = 0;

			foreach (var kvp in _thumbnailCache)
			{
				if (kvp.Value.TryGetTarget(out _))
					activeCount++;
				else
					deadCount++;
			}

			return $"Cache: {activeCount} active, {deadCount} dead references";
		}

		private void CleanupCache(object? state)
		{
			var deadReferences = _thumbnailCache
				.Where(kvp => !kvp.Value.TryGetTarget(out _))
				.Select(kvp => kvp.Key)
				.ToList();
			
			foreach (var key in deadReferences)
			{
				_thumbnailCache.Remove(key);
			}

			System.Diagnostics.Debug.WriteLine($"Cache cleanup: removed {deadReferences.Count} dead references");
		}

		/// <summary>
		/// Priority-based thread pool for thumbnail loading
		/// </summary>
		private class PriorityThreadPool
		{
			private readonly SemaphoreSlim _highPrioritySemaphore = new(Environment.ProcessorCount, Environment.ProcessorCount);
			private readonly SemaphoreSlim _lowPrioritySemaphore = new(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);
			
			public async Task<T> ExecuteAsync<T>(Func<Task<T>> work, ThumbnailPriority priority)
			{
				var semaphore = priority == ThumbnailPriority.Immediate ? _highPrioritySemaphore : _lowPrioritySemaphore;
				
				await semaphore.WaitAsync();
				try
				{
					return await work();
				}
				finally
				{
					semaphore.Release();
				}
			}

			public void Dispose()
			{
				_highPrioritySemaphore?.Dispose();
				_lowPrioritySemaphore?.Dispose();
			}
		}

		/// <summary>
		/// Process items in batches with parallel execution for better performance
		/// </summary>
		private async Task ProcessItemsInBatchesAsync<T>(
			IEnumerable<T> items, 
			Func<T, Task> processor, 
			int batchSize = 100,
			int maxConcurrency = 8) // Use constant instead of Environment.ProcessorCount
		{
			var batches = items.Chunk(batchSize);
			
			await Parallel.ForEachAsync(
				batches,
				new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
				async (batch, token) =>
				{
					var tasks = batch.Select(processor);
					await Task.WhenAll(tasks);
				});
		}

		/// <summary>
		/// Get file information in parallel for better performance
		/// </summary>
		private static async Task<FileInfo[]> GetFilesParallelAsync(string path, string searchPattern = "*")
		{
			return await Task.Run(() =>
			{
				var directory = new DirectoryInfo(path);
				return directory.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
			});
		}

		/// <summary>
		/// Memory-efficient batch processing with proper resource management
		/// </summary>
		private async Task ProcessBatchWithMemoryManagement<T>(
			IEnumerable<T> items,
			Func<T, Task> processor,
			int batchSize = 50,
			CancellationToken cancellationToken = default)
		{
			using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
			
			await Parallel.ForEachAsync(
				items.Chunk(batchSize),
				new ParallelOptions 
				{ 
					MaxDegreeOfParallelism = Environment.ProcessorCount,
					CancellationToken = cancellationToken 
				},
				async (batch, token) =>
				{
					await semaphore.WaitAsync(token);
					try
					{
						var tasks = batch.Select(processor);
						await Task.WhenAll(tasks);
					}
					finally
					{
						semaphore.Release();
					}
				});
		}

		/// <summary>
		/// Execute async operation with timeout for better reliability
		/// </summary>
		private async Task<T?> ExecuteWithTimeoutAsync<T>(
			Func<Task<T>> operation,
			TimeSpan timeout,
			T? defaultValue = default)
		{
			try
			{
				using var cts = new CancellationTokenSource(timeout);
				var operationTask = operation();
				var timeoutTask = Task.Delay(timeout, cts.Token);
				
				var completedTask = await Task.WhenAny(operationTask, timeoutTask);
				
				if (completedTask == operationTask)
				{
					return await operationTask;
				}
				
				return defaultValue;
			}
			catch (OperationCanceledException)
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Load file properties in parallel for better performance
		/// </summary>
		private async Task LoadFilePropertiesParallelAsync(ListedItem item)
		{
			await Task.Run(async () =>
			{
				var tasks = new List<Task>();
				
				// Load thumbnail in parallel with other properties
				if (UserSettingsService.FoldersSettingsService.ShowThumbnails)
				{
					tasks.Add(LoadThumbnailWithCacheAsync(item, CancellationToken.None));
				}
				
				// Load other properties in parallel
				tasks.Add(LoadFilePropertiesAsync(item));
				tasks.Add(LoadGitPropertiesAsync(item));
				
				await Task.WhenAll(tasks);
			}, CancellationToken.None);
		}

		private async Task LoadFilePropertiesAsync(ListedItem item)
		{
			// Placeholder for file properties loading
			await Task.CompletedTask;
		}

		private async Task LoadGitPropertiesAsync(ListedItem item)
		{
			// Placeholder for git properties loading
			await Task.CompletedTask;
		}

		/// <summary>
		/// Enumerate files concurrently for better performance
		/// </summary>
		private async Task<List<ListedItem>> EnumerateFilesParallelAsync(string path, CancellationToken cancellationToken)
		{
			var results = new ConcurrentBag<ListedItem>();
			
			await Parallel.ForEachAsync(
				GetFilePaths(path),
				new ParallelOptions 
				{ 
					MaxDegreeOfParallelism = Environment.ProcessorCount,
					CancellationToken = cancellationToken 
				},
				async (filePath, token) =>
				{
					var item = await CreateListedItemAsync(filePath, token);
					if (item != null)
					{
						results.Add(item);
					}
				});
			
			return results.ToList();
		}

		private IEnumerable<string> GetFilePaths(string path)
		{
			// Placeholder for getting file paths
			return Directory.GetFiles(path);
		}

		private async Task<ListedItem?> CreateListedItemAsync(string filePath, CancellationToken cancellationToken)
		{
			// Placeholder for creating ListedItem
			await Task.CompletedTask;
			return null;
		}

		// Properties

		protected override ListViewBase ListViewBase => FileList;
		protected override SemanticZoom RootZoom => RootGridZoom;

		public ColumnsViewModel ColumnsViewModel { get; } = new();

		private RelayCommand<string>? UpdateSortOptionsCommand { get; set; }

		public ScrollViewer? ContentScroller { get; private set; }

		private double maxWidthForRenameTextbox;
		public double MaxWidthForRenameTextbox
		{
			get => maxWidthForRenameTextbox;
			set
			{
				if (value != maxWidthForRenameTextbox)
				{
					maxWidthForRenameTextbox = value;
					NotifyPropertyChanged(nameof(MaxWidthForRenameTextbox));
				}
			}
		}

		/// <summary>
		/// Row height for items in the Details View
		/// </summary>
		public int RowHeight
		{
			get => LayoutSizeKindHelper.GetDetailsViewRowHeight((DetailsViewSizeKind)UserSettingsService.LayoutSettingsService.DetailsViewSize);
		}


		// Constructor

		public DetailsLayoutPage() : base()
		{
			InitializeComponent();
			DataContext = this;
			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;

			UpdateSortOptionsCommand = new RelayCommand<string>(x =>
			{
				if (!Enum.TryParse<SortOption>(x, out var val))
					return;
				if (FolderSettings.DirectorySortOption == val)
				{
					FolderSettings.DirectorySortDirection = (SortDirection)(((int)FolderSettings.DirectorySortDirection + 1) % 2);
				}
				else
				{
					FolderSettings.DirectorySortOption = val;
					FolderSettings.DirectorySortDirection = SortDirection.Ascending;
				}
			});

			// Initialize scroll throttling
			InitializeScrollThrottling();
		
			// Hook up scroll events
			FileList.Loaded += FileList_Loaded;
			FileList.Unloaded += FileList_Unloaded;

			// Initialize cache cleanup timer
			_cacheCleanupTimer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
		}

		// Event handlers for scroll events
		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			var scrollViewer = FileList.FindDescendant<ScrollViewer>();
			if (scrollViewer != null)
			{
				ContentScroller = scrollViewer;
				scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
				scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
				
				// Initial thumbnail load
				_ = ShowIconsAndThumbnailsAsync();
			}
		}

		private void FileList_Unloaded(object sender, RoutedEventArgs e)
		{
			var scrollViewer = FileList.FindDescendant<ScrollViewer>();
			if (scrollViewer != null)
			{
				scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
				scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
			}
			
			// Stop scroll timer
			_scrollEndTimer?.Stop();
		}

		private void ScrollViewer_ViewChanging(object? sender, ScrollViewerViewChangingEventArgs e)
		{
			_isScrolling = true;
			_scrollEndTimer?.Stop();
		}

		private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
		{
			if (!e.IsIntermediate)
			{
				// Final view change
				_scrollEndTimer?.Stop();
				_scrollEndTimer?.Start();
			}
			else
			{
				// Still scrolling
				_scrollEndTimer?.Stop();
				_scrollEndTimer?.Start();
			}
		}

		// Methods

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
			ContentScroller?.ChangeView(null, FileList.Items.IndexOf(e) * RowHeight, null, true); // Scroll to index * item height
		}

		protected override void ItemManipulationModel_ScrollToTopInvoked(object? sender, EventArgs e)
		{
			ContentScroller?.ChangeView(null, 0, null, true);
		}

		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems?.Any() ?? false)
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				ContentScroller?.ChangeView(null, FileList.Items.IndexOf(SelectedItems.Last()) * RowHeight, null, false);
				(FileList.ContainerFromItem(SelectedItems.Last()) as ListViewItem)?.Focus(FocusState.Keyboard);
			}
		}

		protected override void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (NextRenameIndex != 0)
			{
				_nextItemToSelect = e;
				FileList.LayoutUpdated += FileList_LayoutUpdated;
			}
			else if (FileList?.Items.Contains(e) ?? false)
				FileList!.SelectedItems.Add(e);
		}

		protected override void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (FileList?.Items.Contains(e) ?? false)
				FileList.SelectedItems.Remove(e);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
				navArgs.FocusOnNavigation = true;

			base.OnNavigatedTo(eventArgs);

			currentIconSize = LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.DetailsView);

			if (FolderSettings?.ColumnsViewModel is not null)
			{
				// Don't assign the columns view model directly, instead update each property individually using the Update method.
				// This is done to workaround a bug where CsWinRT doesn't properly track the memory of the object so that
				// an invalid memory access can occur when the object is moved.
				// See https://github.com/microsoft/CsWinRT/issues/1834.
				ColumnsViewModel.DateCreatedColumn.Update(FolderSettings.ColumnsViewModel.DateCreatedColumn);
				ColumnsViewModel.DateDeletedColumn.Update(FolderSettings.ColumnsViewModel.DateDeletedColumn);
				ColumnsViewModel.DateModifiedColumn.Update(FolderSettings.ColumnsViewModel.DateModifiedColumn);
				ColumnsViewModel.IconColumn.Update(FolderSettings.ColumnsViewModel.IconColumn);
				ColumnsViewModel.ItemTypeColumn.Update(FolderSettings.ColumnsViewModel.ItemTypeColumn);
				ColumnsViewModel.NameColumn.Update(FolderSettings.ColumnsViewModel.NameColumn);
				ColumnsViewModel.PathColumn.Update(FolderSettings.ColumnsViewModel.PathColumn);
				ColumnsViewModel.OriginalPathColumn.Update(FolderSettings.ColumnsViewModel.OriginalPathColumn);
				ColumnsViewModel.SizeColumn.Update(FolderSettings.ColumnsViewModel.SizeColumn);
				ColumnsViewModel.StatusColumn.Update(FolderSettings.ColumnsViewModel.StatusColumn);
				ColumnsViewModel.TagColumn.Update(FolderSettings.ColumnsViewModel.TagColumn);
				ColumnsViewModel.GitStatusColumn.Update(FolderSettings.ColumnsViewModel.GitStatusColumn);
				ColumnsViewModel.GitLastCommitDateColumn.Update(FolderSettings.ColumnsViewModel.GitLastCommitDateColumn);
				ColumnsViewModel.GitLastCommitMessageColumn.Update(FolderSettings.ColumnsViewModel.GitLastCommitMessageColumn);
				ColumnsViewModel.GitCommitAuthorColumn.Update(FolderSettings.ColumnsViewModel.GitCommitAuthorColumn);
				ColumnsViewModel.GitLastCommitShaColumn.Update(FolderSettings.ColumnsViewModel.GitLastCommitShaColumn);
			}

			ParentShellPageInstance.ShellViewModel.EnabledGitProperties = GetEnabledGitProperties(ColumnsViewModel);

			FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			FolderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
			FolderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
			ParentShellPageInstance.ShellViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			UserSettingsService.LayoutSettingsService.PropertyChanged += LayoutSettingsService_PropertyChanged;

			var parameters = (NavigationArguments)eventArgs.Parameter;
			if (parameters.IsLayoutSwitch)
				_ = ReloadItemIconsAsync();

			FilesystemViewModel_PageTypeUpdated(null, new PageTypeUpdatedEventArgs()
			{
				IsTypeCloudDrive = InstanceViewModel?.IsPageTypeCloudDrive ?? false,
				IsTypeRecycleBin = InstanceViewModel?.IsPageTypeRecycleBin ?? false,
				IsTypeGitRepository = InstanceViewModel?.IsGitRepository ?? false,
				IsTypeSearchResults = InstanceViewModel?.IsPageTypeSearchResults ?? false
			});

			RootGrid_SizeChanged(null, null);

			SetItemContainerStyle();

			// Schedule a fallback thumbnail load after a shorter delay to catch any items that didn't load through progressive loading
			_ = Task.Run(async () =>
			{
				await Task.Delay(1000); // Wait 1 second for progressive loading to complete
				await ForceLoadMissingThumbnailsAsync();
			});
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
			FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
			ParentShellPageInstance.ShellViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
			UserSettingsService.LayoutSettingsService.PropertyChanged -= LayoutSettingsService_PropertyChanged;
			
			if (FileList != null)
				FileList.ContainerContentChanging -= OnFileListContainerContentChanging;
		}

		private void LayoutSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ILayoutSettingsService.DetailsViewSize))
			{
				// Get current scroll position
				var previousOffset = ContentScroller?.VerticalOffset;

				NotifyPropertyChanged(nameof(RowHeight));

				// Update the container style to match the item size
				SetItemContainerStyle();

				// Restore correct scroll position
				ContentScroller?.ChangeView(null, previousOffset, null);

				// Check if icons need to be reloaded
				var newIconSize = LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.DetailsView);
				if (newIconSize != currentIconSize)
				{
					currentIconSize = newIconSize;
					_ = ReloadItemIconsAsync();
				}
			}
			else
			{
				var settings = sender as ILayoutSettingsService;
				var isDefaultPath = FolderSettings?.IsPathUsingDefaultLayout(ParentShellPageInstance?.ShellViewModel.CurrentFolder?.ItemPath);
				if (settings is not null && (isDefaultPath ?? true))
				{
					switch (e.PropertyName)
					{
						case nameof(ILayoutSettingsService.ShowFileTagColumn):
							ColumnsViewModel.TagColumn.UserCollapsed = !settings.ShowFileTagColumn;
							break;
						case nameof(ILayoutSettingsService.ShowSizeColumn):
							ColumnsViewModel.SizeColumn.UserCollapsed = !settings.ShowSizeColumn;
							break;
						case nameof(ILayoutSettingsService.ShowTypeColumn):
							ColumnsViewModel.ItemTypeColumn.UserCollapsed = !settings.ShowTypeColumn;
							break;
						case nameof(ILayoutSettingsService.ShowDateCreatedColumn):
							ColumnsViewModel.DateCreatedColumn.UserCollapsed = !settings.ShowDateCreatedColumn;
							break;
						case nameof(ILayoutSettingsService.ShowDateColumn):
							ColumnsViewModel.DateModifiedColumn.UserCollapsed = !settings.ShowDateColumn;
							break;
					}
				}
			}
		}

		/// <summary>
		/// Sets the item size and spacing
		/// </summary>
		private void SetItemContainerStyle()
		{
			// Directly set the appropriate style without toggling
			FileList.ItemContainerStyle = UserSettingsService.LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Compact
				? CompactItemContainerStyle
				: RegularItemContainerStyle;

			// Force layout update without full re-render
			FileList.UpdateLayout();

			// Set the width of the icon column. The value is increased by 4px to account for icon overlays.
			ColumnsViewModel.IconColumn.UserLength = new GridLength(LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.DetailsView) + 4);
		}

		private void FileList_LayoutUpdated(object? sender, object e)
		{
			FileList.LayoutUpdated -= FileList_LayoutUpdated;
			TryStartRenameNextItem(_nextItemToSelect!);
			_nextItemToSelect = null;
		}

		private void FolderSettings_SortOptionPreferenceUpdated(object? sender, SortOption e)
		{
			UpdateSortIndicator();
		}

		private void FolderSettings_SortDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			UpdateSortIndicator();
		}

		private void UpdateSortIndicator()
		{
			NameHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Name ? FolderSettings.DirectorySortDirection : null;
			TagHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileTag ? FolderSettings.DirectorySortDirection : null;
			PathHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Path ? FolderSettings.DirectorySortDirection : null;
			OriginalPathHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.OriginalFolder ? FolderSettings.DirectorySortDirection : null;
			DateDeletedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateDeleted ? FolderSettings.DirectorySortDirection : null;
			DateModifiedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateModified ? FolderSettings.DirectorySortDirection : null;
			DateCreatedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateCreated ? FolderSettings.DirectorySortDirection : null;
			FileTypeHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileType ? FolderSettings.DirectorySortDirection : null;
			ItemSizeHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Size ? FolderSettings.DirectorySortDirection : null;
			SyncStatusHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.SyncStatus ? FolderSettings.DirectorySortDirection : null;
		}

		private void FilesystemViewModel_PageTypeUpdated(object? sender, PageTypeUpdatedEventArgs e)
		{
			if (e.IsTypeRecycleBin)
			{
				ColumnsViewModel.OriginalPathColumn.Show();
				ColumnsViewModel.DateDeletedColumn.Show();
			}
			else
			{
				ColumnsViewModel.OriginalPathColumn.Hide();
				ColumnsViewModel.DateDeletedColumn.Hide();
			}

			if (e.IsTypeCloudDrive)
				ColumnsViewModel.StatusColumn.Show();
			else
				ColumnsViewModel.StatusColumn.Hide();

			if (e.IsTypeGitRepository && !e.IsTypeSearchResults)
			{
				ColumnsViewModel.GitCommitAuthorColumn.Show();
				ColumnsViewModel.GitLastCommitDateColumn.Show();
				ColumnsViewModel.GitLastCommitMessageColumn.Show();
				ColumnsViewModel.GitLastCommitShaColumn.Show();
				ColumnsViewModel.GitStatusColumn.Show();
			}
			else
			{
				ColumnsViewModel.GitCommitAuthorColumn.Hide();
				ColumnsViewModel.GitLastCommitDateColumn.Hide();
				ColumnsViewModel.GitLastCommitMessageColumn.Hide();
				ColumnsViewModel.GitLastCommitShaColumn.Hide();
				ColumnsViewModel.GitStatusColumn.Hide();
			}

			if (e.IsTypeSearchResults)
				ColumnsViewModel.PathColumn.Show();
			else
				ColumnsViewModel.PathColumn.Hide();

			UpdateSortIndicator();
		}

		private void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{

		}

		private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItems = FileList.SelectedItems.Cast<ListedItem>().Where(x => x is not null).ToList();

			if (e != null)
			{
				foreach (var item in e.AddedItems)
					SetCheckboxSelectionState(item);

				foreach (var item in e.RemovedItems)
					SetCheckboxSelectionState(item);
			}
		}

		override public void StartRenameItem()
		{
			StartRenameItem("ItemNameTextBox");

			if (FileList.ContainerFromItem(RenamingItem) is not ListViewItem listViewItem)
				return;

			var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
			if (textBox is null || textBox.FindParent<Grid>() is null)
				return;

			Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);
		}

		private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (IsRenamingItem)
			{
				ValidateItemNameInputTextAsync(textBox, args, (showError) =>
				{
					FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
					FileNameTeachingTip.IsOpen = showError;
				});
			}
		}

		protected override void EndRename(TextBox textBox)
		{
			if (textBox is not null && textBox.FindParent<Grid>() is FrameworkElement parent)
				Grid.SetColumnSpan(parent, 1);

			ListViewItem? listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;

			if (textBox is null || listViewItem is null)
			{
				// Navigating away, do nothing
			}
			else
			{
				TextBlock? textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
				textBox.Visibility = Visibility.Collapsed;
				textBlock!.Visibility = Visibility.Visible;
			}

			// Unsubscribe from events
			if (textBox is not null)
			{
				textBox!.LostFocus -= RenameTextBox_LostFocus;
				textBox.KeyDown -= RenameTextBox_KeyDown;
			}

			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;

			// Re-focus selected list item
			listViewItem?.Focus(FocusState.Programmatic);
		}

		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ParentShellPageInstance is null || IsRenamingItem)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot);
			var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) is not null;
			var isFooterFocused = focusedElement is HyperlinkButton;

			// Debug: Force load thumbnails with Ctrl+Shift+T
			if (e.Key == VirtualKey.T && ctrlPressed && shiftPressed)
			{
				e.Handled = true;
				_ = ForceLoadMissingThumbnailsAsync();
				return;
			}

			if (ctrlPressed && e.Key is VirtualKey.A)
			{
				e.Handled = true;

				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				var hotKey = new HotKey(Keys.A, KeyModifiers.Ctrl);

				await commands[hotKey].ExecuteAsync();
			}
			else if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
			{
				e.Handled = true;

				if (ctrlPressed && !shiftPressed)
				{
					var folders = ParentShellPageInstance?.SlimContentPage.SelectedItems?.Where(file => file.PrimaryItemAttribute == StorageItemTypes.Folder);
					if (folders is not null)
					{
						foreach (ListedItem folder in folders)
							await NavigationHelpers.OpenPathInNewTab(folder.ItemPath);
					}
				}
				else if (ctrlPressed && shiftPressed)
				{
					var selectedFolder = SelectedItems?.FirstOrDefault(item => item.PrimaryItemAttribute == StorageItemTypes.Folder);
					if (selectedFolder is not null)
						NavigationHelpers.OpenInSecondaryPane(ParentShellPageInstance, selectedFolder);
				}
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.OpenPropertiesWindow(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				e.Handled = true;
			}
			else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
			{
				// Unfocus the GridView so keyboard shortcut can be handled
				Focus(FocusState.Pointer);
			}
			else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
			{
				// Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
				Focus(FocusState.Pointer);
			}
			else if (e.Key == VirtualKey.Down)
			{
				// Focus the first item in the file list if Header header has focus,
				// or if there is only one item in the file list (#13774)
				if (isHeaderFocused || FileList.Items.Count == 1)
				{
					var selectIndex = FileList.SelectedIndex < 0 ? 0 : FileList.SelectedIndex;
					if (FileList.ContainerFromIndex(selectIndex) is ListViewItem item)
					{
						// Focus selected list item or first item
						item.Focus(FocusState.Programmatic);
						if (!IsItemSelected)
							FileList.SelectedIndex = 0;
						e.Handled = true;
					}
				}
			}
		}

		protected override bool CanGetItemFromElement(object element)
			=> element is ListViewItem;

		private async Task ReloadItemIconsAsync()
		{
			if (ParentShellPageInstance is null)
				return;

			ParentShellPageInstance.ShellViewModel.CancelExtendedPropertiesLoading();
			var filesAndFolders = ParentShellPageInstance.ShellViewModel.FilesAndFolders.ToList();

			await Task.WhenAll(filesAndFolders.Select(listedItem =>
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					return ParentShellPageInstance.ShellViewModel.LoadExtendedItemPropertiesAsync(listedItem);
				else
					return Task.CompletedTask;
			}));

			if (ParentShellPageInstance.ShellViewModel.EnabledGitProperties is not GitProperties.None)
			{
				await Task.WhenAll(filesAndFolders.Select(item =>
				{
					if (item is IGitItem gitItem)
						return ParentShellPageInstance.ShellViewModel.LoadGitPropertiesAsync(gitItem);

					return Task.CompletedTask;
				}));
			}
		}

		private async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var clickedItem = e.OriginalSource as FrameworkElement;
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var item = clickedItem?.DataContext as ListedItem;
			if (item is null)
			{
				if (IsRenamingItem && RenamingItem is not null)
				{
					ListViewItem? listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
					if (listViewItem is not null)
					{
						var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
						if (textBox is not null)
							await CommitRenameAsync(textBox);
					}
				}
				return;
			}

			// Skip code if the control or shift key is pressed or if the user is using multiselect
			if
			(
				ctrlPressed ||
				shiftPressed ||
				clickedItem is Microsoft.UI.Xaml.Shapes.Rectangle
			)
			{
				e.Handled = true;
				return;
			}

			// Check if the setting to open items with a single click is turned on
			if (UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				ResetRenameDoubleClick();
				await Commands.OpenItem.ExecuteAsync();
			}
			else
			{
				if (clickedItem is TextBlock && ((TextBlock)clickedItem).Name == "ItemName")
				{
					CheckRenameDoubleClick(clickedItem.DataContext);
				}
				else if (IsRenamingItem && RenamingItem is not null)
				{
					ListViewItem? listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
					if (listViewItem is not null)
					{
						var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
						if (textBox is not null)
							await CommitRenameAsync(textBox);
					}
				}
			}
		}

		private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item && !UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
				await Commands.OpenItem.ExecuteAsync();
			else if ((e.OriginalSource as FrameworkElement)?.DataContext is not ListedItem && UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
				await Commands.NavigateUp.ExecuteAsync();

			ResetRenameDoubleClick();
		}

		private void StackPanel_Loaded(object sender, RoutedEventArgs e)
		{
			// This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
			// because you can't use bindings in the setters
			DependencyObject item = VisualTreeHelper.GetParent(sender as StackPanel);
			while (item is not ListViewItem)
				item = VisualTreeHelper.GetParent(item);
			if (item is ListViewItem itemContainer)
				itemContainer.ContextFlyout = ItemContextMenuFlyout;
		}

		private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// This prevents the drag selection rectangle from appearing when resizing the columns
			e.Handled = true;
		}

		private void GridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			UpdateColumnLayout();
		}

		private void GridSplitter_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
			{
				UpdateColumnLayout();
				FolderSettings.ColumnsViewModel = ColumnsViewModel;
			}
		}

		private void UpdateColumnLayout()
		{
			ColumnsViewModel.IconColumn.UserLength = Column2.Width;
			ColumnsViewModel.NameColumn.UserLength = Column3.Width;

			// Git
			ColumnsViewModel.GitStatusColumn.UserLength = GitStatusColumnDefinition.Width;
			ColumnsViewModel.GitLastCommitDateColumn.UserLength = GitLastCommitDateColumnDefinition.Width;
			ColumnsViewModel.GitLastCommitMessageColumn.UserLength = GitLastCommitMessageColumnDefinition.Width;
			ColumnsViewModel.GitCommitAuthorColumn.UserLength = GitCommitAuthorColumnDefinition.Width;
			ColumnsViewModel.GitLastCommitShaColumn.UserLength = GitLastCommitShaColumnDefinition.Width;

			ColumnsViewModel.TagColumn.UserLength = Column4.Width;
			ColumnsViewModel.PathColumn.UserLength = Column5.Width;
			ColumnsViewModel.OriginalPathColumn.UserLength = Column6.Width;
			ColumnsViewModel.DateDeletedColumn.UserLength = Column7.Width;
			ColumnsViewModel.DateModifiedColumn.UserLength = Column8.Width;
			ColumnsViewModel.DateCreatedColumn.UserLength = Column9.Width;
			ColumnsViewModel.ItemTypeColumn.UserLength = Column10.Width;
			ColumnsViewModel.SizeColumn.UserLength = Column11.Width;
			ColumnsViewModel.StatusColumn.UserLength = Column12.Width;
		}

		private void RootGrid_SizeChanged(object? sender, SizeChangedEventArgs? e)
		{
			MaxWidthForRenameTextbox = Math.Max(0, RootGrid.ActualWidth - 80);
		}

		private void GridSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void GridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			FolderSettings.ColumnsViewModel = ColumnsViewModel;
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void GridSplitter_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as UIElement)?.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			FolderSettings.ColumnsViewModel = ColumnsViewModel;
			ParentShellPageInstance.ShellViewModel.EnabledGitProperties = GetEnabledGitProperties(ColumnsViewModel);
		}

		private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var columnToResize = Grid.GetColumn(sender as Files.App.Controls.GridSplitter) / 2 + 1;
			ResizeColumnToFit(columnToResize);

			e.Handled = true;
		}

		private void SizeAllColumnsToFit_Click(object sender, RoutedEventArgs e)
		{
			// If there aren't items, do not make columns fit
			if (!FileList.Items.Any())
				return;

			// For scalability, just count the # of public `ColumnViewModel` properties in ColumnsViewModel
			int totalColumnCount = ColumnsViewModel.GetType().GetProperties().Count(prop => prop.PropertyType == typeof(DetailsLayoutColumnItem));
			for (int columnIndex = 1; columnIndex <= totalColumnCount; columnIndex++)
				ResizeColumnToFit(columnIndex);
		}

		private void ResizeColumnToFit(int columnToResize)
		{
			if (!FileList.Items.Any())
				return;

			var maxItemLength = columnToResize switch
			{
				1 => 40, // Check all items columns
				2 => FileList.Items.Cast<ListedItem>().Select(x => x.Name?.Length ?? 0).Max(), // file name column
				4 => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitDateHumanized?.Length ?? 0).Max(), // git
				5 => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitMessage?.Length ?? 0).Max(), // git
				6 => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitAuthor?.Length ?? 0).Max(), // git
				7 => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitSha?.Length ?? 0).Max(), // git
				8 => FileList.Items.Cast<ListedItem>().Select(x => x.FileTagsUI?.Sum(x => x?.Name?.Length ?? 0) ?? 0).Max(), // file tag column
				9 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemPath?.Length ?? 0).Max(), // path column
				10 => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemOriginalPath?.Length ?? 0).Max(), // original path column
				11 => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemDateDeleted?.Length ?? 0).Max(), // date deleted column
				12 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateModified?.Length ?? 0).Max(), // date modified column
				13 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateCreated?.Length ?? 0).Max(), // date created column
				14 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemType?.Length ?? 0).Max(), // item type column
				15 => FileList.Items.Cast<ListedItem>().Select(x => x.FileSize?.Length ?? 0).Max(), // item size column
				_ => 20 // cloud status column
			};

			// if called programmatically, the column could be hidden
			// in this case, resizing doesn't need to be done at all
			if (maxItemLength == 0)
				return;

			var columnSizeToFit = MeasureColumnEstimate(columnToResize, 5, maxItemLength);

			if (columnSizeToFit > 1)
			{
				var column = columnToResize switch
				{
					2 => ColumnsViewModel.NameColumn,
					3 => ColumnsViewModel.GitStatusColumn,
					4 => ColumnsViewModel.GitLastCommitDateColumn,
					5 => ColumnsViewModel.GitLastCommitMessageColumn,
					6 => ColumnsViewModel.GitCommitAuthorColumn,
					7 => ColumnsViewModel.GitLastCommitShaColumn,
					8 => ColumnsViewModel.TagColumn,
					9 => ColumnsViewModel.PathColumn,
					10 => ColumnsViewModel.OriginalPathColumn,
					11 => ColumnsViewModel.DateDeletedColumn,
					12 => ColumnsViewModel.DateModifiedColumn,
					13 => ColumnsViewModel.DateCreatedColumn,
					14 => ColumnsViewModel.ItemTypeColumn,
					15 => ColumnsViewModel.SizeColumn,
					_ => ColumnsViewModel.StatusColumn
				};

				if (columnToResize == 2) // file name column
					columnSizeToFit += 20;

				var minFitLength = Math.Max(columnSizeToFit, column.NormalMinLength);
				var maxFitLength = Math.Min(minFitLength + 36, column.NormalMaxLength); // 36 to account for SortIcon & padding

				column.UserLength = new GridLength(maxFitLength, GridUnitType.Pixel);
			}

			FolderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private double MeasureColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			if (columnIndex == 15) // sync status
				return maxItemLength;

			if (columnIndex == 8) // file tag
				return MeasureTagColumnEstimate(columnIndex);

			return MeasureTextColumnEstimate(columnIndex, measureItemsCount, maxItemLength);
		}

		private double MeasureTagColumnEstimate(int columnIndex)
		{
			var grids = DependencyObjectHelpers
				.FindChildren<Grid>(FileList.ItemsPanelRoot)
				.Where(grid => IsCorrectColumn(grid, columnIndex));

			// Get the list of stack panels with the most letters
			var stackPanels = grids
				.Select(DependencyObjectHelpers.FindChildren<StackPanel>)
				.OrderByDescending(sps => sps.Select(sp => DependencyObjectHelpers.FindChildren<TextBlock>(sp).Select(tb => tb.Text.Length).Sum()).Sum())
				.First()
				.ToArray();

			var mesuredSize = stackPanels.Select(x =>
			{
				x.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

				return x.DesiredSize.Width;
			}).Sum();

			if (stackPanels.Length >= 2)
				mesuredSize += 4 * (stackPanels.Length - 1); // The spacing between the tags

			return mesuredSize;
		}

		private double MeasureTextColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			var tbs = DependencyObjectHelpers
				.FindChildren<TextBlock>(FileList.ItemsPanelRoot)
				.Where(tb => IsCorrectColumn(tb, columnIndex));

			// heuristic: usually, text with more letters are wider than shorter text with wider letters
			// with this, we can calculate avg width using longest text(s) to avoid overshooting the width
			var widthPerLetter = tbs
				.OrderByDescending(x => x.Text.Length)
				.Where(tb => !string.IsNullOrEmpty(tb.Text))
				.Take(measureItemsCount)
				.Select(tb =>
				{
					var sampleTb = new TextBlock { Text = tb.Text, FontSize = tb.FontSize, FontFamily = tb.FontFamily };
					sampleTb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

					return sampleTb.DesiredSize.Width / Math.Max(1, tb.Text.Length);
				});

			if (!widthPerLetter.Any())
				return 0;

			// Take weighted avg between mean and max since width is an estimate
			var weightedAvg = (widthPerLetter.Average() + widthPerLetter.Max()) / 2;
			return weightedAvg * maxItemLength;
		}

		private bool IsCorrectColumn(FrameworkElement element, int columnIndex)
		{
			int columnIndexFromName = element.Name switch
			{
				"ItemName" => 2,
				"ItemGitStatusTextBlock" => 3,
				"ItemGitLastCommitDateTextBlock" => 4,
				"ItemGitLastCommitMessageTextBlock" => 5,
				"ItemGitCommitAuthorTextBlock" => 6,
				"ItemGitLastCommitShaTextBlock" => 7,
				"ItemTagGrid" => 8,
				"ItemPath" => 9,
				"ItemOriginalPath" => 10,
				"ItemDateDeleted" => 11,
				"ItemDateModified" => 12,
				"ItemDateCreated" => 13,
				"ItemType" => 14,
				"ItemSize" => 15,
				"ItemStatus" => 16,
				_ => -1,
			};

			return columnIndexFromName != -1 && columnIndexFromName == columnIndex;
		}



		private void SetDetailsColumnsAsDefault_Click(object sender, RoutedEventArgs e)
		{
			LayoutPreferencesManager.SetDefaultLayoutPreferences(ColumnsViewModel);
		}

		private void ItemSelected_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox && checkBox.DataContext is ListedItem item && !FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Add(item);
		}

		private void ItemSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox && checkBox.DataContext is ListedItem item && FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Remove(item);
		}

		private new void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			var selectionCheckbox = args.ItemContainer.FindDescendant("SelectionCheckbox")!;

			selectionCheckbox.PointerEntered -= SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited -= SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled -= SelectionCheckbox_PointerCanceled;

			base.FileList_ContainerContentChanging(sender, args);
			SetCheckboxSelectionState(args.Item, args.ItemContainer as ListViewItem);

			selectionCheckbox.PointerEntered += SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited += SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled += SelectionCheckbox_PointerCanceled;
		}

		private void SetCheckboxSelectionState(object item, ListViewItem? lviContainer = null)
		{
			var container = lviContainer ?? FileList.ContainerFromItem(item) as ListViewItem;
			if (container is not null)
			{
				var checkbox = container.FindDescendant("SelectionCheckbox") as CheckBox;
				if (checkbox is not null)
				{
					// Temporarily disable events to avoid selecting wrong items
					checkbox.Checked -= ItemSelected_Checked;
					checkbox.Unchecked -= ItemSelected_Unchecked;

					checkbox.IsChecked = FileList.SelectedItems.Contains(item);

					checkbox.Checked += ItemSelected_Checked;
					checkbox.Unchecked += ItemSelected_Unchecked;
				}
				UpdateCheckboxVisibility(container, checkbox?.IsPointerOver ?? false);
			}
		}

		private void TagItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var tagName = ((sender as StackPanel)?.Children[TAG_TEXT_BLOCK] as TextBlock)?.Text;
			if (tagName is null)
				return;

			ParentShellPageInstance?.SubmitSearch($"tag:{tagName}");
		}

		private void FileTag_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "PointerOver", true);
		}

		private void FileTag_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "Normal", true);
		}

		private async void RemoveTagIcon_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var parent = (sender as FontIcon)?.Parent as StackPanel;
			var tagName = (parent?.Children[TAG_TEXT_BLOCK] as TextBlock)?.Text;

			if (tagName is null || parent?.DataContext is not ListedItem item)
				return;

			var tagId = FileTagsSettingsService.GetTagsByName(tagName).FirstOrDefault()?.Uid;

			if (tagId is not null)
			{
				item.FileTags = item.FileTags
					.Except([tagId])
					.ToArray();

				if (ParentShellPageInstance is not null)
					await ParentShellPageInstance.ShellViewModel.RefreshTagGroups();
			}

			e.Handled = true;
		}

		private void SelectionCheckbox_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, true);
		}

		private void SelectionCheckbox_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, false);
		}

		private void SelectionCheckbox_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, false);
		}

		private void UpdateCheckboxVisibility(object sender, bool isPointerOver)
		{
			if (sender is ListViewItem control && control.FindDescendant<UserControl>() is UserControl userControl)
			{
				// Handle visual states
				// Show checkboxes when items are selected (as long as the setting is enabled)
				// Show checkboxes when hovering of the thumbnail (regardless of the setting to hide them)
				if (UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems && control.IsSelected
					|| isPointerOver)
					VisualStateManager.GoToState(userControl, "ShowCheckbox", true);
				else
					VisualStateManager.GoToState(userControl, "HideCheckbox", true);
			}
		}

		// Workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/170
		private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs e)
		{
			SetToolTip(sender);
		}

		private void TextBlock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
		{
			if (sender is TextBlock textBlock)
				SetToolTip(textBlock);
		}

		private void SetToolTip(TextBlock textBlock)
		{
			ToolTipService.SetToolTip(textBlock, textBlock.IsTextTrimmed ? textBlock.Text : null);
		}

		private void FileList_LosingFocus(UIElement sender, LosingFocusEventArgs args)
		{
			// Fixes an issue where clicking an empty space would scroll to the top of the file list
			if (args.NewFocusedElement == FileList)
				args.TryCancel();
		}

		private void FileListHeader_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Fixes an issue where double clicking the column header would navigate back as if clicking on empty space
			e.Handled = true;
		}

		private static GitProperties GetEnabledGitProperties(ColumnsViewModel columnsViewModel)
		{
			var enableStatus = !columnsViewModel.GitStatusColumn.IsHidden && !columnsViewModel.GitStatusColumn.UserCollapsed;
			var enableCommit = !columnsViewModel.GitLastCommitDateColumn.IsHidden && !columnsViewModel.GitLastCommitDateColumn.UserCollapsed
				|| !columnsViewModel.GitLastCommitMessageColumn.IsHidden && !columnsViewModel.GitLastCommitMessageColumn.UserCollapsed
				|| !columnsViewModel.GitCommitAuthorColumn.IsHidden && !columnsViewModel.GitCommitAuthorColumn.UserCollapsed
				|| !columnsViewModel.GitLastCommitShaColumn.IsHidden && !columnsViewModel.GitLastCommitShaColumn.UserCollapsed;
			return (enableStatus, enableCommit) switch
			{
				(true, true) => GitProperties.All,
				(true, false) => GitProperties.Status,
				(false, true) => GitProperties.Commit,
				(false, false) => GitProperties.None
			};
		}

		/// <summary>
		/// Implements progressive rendering for better performance
		/// </summary>
		private void OnFileListContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.InRecycleQueue)
				return;

			var item = args.Item as ListedItem;
			if (item is null)
				return;

			// Phase 0: Show name and basic info immediately
			if (args.Phase == 0)
			{
				ShowBasicInfo(args.ItemContainer, item);
				args.RegisterUpdateCallback(1, LoadPhase1);
			}
		}

		private void LoadPhase1(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.Phase == 1)
			{
				var item = args.Item as ListedItem;
				if (item is null)
					return;

				// Phase 1: Show file size, dates, and type
				ShowExtendedInfo(args.ItemContainer, item);
				args.RegisterUpdateCallback(2, LoadPhase2);
			}
		}

		private void LoadPhase2(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.Phase == 2)
			{
				var item = args.Item as ListedItem;
				if (item is null)
					return;

				// Phase 2: Load icons and thumbnails
				ShowIconsAndThumbnails(args.ItemContainer, item);
				args.RegisterUpdateCallback(3, LoadPhase3);
			}
		}

		private void LoadPhase3(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.Phase == 3)
			{
				var item = args.Item as ListedItem;
				if (item is null)
					return;

				// Phase 3: Load tags and other expensive operations
				ShowRemainingContent(args.ItemContainer, item);
			}
		}

		private void ShowBasicInfo(DependencyObject container, ListedItem item)
		{
			// Show name immediately
			var nameTextBlock = container.FindDescendant("ItemName") as TextBlock;
			if (nameTextBlock is not null)
			{
				nameTextBlock.Text = item.Name;
				nameTextBlock.Opacity = item.Opacity;
			}
		}

		private void ShowExtendedInfo(DependencyObject container, ListedItem item)
		{
			// Show dates, size, and type
			var dateModifiedTextBlock = container.FindDescendant("ItemDateModified") as TextBlock;
			if (dateModifiedTextBlock is not null)
				dateModifiedTextBlock.Text = item.ItemDateModified;

			var dateCreatedTextBlock = container.FindDescendant("ItemDateCreated") as TextBlock;
			if (dateCreatedTextBlock is not null)
				dateCreatedTextBlock.Text = item.ItemDateCreated;

			var sizeTextBlock = container.FindDescendant("ItemSize") as TextBlock;
			if (sizeTextBlock is not null)
				sizeTextBlock.Text = item.FileSize;

			var typeTextBlock = container.FindDescendant("ItemType") as TextBlock;
			if (typeTextBlock is not null)
				typeTextBlock.Text = item.ItemType;
		}

		private async void ShowIconsAndThumbnails(DependencyObject container, ListedItem item)
		{
			// Load file icon/thumbnail with smart viewport detection
			// This loads thumbnails for visible items and items that will be visible soon
			var listViewItem = container as ListViewItem;
			if (listViewItem is null)
				return;

			// Check if item is in extended viewport (much larger buffer)
			if (!IsItemInExtendedViewport(listViewItem))
				return;

			var iconBox = container.FindDescendant("IconBox") as Grid;
			if (iconBox is not null)
			{
				iconBox.Opacity = item.Opacity;
				
				var picturePresenter = container.FindDescendant("PicturePresenter") as ContentPresenter;
				if (picturePresenter is not null)
				{
					// Load thumbnail if not already loaded
					if (item.FileImage is null)
					{
						// Check if item is actually visible (not just in extended viewport)
						if (IsItemInViewport(listViewItem))
						{
							// Load high-resolution thumbnail for visible items
							await LoadItemThumbnailAsync(item);
						}
						else
						{
							// Load low-resolution thumbnail for items that will be visible soon
							await LoadLowResThumbnailAsync(item);
						}
					}
					
					var picture = container.FindDescendant("Picture") as Image;
					if (picture is not null && item.FileImage is not null)
						picture.Source = item.FileImage;
				}
			}
		}



		private bool IsItemInViewport(ListViewItem item)
		{
			if (ContentScroller is null)
				return true; // Assume visible if we can't check

			// Check if item has valid dimensions
			if (item.ActualWidth <= 0 || item.ActualHeight <= 0)
				return true; // Assume visible if item hasn't been measured yet

			try
			{
				var itemBounds = item.TransformToVisual(FileList).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
				var viewportHeight = ContentScroller.ViewportHeight;
				var verticalOffset = ContentScroller.VerticalOffset;

				// Much more aggressive viewport detection - load items that are within a much larger range
				// This ensures we load thumbnails for items that will be visible when scrolling
				var buffer = Math.Max(RowHeight * 20, 1000); // Load items within 20 rows or 1000 pixels
				var isInViewport = itemBounds.Bottom >= verticalOffset - buffer && itemBounds.Top <= verticalOffset + viewportHeight + buffer;
				
				// Debug output for troubleshooting
				if (!isInViewport)
				{
					System.Diagnostics.Debug.WriteLine($"Item not in viewport: {item.DataContext} - Bounds: {itemBounds}, Viewport: {verticalOffset}-{verticalOffset + viewportHeight}, Buffer: {buffer}");
				}
				
				return isInViewport;
			}
			catch
			{
				// If viewport detection fails, assume item is visible
				return true;
			}
		}

		/// <summary>
		/// Extended viewport detection with much larger buffer for thumbnail loading
		/// This loads thumbnails for items that are visible or will be visible soon
		/// </summary>
		private bool IsItemInExtendedViewport(ListViewItem item)
		{
			if (ContentScroller is null)
				return true; // Assume visible if we can't check

			// Check if item has valid dimensions
			if (item.ActualWidth <= 0 || item.ActualHeight <= 0)
				return true; // Assume visible if item hasn't been measured yet

			try
			{
				var itemBounds = item.TransformToVisual(FileList).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
				var viewportHeight = ContentScroller.ViewportHeight;
				var verticalOffset = ContentScroller.VerticalOffset;

				// Very large buffer for thumbnail loading - load items that are within a huge range
				// This ensures smooth scrolling experience with thumbnails
				var buffer = Math.Max(RowHeight * 50, 2000); // Load items within 50 rows or 2000 pixels
				var isInExtendedViewport = itemBounds.Bottom >= verticalOffset - buffer && itemBounds.Top <= verticalOffset + viewportHeight + buffer;
				
				// Debug output for troubleshooting
				if (!isInExtendedViewport)
				{
					System.Diagnostics.Debug.WriteLine($"Item not in extended viewport: {item.DataContext} - Bounds: {itemBounds}, Viewport: {verticalOffset}-{verticalOffset + viewportHeight}, Buffer: {buffer}");
				}
				
				return isInExtendedViewport;
			}
			catch
			{
				// If viewport detection fails, assume item is visible
				return true;
			}
		}

		private async Task LoadItemThumbnailAsync(ListedItem item)
		{
			try
			{
				// Load extended properties including thumbnail with timeout
				if (ParentShellPageInstance?.ShellViewModel != null && FileList.ContainerFromItem(item) is not null)
				{
					System.Diagnostics.Debug.WriteLine($"Loading thumbnail for: {item.Name} ({item.ItemPath})");
					
					await ExecuteWithTimeoutAsync<Task>(
						async () => 
						{
							await ParentShellPageInstance.ShellViewModel.LoadExtendedItemPropertiesAsync(item);
							return Task.CompletedTask;
						},
						TimeSpan.FromSeconds(10),
						null);
					
					System.Diagnostics.Debug.WriteLine($"Finished loading thumbnail for: {item.Name}");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"Skipping thumbnail load for: {item.Name} - ParentShellPageInstance: {ParentShellPageInstance?.ShellViewModel != null}, Container: {FileList.ContainerFromItem(item) != null}");
				}
			}
			catch (Exception ex)
			{
				// Log errors for debugging
				System.Diagnostics.Debug.WriteLine($"Error loading thumbnail for {item.Name}: {ex.Message}");
			}
		}

		/// <summary>
		/// Load a lower resolution thumbnail for better performance
		/// This is used for items that are not immediately visible but should have thumbnails loaded
		/// </summary>
		private async Task LoadLowResThumbnailAsync(ListedItem item)
		{
			try
			{
				if (ParentShellPageInstance?.ShellViewModel == null)
					return;

				// Use a smaller thumbnail size for better performance
				var smallThumbnailSize = 32u; // Much smaller than the default
				
				System.Diagnostics.Debug.WriteLine($"Loading low-res thumbnail for: {item.Name} ({item.ItemPath})");
				
				// Load a small thumbnail directly using FileThumbnailHelper with timeout
				var result = await ExecuteWithTimeoutAsync<byte[]?>(
					async () => await FileThumbnailHelper.GetIconAsync(
						item.ItemPath,
						smallThumbnailSize,
						item.IsFolder,
						IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale),
					TimeSpan.FromSeconds(5),
					null);

				if (result is not null)
				{
					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
					{
						var image = await result.ToBitmapAsync();
						if (image is not null)
						{
							item.FileImage = image;
							System.Diagnostics.Debug.WriteLine($"Finished loading low-res thumbnail for: {item.Name}");
						}
					}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
				}
			}
			catch (Exception ex)
			{
				// Log errors for debugging
				System.Diagnostics.Debug.WriteLine($"Error loading low-res thumbnail for {item.Name}: {ex.Message}");
			}
		}

		private void ShowRemainingContent(DependencyObject container, ListedItem item)
		{
			// Load tags (expensive operation)
			var tagsRepeater = container.FindDescendant("TagsRepeater") as ItemsRepeater;
			if (tagsRepeater is not null && item.FileTagsUI is not null)
			{
				tagsRepeater.ItemsSource = item.FileTagsUI;
			}

			// Load git information if applicable
			if (item is IGitItem gitItem)
			{
				var gitStatusIcon = container.FindDescendant("ItemGitStatusTextBlock") as Border;
				if (gitStatusIcon is not null)
					gitStatusIcon.Visibility = Visibility.Visible;
			}
		}

		private void ClearContainer(DependencyObject container)
		{
			// Clear content when recycling to prevent stale data
			var nameTextBlock = container.FindDescendant("ItemName") as TextBlock;
			if (nameTextBlock is not null)
				nameTextBlock.Text = string.Empty;

			var picture = container.FindDescendant("Picture") as Image;
			if (picture is not null)
				picture.Source = null;

			var tagsRepeater = container.FindDescendant("TagsRepeater") as ItemsRepeater;
			if (tagsRepeater is not null)
				tagsRepeater.ItemsSource = null;
		}
		// Dispose
		public void Dispose()
		{
			// Clean up thumbnail loading resources
			_scrollEndTimer?.Stop();
			_thumbnailBatchSemaphore?.Dispose();
			_thumbnailLoadSemaphore?.Dispose();
			_cacheCleanupTimer?.Dispose();
			_thumbnailCache.Clear();
			_thumbnailQueue.Clear();
			_thumbnailLoadTasks.Clear();

			// Unhook events
			FileList.Loaded -= FileList_Loaded;
			FileList.Unloaded -= FileList_Unloaded;
		}

		/// <summary>
		/// Memory-efficient batch processing with proper resource management and cleanup
		/// </summary>
		private async Task ProcessBatchWithCleanupAsync<T>(
			IEnumerable<T> items,
			Func<T, Task> processor,
			int batchSize = 50,
			CancellationToken cancellationToken = default)
		{
			using var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
			using var memoryPressure = new MemoryPressureMonitor();
			
			await Parallel.ForEachAsync(
				items.Chunk(batchSize),
				new ParallelOptions 
				{ 
					MaxDegreeOfParallelism = Environment.ProcessorCount,
					CancellationToken = cancellationToken 
				},
				async (batch, token) =>
				{
					await semaphore.WaitAsync(token);
					try
					{
						var tasks = batch.Select(processor);
						await Task.WhenAll(tasks);
						
						// Check memory pressure and trigger cleanup if needed
						if (memoryPressure.IsHighPressure)
						{
							CleanupCache(null);
							GC.Collect();
						}
					}
					finally
					{
						semaphore.Release();
					}
				});
		}

		/// <summary>
		/// Simple memory pressure monitor
		/// </summary>
		private class MemoryPressureMonitor : IDisposable
		{
			private readonly long _initialMemory;
			
			public MemoryPressureMonitor()
			{
				_initialMemory = GC.GetTotalMemory(false);
			}
			
			public bool IsHighPressure => GC.GetTotalMemory(false) > _initialMemory * 2;
			
			public void Dispose()
			{
				// Cleanup if needed
			}
		}
	}
}
