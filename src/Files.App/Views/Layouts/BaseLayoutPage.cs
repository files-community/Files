// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.Helpers.ContextFlyouts;
using Files.App.UserControls.Menus;
using Files.App.ViewModels.Layouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using static Files.App.Helpers.PathNormalization;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using SortDirection = Files.App.Data.Enums.SortDirection;
using VanaraWindowsShell = Vanara.Windows.Shell;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the base class which every layout page must derive from
	/// </summary>
	public abstract class BaseLayoutPage : Page, IBaseLayoutPage, INotifyPropertyChanged
	{
		// Dependency injections

		protected IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected ILayoutSettingsService LayoutSettingsService { get; } = Ioc.Default.GetRequiredService<ILayoutSettingsService>();
		protected ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		public InfoPaneViewModel InfoPaneViewModel { get; } = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		protected readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();
		protected readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		// ViewModels

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }
		public StatusBarViewModel StatusBarViewModel { get; }
		public ItemManipulationModel ItemManipulationModel { get; private set; }
		public BaseLayoutViewModel? CommandsViewModel { get; protected set; }

		// Fields

		private readonly DispatcherQueueTimer jumpTimer;
		private readonly DispatcherQueueTimer dragOverTimer;
		private readonly DispatcherQueueTimer tapDebounceTimer;
		private readonly DispatcherQueueTimer hoverTimer;

		private readonly DragEventHandler Item_DragOverEventHandler;
		public event PropertyChangedEventHandler? PropertyChanged;

		protected NavigationArguments? navigationArguments;

		private CancellationTokenSource? shellContextMenuItemCancellationToken;
		private CancellationTokenSource? groupingCancellationToken;

		private bool shiftPressed;
		private bool itemDragging;

		protected bool isDraggingSelectionRectangle;

		private ListedItem? dragOverItem = null;
		private ListedItem? hoveredItem = null;
		private ListedItem? preRenamingItem = null;

		// Properties

		protected NavigationToolbar? NavToolbar
			=> (MainWindow.Instance.Content as Frame)?.FindDescendant<NavigationToolbar>();

		public LayoutPreferencesManager? FolderSettings
			=> ParentShellPageInstance?.InstanceViewModel.FolderSettings;

		public CurrentInstanceViewModel? InstanceViewModel
			=> ParentShellPageInstance?.InstanceViewModel;

		public static AppModel AppModel
			=> App.AppModel;

		public bool AllowItemDrag
			=> WindowContext.CanDragAndDrop;

		public CommandBarFlyout ItemContextMenuFlyout { get; set; } = new()
		{
			AlwaysExpanded = true,
			AreOpenCloseAnimationsEnabled = false,
			Placement = FlyoutPlacementMode.Right,
		};

		public CommandBarFlyout BaseContextMenuFlyout { get; set; } = new()
		{
			AlwaysExpanded = true,
			AreOpenCloseAnimationsEnabled = false,
			Placement = FlyoutPlacementMode.Right,
		};

		protected abstract ItemsControl ItemsControl { get; }

		public IShellPage? ParentShellPageInstance { get; private set; }

		public bool IsRenamingItem { get; set; }
		public bool LockPreviewPaneContent { get; set; }

		public ListedItem? RenamingItem { get; set; }
		public ListedItem? SelectedItem { get; private set; }

		public string? OldItemName { get; set; }

		private bool isMiddleClickToScrollEnabled = true;
		public bool IsMiddleClickToScrollEnabled
		{
			get => isMiddleClickToScrollEnabled;
			set
			{
				if (isMiddleClickToScrollEnabled != value)
				{
					isMiddleClickToScrollEnabled = value;

					NotifyPropertyChanged(nameof(IsMiddleClickToScrollEnabled));
				}
			}
		}

		private CollectionViewSource collectionViewSource = new()
		{
			IsSourceGrouped = true,
		};
		public CollectionViewSource CollectionViewSource
		{
			get => collectionViewSource;
			set
			{
				if (collectionViewSource == value)
					return;

				if (collectionViewSource.View is not null)
					collectionViewSource.View.VectorChanged -= View_VectorChanged;

				collectionViewSource = value;

				NotifyPropertyChanged(nameof(CollectionViewSource));

				if (collectionViewSource.View is not null)
					collectionViewSource.View.VectorChanged += View_VectorChanged;
			}
		}

		private bool isItemSelected = false;
		public bool IsItemSelected
		{
			get => isItemSelected;
			internal set
			{
				if (value != isItemSelected)
				{
					isItemSelected = value;

					NotifyPropertyChanged(nameof(IsItemSelected));
				}
			}
		}

		private string jumpString = string.Empty;
		public string JumpString
		{
			get => jumpString;
			set
			{
				// If current string is "a", and the next character typed is "a",
				// search for next file that starts with "a" (a.k.a. _jumpString = "a")
				if (jumpString.Length == 1 && value == jumpString + jumpString)
					value = jumpString;
				if (value != string.Empty)
				{
					var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
					ListedItem? jumpedToItem = null;
					ListedItem? previouslySelectedItem = IsItemSelected ? SelectedItem : null;

					// Select first matching item after currently selected item
					if (previouslySelectedItem is not null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<ListedItem> candidateItems = shellViewModel.FilesAndFolders.ToList()
							.SkipWhile(x => x != previouslySelectedItem)
							.Skip(value.Length == 1 ? 1 : 0) // User is trying to cycle through items starting with the same letter
							.Where(f =>
							{
								var name = f.Name ?? throw new InvalidOperationException("A listed item does not have a name.");
								return name.Length >= value.Length && string.Equals(name.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase);
							});
						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<ListedItem> candidateItems = shellViewModel.FilesAndFolders.ToList()
							.Where(f =>
							{
								var name = f.Name ?? throw new InvalidOperationException("A listed item does not have a name.");
								return name.Length >= value.Length && string.Equals(name.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase);
							});
						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is not null)
					{
						ItemManipulationModel.SetSelectedItem(jumpedToItem);
						ItemManipulationModel.ScrollIntoView(jumpedToItem);
						ItemManipulationModel.FocusSelectedItems();
					}

					// Restart the timer
					jumpTimer.Start();
				}

				jumpString = value;
			}
		}

		private bool isSelectedItemsSorted = false;
		private List<ListedItem>? selectedItems = [];
		public List<ListedItem> SelectedItems
		{
			get
			{
				var currentItems = selectedItems
					?? throw new InvalidOperationException("The selected items collection has not been initialized.");
				if (!isSelectedItemsSorted)
				{
					var folderSettings = FolderSettings
						?? throw new InvalidOperationException("The layout does not have folder settings.");
					var orderedItems = SortingHelper.OrderFileList(currentItems, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles, folderSettings.SortFilesFirst).ToList();
					selectedItems = orderedItems;
					currentItems = orderedItems;
					isSelectedItemsSorted = true;
				}

				return SelectedItem is null || !currentItems.Contains(SelectedItem)
					? currentItems
					: currentItems
						.SkipWhile(x => x != SelectedItem)
						.Concat(currentItems.TakeWhile(x => x != SelectedItem))
						.ToList();
			}
			internal set
			{
				if (value != selectedItems)
				{
					isSelectedItemsSorted = false;
					selectedItems = value;
					var currentItems = value;

					if (currentItems.Count == 0)
					{
						IsItemSelected = false;
						SelectedItem = null;
						SelectedItemsPropertiesViewModel.IsItemSelected = false;

						ResetRenameDoubleClick();
						UpdateSelectionSize();
					}
					else
					{
						IsItemSelected = true;
						SelectedItem = currentItems.First();
						SelectedItemsPropertiesViewModel.IsItemSelected = true;

						UpdateSelectionSize();

						SelectedItemsPropertiesViewModel.SelectedItemsCount = currentItems.Count;
						SelectedItemsPropertiesViewModel.SelectedItemsCountString = Strings.SelectedItems.GetLocalizedFormatResource(currentItems.Count);

						if (currentItems.Count == 1)
						{
							DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								// Tapped event must be executed first
								await Task.Delay(50);
								preRenamingItem = SelectedItem;
							});
						}
						else
							ResetRenameDoubleClick();
					}

					NotifyPropertyChanged(nameof(SelectedItems));
				}
				if (!isDraggingSelectionRectangle)
				{
					var parentShellPage = ParentShellPageInstance
						?? throw new InvalidOperationException("The layout does not have a parent shell page.");
					parentShellPage.ToolbarViewModel.SelectedItems = value;
				}
			}
		}

		protected void FlushSelectionToToolbar()
		{
			if (ParentShellPageInstance is not null)
				ParentShellPageInstance.ToolbarViewModel.SelectedItems = selectedItems;
		}

		// Constructor

		public BaseLayoutPage()
		{
			ItemManipulationModel = new ItemManipulationModel();

			HookBaseEvents();
			HookEvents();

			jumpTimer = DispatcherQueue.CreateTimer();
			jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
			jumpTimer.Tick += JumpTimer_Tick;

			Item_DragOverEventHandler = new DragEventHandler(Item_DragOver);

			SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel();
			StatusBarViewModel = new StatusBarViewModel();

			dragOverTimer = DispatcherQueue.CreateTimer();
			tapDebounceTimer = DispatcherQueue.CreateTimer();
			hoverTimer = DispatcherQueue.CreateTimer();
		}

		// Abstract methods

		protected abstract void HookEvents();
		protected abstract void UnhookEvents();
		protected abstract void InitializeCommandsViewModel();
		protected abstract bool CanGetItemFromElement(object element);

		// Methods

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
			jumpString = string.Empty;
			jumpTimer.Stop();
		}

		protected IEnumerable<ListedItem> GetAllItems()
		{
			var items = CollectionViewSource.IsSourceGrouped
				? (CollectionViewSource.Source as BulkConcurrentObservableCollection<GroupedCollection<ListedItem>>)?.SelectMany(g => g) // add all items from each group to the new list
				: CollectionViewSource.Source as IEnumerable<ListedItem>;

			return items ?? new List<ListedItem>();
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

		protected ListedItem? GetItemFromElement(object element)
		{
			if (element is not ContentControl item || !CanGetItemFromElement(element))
				return null;

			return (item.DataContext as ListedItem) ?? (item.Content as ListedItem) ?? (ItemsControl.ItemFromContainer(item) as ListedItem);
		}

		protected virtual void BaseFolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			if (ParentShellPageInstance is { SlimContentPage: not null } parentShellPage)
			{
				var shellViewModel = parentShellPage.GetRequiredShellViewModel();
				var folderSettings = parentShellPage.InstanceViewModel.FolderSettings;
				var workingDirectory = shellViewModel.WorkingDirectory
					?? throw new InvalidOperationException("The shell page does not have a working directory.");
				var layoutType = folderSettings.GetLayoutType(workingDirectory);

				if (layoutType != parentShellPage.CurrentPageType)
				{
					var args = navigationArguments
						?? throw new InvalidOperationException("The layout navigation arguments are not available.");
					folderSettings.PendingLayoutSwitchSelection = SelectedItems.Select(item => item.ItemNameRaw!).ToList();

					parentShellPage.NavigateWithArguments(layoutType, new NavigationArguments()
					{
						NavPathParam = args.NavPathParam,
						IsSearchResultPage = args.IsSearchResultPage,
						SearchPathParam = args.SearchPathParam,
						SearchQuery = args.SearchQuery,
						IsLayoutSwitch = true,
						AssociatedTabInstance = parentShellPage
					});

					// Remove old layout from back stack
					parentShellPage.RemoveLastPageFromBackStack();
					parentShellPage.ResetNavigationStackLayoutMode();
				}

				shellViewModel.UpdateEmptyTextType();
				shellViewModel.UpdateNetworkAvailabilityInfoBar();

				// Focus on the active pane in case it was lost during the layout switch.
				// Allthough the focus is also set from SetSelectedItemsOnNavigation,
				// that is only called when switching between a Grid based layout and Details,
				// not between different Grid based layouts (eg. List and Cards).
				// Adaptive layout fires this handler on folder-load completion - skip the focus
				// restore so an in-progress omnibar query isn't lost.
				if (!UIHelpers.IsTextInputFocused(XamlRoot))
				{
					var paneHolder = parentShellPage.GetRequiredPaneHolder();
					paneHolder.FocusActivePane();
				}
			}
		}

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			// Add item jumping handler
			CharacterReceived += Page_CharacterReceived;

			if (e.Parameter is not NavigationArguments { AssociatedTabInstance: { } parentShellPage } args ||
				parentShellPage.ShellViewModel is not { } shellViewModel)
			{
				throw new InvalidOperationException("Layout navigation requires an initialized shell page.");
			}

			navigationArguments = args;
			ParentShellPageInstance = parentShellPage;
			var folderSettings = parentShellPage.InstanceViewModel.FolderSettings;

			// Git properties are not loaded by default
			shellViewModel.EnabledGitProperties = GitProperties.None;

			InitializeCommandsViewModel();

			IsItemSelected = false;

			folderSettings.LayoutModeChangeRequested += BaseFolderSettings_LayoutModeChangeRequested;
			folderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
			folderSettings.GroupDirectionPreferenceUpdated += FolderSettings_GroupDirectionPreferenceUpdated;
			folderSettings.GroupByDateUnitPreferenceUpdated += FolderSettings_GroupByDateUnitPreferenceUpdated;

			shellViewModel.EmptyTextType = EmptyTextType.None;
			parentShellPage.ToolbarViewModel.CanRefresh = true;

			if (!args.IsSearchResultPage)
			{
				var navigationPath = args.NavPathParam;
				var previousDir = shellViewModel.WorkingDirectory;
				await shellViewModel.SetWorkingDirectoryAsync(navigationPath);

				// pathRoot will be empty on recycle bin path
				var workingDir = shellViewModel.WorkingDirectory ?? string.Empty;
				var pathRoot = GetPathRoot(workingDir);

				var isRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				parentShellPage.InstanceViewModel.IsPageTypeRecycleBin = isRecycleBin;

				// Can't go up from recycle bin
				parentShellPage.ToolbarViewModel.CanNavigateToParent = !(string.IsNullOrEmpty(pathRoot) || isRecycleBin);

				parentShellPage.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				parentShellPage.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				parentShellPage.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				parentShellPage.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				parentShellPage.InstanceViewModel.IsPageTypeSearchResults = false;
				parentShellPage.InstanceViewModel.IsPageTypeReleaseNotes = false;
				parentShellPage.InstanceViewModel.IsPageTypeSettings = false;
				parentShellPage.ToolbarViewModel.PathControlDisplayText = navigationPath;

				if (folderSettings.DirectorySortOption == SortOption.Path)
					folderSettings.DirectorySortOption = SortOption.Name;

				if (folderSettings.DirectoryGroupOption == GroupOption.FolderPath &&
					!parentShellPage.InstanceViewModel.IsPageTypeLibrary)
					folderSettings.DirectoryGroupOption = GroupOption.None;

				if (!args.IsLayoutSwitch || previousDir != workingDir)
					shellViewModel.RefreshItems(previousDir, SetSelectedItemsOnNavigation);
				else
					parentShellPage.ToolbarViewModel.CanGoForward = false;
			}
			else
			{
				var searchPath = args.SearchPathParam;
				await shellViewModel.SetWorkingDirectoryAsync(searchPath);

				parentShellPage.ToolbarViewModel.CanGoForward = false;

				// Impose no artificial restrictions on back navigation. Even in a search results page.
				parentShellPage.ToolbarViewModel.CanGoBack = true;

				parentShellPage.ToolbarViewModel.CanNavigateToParent = false;

				var workingDir = shellViewModel.WorkingDirectory ?? string.Empty;

				parentShellPage.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				parentShellPage.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				parentShellPage.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				parentShellPage.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				parentShellPage.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				parentShellPage.InstanceViewModel.IsPageTypeSearchResults = true;
				parentShellPage.InstanceViewModel.IsPageTypeReleaseNotes = false;
				parentShellPage.InstanceViewModel.IsPageTypeSettings = false;

				if (!args.IsLayoutSwitch)
				{
					var displayName = App.LibraryManager.TryGetLibrary(searchPath, out var lib) ? lib.Text : searchPath;
					await parentShellPage.UpdatePathUIToWorkingDirectoryAsync(null, string.Format(Strings.SearchPagePathBoxOverrideText.GetLocalizedResource(), args.SearchQuery, displayName));
					var searchInstance = new Utils.Storage.FolderSearch
					{
						Query = args.SearchQuery,
						Folder = searchPath,
					};

					_ = shellViewModel.SearchAsync(searchInstance);
				}
			}

			// Show controls that were hidden on the home page
			parentShellPage.InstanceViewModel.IsPageTypeNotHome = true;
			shellViewModel.UpdateGroupOptions();

			UpdateCollectionViewSource();
			folderSettings.IsLayoutModeChanging = false;

			SetSelectedItemsOnNavigation();

			ItemContextMenuFlyout.Opening += ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening += BaseContextFlyout_Opening;
		}

		public async void SetSelectedItemsOnNavigation()
		{
			try
			{
				// Consume synchronously so a concurrent navigation can't capture the pending value
				IEnumerable<string>? layoutSwitchSelection = null;
				if (navigationArguments is not null && navigationArguments.IsLayoutSwitch && FolderSettings is not null)
				{
					layoutSwitchSelection = FolderSettings.PendingLayoutSwitchSelection;
					FolderSettings.PendingLayoutSwitchSelection = null;
				}

				// Delay to ensure the new layout is loaded
				if (navigationArguments is not null && navigationArguments.IsLayoutSwitch)
					await Task.Delay(100);

				var itemsToSelect = layoutSwitchSelection ?? navigationArguments?.SelectItems;

				if (navigationArguments is not null &&
					itemsToSelect is not null &&
					itemsToSelect.Any())
				{
					if (ParentShellPageInstance?.ShellViewModel is not { } shellViewModel)
						return;

					List<ListedItem> listedItemsToSelect =
					[
						.. shellViewModel.FilesAndFolders.ToList().Where((li) => itemsToSelect.Contains(li.ItemNameRaw)),
					];

					ItemManipulationModel.SetSelectedItems(listedItemsToSelect);

					// Invoked as a post-load callback that can fire long after navigation if the folder
					// is slow to enumerate; by then the user may be typing into the omnibar - don't yank
					// focus away. The omnibar also cancels programmatic LosingFocus moves, but its
					// FocusSelectedItems path uses FocusState.Keyboard which slips past that guard.
					if (!UIHelpers.IsTextInputFocused(XamlRoot))
						ItemManipulationModel.FocusSelectedItems();
				}
				else if (navigationArguments is not null &&
					ParentShellPageInstance is { } parentShellPage &&
					parentShellPage.InstanceViewModel.FolderSettings.LayoutMode is not FolderLayoutModes.ColumnView)
				{
					if (!UIHelpers.IsTextInputFocused(XamlRoot))
						parentShellPage.PaneHolder?.FocusActivePane();
				}
			}
			catch (Exception) { }
		}

		private async void FolderSettings_GroupOptionPreferenceUpdated(object? sender, GroupOption e)
		{
			await GroupPreferenceUpdatedAsync();
		}

		private async void FolderSettings_GroupDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			await GroupPreferenceUpdatedAsync();
		}

		private async void FolderSettings_GroupByDateUnitPreferenceUpdated(object? sender, GroupByDateUnit e)
		{
			await GroupPreferenceUpdatedAsync();
		}

		private async Task GroupPreferenceUpdatedAsync()
		{
			var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();

			// Two or more of these running at the same time will cause a crash, so cancel the previous one before beginning
			groupingCancellationToken?.Cancel();
			groupingCancellationToken = new CancellationTokenSource();
			var token = groupingCancellationToken.Token;

			await shellViewModel.GroupOptionsUpdatedAsync(token);

			UpdateCollectionViewSource();

			await shellViewModel.ReloadItemGroupHeaderImagesAsync();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			// Remove item jumping handler
			CharacterReceived -= Page_CharacterReceived;
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The layout does not have folder settings.");
			folderSettings.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
			folderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
			folderSettings.GroupDirectionPreferenceUpdated -= FolderSettings_GroupDirectionPreferenceUpdated;
			folderSettings.GroupByDateUnitPreferenceUpdated -= FolderSettings_GroupByDateUnitPreferenceUpdated;
			ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

			var parameter = e.Parameter as NavigationArguments;
			if (parameter is not null && !parameter.IsLayoutSwitch)
			{
				var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
				shellViewModel.CancelLoadAndClearFiles();
			}
		}

		private async void ItemContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				var parentShellPage = ParentShellPageInstance
					?? throw new InvalidOperationException("The layout does not have a parent shell page.");
				var shellViewModel = parentShellPage.GetRequiredShellViewModel();
				var commandsViewModel = CommandsViewModel
					?? throw new InvalidOperationException("The layout commands are not initialized.");

				var instanceViewModel = parentShellPage.InstanceViewModel;
				if (!parentShellPage.IsCurrentInstance || !parentShellPage.IsCurrentPane)
				{
					// Wait until the pane and column become current
					await Task.WhenAny(parentShellPage.WhenIsCurrent(), Task.Delay(500));
					// Wait a little longer to ensure the page context is updated
					await Task.Delay(10);
				}

				// Workaround for item sometimes not getting selected
				if (!IsItemSelected && (sender as CommandBarFlyout)?.Target is ListViewItem { Content: ListedItem li })
					ItemManipulationModel.SetSelectedItem(li);

				if (IsItemSelected)
				{
					// Reset menu max height
					if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
						itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

					shellContextMenuItemCancellationToken?.Cancel();
					shellContextMenuItemCancellationToken = new CancellationTokenSource();
					SelectedItemsPropertiesViewModel.CheckAllFileExtensions(SelectedItems.Select(selectedItem => selectedItem?.FileExtension).ToList());

					shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
					var items = ContentPageContextFlyoutFactory.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: instanceViewModel, selectedItems: SelectedItems, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: commandsViewModel, shiftPressed: shiftPressed, itemViewModel: null);

					ItemContextMenuFlyout.PrimaryCommands.Clear();
					ItemContextMenuFlyout.SecondaryCommands.Clear();

					var (primaryElements, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(items);
					AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);
					primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
					secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
					secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

					if (instanceViewModel.CanTagFilesInPage)
						AddNewFileTagsToMenu(ItemContextMenuFlyout);

					if (!instanceViewModel.IsPageTypeZipFolder && !instanceViewModel.IsPageTypeFtp)
					{
						var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(workingDir: shellViewModel.WorkingDirectory, selectedItems: SelectedItems, shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
						if (shellMenuItems.Any())
							await AddShellMenuItemsAsync(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
						else
							RemoveOverflow(ItemContextMenuFlyout);
					}
					else
					{
						RemoveOverflow(ItemContextMenuFlyout);
					}
				}
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		private async void BaseContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				var parentShellPage = ParentShellPageInstance
					?? throw new InvalidOperationException("The layout does not have a parent shell page.");
				var shellViewModel = parentShellPage.GetRequiredShellViewModel();
				var commandsViewModel = CommandsViewModel
					?? throw new InvalidOperationException("The layout commands are not initialized.");

				var instanceViewModel = parentShellPage.InstanceViewModel;
				if (!parentShellPage.IsCurrentInstance || !parentShellPage.IsCurrentPane)
				{
					// Wait until the pane and column become current
					await Task.WhenAny(parentShellPage.WhenIsCurrent(), Task.Delay(500));
					// Wait a little longer to ensure the page context is updated
					await Task.Delay(10);
				}

				ItemManipulationModel.ClearSelection();

				// Reset menu max height
				if (BaseContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
					itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

				shellContextMenuItemCancellationToken?.Cancel();
				shellContextMenuItemCancellationToken = new CancellationTokenSource();

				shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				var currentFolder = shellViewModel.CurrentFolder
					?? throw new InvalidOperationException("The current folder is not available.");
				List<ListedItem> contextItems = [currentFolder];
				var items = ContentPageContextFlyoutFactory.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: instanceViewModel, selectedItems: contextItems, commandsViewModel: commandsViewModel, shiftPressed: shiftPressed, itemViewModel: shellViewModel, selectedItemsPropertiesViewModel: null);

				BaseContextMenuFlyout.PrimaryCommands.Clear();
				BaseContextMenuFlyout.SecondaryCommands.Clear();

				var (primaryElements, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(items);

				AddCloseHandler(BaseContextMenuFlyout, primaryElements, secondaryElements);

				primaryElements.ForEach(i => BaseContextMenuFlyout.PrimaryCommands.Add(i));

				// Set menu min width
				secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);
				secondaryElements.ForEach(i => BaseContextMenuFlyout.SecondaryCommands.Add(i));

				if (!instanceViewModel.IsPageTypeSearchResults && !instanceViewModel.IsPageTypeZipFolder && !instanceViewModel.IsPageTypeFtp)
				{
					var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(workingDir: shellViewModel.WorkingDirectory, selectedItems: [], shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
					if (shellMenuItems.Any())
						await AddShellMenuItemsAsync(shellMenuItems, BaseContextMenuFlyout, shiftPressed);
					else
						RemoveOverflow(BaseContextMenuFlyout);
				}
				else
				{
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
			var items = (selectedItems?.Any() ?? false) ? selectedItems : SafetyExtensions.IgnoreExceptions(GetAllItems, App.Logger);
			if (items is null)
				return;

			var isSizeKnown = !items.Any(item => string.IsNullOrEmpty(item.FileSize));
			if (isSizeKnown)
			{
				decimal size = items.Sum(item => item.FileSizeBytes);
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
				Label = Strings.EditTags.GetLocalizedResource(),
				Content = new ThemedIcon()
				{
					Style = (Style)Application.Current.Resources["App.ThemedIcons.TagEdit"],
				},
				Flyout = fileTagsContextMenu
			});

			fileTagsContextMenu.TagsChanged += RequireTagGroupsUpdate;
			fileTagsContextMenu.Closed += HandleClosed;

			async void RequireTagGroupsUpdate(object? sender, EventArgs e)
			{
				if (ParentShellPageInstance is not null)
				{
					var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
					await shellViewModel.RefreshTagGroups();
				}
			}

			void HandleClosed(object? sender, object e)
			{
				fileTagsContextMenu.TagsChanged -= RequireTagGroupsUpdate;
				fileTagsContextMenu.Closed -= HandleClosed;
			}
		}

		private async Task AddShellMenuItemsAsync(List<ContextMenuFlyoutItemViewModel> shellMenuItems, CommandBarFlyout contextMenuFlyout, bool shiftPressed)
		{
			var openWithMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
			var sendToMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
			var turnOnBitLockerMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem menuItem && menuItem.CommandString is not null && menuItem.CommandString.StartsWith("encrypt-bde"));
			var manageBitLockerMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "manage-bde" });
			var shellMenuItemsFiltered = shellMenuItems.Where(x => x != openWithMenuItem && x != sendToMenuItem && x != turnOnBitLockerMenuItem && x != manageBitLockerMenuItem).ToList();
			var mainShellMenuItems = shellMenuItemsFiltered.RemoveFrom(!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 0);
			var overflowShellMenuItemsUnfiltered = shellMenuItemsFiltered.Except(mainShellMenuItems).ToList();
			var overflowShellMenuItems = overflowShellMenuItemsUnfiltered.Where(
				(x, i) => (x.ItemType == ContextMenuFlyoutItemType.Separator &&
				overflowShellMenuItemsUnfiltered[i + 1 < overflowShellMenuItemsUnfiltered.Count ? i + 1 : i].ItemType != ContextMenuFlyoutItemType.Separator)
				|| x.ItemType != ContextMenuFlyoutItemType.Separator).ToList();

			var subMenuLoadTasks = mainShellMenuItems.Concat(overflowShellMenuItems)
				.Where(x => x.LoadSubMenuAction is not null)
				.Select(x => x.LoadSubMenuAction!());
			await Task.WhenAll(subMenuLoadTasks);

			var overflowItems = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(overflowShellMenuItems)!;
			var mainItems = ContextFlyoutModelToElementHelper.GetAppBarButtonsFromModelIgnorePrimary(mainShellMenuItems);

			var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(MainWindow.Instance);
			var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

			var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
			if (itemsControl is not null && secondaryMenu is not null)
			{
				contextMenuFlyout.SetValue(ContextMenuExtensions.ItemsControlProperty, itemsControl);

				var ttv = secondaryMenu.TransformToVisual(MainWindow.Instance.Content);
				var cMenuPos = ttv.TransformPoint(new Point(0, 0));

				var requiredHeight = contextMenuFlyout.SecondaryCommands.Concat(mainItems).Count(x => x is not AppBarSeparator) * Constants.UI.ContextMenuSecondaryItemsHeight;
				var availableHeight = MainWindow.Instance.Bounds.Height - cMenuPos.Y - Constants.UI.ContextMenuPrimaryItemsHeight;

				// Set menu max height to current height (Avoid menu repositioning)
				if (requiredHeight > availableHeight)
					itemsControl.MaxHeight = Math.Min(Constants.UI.ContextMenuMaxHeight, Math.Max(itemsControl.ActualHeight, Math.Min(availableHeight, requiredHeight)));

				// Set items max width to current menu width (#5555)
				mainItems.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin);
			}

			ContentPageContextFlyoutFactory.SwapPlaceholderWithShellOption(
				contextMenuFlyout,
				"TurnOnBitLockerPlaceholder",
				turnOnBitLockerMenuItem,
				contextMenuFlyout.SecondaryCommands.Count - 2
			);
			ContentPageContextFlyoutFactory.SwapPlaceholderWithShellOption(
				contextMenuFlyout,
				"ManageBitLockerPlaceholder",
				manageBitLockerMenuItem,
				contextMenuFlyout.SecondaryCommands.Count - 2
			);

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
						overflowItem.Label = Strings.ShowMoreOptions.GetLocalizedResource();
						overflowItem.IsEnabled = true;
					}
					else
					{
						overflowItem.Visibility = Visibility.Collapsed;

						// Hide separators at the end of the menu
						while (contextMenuFlyout.SecondaryCommands.LastOrDefault(x => x is UIElement element && element.Visibility is Visibility.Visible) is AppBarSeparator separator)
							separator.Visibility = Visibility.Collapsed;
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
				var openWithSubItems = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(ShellContextFlyoutFactory.GetOpenWithItems(shellMenuItems));

				if (openWithSubItems is not null)
				{
					var flyout = (MenuFlyout)openWithOverflow.Flyout;

					flyout.Items.Clear();

					foreach (var item in openWithSubItems)
						flyout.Items.Add(item);

					openWithOverflow.Flyout = flyout;
					openWith.Visibility = Visibility.Collapsed;
					openWithOverflow.Visibility = Visibility.Visible;

					// TODO delete this when https://github.com/microsoft/microsoft-ui-xaml/issues/9409 is resolved
					openWithOverflow.Content = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.OpenWith"
					}.ToThemedIcon();
					openWithOverflow.Label = Strings.OpenWith.GetLocalizedResource();
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
					var sendToSubItems = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(ShellContextFlyoutFactory.GetSendToItems(shellMenuItems));

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

			// Filter mainShellMenuItems that have a non-null LoadSubMenuAction
			var mainItemsWithSubMenu = mainShellMenuItems.Where(x => x.LoadSubMenuAction is not null);

			var mainSubMenuTasks = mainItemsWithSubMenu.Select(async item =>
			{
				await item.LoadSubMenuAction!();
				ShellContextFlyoutFactory.AddItemsToMainMenu(mainItems, item);
			});

			// Filter overflowShellMenuItems that have a non-null LoadSubMenuAction
			var overflowItemsWithSubMenu = overflowShellMenuItems.Where(x => x.LoadSubMenuAction is not null);

			var overflowSubMenuTasks = overflowItemsWithSubMenu.Select(async item =>
			{
				await item.LoadSubMenuAction!();
				ShellContextFlyoutFactory.AddItemsToOverflowMenu(overflowItem, item);
			});

			itemsControl?.Items.OfType<FrameworkElement>().ForEach(item =>
			{
				// Enable CharacterEllipsis text trimming for menu items
				if (item.FindDescendant("OverflowTextLabel") is TextBlock label)
					label.TextTrimming = TextTrimming.CharacterEllipsis;

				// Close main menu when clicking on subitems (#5508)
				if ((item as AppBarButton)?.Flyout as MenuFlyout is MenuFlyout flyout)
				{
					AddClickHandlers(flyout.Items);

					void AddClickHandlers(IList<MenuFlyoutItemBase> items)
					{
						items.OfType<MenuFlyoutItem>().ForEach(i =>
						{
							i.Click += new RoutedEventHandler((s, e) => contextMenuFlyout.Hide());
						});
						items.OfType<MenuFlyoutSubItem>().ForEach(i =>
						{
							AddClickHandlers(i.Items);
						});
					}
				}
			});

			await Task.WhenAll(mainSubMenuTasks.Concat(overflowSubMenuTasks));
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
			var parentShellPage = ParentShellPageInstance
				?? throw new InvalidOperationException("The layout does not have a parent shell page.");
			if (parentShellPage.IsCurrentInstance)
			{
				char letter = args.Character;
				JumpString += letter.ToString().ToLowerInvariant();
			}
		}

		protected virtual void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			try
			{
				if (FolderSettings is not { } folderSettings ||
					InstanceViewModel is not { } instanceViewModel)
				{
					e.Cancel = true;
					return;
				}

				var itemList = e.Items.OfType<ListedItem>().ToList();
				var firstItem = itemList.FirstOrDefault();
				var sortedItems = SortingHelper.OrderFileList(itemList, folderSettings.DirectorySortOption, folderSettings.DirectorySortDirection, folderSettings.SortDirectoriesAlongsideFiles, folderSettings.SortFilesFirst).ToList();
				var orderedItems = sortedItems.SkipWhile(x => x != firstItem).Concat(sortedItems.TakeWhile(x => x != firstItem)).ToList();

				var shellItemList = SafetyExtensions.IgnoreExceptions(() => orderedItems.Select(item => new VanaraWindowsShell.ShellItem(
					item.GetRequiredPath())).ToArray());
				if (shellItemList?[0].FileSystemPath is not null &&
					!instanceViewModel.IsPageTypeSearchResults)
				{
					var parentShellItem = shellItemList[0].Parent
						?? throw new InvalidOperationException("The dragged shell item does not have a parent.");
					var iddo = parentShellItem.GetChildrenUIObjects<IDataObject>(HWND.NULL, shellItemList);
					shellItemList.ForEach(x => x.Dispose());

					var format = System.Windows.Forms.DataFormats.GetFormat("Shell IDList Array");
					if (iddo.TryGetData<byte[]>((uint)format.Id, out var data))
					{
						var mem = new MemoryStream(data
							?? throw new InvalidOperationException("The shell drag data is empty.")).AsRandomAccessStream();
						e.Data.SetData(format.Name, mem);
					}
				}
				else
				{
					// Only support IStorageItem capable paths
					var storageItemList = orderedItems.Where(x => !(x.IsHiddenItem && x.IsLinkItem && x.IsRecycleBinItem && x.IsShortcut)).Select(x => VirtualStorageItem.FromListedItem(x));
					e.Data.SetStorageItems(storageItemList, false);
				}

				// Set can window to front (#13255)
				MainWindow.Instance.SetCanWindowToFront(false);
				itemDragging = true;
			}
			catch (Exception)
			{
				e.Cancel = true;
			}
		}

		protected virtual void FileList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
		{
			// Set can window to front (#13255)
			itemDragging = false;
			MainWindow.Instance.SetCanWindowToFront(true);
		}

		private void Item_DragLeave(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);

			// Reset dragged over item
			if (item == dragOverItem)
				dragOverItem = null;
		}

		private async void Item_DragOver(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);
			if (item is null)
				return;

			DragOperationDeferral? deferral = null;

			try
			{
				deferral = e.GetDeferral();

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

						if (item.IsExecutable || item.IsScriptFile)
						{
							e.DragUIOverride.Caption = $"{Strings.OpenWith.GetLocalizedResource()} {item.Name}";
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						// Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
						else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format(Strings.LinkToFolderCaptionText.GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
						{
							e.DragUIOverride.Caption = string.Format(Strings.CopyToFolderCaptionText.GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format(Strings.MoveToFolderCaptionText.GetLocalizedResource(), item.Name);
							// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
							e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
						}
						else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
							|| ZipStorageFolder.IsZipPath(item.ItemPath!))
						{
							e.DragUIOverride.Caption = string.Format(Strings.CopyToFolderCaptionText.GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
						{
							e.DragUIOverride.Caption = string.Format(Strings.MoveToFolderCaptionText.GetLocalizedResource(), item.Name);
							// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
							e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
						}
						else
						{
							e.DragUIOverride.Caption = string.Format(Strings.CopyToFolderCaptionText.GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
					}
				}

				if (dragOverItem != item)
				{
					dragOverItem = item;
					dragOverTimer.Stop();

					if (e.AcceptedOperation != DataPackageOperation.None)
					{
						dragOverTimer.Debounce(() =>
						{
							if (dragOverItem is not null && !dragOverItem.IsExecutable)
							{
								dragOverTimer.Stop();
								ItemManipulationModel.SetSelectedItem(dragOverItem);
								dragOverItem = null;
								Commands.OpenItem.ExecuteAsync();
							}
						},
						TimeSpan.FromMilliseconds(Constants.DragAndDrop.HoverToOpenTimespan), false);
					}
				}
			}
			finally
			{
				deferral?.Complete();
			}
		}

		protected virtual async void Item_Drop(object sender, DragEventArgs e)
		{
			var deferral = e.GetDeferral();
			e.Handled = true;

			try
			{
				_ = e.Data.Properties;
				var exists = e.Data.Properties.TryGetValue("Files_ActionBinder", out var val);
				_ = val;
			}
			catch (NullReferenceException)
			{
				// e.Data or e.Data.Properties is null, continue without the property check
			}

			// Reset dragged over item
			dragOverItem = null;
			var item = GetItemFromElement(sender);
			if (item is not null)
			{
				var parentShellPage = ParentShellPageInstance
					?? throw new InvalidOperationException("The layout page does not have a parent shell page.");
				var targetPath = (item as IShortcutItem)?.TargetPath;
				var destination = !string.IsNullOrEmpty(targetPath) ? targetPath : item.GetRequiredPath();
				await parentShellPage.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, destination, false, true, item.IsExecutable, item.IsScriptFile);
			}

			deferral.Complete();
		}

		protected void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			RefreshContainer(args.ItemContainer, args.InRecycleQueue);
			RefreshItem(args.ItemContainer, args.Item, args.InRecycleQueue, args);

			// Set can window to front (#13255)
			itemDragging = false;
			MainWindow.Instance.SetCanWindowToFront(true);
		}

		private void RefreshContainer(SelectorItem container, bool inRecycleQueue)
		{
			container.Loaded -= FileListItem_Loaded;
			container.PointerPressed -= FileListItem_PointerPressed;
			container.PointerEntered -= FileListItem_PointerEntered;
			container.PointerExited -= FileListItem_PointerExited;
			container.Tapped -= FileListItem_Tapped;
			container.DoubleTapped -= FileListItem_DoubleTapped;
			container.RightTapped -= FileListItem_RightTapped;

			if (inRecycleQueue)
			{
				UninitializeDrag(container);
			}
			else
			{
				container.Loaded += FileListItem_Loaded;
				container.PointerPressed += FileListItem_PointerPressed;
				container.PointerEntered += FileListItem_PointerEntered;
				container.PointerExited += FileListItem_PointerExited;
				container.Tapped += FileListItem_Tapped;
				container.DoubleTapped += FileListItem_DoubleTapped;
				container.RightTapped += FileListItem_RightTapped;
			}
		}

		private void RefreshItem(SelectorItem container, object item, bool inRecycleQueue, ContainerContentChangingEventArgs args)
		{
			if (item is not ListedItem listedItem)
				return;

			if (inRecycleQueue)
			{
				UpdateItemToolTip(container, null);
				var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
				shellViewModel.CancelExtendedPropertiesLoadingForItem(listedItem);
			}
			else
			{
				UpdateItemToolTip(container, listedItem.ItemTooltipText);
				InitializeDrag(container, listedItem);

				if (listedItem.PreloadedIconData is not null && listedItem.FileImage is null)
					_ = ApplyPreloadedIconAsync(listedItem);

				if (!listedItem.ItemPropertiesInitialized)
				{
					uint callbackPhase = 3;
					args.RegisterUpdateCallback(callbackPhase, async (s, c) =>
					{
						var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();

						await shellViewModel.LoadExtendedItemPropertiesAsync(listedItem);
						if (shellViewModel.EnabledGitProperties is not GitProperties.None && listedItem is IGitItem gitItem)
							await shellViewModel.LoadGitPropertiesAsync(gitItem);
					});
				}
			}
		}

		private static void UpdateItemToolTip(SelectorItem container, string? tooltipText)
		{
			// Apply the tooltip to both the container and the realized template root so every layout
			// gets the same behavior, even when the DataTemplate is wrapped in a UserControl.
			UpdateItemToolTip(container as FrameworkElement, tooltipText);

			if (container.ContentTemplateRoot is FrameworkElement contentTemplateRoot)
				UpdateItemToolTip(contentTemplateRoot, tooltipText);
		}

		private static void UpdateItemToolTip(FrameworkElement? target, string? tooltipText)
		{
			if (target is null)
				return;

			ToolTipService.SetToolTip(target, tooltipText);
			target.SetValue(ToolTipService.PlacementProperty, PlacementMode.Mouse);
		}

		private void FileListItem_Loaded(object sender, RoutedEventArgs e)
		{
			// Set the initial tooltip before hover starts so WinUI doesn't miss the first dwell.
			if (sender is SelectorItem container && container.Content is ListedItem listedItem)
				UpdateItemToolTip(container, listedItem.ItemTooltipText);
		}

		private static async Task ApplyPreloadedIconAsync(ListedItem item)
		{
			var image = await item.PreloadedIconData.ToBitmapAsync();
			if (image is not null)
				item.FileImage = image;
		}

		protected internal void FileListItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));

			if (sender is not SelectorItem selectorItem)
				return;

			if (selectorItem.IsSelected)
			{
				if (e.KeyModifiers == VirtualKeyModifiers.Control)
				{
					selectorItem.IsSelected = false;

					// Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
					e.Handled = true;
				}
				else
				{
					SelectedItem = GetItemFromElement(sender);
				}
			}
			else if (e.GetCurrentPoint(selectorItem).Properties.IsLeftButtonPressed)
			{
				selectorItem.IsSelected = true;
			}
		}

		protected internal void FileListItem_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// Set can window to front (#13255)
			if (sender is SelectorItem selectorItem && selectorItem.IsSelected)
				MainWindow.Instance.SetCanWindowToFront(false);

			if (sender is SelectorItem tooltipContainer && tooltipContainer.Content is ListedItem listedItem)
				UpdateItemToolTip(tooltipContainer, listedItem.ItemTooltipText);

			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			hoveredItem = GetItemFromElement(sender);

			hoverTimer.Stop();
			hoverTimer.Debounce(() =>
			{
				if (hoveredItem is null)
					return;

				hoverTimer.Stop();

				// Selection of multiple individual items with control
				if (e.KeyModifiers == VirtualKeyModifiers.Control &&
					selectedItems is not null)
				{
					ItemManipulationModel.AddSelectedItem(hoveredItem);
				}
				// Selection of a range of items with shift
				else if (e.KeyModifiers == VirtualKeyModifiers.Shift &&
					selectedItems is not null &&
					selectedItems.Any())
				{
					var last = selectedItems.Last();
					byte found = 0;
					for (int i = 0; i < ItemsControl.Items.Count && found != 2; i++)
					{
						if (ItemsControl.Items[i] == last || ItemsControl.Items[i] == hoveredItem)
							found++;

						if (found != 0 && !selectedItems.Contains(ItemsControl.Items[i]))
							ItemManipulationModel.AddSelectedItem((ListedItem)ItemsControl.Items[i]);
					}
				}
				// Avoid resetting the selection if multiple items are selected
				else if (SelectedItems is null || SelectedItems.Count <= 1)
				{
					ItemManipulationModel.SetSelectedItem(hoveredItem);
				}
			},
			TimeSpan.FromMilliseconds(1000), false);
		}

		protected internal void FileListItem_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			// Set can window to front (#13255)
			if (!itemDragging)
				MainWindow.Instance.SetCanWindowToFront(true);

			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			hoverTimer.Stop();
			hoveredItem = null;
		}

		protected void FileListItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));
		}

		protected void FileListItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));
		}

		protected void FileListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));

			var rightClickedItem = GetItemFromElement(sender);

			if (rightClickedItem is not null && !((SelectorItem)sender).IsSelected)
				ItemManipulationModel.SetSelectedItem(rightClickedItem);
		}

		protected void InitializeDrag(UIElement container, ListedItem item)
		{
			if (item is null)
				return;

			UninitializeDrag(container);
			if ((item.PrimaryItemAttribute == StorageItemTypes.Folder && !StorageTrashBinService.IsUnderTrashBin(item.ItemPath))
				|| item.IsExecutable
				|| item.IsScriptFile)
			{
				container.AllowDrop = true;
				container.AddHandler(UIElement.DragOverEvent, Item_DragOverEventHandler, true);
				container.DragLeave += Item_DragLeave;
				container.Drop += Item_Drop;
			}
		}

		protected void UninitializeDrag(UIElement element)
		{
			element.AllowDrop = false;
			element.RemoveHandler(UIElement.DragOverEvent, Item_DragOverEventHandler);
			element.DragLeave -= Item_DragLeave;
			element.Drop -= Item_Drop;
		}

		public virtual void Dispose()
		{
			UnhookBaseEvents();
		}

		protected void ItemsLayout_DragOver(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DragOverCommand?.Execute(e);
		}

		protected virtual void ItemsLayout_Drop(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DropCommand?.Execute(e);
		}

		private void UpdateCollectionViewSource()
		{
			if (ParentShellPageInstance is not { } parentShellPage)
				return;
			var shellViewModel = parentShellPage.GetRequiredShellViewModel();

			if (shellViewModel.FilesAndFolders.IsGrouped)
			{
				var newSource = new CollectionViewSource()
				{
					IsSourceGrouped = true,
					Source = shellViewModel.FilesAndFolders.GroupedCollection
				};
				CollectionViewSource = newSource;
			}
			else
			{
				ZoomIn();

				var newSource = new CollectionViewSource()
				{
					IsSourceGrouped = false,
					Source = shellViewModel.FilesAndFolders
				};
				CollectionViewSource = newSource;
			}
		}

		protected virtual void ZoomIn()
		{
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

		public void CheckRenameDoubleClick(object? clickedItem)
		{
			if (clickedItem is ListedItem item)
			{
				if (item == preRenamingItem)
				{
					tapDebounceTimer.Debounce(() =>
					{
						if (item == preRenamingItem)
						{
							StartRenameItem();
							tapDebounceTimer.Stop();
						}
					},
					TimeSpan.FromMilliseconds(1500));
				}
				else
				{
					tapDebounceTimer.Stop();
					preRenamingItem = item;
				}
			}
			else
			{
				ResetRenameDoubleClick();
			}
		}

		public void ResetRenameDoubleClick()
		{
			preRenamingItem = null;
			tapDebounceTimer.Stop();
		}

		protected async Task ValidateItemNameInputTextAsync(TextBox textBox, TextBoxBeforeTextChangingEventArgs args, Action<bool> showError)
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

		public sealed class ContextMenuExtensions : DependencyObject
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
				DependencyProperty.RegisterAttached("ItemsControl", typeof(ItemsControl), typeof(ContextMenuExtensions), new PropertyMetadata(null));
		}
	}
}
