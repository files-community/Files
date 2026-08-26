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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using WinRT;
using static Files.App.Helpers.PathNormalization;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using SortDirection = Files.App.Data.Enums.SortDirection;

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

		private DispatcherQueueTimer? jumpTimer;
		private DispatcherQueueTimer? dragOverTimer;
		private DispatcherQueueTimer? tapDebounceTimer;
		private DispatcherQueueTimer? hoverTimer;

		private readonly DragEventHandler Item_DragOverEventHandler;
		public event PropertyChangedEventHandler? PropertyChanged;

		protected NavigationArguments? navigationArguments;

		private CancellationTokenSource? shellContextMenuItemCancellationToken;
		private CancellationTokenSource? groupingCancellationToken;

		private bool shiftPressed;
		private bool itemDragging;
		private bool isDisposed;

		protected bool isDraggingSelectionRectangle;

		private ListedItem? dragOverItem = null;
		private ListedItem? hoveredItem = null;
		private ListedItem? preRenamingItem = null;

		// Page-relative point of the pending context-menu invocation, from ContextRequested (fires for every input,
		// unlike RightTapped which a touch long-press can skip). Invalid for keyboard, which has no pointer point.
		private Point contextInvocationPosition;
		private bool contextInvocationValid;
		private TypedEventHandler<UIElement, ContextRequestedEventArgs>? contextRequestedHandler;

		// Properties

		protected NavigationToolbar? NavToolbar
		{
			[DynamicWindowsRuntimeCast(typeof(Frame))]
			get => (MainWindow.Instance.Content as Frame)?.FindDescendant<NavigationToolbar>();
		}

		public LayoutPreferencesManager? FolderSettings
			=> ParentShellPageInstance?.InstanceViewModel.FolderSettings;

		public CurrentInstanceViewModel? InstanceViewModel
			=> ParentShellPageInstance?.InstanceViewModel;

		public static AppModel AppModel
			=> App.AppModel;

		public bool AllowItemDrag
			=> WindowContext.CanDragAndDrop;

		protected FastContextFlyout ItemContextFlyoutHost { get; } = new();
		protected FastContextFlyout BaseContextFlyoutHost { get; } = new();

		public MenuFlyout ItemContextMenuFlyout => ItemContextFlyoutHost.Flyout;
		public MenuFlyout BaseContextMenuFlyout => BaseContextFlyoutHost.Flyout;

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
					JumpTimer.Start();
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

			Item_DragOverEventHandler = new DragEventHandler(Item_DragOver);

			SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel();
			StatusBarViewModel = new StatusBarViewModel();
		}

		protected void LayoutPage_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var command = CommandsViewModel?.ItemPointerPressedCommand;
			if (command?.CanExecute(e) is true)
				command.Execute(e);
		}

		protected void LayoutPage_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			var command = CommandsViewModel?.PointerWheelChangedCommand;
			if (command?.CanExecute(e) is true)
				command.Execute(e);
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
			jumpTimer?.Stop();
			if (jumpTimer is not null)
			{
				jumpTimer.Tick -= JumpTimer_Tick;
			}
			dragOverTimer?.Stop();
			tapDebounceTimer?.Stop();
			hoverTimer?.Stop();
			jumpTimer = null;
			dragOverTimer = null;
			tapDebounceTimer = null;
			hoverTimer = null;

			shellContextMenuItemCancellationToken?.Cancel();
			shellContextMenuItemCancellationToken?.Dispose();
			shellContextMenuItemCancellationToken = null;

			groupingCancellationToken?.Cancel();
			groupingCancellationToken?.Dispose();
			groupingCancellationToken = null;
		}

		private void JumpTimer_Tick(object sender, object e)
		{
			jumpString = string.Empty;
			jumpTimer?.Stop();
		}

		private DispatcherQueueTimer JumpTimer
		{
			get
			{
				if (jumpTimer is null)
				{
					jumpTimer = DispatcherQueue.CreateTimer();
					jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
					jumpTimer.Tick += JumpTimer_Tick;
				}

				return jumpTimer;
			}
		}

		private DispatcherQueueTimer DragOverTimer => dragOverTimer ??= DispatcherQueue.CreateTimer();
		private DispatcherQueueTimer TapDebounceTimer => tapDebounceTimer ??= DispatcherQueue.CreateTimer();
		private DispatcherQueueTimer HoverTimer => hoverTimer ??= DispatcherQueue.CreateTimer();

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

		[DynamicWindowsRuntimeCast(typeof(ContentControl))]
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

			// On the page so it covers item rows and the empty background; handledEventsToo so it still runs after
			// the built-in flyout handling. The hosts pull the captured point when the menu opens.
			contextRequestedHandler = OnContextRequestedForPlacement;
			AddHandler(UIElement.ContextRequestedEvent, contextRequestedHandler, true);
			ItemContextFlyoutHost.InvocationPointProvider = () => contextInvocationValid ? (this, contextInvocationPosition) : null;
			BaseContextFlyoutHost.InvocationPointProvider = () => contextInvocationValid ? (this, contextInvocationPosition) : null;
		}

		private void OnContextRequestedForPlacement(UIElement sender, ContextRequestedEventArgs e)
		{
			contextInvocationValid = e.TryGetPosition(this, out contextInvocationPosition);
		}

		private async Task<IShellPage> EnsurePageIsCurrentAsync()
		{
			var parentShellPage = ParentShellPageInstance
				?? throw new InvalidOperationException("The layout does not have a parent shell page.");
			if (!parentShellPage.IsCurrentInstance || !parentShellPage.IsCurrentPane)
			{
				// Wait until the pane and column become current, then let the page context update
				await Task.WhenAny(parentShellPage.WhenIsCurrent(), Task.Delay(500));
				await Task.Delay(10);
			}

			return parentShellPage;
		}

		private CancellationToken RenewShellMenuToken()
		{
			shellContextMenuItemCancellationToken?.Cancel();
			shellContextMenuItemCancellationToken = new CancellationTokenSource();
			return shellContextMenuItemCancellationToken.Token;
		}

		[DynamicWindowsRuntimeCast(typeof(MenuFlyout))]
		private async void ItemContextFlyout_Opening(object? sender, object e)
		{
			try
			{
				var parentShellPage = await EnsurePageIsCurrentAsync();
				var shellViewModel = parentShellPage.GetRequiredShellViewModel();
				var commandsViewModel = CommandsViewModel
					?? throw new InvalidOperationException("The layout commands are not initialized.");
				var instanceViewModel = parentShellPage.InstanceViewModel;

				// Workaround for item sometimes not getting selected
				if (!IsItemSelected && (sender as MenuFlyout)?.Target is SelectorItem { Content: ListedItem li })
					ItemManipulationModel.SetSelectedItem(li);

				if (!IsItemSelected)
					return;

				var selectedItems = SelectedItems;
				if (selectedItems is null or { Count: 0 })
					return;

				shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				SelectedItemsPropertiesViewModel.CheckAllFileExtensions(selectedItems.Select(x => x.FileExtension).ToList());

				var items = ContentPageContextFlyoutFactory.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: instanceViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: commandsViewModel, shiftPressed: shiftPressed, itemViewModel: null);
				var host = ItemContextFlyoutHost;
				host.Build(items);

				// Edit tags: a submenu of the available tags (FileTagsContextMenu is a standalone MenuFlyout that
				// can't be nested, so build the tag toggles directly).
				if (instanceViewModel.CanTagFilesInPage && UserSettingsService.GeneralSettingsService.ShowEditTagsMenu)
				{
					host.AddSeparatorIfNeeded();
					host.Items.Add(BuildEditTagsSubItem(selectedItems));
				}

				// Shell extensions. Open with / Send to belong in the MAIN menu (they replace placeholders there);
				// the rest go under a single "Show more options" submenu (Win11) or inline (Win10) per the setting.
				if (!instanceViewModel.IsPageTypeZipFolder && !instanceViewModel.IsPageTypeFtp)
				{
					var token = RenewShellMenuToken();

					// Pre-add "Show more options" (with the synchronously-known built-in overflow items) BEFORE the
					// async shell fetch so its placeholder shows while the extensions load.
					var (moreOptions, moreSeparator) = host.AddShowMoreOptionsIfEnabled(items);

					// Open with / Send to: swap the leaf placeholders for their submenus synchronously (before the
					// menu renders) so the main menu does not reflow when the shell sub-items load - only the submenu
					// contents fill in. Reverted after the fetch if the shell has no such items.
					var openWithSwap = host.SwapLeafForSubMenu("OpenWith", "OpenWithOverflow", Strings.OpenWith.GetLocalizedResource(), "App.ThemedIcons.OpenWith");
					var sendToSwap = UserSettingsService.GeneralSettingsService.ShowSendToMenu
						? host.SwapLeafForSubMenu("SendTo", "SendToOverflow", null, null)
						: null;

					// Place the primary row BEFORE the menu renders so it does not jump on an upward open.
					host.ResolvePlacement();

					var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(
						shellViewModel.WorkingDirectory, selectedItems, shiftPressed, false, token);

					if (token.IsCancellationRequested)
						return;

					var openWithModel = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
					var sendToModel = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });

					// BitLocker: replace the placeholders with whichever entries the shell offers (drives)
					host.ApplyBitLockerModels(shellMenuItems, moreOptions, moreSeparator);

					// Fill the swapped-in submenus off the open path; revert to the leaf form when the shell
					// offers no such entry.
					FastContextFlyout.FillOrRevert(openWithSwap, openWithModel, ShellContextFlyoutFactory.GetOpenWithItems);
					FastContextFlyout.FillOrRevert(sendToSwap, sendToModel, ShellContextFlyoutFactory.GetSendToItems);

					var shellModelsFiltered = shellMenuItems
						.Where(x => x != openWithModel && x != sendToModel)
						.ToList();
					host.AddShellModels(shellModelsFiltered, shiftPressed, moreOptions, moreSeparator);
				}
				else
				{
					host.ResolvePlacement();
				}

				host.FinalizePrimaryRowPosition();
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Style))]
		[DynamicWindowsRuntimeCast(typeof(Geometry))]
		[DynamicWindowsRuntimeCast(typeof(ToggleMenuFlyoutItem))]
		private MenuFlyoutSubItem BuildEditTagsSubItem(List<ListedItem> selected)
		{
			var subItem = new MenuFlyoutSubItem
			{
				Text = Strings.EditTags.GetLocalizedResource(),
			};
			if (App.Current.Resources["App.ThemedIcons.TagEdit"] is Style tagEditIconStyle)
			{
				subItem.Style = App.Current.Resources["MenuFlyoutSubItemWithThemedIconStyle"] as Style;
				MenuFlyoutSubItemCustomProperties.SetThemedIconStyle(subItem, tagEditIconStyle);
			}

			var commonTags = selected
				.Select(x => (IEnumerable<string>)(x?.FileTags ?? []))
				.Aggregate((a, b) => a.Intersect(b))
				.ToHashSet();

			var tagPathData = (string)Application.Current.Resources["App.Theme.PathIcon.FilledTag"];

			foreach (var tag in FileTagsSettingsService.FileTagList)
			{
				var toggle = new ToggleMenuFlyoutItem
				{
					Text = tag.Name,
					Tag = tag,
					IsChecked = commonTags.Contains(tag.Uid),
					Icon = new Microsoft.UI.Xaml.Controls.PathIcon
					{
						Data = (Microsoft.UI.Xaml.Media.Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Microsoft.UI.Xaml.Media.Geometry), tagPathData),
						Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(ColorHelpers.FromHex(tag.Color)),
					},
				};
				toggle.Click += async (s, _) =>
				{
					var toggled = (ToggleMenuFlyoutItem)s;
					var tv = (TagViewModel)toggled.Tag;
					foreach (var it in selected.Where(i => i is not null))
					{
						var existing = it.FileTags ?? [];
						it.FileTags = toggled.IsChecked
							? (existing.Contains(tv.Uid) ? existing : [.. existing, tv.Uid])
							: existing.Where(u => u != tv.Uid).ToArray();
					}
					if (ParentShellPageInstance is { } parentShellPage)
						await parentShellPage.GetRequiredShellViewModel().RefreshTagGroups();
				};
				subItem.Items.Add(toggle);
			}

			subItem.Items.Add(new MenuFlyoutSeparator());
			var removeTags = new MenuFlyoutItem
			{
				Text = Strings.RemoveTags.GetLocalizedResource(),
				IsEnabled = selected.Any(x => x?.FileTags is { Length: > 0 }),
			};
			removeTags.Click += async (_, _) =>
			{
				if (await FileTagsHelper.RemoveTagsAsync(selected) && ParentShellPageInstance is { } parentShellPage)
					await parentShellPage.GetRequiredShellViewModel().RefreshTagGroups();
			};
			subItem.Items.Add(removeTags);

			return subItem;
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
			UnhookScrollDeferTracking();
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The layout does not have folder settings.");
			folderSettings.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
			folderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
			folderSettings.GroupDirectionPreferenceUpdated -= FolderSettings_GroupDirectionPreferenceUpdated;
			folderSettings.GroupByDateUnitPreferenceUpdated -= FolderSettings_GroupByDateUnitPreferenceUpdated;
			ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;
			if (contextRequestedHandler is not null)
			{
				RemoveHandler(UIElement.ContextRequestedEvent, contextRequestedHandler);
				contextRequestedHandler = null;
			}
			ItemContextFlyoutHost.InvocationPointProvider = null;
			BaseContextFlyoutHost.InvocationPointProvider = null;

			var parameter = e.Parameter as NavigationArguments;
			if (parameter is not null && !parameter.IsLayoutSwitch)
			{
				var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();

				// The incoming page's first batch replaces the visible listing, avoiding an empty flash between folders.
				// When the target folder uses a different layout, the old items would re-render in the wrong layout, so drop them instead.
				shellViewModel.CancelLoadAndClearFiles(clearDisplay: e.SourcePageType != GetType());
			}
		}

		private async void BaseContextFlyout_Opening(object? sender, object e)
		{
			try
			{
				var parentShellPage = await EnsurePageIsCurrentAsync();
				var shellViewModel = parentShellPage.GetRequiredShellViewModel();
				var commandsViewModel = CommandsViewModel
					?? throw new InvalidOperationException("The layout commands are not initialized.");
				var instanceViewModel = parentShellPage.InstanceViewModel;

				ItemManipulationModel.ClearSelection();
				shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				var currentFolder = shellViewModel.CurrentFolder
					?? throw new InvalidOperationException("The current folder is not available.");
				List<ListedItem> contextItems = [currentFolder];
				var items = ContentPageContextFlyoutFactory.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: instanceViewModel, selectedItems: contextItems, commandsViewModel: commandsViewModel, shiftPressed: shiftPressed, itemViewModel: shellViewModel, selectedItemsPropertiesViewModel: null);
				var host = BaseContextFlyoutHost;
				host.Build(items);

				if (!instanceViewModel.IsPageTypeSearchResults && !instanceViewModel.IsPageTypeZipFolder && !instanceViewModel.IsPageTypeFtp)
				{
					var token = RenewShellMenuToken();

					// Pre-add "Show more options" (with the synchronously-known built-in overflow items) BEFORE the
					// async shell fetch so its placeholder shows while the extensions load.
					var (moreOptions, moreSeparator) = host.AddShowMoreOptionsIfEnabled(items);

					host.ResolvePlacement();

					var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(workingDir: shellViewModel.WorkingDirectory, selectedItems: [], shiftPressed: shiftPressed, showOpenMenu: false, token);
					if (token.IsCancellationRequested)
						return;

					// BitLocker: replace the placeholders with whichever entries the shell offers (drives)
					host.ApplyBitLockerModels(shellMenuItems, moreOptions, moreSeparator);

					// The background menu has no Open with / Send to entries - drop them from the shell list
					var shellModelsFiltered = shellMenuItems
						.Where(x => x.Tag is not Win32ContextMenuItem { CommandString: "openas" or "sendto" })
						.ToList();
					host.AddShellModels(shellModelsFiltered, shiftPressed, moreOptions, moreSeparator);
				}
				else
				{
					host.ResolvePlacement();
				}

				host.FinalizePrimaryRowPosition();
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

				var shellItemList = SafetyExtensions.IgnoreExceptions(() => orderedItems.Select(item => new ShellItem(item.GetRequiredPath())).ToArray());
				try
				{
					if (shellItemList?[0].FileSystemPath is not null && !instanceViewModel.IsPageTypeSearchResults)
					{
						var dataObject = ShellDataObject.Create(shellItemList);
						if (ShellDataObject.GetShellIdListArray(dataObject) is byte[] data)
						{
							var stream = new MemoryStream(data).AsRandomAccessStream();
							e.Data.SetData(ShellDataObject.ShellIdListArrayFormat, stream);
						}
					}
					else
					{
						// Only support IStorageItem capable paths
						var storageItemList = orderedItems.Where(x => !(x.IsHiddenItem && x.IsLinkItem && x.IsRecycleBinItem && x.IsShortcut)).Select(x => VirtualStorageItem.FromListedItem(x)).ToArray();
						e.Data.SetStorageItems(storageItemList, false);
					}
				}
				finally
				{
					if (shellItemList is not null)
					{
						foreach (ShellItem item in shellItemList)
							item.Dispose();
					}
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
					DragOverTimer.Stop();

					if (e.AcceptedOperation != DataPackageOperation.None)
					{
						DragOverTimer.Debounce(() =>
						{
							if (dragOverItem is not null && !dragOverItem.IsExecutable)
							{
								dragOverTimer?.Stop();
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
			HookScrollDeferTracking();
			RefreshContainer(args.ItemContainer, args.InRecycleQueue);
			RefreshItem(args.ItemContainer, args.Item, args.InRecycleQueue, args);

			// Set can window to front (#13255)
			itemDragging = false;
			MainWindow.Instance.SetCanWindowToFront(true);
		}

		private ScrollViewer? deferScrollViewer;

		// Hooked lazily from the first container callback, when the list's template is guaranteed realized
		[DynamicWindowsRuntimeCast(typeof(ScrollViewer))]
		private void HookScrollDeferTracking()
		{
			if (deferScrollViewer is not null)
				return;

			deferScrollViewer = ItemsControl.FindDescendant<ScrollViewer>();
			if (deferScrollViewer is not null)
				deferScrollViewer.ViewChanged += DeferScrollViewer_ViewChanged;
		}

		private void UnhookScrollDeferTracking()
		{
			scrollSettleTimer?.Stop();

			if (deferScrollViewer is not null)
			{
				deferScrollViewer.ViewChanged -= DeferScrollViewer_ViewChanged;
				deferScrollViewer = null;
			}
		}

		private DispatcherQueueTimer? scrollSettleTimer;

		// Rapid successive gestures raise a final ViewChanged between steps; debouncing keeps loads parked through the whole burst
		private void DeferScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
		{
			ParentShellPageInstance?.ShellViewModel?.NotifyScrollStateChanged(true);
			AppMemoryHelper.NotifyActivity();

			if (scrollSettleTimer is null)
			{
				scrollSettleTimer = DispatcherQueue.CreateTimer();
				scrollSettleTimer.Interval = TimeSpan.FromMilliseconds(200);
				scrollSettleTimer.IsRepeating = false;
				scrollSettleTimer.Tick += (_, _) => ParentShellPageInstance?.ShellViewModel?.NotifyScrollStateChanged(false);
			}

			scrollSettleTimer.Stop();
			if (!e.IsIntermediate)
				scrollSettleTimer.Start();
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
			if (inRecycleQueue)
			{
				UpdateItemToolTip(container, null);
				if (container.Content is ListedItem recycledItem)
				{
					var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
					shellViewModel.CancelExtendedPropertiesLoadingForItem(recycledItem);
					shellViewModel.ReleaseExtendedProperties(recycledItem);
				}
				return;
			}

			if (item is ListedItem listedItem)
			{
				UpdateItemToolTip(container, listedItem.ItemTooltipText);
				InitializeDrag(container, listedItem);

				if (listedItem.PreloadedIconData is not null && listedItem.FileImage is null)
					_ = ParentShellPageInstance.GetRequiredShellViewModel().ApplyCachedThumbnailOrPreloadedIconAsync(listedItem);

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

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
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

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
		private void FileListItem_Loaded(object sender, RoutedEventArgs e)
		{
			// Set the initial tooltip before hover starts so WinUI doesn't miss the first dwell.
			if (sender is SelectorItem container && container.Content is ListedItem listedItem)
				UpdateItemToolTip(container, listedItem.ItemTooltipText);
		}

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
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

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
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

			HoverTimer.Stop();
			HoverTimer.Debounce(() =>
			{
				if (hoveredItem is null)
					return;

				hoverTimer?.Stop();

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

			hoverTimer?.Stop();
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

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
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
			if (isDisposed)
				return;

			isDisposed = true;
			UnhookBaseEvents();
			UnhookScrollDeferTracking();
			StatusBarViewModel.Dispose();
			dragOverItem = null;
			hoveredItem = null;
			preRenamingItem = null;
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
				// Replacing the source rebuilds the list from scratch (a visible empty flash), so keep it when unchanged
				if (CollectionViewSource.IsSourceGrouped && ReferenceEquals(CollectionViewSource.Source, shellViewModel.FilesAndFolders.GroupedCollection))
					return;

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

				if (!CollectionViewSource.IsSourceGrouped && ReferenceEquals(CollectionViewSource.Source, shellViewModel.FilesAndFolders))
					return;

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

		[DynamicWindowsRuntimeCast(typeof(UIElement))]
		protected void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "PointerOver", true);
		}

		[DynamicWindowsRuntimeCast(typeof(UIElement))]
		protected void StackPanel_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "Normal", true);
		}

		[DynamicWindowsRuntimeCast(typeof(UIElement))]
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
					TapDebounceTimer.Debounce(() =>
					{
						if (item == preRenamingItem)
						{
							StartRenameItem();
							tapDebounceTimer?.Stop();
						}
					},
					TimeSpan.FromMilliseconds(1500));
				}
				else
				{
					tapDebounceTimer?.Stop();
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
			tapDebounceTimer?.Stop();
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

	}
}
