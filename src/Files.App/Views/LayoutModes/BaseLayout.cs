// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.Utils.StorageItems;
using Files.App.Helpers.ContextFlyouts;
using Files.App.UserControls.Menus;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using static Files.App.Helpers.PathNormalization;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using SortDirection = Files.Shared.Enums.SortDirection;
using VanaraWindowsShell = Vanara.Windows.Shell;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents the base class which every layout page must derive from.
	/// </summary>
	public abstract class BaseLayout : Page, IBaseLayout, INotifyPropertyChanged
	{
		protected readonly IUserSettingsService UserSettingsService;

		protected readonly IFileTagsSettingsService FileTagsSettingsService;

		private readonly DispatcherQueueTimer _jumpTimer;

		private readonly DispatcherQueueTimer _dragOverTimer;

		private readonly DispatcherQueueTimer _tapDebounceTimer;

		private readonly DispatcherQueueTimer _hoverTimer;

		private CancellationTokenSource? _groupingCancellationToken;

		private CancellationTokenSource? _shellContextMenuItemCancellationToken;

		private ListedItem? _dragOverItem;

		private ListedItem? _hoveredItem;

		private ListedItem? _preRenamingItem;

		protected NavigationArguments? _navigationArguments;

		protected abstract uint IconSize { get; }

		protected abstract ItemsControl ItemsControl { get; }

		protected static AddressToolbar? NavToolbar
			=> (App.Window.Content as Frame)?.FindDescendant<AddressToolbar>();

		public event PropertyChangedEventHandler? PropertyChanged;

		public static AppModel AppModel
			=> App.AppModel;

		/// <inheritdoc/>
		public PreviewPaneViewModel PreviewPaneViewModel { get; }

		/// <inheritdoc/>
		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		/// <inheritdoc/>
		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		/// <inheritdoc/>
		public CommandBarFlyout ItemContextMenuFlyout { get; set; }

		/// <inheritdoc/>
		public CommandBarFlyout BaseContextMenuFlyout { get; set; }

		/// <inheritdoc/>
		public BaseLayoutCommandsViewModel? CommandsViewModel { get; protected set; }

		/// <inheritdoc/>
		public bool IsRenamingItem { get; set; }

		/// <inheritdoc/>
		public ItemManipulationModel ItemManipulationModel { get; private set; }

		/// <inheritdoc/>
		public ListedItem? SelectedItem { get; private set; }

		/// <inheritdoc/>
		public IShellPage? ParentShellPageInstance { get; private set; }

		/// <inheritdoc/>
		public ListedItem? RenamingItem { get; set; }

		/// <inheritdoc/>
		public string? OldItemName { get; set; }

		/// <inheritdoc/>
		public bool LockPreviewPaneContent { get; set; }

		public FolderSettingsViewModel? FolderSettings
			=> ParentShellPageInstance?.InstanceViewModel.FolderSettings;

		public CurrentInstanceViewModel? InstanceViewModel
			=> ParentShellPageInstance?.InstanceViewModel;

		private bool _IsMiddleClickToScrollEnabled;
		public bool IsMiddleClickToScrollEnabled
		{
			get => _IsMiddleClickToScrollEnabled;
			set
			{
				if (_IsMiddleClickToScrollEnabled != value)
				{
					_IsMiddleClickToScrollEnabled = value;

					NotifyPropertyChanged(nameof(IsMiddleClickToScrollEnabled));
				}
			}
		}

		private CollectionViewSource _CollectionViewSource;
		public CollectionViewSource CollectionViewSource
		{
			get => _CollectionViewSource;
			set
			{
				if (_CollectionViewSource == value)
					return;

				if (_CollectionViewSource.View is not null)
					_CollectionViewSource.View.VectorChanged -= View_VectorChanged;

				_CollectionViewSource = value;

				NotifyPropertyChanged(nameof(CollectionViewSource));

				if (_CollectionViewSource.View is not null)
					_CollectionViewSource.View.VectorChanged += View_VectorChanged;
			}
		}

		private bool _IsItemSelected;
		public bool IsItemSelected
		{
			get => _IsItemSelected;
			internal set
			{
				if (value != _IsItemSelected)
				{
					_IsItemSelected = value;

					NotifyPropertyChanged(nameof(IsItemSelected));
				}
			}
		}

		private string _JumpString;
		public string JumpString
		{
			get => _JumpString;
			set
			{
				// If current string is "a", and the next character typed is "a",
				// search for next file that starts with "a" (a.k.a. _jumpString = "a")
				if (_JumpString.Length == 1 && value == _JumpString + _JumpString)
					value = _JumpString;
				if (value != string.Empty)
				{
					ListedItem? jumpedToItem = null;
					ListedItem? previouslySelectedItem = IsItemSelected ? SelectedItem : null;

					// Select first matching item after currently selected item
					if (previouslySelectedItem is not null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<ListedItem> candidateItems = ParentShellPageInstance!.FilesystemViewModel.FilesAndFolders
							.SkipWhile(x => x != previouslySelectedItem)
							.Skip(value.Length == 1 ? 1 : 0) // User is trying to cycle through items starting with the same letter
							.Where(f => f.Name.Length >= value.Length && string.Equals(f.Name[..value.Length], value, StringComparison.OrdinalIgnoreCase));

						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<ListedItem> candidateItems = ParentShellPageInstance!.FilesystemViewModel.FilesAndFolders
							.Where(f => f.Name.Length >= value.Length && string.Equals(f.Name[..value.Length], value, StringComparison.OrdinalIgnoreCase));

						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is not null)
					{
						ItemManipulationModel.SetSelectedItem(jumpedToItem);
						ItemManipulationModel.ScrollIntoView(jumpedToItem);
						ItemManipulationModel.FocusSelectedItems();
					}

					// Restart the timer
					_jumpTimer.Start();
				}

				_JumpString = value;
			}
		}

		private List<ListedItem>? _SelectedItems;
		public List<ListedItem>? SelectedItems
		{
			get => _SelectedItems;
			internal set
			{
				if (value != _SelectedItems)
				{
					UpdatePreviewPaneSelection(value);

					_SelectedItems = value;

					if (_SelectedItems?.Count == 0 || _SelectedItems?[0] is null)
					{
						IsItemSelected = false;
						SelectedItem = null;
						SelectedItemsPropertiesViewModel.IsItemSelected = false;

						ResetRenameDoubleClick();
						UpdateSelectionSize();
					}
					else if (_SelectedItems is not null)
					{
						IsItemSelected = true;
						SelectedItem = _SelectedItems.First();
						SelectedItemsPropertiesViewModel.IsItemSelected = true;

						UpdateSelectionSize();

						SelectedItemsPropertiesViewModel.SelectedItemsCount = _SelectedItems.Count;

						if (_SelectedItems.Count == 1)
						{
							SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{_SelectedItems.Count} {"ItemSelected/Text".GetLocalizedResource()}";
							DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								// Tapped event must be executed first
								await Task.Delay(50);
								_preRenamingItem = SelectedItem;
							});
						}
						else
						{
							SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{_SelectedItems!.Count} {"ItemsSelected/Text".GetLocalizedResource()}";
							ResetRenameDoubleClick();
						}
					}

					NotifyPropertyChanged(nameof(SelectedItems));
				}

				if (value is not null)
					ParentShellPageInstance!.ToolbarViewModel.SelectedItems = value;
			}
		}

		public BaseLayout()
		{
			// Dependency injection
			UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			FileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
			PreviewPaneViewModel = Ioc.Default.GetRequiredService<PreviewPaneViewModel>();

			// Initialize
			SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel();
			DirectoryPropertiesViewModel = new DirectoryPropertiesViewModel();
			ItemManipulationModel = new ItemManipulationModel();

			ItemContextMenuFlyout = new()
			{
				AlwaysExpanded = true,
				AreOpenCloseAnimationsEnabled = false,
				Placement = FlyoutPlacementMode.Right,
			};

			BaseContextMenuFlyout = new()
			{
				AlwaysExpanded = true,
				AreOpenCloseAnimationsEnabled = false,
				Placement = FlyoutPlacementMode.Right,
			};

			_IsMiddleClickToScrollEnabled = true;
			_CollectionViewSource = new() { IsSourceGrouped = true };
			_JumpString = string.Empty;
			_SelectedItems = new();

			// Hook events
			HookBaseEvents();
			HookEvents();

			// Initialize timers.
			_jumpTimer = DispatcherQueue.CreateTimer();
			_jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
			_jumpTimer.Tick += JumpTimer_Tick;

			_dragOverTimer = DispatcherQueue.CreateTimer();
			_tapDebounceTimer = DispatcherQueue.CreateTimer();
			_hoverTimer = DispatcherQueue.CreateTimer();
		}

		protected abstract void HookEvents();

		protected abstract void UnhookEvents();

		protected abstract void InitializeCommandsViewModel();

		protected abstract bool CanGetItemFromElement(object element);

		private void HookBaseEvents()
		{
			ItemManipulationModel.RefreshItemsOpacityInvoked += ItemManipulationModel_RefreshItemsOpacityInvoked;
		}

		private void UnhookBaseEvents()
		{
			ItemManipulationModel.RefreshItemsOpacityInvoked -= ItemManipulationModel_RefreshItemsOpacityInvoked;
		}

		private void JumpTimer_Tick(object sender, object e)
		{
			_JumpString = string.Empty;
			_jumpTimer.Stop();
		}

		public virtual void ResetItemOpacity()
		{
			var items = GetAllItems();
			if (items is null)
				return;

			foreach (var item in items)
			{
				if (item is not null)
					item.Opacity = item.IsHiddenItem ? Constants.UI.DimItemOpacity : 1.0d;
			}
		}

		protected IEnumerable<ListedItem>? GetAllItems()
		{
			var items = CollectionViewSource.IsSourceGrouped
				? (CollectionViewSource.Source as BulkConcurrentObservableCollection<GroupedCollection<ListedItem>>)?.SelectMany(g => g) // add all items from each group to the new list
				: CollectionViewSource.Source as IEnumerable<ListedItem>;

			return items ?? new List<ListedItem>();
		}

		protected ListedItem? GetItemFromElement(object element)
		{
			if (element is not ContentControl item || !CanGetItemFromElement(element))
				return null;

			return (item.DataContext as ListedItem) ?? (item.Content as ListedItem) ?? (ItemsControl.ItemFromContainer(item) as ListedItem);
		}

		protected virtual void BaseFolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			if (ParentShellPageInstance?.SlimContentPage is not null)
			{
				var layoutType = FolderSettings!.GetLayoutType(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory);

				if (layoutType != ParentShellPageInstance.CurrentPageType)
				{
					ParentShellPageInstance.NavigateWithArguments(layoutType, new NavigationArguments()
					{
						NavPathParam = _navigationArguments!.NavPathParam,
						IsSearchResultPage = _navigationArguments.IsSearchResultPage,
						SearchPathParam = _navigationArguments.SearchPathParam,
						SearchQuery = _navigationArguments.SearchQuery,
						SearchUnindexedItems = _navigationArguments.SearchUnindexedItems,
						IsLayoutSwitch = true,
						AssociatedTabInstance = ParentShellPageInstance
					});

					// Remove old layout from back stack
					ParentShellPageInstance.RemoveLastPageFromBackStack();
					ParentShellPageInstance.ResetNavigationStackLayoutMode();
				}

				ParentShellPageInstance.FilesystemViewModel.UpdateEmptyTextType();
			}
		}

		protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			// Add item jumping handler
			CharacterReceived += Page_CharacterReceived;

			_navigationArguments = (NavigationArguments)eventArgs.Parameter;

			ParentShellPageInstance = _navigationArguments.AssociatedTabInstance;
			if (ParentShellPageInstance is null)
				throw new NullReferenceException($"{ParentShellPageInstance} is null.");

			InitializeCommandsViewModel();

			IsItemSelected = false;

			FolderSettings!.LayoutModeChangeRequested += BaseFolderSettings_LayoutModeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
			FolderSettings.GroupDirectionPreferenceUpdated += FolderSettings_GroupDirectionPreferenceUpdated;
			FolderSettings.GroupByDateUnitPreferenceUpdated += FolderSettings_GroupByDateUnitPreferenceUpdated;

			ParentShellPageInstance.FilesystemViewModel.EmptyTextType = EmptyTextType.None;
			ParentShellPageInstance.ToolbarViewModel.CanRefresh = true;

			if (!_navigationArguments.IsSearchResultPage)
			{
				var previousDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
				await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(_navigationArguments.NavPathParam);

				// pathRoot will be empty on recycle bin path
				var workingDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory ?? string.Empty;
				var pathRoot = GetPathRoot(workingDir);

				var isRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = isRecycleBin;

				// Can't go up from recycle bin
				ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = !(string.IsNullOrEmpty(pathRoot) || isRecycleBin);

				ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
				ParentShellPageInstance.ToolbarViewModel.PathControlDisplayText = _navigationArguments.NavPathParam ?? string.Empty;

				if (ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.Path)
					ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.Name;

				if (ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FolderPath &&
					!ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary)
					ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.None;

				if (!_navigationArguments.IsLayoutSwitch || previousDir != workingDir)
					ParentShellPageInstance.FilesystemViewModel.RefreshItems(previousDir, SetSelectedItemsOnNavigation);
				else
					ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;
			}
			else
			{
				await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(_navigationArguments.SearchPathParam);

				ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;

				// Impose no artificial restrictions on back navigation. Even in a search results page.
				ParentShellPageInstance.ToolbarViewModel.CanGoBack = true;

				ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = false;

				var workingDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory ?? string.Empty;

				ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;

				if (!_navigationArguments.IsLayoutSwitch)
				{
					var displayName = App.LibraryManager.TryGetLibrary(_navigationArguments.SearchPathParam, out var lib) ? lib.Text : _navigationArguments.SearchPathParam;
					ParentShellPageInstance.UpdatePathUIToWorkingDirectory(null, string.Format("SearchPagePathBoxOverrideText".GetLocalizedResource(), _navigationArguments.SearchQuery, displayName));

					var searchInstance = new Utils.Search.FolderSearch()
					{
						Query = _navigationArguments.SearchQuery,
						Folder = _navigationArguments.SearchPathParam,
						ThumbnailSize = InstanceViewModel!.FolderSettings.GetIconSize(),
						SearchUnindexedItems = _navigationArguments.SearchUnindexedItems
					};

					_ = ParentShellPageInstance.FilesystemViewModel.SearchAsync(searchInstance);
				}
			}

			// Show controls that were hidden on the home page
			ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true;
			ParentShellPageInstance.FilesystemViewModel.UpdateGroupOptions();

			UpdateCollectionViewSource();
			FolderSettings.IsLayoutModeChanging = false;

			SetSelectedItemsOnNavigation();

			ItemContextMenuFlyout.Opening += ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening += BaseContextFlyout_Opening;
		}

		public void SetSelectedItemsOnNavigation()
		{
			try
			{
				if (_navigationArguments is not null &&
					_navigationArguments.SelectItems is not null &&
					_navigationArguments.SelectItems.Any())
				{
					List<ListedItem> listedItemsToSelect = new();
					listedItemsToSelect.AddRange(ParentShellPageInstance!.FilesystemViewModel.FilesAndFolders.Where((li) => _navigationArguments.SelectItems.Contains(li.ItemNameRaw)));

					ItemManipulationModel.SetSelectedItems(listedItemsToSelect);
					ItemManipulationModel.FocusSelectedItems();
				}
				else if (_navigationArguments is not null && _navigationArguments.FocusOnNavigation)
				{
					if (SelectedItems?.Count == 0)
						UpdatePreviewPaneSelection(null);

					// Set focus on layout specific file list control
					ItemManipulationModel.FocusFileList();
				}
			}
			catch (Exception) { }
		}

		private async void FolderSettings_GroupOptionPreferenceUpdated(object? sender, GroupOption e)
		{
			await GroupPreferenceUpdated();
		}

		private async void FolderSettings_GroupDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			await GroupPreferenceUpdated();
		}

		private async void FolderSettings_GroupByDateUnitPreferenceUpdated(object? sender, GroupByDateUnit e)
		{
			await GroupPreferenceUpdated();
		}

		private async Task GroupPreferenceUpdated()
		{
			// Two or more of these running at the same time will cause a crash, so cancel the previous one before beginning
			_groupingCancellationToken?.Cancel();
			_groupingCancellationToken = new CancellationTokenSource();
			var token = _groupingCancellationToken.Token;

			await ParentShellPageInstance!.FilesystemViewModel.GroupOptionsUpdated(token);

			UpdateCollectionViewSource();

			await ParentShellPageInstance.FilesystemViewModel.ReloadItemGroupHeaderImagesAsync();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			// Remove item jumping handler
			CharacterReceived -= Page_CharacterReceived;
			FolderSettings!.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
			FolderSettings.GroupDirectionPreferenceUpdated -= FolderSettings_GroupDirectionPreferenceUpdated;
			FolderSettings.GroupByDateUnitPreferenceUpdated -= FolderSettings_GroupByDateUnitPreferenceUpdated;
			ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

			var parameter = e.Parameter as NavigationArguments;
			if (parameter is not null && !parameter.IsLayoutSwitch)
				ParentShellPageInstance!.FilesystemViewModel.CancelLoadAndClearFiles();
		}

		public async void ItemContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				// Workaround for item sometimes not getting selected
				if (!IsItemSelected && sender is CommandBarFlyout { Target: ListViewItem { Content: ListedItem li } })
					ItemManipulationModel.SetSelectedItem(li);

				if (IsItemSelected)
					await LoadMenuItemsAsync();
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		public async void BaseContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				ItemManipulationModel.ClearSelection();

				// Reset menu max height
				if (BaseContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
					itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

				_shellContextMenuItemCancellationToken?.Cancel();
				_shellContextMenuItemCancellationToken = new CancellationTokenSource();

				var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(
					InstanceViewModel!,
					new List<ListedItem> { ParentShellPageInstance!.FilesystemViewModel.CurrentFolder },
					CommandsViewModel!,
					shiftPressed,
					null,
					ParentShellPageInstance!.FilesystemViewModel);

				BaseContextMenuFlyout.PrimaryCommands.Clear();
				BaseContextMenuFlyout.SecondaryCommands.Clear();

				var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);

				AddCloseHandler(BaseContextMenuFlyout, primaryElements, secondaryElements);

				primaryElements.ForEach(i => BaseContextMenuFlyout.PrimaryCommands.Add(i));

				// Set menu min width
				secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);
				secondaryElements.ForEach(i => BaseContextMenuFlyout.SecondaryCommands.Add(i));

				if (!InstanceViewModel!.IsPageTypeSearchResults && !InstanceViewModel.IsPageTypeZipFolder)
				{
					var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, selectedItems: new List<ListedItem>(), shiftPressed: shiftPressed, showOpenMenu: false, _shellContextMenuItemCancellationToken.Token);
					if (shellMenuItems.Any())
						await AddShellMenuItemsAsync(shellMenuItems, BaseContextMenuFlyout, shiftPressed);
					else
						RemoveOverflow(BaseContextMenuFlyout);
				}
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		public void UpdateSelectionSize()
		{
			var items = (_SelectedItems?.Any() ?? false) ? _SelectedItems : GetAllItems();
			if (items is null)
				return;

			var isSizeKnown = !items.Any(item => string.IsNullOrEmpty(item.FileSize));
			if (isSizeKnown)
			{
				long size = items.Sum(item => item.FileSizeBytes);
				SelectedItemsPropertiesViewModel.ItemSizeBytes = size;
				SelectedItemsPropertiesViewModel.ItemSize = size.ToSizeString();
			}
			else
			{
				SelectedItemsPropertiesViewModel.ItemSizeBytes = 0;
				SelectedItemsPropertiesViewModel.ItemSize = string.Empty;
			}

			SelectedItemsPropertiesViewModel.ItemSizeVisibility = isSizeKnown;
		}

		private async Task LoadMenuItemsAsync()
		{
			// Reset menu max height
			if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
				itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

			_shellContextMenuItemCancellationToken?.Cancel();
			_shellContextMenuItemCancellationToken = new CancellationTokenSource();

			SelectedItemsPropertiesViewModel.CheckAllFileExtensions(SelectedItems!.Select(selectedItem => selectedItem?.FileExtension).ToList()!);

			var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
			var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, selectedItems: SelectedItems!, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, itemViewModel: null);

			ItemContextMenuFlyout.PrimaryCommands.Clear();
			ItemContextMenuFlyout.SecondaryCommands.Clear();

			var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
			AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);

			primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
			secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
			secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

			if (InstanceViewModel!.CanTagFilesInPage)
				AddNewFileTagsToMenu(ItemContextMenuFlyout);

			if (!InstanceViewModel.IsPageTypeZipFolder)
			{
				var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(
					ParentShellPageInstance.FilesystemViewModel.WorkingDirectory,
					SelectedItems!,
					shiftPressed,
					false,
					_shellContextMenuItemCancellationToken.Token);

				if (shellMenuItems.Any())
					await AddShellMenuItemsAsync(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
				else
					RemoveOverflow(ItemContextMenuFlyout);
			}
		}

		private void AddCloseHandler(CommandBarFlyout flyout, IList<ICommandBarElement> primaryElements, IList<ICommandBarElement> secondaryElements)
		{
			// Workaround for WinUI (#5508)
			var closeHandler = new RoutedEventHandler((s, e) => flyout.Hide());

			primaryElements
				.OfType<AppBarButton>()
				.ForEach(button => button.Click += closeHandler);

			var menuFlyoutItems = secondaryElements
				.OfType<AppBarButton>()
				.Select(item => item.Flyout)
				.OfType<MenuFlyout>()
				.SelectMany(menu => menu.Items);

			addCloseHandler(menuFlyoutItems);

			void addCloseHandler(IEnumerable<MenuFlyoutItemBase> menuFlyoutItems)
			{
				menuFlyoutItems.OfType<MenuFlyoutItem>()
					.ForEach(button => button.Click += closeHandler);

				menuFlyoutItems.OfType<MenuFlyoutSubItem>()
					.ForEach(menu => addCloseHandler(menu.Items));
			}
		}

		private void AddNewFileTagsToMenu(CommandBarFlyout contextMenu)
		{
			var fileTagsContextMenu = new FileTagsContextMenu(SelectedItems!);
			var overflowSeparator = contextMenu.SecondaryCommands.FirstOrDefault(x => x is FrameworkElement fe && fe.Tag as string == "OverflowSeparator") as AppBarSeparator;
			var index = contextMenu.SecondaryCommands.IndexOf(overflowSeparator);
			index = index >= 0 ? index : contextMenu.SecondaryCommands.Count;

			// Only show the edit tags flyout if settings is enabled
			if (!UserSettingsService.GeneralSettingsService.ShowEditTagsMenu)
				return;

			contextMenu.SecondaryCommands.Insert(index, new AppBarSeparator());
			contextMenu.SecondaryCommands.Insert(index + 1, new AppBarButton()
			{
				Label = "SettingsEditFileTagsExpander/Title".GetLocalizedResource(),
				Content = new OpacityIcon()
				{
					Style = (Style)Application.Current.Resources["ColorIconTag"],
				},
				Flyout = fileTagsContextMenu
			});
		}

		private async Task AddShellMenuItemsAsync(List<ContextMenuFlyoutItemViewModel> shellMenuItems, CommandBarFlyout contextMenuFlyout, bool shiftPressed)
		{
			var openWithMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });

			var sendToMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });

			var shellMenuItemsFiltered = shellMenuItems.Where(x => x != openWithMenuItem && x != sendToMenuItem).ToList();

			var mainShellMenuItems = shellMenuItemsFiltered.RemoveFrom(!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 0);

			var overflowShellMenuItemsUnfiltered = shellMenuItemsFiltered.Except(mainShellMenuItems).ToList();
			var overflowShellMenuItems = overflowShellMenuItemsUnfiltered.Where((x, i) =>
				(x.ItemType == ContextMenuFlyoutItemType.Separator &&
				overflowShellMenuItemsUnfiltered[i + 1 < overflowShellMenuItemsUnfiltered.Count ? i + 1 : i].ItemType != ContextMenuFlyoutItemType.Separator) ||
				x.ItemType != ContextMenuFlyoutItemType.Separator).ToList();

			var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(overflowShellMenuItems);

			var mainItems = ItemModelListToContextFlyoutHelper.GetAppBarButtonsFromModelIgnorePrimary(mainShellMenuItems);

			var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(App.Window);

			var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

			var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
			if (itemsControl is not null && secondaryMenu is not null)
			{
				contextMenuFlyout.SetValue(ContextMenuExtensions.ItemsControlProperty, itemsControl);

				var ttv = secondaryMenu.TransformToVisual(App.Window.Content);
				var cMenuPos = ttv.TransformPoint(new Point(0, 0));

				var requiredHeight = contextMenuFlyout.SecondaryCommands.Concat(mainItems).Where(x => x is not AppBarSeparator).Count() * Constants.UI.ContextMenuSecondaryItemsHeight;
				var availableHeight = App.Window.Bounds.Height - cMenuPos.Y - Constants.UI.ContextMenuPrimaryItemsHeight;

				// Set menu max height to current height (Avoid menu repositioning)
				if (requiredHeight > availableHeight)
					itemsControl.MaxHeight = Math.Min(Constants.UI.ContextMenuMaxHeight, Math.Max(itemsControl.ActualHeight, Math.Min(availableHeight, requiredHeight)));

				// Set items max width to current menu width (#5555)
				mainItems.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin);
			}

			var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
			if (overflowItem is not null)
			{
				var overflowItemFlyout = overflowItem.Flyout as MenuFlyout;
				if (overflowItemFlyout is not null)
				{
					if (overflowItemFlyout.Items.Count > 0)
						overflowItemFlyout.Items.Insert(0, new MenuFlyoutSeparator());

					var index = contextMenuFlyout.SecondaryCommands.Count - 2;
					foreach (var i in mainItems)
					{
						index++;
						contextMenuFlyout.SecondaryCommands.Insert(index, i);
					}

					index = 0;
					foreach (var i in overflowItems)
					{
						overflowItemFlyout.Items.Insert(index, i);
						index++;
					}

					if (overflowItemFlyout.Items.Count > 0 && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
					{
						overflowItem.Label = "ShowMoreOptions".GetLocalizedResource();
						overflowItem.IsEnabled = true;
					}
					else if (!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
					{
						overflowItem.Visibility = Visibility.Collapsed;
					}
				}
			}
			else
			{
				mainItems.ForEach(x => contextMenuFlyout.SecondaryCommands.Add(x));
			}

			// Add items to openwith dropdown
			var openWithOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWithOverflow") as AppBarButton;

			var openWith = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWith") as AppBarButton;
			if (openWithMenuItem?.LoadSubMenuAction is not null && openWithOverflow is not null && openWith is not null)
			{
				await openWithMenuItem.LoadSubMenuAction();
				var openWithSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(ShellContextmenuHelper.GetOpenWithItems(shellMenuItems));

				if (openWithSubItems is not null)
				{
					var flyout = (MenuFlyout)openWithOverflow.Flyout;

					flyout.Items.Clear();

					foreach (var item in openWithSubItems)
						flyout.Items.Add(item);

					openWithOverflow.Flyout = flyout;
					openWith.Visibility = Visibility.Collapsed;
					openWithOverflow.Visibility = Visibility.Visible;
				}
			}

			// Add items to sendto dropdown
			if (UserSettingsService.GeneralSettingsService.ShowSendToMenu)
			{
				var sendToOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendToOverflow") as AppBarButton;

				var sendTo = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendTo") as AppBarButton;
				if (sendToMenuItem?.LoadSubMenuAction is not null && sendToOverflow is not null && sendTo is not null)
				{
					await sendToMenuItem.LoadSubMenuAction();
					var sendToSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(ShellContextmenuHelper.GetSendToItems(shellMenuItems));

					if (sendToSubItems is not null)
					{
						var flyout = (MenuFlyout)sendToOverflow.Flyout;

						flyout.Items.Clear();

						foreach (var item in sendToSubItems)
							flyout.Items.Add(item);

						sendToOverflow.Flyout = flyout;
						sendTo.Visibility = Visibility.Collapsed;
						sendToOverflow.Visibility = Visibility.Visible;
					}
				}
			}

			// Add items to main shell submenu
			mainShellMenuItems.Where(x => x.LoadSubMenuAction is not null).ForEach(async x =>
			{
				await x.LoadSubMenuAction();

				ShellContextmenuHelper.AddItemsToMainMenu(mainItems, x);
			});

			// Add items to overflow shell submenu
			overflowShellMenuItems.Where(x => x.LoadSubMenuAction is not null).ForEach(async x =>
			{
				await x.LoadSubMenuAction();

				ShellContextmenuHelper.AddItemsToOverflowMenu(overflowItem, x);
			});

			itemsControl?.Items.OfType<FrameworkElement>().ForEach(item =>
			{
				// Enable CharacterEllipsis text trimming for menu items
				if (item.FindDescendant("OverflowTextLabel") is TextBlock label)
					label.TextTrimming = TextTrimming.CharacterEllipsis;

				// Close main menu when clicking on subitems (#5508)
				if ((item as AppBarButton)?.Flyout as MenuFlyout is MenuFlyout flyout)
				{
					Action<IList<MenuFlyoutItemBase>> clickAction = null!;
					clickAction = (items) =>
					{
						items.OfType<MenuFlyoutItem>().ForEach(i =>
						{
							i.Click += new RoutedEventHandler((s, e) => contextMenuFlyout.Hide());
						});
						items.OfType<MenuFlyoutSubItem>().ForEach(i =>
						{
							clickAction(i.Items);
						});
					};

					clickAction(flyout.Items);
				}
			});
		}

		private void RemoveOverflow(CommandBarFlyout contextMenuFlyout)
		{
			var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
			var overflowSeparator = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarSeparator appBarSeparator && (appBarSeparator.Tag as string) == "OverflowSeparator") as AppBarSeparator;

			if (overflowItem is not null)
				overflowItem.Visibility = Visibility.Collapsed;

			if (overflowSeparator is not null)
				overflowSeparator.Visibility = Visibility.Collapsed;
		}

		protected virtual void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance!.IsCurrentInstance)
			{
				char letter = args.Character;
				JumpString += letter.ToString().ToLowerInvariant();
			}
		}

		protected void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			try
			{
				var shellItemList = e.Items.OfType<ListedItem>().Select(x => new VanaraWindowsShell.ShellItem(x.ItemPath)).ToArray();

				if (shellItemList[0].FileSystemPath is not null &&
					InstanceViewModel is not null && 
					!InstanceViewModel.IsPageTypeSearchResults)
				{
					var iddo = shellItemList[0].Parent.GetChildrenUIObjects<IDataObject>(HWND.NULL, shellItemList);
					shellItemList.ForEach(x => x.Dispose());

					var format = System.Windows.Forms.DataFormats.GetFormat("Shell IDList Array");
					if (iddo.TryGetData<byte[]>((uint)format.Id, out var data))
					{
						var mem = new MemoryStream(data).AsRandomAccessStream();
						e.Data.SetData(format.Name, mem);
					}
				}
				else
				{
					// Only support IStorageItem capable paths
					var storageItemList =
						e.Items
							.OfType<ListedItem>()
							.Where(x => !(x.IsHiddenItem && x.IsLinkItem && x.IsRecycleBinItem && x.IsShortcut))
							.Select(VirtualStorageItem.FromListedItem);

					e.Data.SetStorageItems(storageItemList, false);
				}
			}
			catch (Exception)
			{
				e.Cancel = true;
			}
		}

		private void Item_DragLeave(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);

			// Reset dragged over item
			if (item == _dragOverItem)
				_dragOverItem = null;
		}

		protected async void Item_DragOver(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);
			if (item is null)
				return;

			DragOperationDeferral? deferral = null;

			try
			{
				deferral = e.GetDeferral();

				if (_dragOverItem != item)
				{
					_dragOverItem = item;
					_dragOverTimer.Stop();
					_dragOverTimer.Debounce(() =>
					{
						if (_dragOverItem is not null && !_dragOverItem.IsExecutable)
						{
							_dragOverTimer.Stop();
							ItemManipulationModel.SetSelectedItem(_dragOverItem);
							_dragOverItem = null;
							_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance!, false);
						}
					},
					TimeSpan.FromMilliseconds(1000), false);
				}

				if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				{
					e.Handled = true;

					var draggedItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

					if (draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else if (!draggedItems.Any())
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						e.DragUIOverride.IsCaptionVisible = true;

						if (item.IsExecutable)
						{
							e.DragUIOverride.Caption = $"{"OpenItemsWithCaptionText".GetLocalizedResource()} {item.Name}";
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						// Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
						else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Move;
						}
						else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder) ||
							ZipStorageFolder.IsZipPath(item.ItemPath))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Move;
						}
						else
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
					}
				}
			}
			finally
			{
				deferral?.Complete();
			}
		}

		protected async void Item_Drop(object sender, DragEventArgs e)
		{
			var deferral = e.GetDeferral();

			e.Handled = true;

			// Reset dragged over item
			_dragOverItem = null;

			var item = GetItemFromElement(sender);
			if (item is not null)
				await ParentShellPageInstance!.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (item as ShortcutItem)?.TargetPath ?? item.ItemPath, false, true, item.IsExecutable);

			deferral.Complete();
		}

		protected void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			RefreshContainer(args.ItemContainer, args.InRecycleQueue);
			RefreshItem(args.ItemContainer, args.Item, args.InRecycleQueue, args);
		}

		private void RefreshContainer(SelectorItem container, bool inRecycleQueue)
		{
			container.PointerPressed -= FileListItem_PointerPressed;
			container.PointerEntered -= FileListItem_PointerEntered;
			container.PointerExited -= FileListItem_PointerExited;
			container.RightTapped -= FileListItem_RightTapped;

			if (inRecycleQueue)
			{
				UninitializeDrag(container);
			}
			else
			{
				container.PointerPressed += FileListItem_PointerPressed;
				container.RightTapped += FileListItem_RightTapped;
				if (UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				{
					container.PointerEntered += FileListItem_PointerEntered;
					container.PointerExited += FileListItem_PointerExited;
				}
			}
		}

		private void RefreshItem(SelectorItem container, object item, bool inRecycleQueue, ContainerContentChangingEventArgs args)
		{
			if (item is not ListedItem listedItem)
				return;

			if (inRecycleQueue)
			{
				ParentShellPageInstance!.FilesystemViewModel.CancelExtendedPropertiesLoadingForItem(listedItem);
			}
			else
			{
				InitializeDrag(container, listedItem);

				if (!listedItem.ItemPropertiesInitialized)
				{
					uint callbackPhase = 3;
					args.RegisterUpdateCallback(callbackPhase, async (s, c) =>
					{
						await ParentShellPageInstance!.FilesystemViewModel.LoadExtendedItemProperties(listedItem, IconSize);
					});
				}
			}
		}

		protected static void FileListItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is not SelectorItem selectorItem)
				return;

			if (selectorItem.IsSelected && e.KeyModifiers == VirtualKeyModifiers.Control)
			{
				selectorItem.IsSelected = false;

				// Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
				e.Handled = true;
			}
			else if (!selectorItem.IsSelected && e.GetCurrentPoint(selectorItem).Properties.IsLeftButtonPressed)
			{
				selectorItem.IsSelected = true;
			}
		}

		protected internal void FileListItem_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			_hoveredItem = GetItemFromElement(sender);

			_hoverTimer.Stop();
			_hoverTimer.Debounce(() =>
			{
				if (_hoveredItem is null)
					return;

				_hoverTimer.Stop();

				// Selection of multiple individual items with control
				if (e.KeyModifiers == VirtualKeyModifiers.Control &&
					_SelectedItems is not null)
				{
					ItemManipulationModel.AddSelectedItem(_hoveredItem);
				}
				// Selection of a range of items with shift
				else if (e.KeyModifiers == VirtualKeyModifiers.Shift &&
					_SelectedItems is not null &&
					_SelectedItems.Any())
				{
					var last = _SelectedItems.Last();
					byte found = 0;
					for (int i = 0; i < ItemsControl.Items.Count && found != 2; i++)
					{
						if (ItemsControl.Items[i] == last || ItemsControl.Items[i] == _hoveredItem)
							found++;

						if (found != 0 && !_SelectedItems.Contains(ItemsControl.Items[i]))
							ItemManipulationModel.AddSelectedItem((ListedItem)ItemsControl.Items[i]);
					}
				}
				// Avoid resetting the selection if multiple items are selected
				else if (SelectedItems is null || SelectedItems.Count <= 1)
				{
					ItemManipulationModel.SetSelectedItem(_hoveredItem);
				}
			},
			TimeSpan.FromMilliseconds(600), false);
		}

		protected internal void FileListItem_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			_hoverTimer.Stop();
			_hoveredItem = null;
		}

		protected void FileListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var rightClickedItem = GetItemFromElement(sender);

			if (rightClickedItem is not null && !((SelectorItem)sender).IsSelected)
				ItemManipulationModel.SetSelectedItem(rightClickedItem);
		}

		protected void InitializeDrag(UIElement containter, ListedItem item)
		{
			if (item is null)
				return;

			UninitializeDrag(containter);
			if ((item.PrimaryItemAttribute == StorageItemTypes.Folder &&
				!RecycleBinHelpers.IsPathUnderRecycleBin(item.ItemPath)) ||
				item.IsExecutable)
			{
				containter.AllowDrop = true;
				containter.DragOver += Item_DragOver;
				containter.DragLeave += Item_DragLeave;
				containter.Drop += Item_Drop;
			}
		}

		protected void UninitializeDrag(UIElement element)
		{
			element.AllowDrop = false;
			element.DragOver -= Item_DragOver;
			element.DragLeave -= Item_DragLeave;
			element.Drop -= Item_Drop;
		}

		public virtual void Dispose()
		{
			PreviewPaneViewModel?.Dispose();
			UnhookBaseEvents();
		}

		protected void ItemsLayout_DragOver(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DragOverCommand?.Execute(e);
		}

		protected void ItemsLayout_Drop(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DropCommand?.Execute(e);
		}

		private void UpdateCollectionViewSource()
		{
			if (ParentShellPageInstance is null)
				return;

			if (ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.IsGrouped)
			{
				CollectionViewSource = new()
				{
					IsSourceGrouped = true,
					Source = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.GroupedCollection
				};
			}
			else
			{
				CollectionViewSource = new()
				{
					IsSourceGrouped = false,
					Source = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders
				};
			}
		}

		protected void SemanticZoom_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
		{
			if (e.IsSourceZoomedInView)
				return;

			// According to the docs this isn't necessary, but it would crash otherwise
			var destination = e.DestinationItem.Item as GroupedCollection<ListedItem>;

			e.DestinationItem.Item = destination?.FirstOrDefault();
		}

		protected void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "PointerOver", true);
		}

		protected void StackPanel_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "Normal", true);
		}

		protected void RootPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "Pressed", true);
		}

		private void ItemManipulationModel_RefreshItemsOpacityInvoked(object? sender, EventArgs e)
		{
			var items = GetAllItems();
			if (items is null)
				return;

			foreach (ListedItem listedItem in items)
			{
				if (listedItem.IsHiddenItem)
					listedItem.Opacity = Constants.UI.DimItemOpacity;
				else
					listedItem.Opacity = 1;
			}
		}

		private void View_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			if (ParentShellPageInstance is not null)
				ParentShellPageInstance.ToolbarViewModel.HasItem = CollectionViewSource.View.Any();
		}

		virtual public void StartRenameItem()
		{
		}

		public void CheckRenameDoubleClick(object clickedItem)
		{
			if (clickedItem is ListedItem item)
			{
				if (item == _preRenamingItem)
				{
					_tapDebounceTimer.Debounce(() =>
					{
						if (item == _preRenamingItem)
						{
							StartRenameItem();
							_tapDebounceTimer.Stop();
						}
					},
					TimeSpan.FromMilliseconds(500));
				}
				else
				{
					_tapDebounceTimer.Stop();
					_preRenamingItem = item;
				}
			}
			else
			{
				ResetRenameDoubleClick();
			}
		}

		public void ResetRenameDoubleClick()
		{
			_preRenamingItem = null;
			_tapDebounceTimer.Stop();
		}

		protected async Task ValidateItemNameInputText(TextBox textBox, TextBoxBeforeTextChangingEventArgs args, Action<bool> showError)
		{
			if (FilesystemHelpers.ContainsRestrictedCharacters(args.NewText))
			{
				args.Cancel = true;

				await DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					var oldSelection = textBox.SelectionStart + textBox.SelectionLength;
					var oldText = textBox.Text;
					textBox.Text = FilesystemHelpers.FilterRestrictedCharacters(args.NewText);
					textBox.SelectionStart = oldSelection + textBox.Text.Length - oldText.Length;
					showError?.Invoke(true);
				});
			}
			else
			{
				showError?.Invoke(false);
			}
		}

		protected void UpdatePreviewPaneSelection(List<ListedItem>? value)
		{
			if (LockPreviewPaneContent || value is null)
				return;

			if (value.FirstOrDefault() != PreviewPaneViewModel.SelectedItem)
			{
				// Update preview pane properties
				PreviewPaneViewModel.IsItemSelected = value.Count > 0;

				if (value.Count == 1)
					PreviewPaneViewModel.SelectedItem = value.First();

				// Check if the preview pane is open before updating the model
				if (PreviewPaneViewModel.IsEnabled)
				{
					var isPaneEnabled = ((App.Window.Content as Frame)?.Content as MainPage)?.ShouldPreviewPaneBeActive ?? false;
					if (isPaneEnabled)
						_ = PreviewPaneViewModel.UpdateSelectedItemPreview();
				}
			}
		}

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private class ContextMenuExtensions : DependencyObject
		{
			public static ItemsControl GetItemsControl(DependencyObject obj)
			{
				return (ItemsControl)obj.GetValue(ItemsControlProperty);
			}

			public static void SetItemsControl(DependencyObject obj, ItemsControl value)
			{
				obj.SetValue(ItemsControlProperty, value);
			}

			public static readonly DependencyProperty ItemsControlProperty =
				DependencyProperty.RegisterAttached(
					nameof(ItemsControl),
					typeof(ItemsControl),
					typeof(ContextMenuExtensions),
					new PropertyMetadata(null));
		}
	}
}
