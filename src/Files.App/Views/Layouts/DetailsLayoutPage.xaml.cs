// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.UserControls.Selection;
using Files.App.ViewModels.Settings;
using Files.App.Views.Settings;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using WinRT;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using SortDirection = Files.App.Data.Enums.SortDirection;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the browser page of Details View
	/// </summary>
	[WinRT.GeneratedBindableCustomProperty([nameof(RowHeight), nameof(ColumnsViewModel), nameof(MaxWidthForRenameTextbox)], [])]
	public sealed partial class DetailsLayoutPage : BaseGroupableLayoutPage
	{
		// Dependency injections

		private ILayoutTemplatesService LayoutTemplatesService { get; } = Ioc.Default.GetRequiredService<ILayoutTemplatesService>();

		// Constants

		private const int TAG_TEXT_BLOCK = 1;

		// Fields

		private ListedItem? _nextItemToSelect;

		/// <summary>
		/// This reference is used to prevent unnecessary icon reloading by only reloading icons when their
		/// size changes, even if the layout size changes (since some layout sizes share the same icon size).
		/// </summary>
		private uint currentIconSize;
		private DetailsViewSizeKind? itemContainerSize;

		private DispatcherQueueTimer? _autoFitColumnsTimer;

		// Properties

		protected override ListViewBase ListViewBase => FileList;
		protected override SemanticZoom RootZoom => RootGridZoom;

		[DynamicWindowsRuntimeCast(typeof(ItemsStackPanel))]
		protected override (int First, int Last) GetVisibleIndexRange()
			=> FileList.ItemsPanelRoot is ItemsStackPanel panel ? (panel.FirstVisibleIndex, panel.LastVisibleIndex) : (-1, -1);

		public ColumnsViewModel ColumnsViewModel { get; } = new();

		private RelayCommand<string>? UpdateSortOptionsCommand { get; set; }

		private ColumnDefinition[] _fixedHeaderColumnDefinitions = [];
		private Dictionary<string, int>? _headerColumnIndices;
		private readonly Dictionary<string, ColumnHeader> _columnHeaders = [];
		private IReadOnlyDictionary<string, ShellPropertyColumnInfo>? _shellPropertyCatalog;
		private readonly HashSet<ListedItem> _shellPropertyLoads = [];
		private string? _contextColumnId;

		private const double ColumnReorderDragThreshold = 6;
		private const double ColumnReorderTouchDragThreshold = 16;
		private const double ColumnReorderScrollMargin = 32;
		private static readonly TimeSpan ColumnReorderAnimationDuration = TimeSpan.FromMilliseconds(150);
		private const double ColumnReorderScrollStep = 16;
		private double _reorderGhostLeft;
		private List<(string Id, double Left, double Right)>? _reorderColumnBounds;
		private FrameworkElement? _reorderHeader;
		private string? _reorderColumnId;
		private string? _reorderTargetId;
		private bool _reorderAfterTarget;
		private bool _isReorderingColumn;
		private bool _suppressHeaderSort;
		private uint _reorderPointerId;
		private double _reorderStartX;

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
			ColumnsViewModel.PropertyChanged += ColumnsViewModel_PropertyChanged;
			foreach (var id in DetailsLayoutColumnIds.All)
				ColumnsViewModel.GetColumn(id).PropertyChanged += ColumnItem_PropertyChanged;
			HeaderGrid.ManipulationMode = ManipulationModes.TranslateX;
			HeaderGrid.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(HeaderGrid_ReorderPointerPressed), true);
			HeaderGrid.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(HeaderGrid_ReorderPointerMoved), true);
			HeaderGrid.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(HeaderGrid_ReorderPointerReleased), true);
			HeaderGrid.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(HeaderGrid_ReorderPointerCaptureLost), true);
			HeaderGrid.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(HeaderGrid_ReorderKeyDown), true);
			FileList.ContainerContentChanging += FileList_ShellPropertyContainerContentChanging;
			_ = LoadShellPropertyCatalogAsync();
			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionStarted += SelectionRectangle_SelectionStarted;
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;

			UpdateSortOptionsCommand = new RelayCommand<string>(x =>
			{
				if (_suppressHeaderSort || string.IsNullOrEmpty(x))
					return;
				var folderSettings = FolderSettings
					?? throw new InvalidOperationException("The details layout does not have folder settings.");
				if (!Enum.TryParse<SortOption>(x, out var val) || val == SortOption.Column)
				{
					if (folderSettings.DirectorySortOption == SortOption.Column && folderSettings.DirectorySortColumnId == x)
					{
						folderSettings.DirectorySortDirection = (SortDirection)(((int)folderSettings.DirectorySortDirection + 1) % 2);
					}
					else
					{
						folderSettings.SetSortColumn(x);
						folderSettings.DirectorySortDirection = SortDirection.Ascending;
					}
				}
				else if (folderSettings.DirectorySortOption == val)
				{
					folderSettings.DirectorySortDirection = (SortDirection)(((int)folderSettings.DirectorySortDirection + 1) % 2);
				}
				else
				{
					folderSettings.DirectorySortOption = val;
					folderSettings.DirectorySortDirection = SortDirection.Ascending;
				}
			});

			ApplyHeaderColumnOrder();
			ActualThemeChanged += (_, _) => RecreateColumnHeaders();
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

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
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

			var parentShellPage = ParentShellPageInstance
				?? throw new InvalidOperationException("The details layout must be associated with a shell page.");
			var shellViewModel = parentShellPage.GetRequiredShellViewModel();
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout requires folder settings.");

			currentIconSize = LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.DetailsView);

			if (FolderSettings?.ColumnsViewModel is not null)
			{
				// Don't assign the columns view model directly, instead update each property individually using the Update method.
				// This is done to workaround a bug where CsWinRT doesn't properly track the memory of the object so that
				// an invalid memory access can occur when the object is moved.
				// See https://github.com/microsoft/CsWinRT/issues/1834.
				ColumnsViewModel.Columns = FolderSettings.ColumnsViewModel.Columns;
				ColumnsViewModel.ColumnOrder = FolderSettings.ColumnsViewModel.ColumnOrder;
				ApplyHeaderColumnOrder();
			}

			shellViewModel.EnabledGitProperties = GetEnabledGitProperties(ColumnsViewModel);

			folderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			folderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
			folderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
			folderSettings.PropertyChanged += FolderSettings_PropertyChanged;
			shellViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			shellViewModel.ItemLoadStatusChanged += ShellViewModel_ItemLoadStatusChanged;
			UserSettingsService.LayoutSettingsService.PropertyChanged += LayoutSettingsService_PropertyChanged;
			FileList.Items.VectorChanged += FileListItems_VectorChanged;

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
		}

		private void FolderSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(LayoutPreferencesManager.ColumnsViewModel) &&
				FolderSettings?.ColumnsViewModel is { } columns &&
				!ReferenceEquals(columns, ColumnsViewModel))
			{
				ColumnsViewModel.Columns = columns.Columns;
				ColumnsViewModel.ColumnOrder = columns.ColumnOrder;
				ApplyHeaderColumnOrder();
				LoadShellPropertiesForRealizedItems();
			}
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout does not have folder settings.");
			folderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			folderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
			folderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
			folderSettings.PropertyChanged -= FolderSettings_PropertyChanged;
			var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
			shellViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
			shellViewModel.ItemLoadStatusChanged -= ShellViewModel_ItemLoadStatusChanged;
			UserSettingsService.LayoutSettingsService.PropertyChanged -= LayoutSettingsService_PropertyChanged;
			FileList.Items.VectorChanged -= FileListItems_VectorChanged;
			_autoFitColumnsTimer?.Stop();
		}

		public override void Dispose()
		{
			Bindings.StopTracking();
			if (FolderSettings is { } folderSettings)
			{
				folderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
				folderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
				folderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
				folderSettings.PropertyChanged -= FolderSettings_PropertyChanged;
			}
			if (ParentShellPageInstance?.ShellViewModel is { } shellViewModel)
			{
				shellViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
				shellViewModel.ItemLoadStatusChanged -= ShellViewModel_ItemLoadStatusChanged;
			}
			UserSettingsService.LayoutSettingsService.PropertyChanged -= LayoutSettingsService_PropertyChanged;
			FileList.Items.VectorChanged -= FileListItems_VectorChanged;
			_autoFitColumnsTimer?.Stop();
			base.Dispose();
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
		}

		/// <summary>
		/// Sets the item size and spacing
		/// </summary>
		private void SetItemContainerStyle()
		{
			var size = UserSettingsService.LayoutSettingsService.DetailsViewSize;
			if (itemContainerSize != size)
			{
				// Changing size still requires a style refresh, even when both sizes use the same style.
				FileList.ItemContainerStyle = size == DetailsViewSizeKind.Compact ? RegularItemContainerStyle : CompactItemContainerStyle;
				FileList.ItemContainerStyle = size == DetailsViewSizeKind.Compact ? CompactItemContainerStyle : RegularItemContainerStyle;
				itemContainerSize = size;
			}

			// Set the width of the icon column. The value is increased by 4px to account for icon overlays.
			ColumnsViewModel.IconColumn.UserLength = new GridLength(LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.DetailsView) + 4);

			// Compact rows use a -2px ItemContainer margin, so the header checkbox needs an extra
			// left offset to stay aligned with row checkboxes.
			var leftOffset = UserSettingsService.LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Compact ? -4 : 0;
			SelectAllCheckbox.Margin = new Thickness(leftOffset, 14, 0, 0);
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
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout does not have folder settings.");

			foreach (var (id, columnHeader) in _columnHeaders)
			{
				var isSorted = folderSettings.DirectorySortOption == SortOption.Column
					? folderSettings.DirectorySortColumnId == id
					: DetailsLayoutColumnDefinitions.Get(id)?.SortOption == folderSettings.DirectorySortOption;

				columnHeader.Header.ColumnSortOption = isSorted ? folderSettings.DirectorySortDirection : null;
			}
		}

		private void FilesystemViewModel_PageTypeUpdated(object? sender, PageTypeUpdatedEventArgs e)
		{
			foreach (var definition in DetailsLayoutColumnDefinitions.All)
			{
				var isAvailable = definition.Id switch
				{
					DetailsLayoutColumnIds.OriginalPath or DetailsLayoutColumnIds.DateDeleted => e.IsTypeRecycleBin,
					DetailsLayoutColumnIds.Status => e.IsTypeCloudDrive,
					DetailsLayoutColumnIds.Path => e.IsTypeSearchResults,
					_ when definition.IsGitColumn => e.IsTypeGitRepository && !e.IsTypeSearchResults,
					_ => true,
				};

				if (isAvailable)
					ColumnsViewModel.GetColumn(definition.Id).Show();
				else
					ColumnsViewModel.GetColumn(definition.Id).Hide();
			}

			UpdateSortIndicator();
		}

		private void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{

		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			foreach (var item in e.AddedItems)
				SetCheckboxSelectionState(item);

			foreach (var item in e.RemovedItems)
				SetCheckboxSelectionState(item);

			UpdateSelectAllCheckboxState();
		}

		private bool _suppressSelectAllCheckboxEvents;

		private void SelectAllCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			if (_suppressSelectAllCheckboxEvents)
				return;

			ItemManipulationModel.SelectAllItems();
		}

		private void SelectAllCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			if (_suppressSelectAllCheckboxEvents)
				return;

			ItemManipulationModel.ClearSelection();
		}

		private void UpdateSelectAllCheckboxState()
		{
			var selectedCount = FileList.SelectedItems.Count;
			bool? newState = selectedCount == 0
				? false
				: selectedCount == FileList.Items.Count ? true : null;

			if (SelectAllCheckbox.IsChecked == newState)
				return;

			_suppressSelectAllCheckboxEvents = true;
			try
			{
				SelectAllCheckbox.IsChecked = newState;
			}
			finally
			{
				_suppressSelectAllCheckboxEvents = false;
			}
		}

		override public void StartRenameItem()
		{
			StartRenameItem("ItemNameTextBox");
		}

		private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (IsRenamingItem)
			{
				_ = ValidateItemNameInputTextAsync(textBox, args, (showError) =>
				{
					FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
					FileNameTeachingTip.IsOpen = showError;
				});
			}
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		protected override void EndRename(TextBox textBox)
		{
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

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(HyperlinkButton))]
		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ParentShellPageInstance is null || IsRenamingItem)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot);
			var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) is not null;
			var isFooterFocused = focusedElement is HyperlinkButton;

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
					var folders = SelectedItems?.Where(file => file.PrimaryItemAttribute == StorageItemTypes.Folder);
					if (folders?.Any() ?? false)
					{
						foreach (ListedItem folder in folders)
							await NavigationHelpers.OpenPathInNewTab(folder.ItemPath);
					}
				}
				else if (ctrlPressed && shiftPressed)
				{
					var selectedFolders = SelectedItems?.Where(item => item.PrimaryItemAttribute == StorageItemTypes.Folder);
					if (selectedFolders?.Count() == 1)
					{
						NavigationHelpers.OpenInSecondaryPane(ParentShellPageInstance, selectedFolders.First());
					}
				}
				else if (!ctrlPressed && !shiftPressed)
				{
					if (SelectedItems?.Any() ?? false)
					{
						foreach (var selectedItem in SelectedItems)
							await OpenItem(selectedItem);
					}
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

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		protected override bool CanGetItemFromElement(object element)
			=> element is ListViewItem;

		private async Task ReloadItemIconsAsync()
		{
			if (ParentShellPageInstance is not { } parentShellPage)
				return;
			var shellViewModel = parentShellPage.GetRequiredShellViewModel();

			shellViewModel.CancelExtendedPropertiesLoading();
			var filesAndFolders = shellViewModel.FilesAndFolders.ToList();

			await Task.WhenAll(filesAndFolders.Select(listedItem =>
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					return shellViewModel.LoadExtendedItemPropertiesAsync(listedItem);
				else
					return Task.CompletedTask;
			}));

			if (shellViewModel.EnabledGitProperties is not GitProperties.None)
			{
				await Task.WhenAll(filesAndFolders.Select(item =>
				{
					if (item is IGitItem gitItem)
						return shellViewModel.LoadGitPropertiesAsync(gitItem);

					return Task.CompletedTask;
				}));
			}
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		[DynamicWindowsRuntimeCast(typeof(Rectangle))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
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
				else
				{
					// Clear selection when clicking empty area via touch
					// https://github.com/files-community/Files/issues/15051
					if (e.PointerDeviceType == PointerDeviceType.Touch)
						ItemManipulationModel.ClearSelection();
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
			if ((item.PrimaryItemAttribute is StorageItemTypes.File && UserSettingsService.FoldersSettingsService.OpenFilesWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType)) ||
				(item.PrimaryItemAttribute is StorageItemTypes.Folder && UserSettingsService.FoldersSettingsService.OpenFoldersWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType)))
			{
				ResetRenameDoubleClick();
				await Commands.OpenItem.ExecuteAsync();
			}
			else
			{
				if (IsWithinRenameDoubleClickWindow && item == RenamingItem)
				{
					// A tap this soon after the tap that started renaming is the second click of a double click
					CancelRenameOnDoubleClick(item);
					ResetRenameDoubleClick();
					await OpenItem(item);
				}
				else if (clickedItem is TextBlock && ((TextBlock)clickedItem).Name == "ItemName")
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

		private async Task OpenItem(ListedItem item)
		{
			if (!Commands.OpenItem.IsExecutable)
			{
				// Fallback if the command is not executable. It occurs only when search is performed from the columns view.
				if (ParentShellPageInstance is not { } parentShellPage)
					throw new InvalidOperationException("The details layout does not have a parent shell page.");

				var itemType = item.PrimaryItemAttribute == StorageItemTypes.Folder ? FilesystemItemType.Directory : FilesystemItemType.File;
				await NavigationHelpers.OpenPath(item.ItemPath!, parentShellPage, itemType);
			}
			else
			{
				await Commands.OpenItem.ExecuteAsync();
			}
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(ListView))]
		private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			var originalElement = e.OriginalSource as FrameworkElement;
			var dataContext = originalElement?.DataContext;

			// Try to get the item from DataContext or from sender (ListView)
			ListedItem? item = dataContext as ListedItem;
			if (item == null && sender is ListView listView && listView.SelectedItem is ListedItem selectedItem)
				item = selectedItem;

			CancelRenameOnDoubleClick(item);

			if (item != null && item.PrimaryItemAttribute == StorageItemTypes.File && !UserSettingsService.FoldersSettingsService.OpenFilesWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType))
				await OpenItem(item);
			else if (item != null && item.PrimaryItemAttribute == StorageItemTypes.Folder && !UserSettingsService.FoldersSettingsService.OpenFoldersWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType))
				await OpenItem(item);
			else if (item == null && UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
				await Commands.NavigateUp.ExecuteAsync();

			ResetRenameDoubleClick();
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		private void RowPanel_Loaded(object sender, RoutedEventArgs e)
		{
			// This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
			// because you can't use bindings in the setters
			DependencyObject item = VisualTreeHelper.GetParent(sender as DetailsLayoutRowPanel);
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
				var folderSettings = FolderSettings
					?? throw new InvalidOperationException("The details layout does not have folder settings.");
				folderSettings.ColumnsViewModel = ColumnsViewModel;
			}
		}

		private void UpdateColumnLayout()
		{
			foreach (var (id, columnHeader) in _columnHeaders)
			{
				var column = ColumnsViewModel.GetColumn(id);
				if (column.Visibility == Visibility.Visible)
					column.UserLength = columnHeader.Column.Width;
			}
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
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout does not have folder settings.");
			folderSettings.ColumnsViewModel = ColumnsViewModel;
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		[DynamicWindowsRuntimeCast(typeof(UIElement))]
		private void GridSplitter_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as UIElement)?.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void SaveColumnVisibility()
		{
			SaveColumnLayout();
			var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
			shellViewModel.EnabledGitProperties = GetEnabledGitProperties(ColumnsViewModel);
		}

		private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (sender is Files.App.Controls.GridSplitter splitter && DetailsLayoutColumnExtensions.GetId(splitter) is { } columnId)
				ResizeColumnToFit(columnId);

			e.Handled = true;
		}

		private void SizeAllColumnsToFit_Click(object sender, RoutedEventArgs e)
		{
			_ = Commands.AutoFitColumns.ExecuteAsync();
		}

		public void AutoFitColumns()
			=> AutoFitColumns(true);

		private void AutoFitColumns(bool save)
		{
			if (!FileList.Items.Any())
				return;

			foreach (var columnId in ColumnsViewModel.AllColumnIds.ToArray())
				ResizeColumnToFit(columnId, save);
		}

		private void AutoFitColumnsIfEnabled()
		{
			if (!UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout)
				return;

			// Trailing debounce so bursts of item adds and extended-property updates during a folder load collapse into one resize.
			_autoFitColumnsTimer ??= DispatcherQueue.CreateTimer();
			_autoFitColumnsTimer.Debounce(
				() => AutoFitColumns(false),
				TimeSpan.FromMilliseconds(250));
		}

		private void ShellViewModel_ItemLoadStatusChanged(object? sender, ItemLoadStatusChangedEventArgs e)
		{
			if (e.Status == ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete)
				AutoFitColumnsIfEnabled();
		}

		private void FileListItems_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
		{
			AutoFitColumnsIfEnabled();
		}

		protected override async Task CommitRenameAsync(TextBox textBox)
		{
			await base.CommitRenameAsync(textBox);
			AutoFitColumnsIfEnabled();
		}

		private void ResizeColumnToFit(string columnId, bool save = true)
		{
			if (!FileList.Items.Any())
				return;

			var maxItemLength = columnId switch
			{
				DetailsLayoutColumnIds.Icon => 40, // Check all items columns
				DetailsLayoutColumnIds.Name => FileList.Items.Cast<ListedItem>().Select(x => x.Name?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.Tag => FileList.Items.Cast<ListedItem>().Select(x => x.FileTagsUI?.Sum(x => x?.Name?.Length ?? 0) ?? 0).Max(),
				DetailsLayoutColumnIds.Size => FileList.Items.Cast<ListedItem>().Select(x => x.FileSize?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.GitStatus or DetailsLayoutColumnIds.Status => 0, // these columns only show icons
				_ => FileList.Items.Cast<ListedItem>().Select(x => ColumnValueHelper.GetText(x, columnId)?.Length ?? 0).Max(),
			};

			// if called programmatically, the column could be hidden
			// in this case, resizing doesn't need to be done at all
			if (maxItemLength == 0)
				return;

			var columnSizeToFit = MeasureColumnEstimate(columnId, 5, maxItemLength);

			if (columnSizeToFit > 1)
			{
				var column = ColumnsViewModel.GetColumn(columnId);

				if (columnId == DetailsLayoutColumnIds.Name)
					columnSizeToFit += 20;

				var minFitLength = Math.Max(columnSizeToFit, column.NormalMinLength);
				var maxFitLength = Math.Min(minFitLength + 36, column.NormalMaxLength); // 36 to account for SortIcon & padding

				column.UserLength = new GridLength(maxFitLength, GridUnitType.Pixel);
			}

			if (!save)
				return;

			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout does not have folder settings.");
			folderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private double MeasureColumnEstimate(string columnId, int measureItemsCount, int maxItemLength)
		{
			if (columnId == DetailsLayoutColumnIds.Tag)
				return MeasureTagColumnEstimate(columnId);

			return MeasureTextColumnEstimate(columnId, measureItemsCount, maxItemLength);
		}

		private double MeasureTagColumnEstimate(string columnId)
		{
			var grids = DependencyObjectHelpers
				.FindChildren<Grid>(FileList.ItemsPanelRoot)
				.Where(grid => IsCorrectColumn(grid, columnId));

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

		private double MeasureTextColumnEstimate(string columnId, int measureItemsCount, int maxItemLength)
		{
			var tbs = DependencyObjectHelpers
				.FindChildren<TextBlock>(FileList.ItemsPanelRoot)
				.Where(tb => IsCorrectColumn(tb, columnId));

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

		private bool IsCorrectColumn(FrameworkElement element, string columnId)
		{
			string? columnIdFromName = element.Name switch
			{
				"ItemName" => DetailsLayoutColumnIds.Name,
				"ItemTagGrid" => DetailsLayoutColumnIds.Tag,
				"ItemSizeTextBlock" => DetailsLayoutColumnIds.Size,
				_ => element is TextBlock ? DetailsLayoutColumnExtensions.GetId(element) : null,
			};

			return columnIdFromName == columnId;
		}

		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
			const double OffsetCorrection = 88; // HeaderGrid (40) + ListViewHeaderItem (44 + 4 margin)

			RootGridZoom.ViewChangeStarted += (_, args) =>
			{
				if (args.IsSourceZoomedInView || ContentScroller is not { } scroller)
					return;
				void OnZoomScrolled(object? s, ScrollViewerViewChangedEventArgs ve)
				{
					scroller.ViewChanged -= OnZoomScrolled; 
					scroller.ChangeView(0, Math.Max(0, scroller.VerticalOffset - OffsetCorrection), null, true);
				}
				scroller.ViewChanged += OnZoomScrolled;
			};
		}

		private void ColumnItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(DetailsLayoutColumnItem.LengthIncludingGridSplitter))
				ColumnsViewModel.NotifyLayoutChanged();
		}

		private void ColumnsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColumnsViewModel.OrderedColumns))
				ApplyHeaderColumnOrder();
		}

		private void ApplyHeaderColumnOrder()
		{
			var definitions = HeaderGrid.ColumnDefinitions;
			if (_fixedHeaderColumnDefinitions.Length == 0)
			{
				if (definitions.Count != 3)
					return;

				_fixedHeaderColumnDefinitions = [definitions[0], definitions[1], definitions[2]];
			}

			SyncColumnHeaders();

			var columnIndices = new Dictionary<string, int>(_columnHeaders.Count);
			definitions.Clear();
			definitions.Add(_fixedHeaderColumnDefinitions[0]);
			definitions.Add(_fixedHeaderColumnDefinitions[1]);
			foreach (var id in ColumnsViewModel.OrderedColumns.Ids)
			{
				if (!_columnHeaders.TryGetValue(id, out var columnHeader))
					continue;

				columnIndices[id] = definitions.Count;
				definitions.Add(columnHeader.Column);
				definitions.Add(columnHeader.Divider);
			}
			definitions.Add(_fixedHeaderColumnDefinitions[2]);
			_headerColumnIndices = columnIndices;
			Grid.SetColumnSpan(ColumnReorderGhost, definitions.Count);

			foreach (var (id, columnHeader) in _columnHeaders)
			{
				if (!columnIndices.TryGetValue(id, out var columnIndex))
					continue;

				Grid.SetColumn(columnHeader.Header, columnIndex);
				Grid.SetColumn(columnHeader.Splitter, columnIndex + 1);
				columnHeader.Header.TabIndex = columnIndex;
			}
		}

		/// <summary>
		/// Recreates the headers created in code so that they pick up the current theme.
		/// </summary>
		private void RecreateColumnHeaders()
		{
			EndColumnReorder();
			foreach (var columnHeader in _columnHeaders.Values)
			{
				columnHeader.Attach(null);
				HeaderGrid.Children.Remove(columnHeader.Header);
				HeaderGrid.Children.Remove(columnHeader.Splitter);
			}

			_columnHeaders.Clear();
			ApplyHeaderColumnOrder();
		}

		private void SyncColumnHeaders()
		{
			var columnIds = ColumnsViewModel.AllColumnIds.Where(id => id != DetailsLayoutColumnIds.Icon).ToHashSet();
			foreach (var id in _columnHeaders.Keys.Where(id => !columnIds.Contains(id)).ToArray())
			{
				var columnHeader = _columnHeaders[id];
				columnHeader.Attach(null);
				HeaderGrid.Children.Remove(columnHeader.Header);
				HeaderGrid.Children.Remove(columnHeader.Splitter);
				_columnHeaders.Remove(id);
			}

			foreach (var id in columnIds)
			{
				if (!_columnHeaders.TryGetValue(id, out var columnHeader))
				{
					columnHeader = CreateColumnHeader(id);
					_columnHeaders[id] = columnHeader;
					HeaderGrid.Children.Add(columnHeader.Header);
					HeaderGrid.Children.Add(columnHeader.Splitter);
				}

				columnHeader.Attach(ColumnsViewModel.GetColumn(id));
			}

			if (FolderSettings is not null)
				UpdateSortIndicator();
		}

		private ColumnHeader CreateColumnHeader(string id)
		{
			var definition = DetailsLayoutColumnDefinitions.Get(id);
			var header = new DataGridHeader
			{
				Header = GetColumnDisplayName(id),
				Command = UpdateSortOptionsCommand,
				CommandParameter = definition?.SortOption?.ToString() ?? id,
			};
			DetailsLayoutColumnExtensions.SetId(header, id);
			if (id == DetailsLayoutColumnIds.Name)
				header.Margin = new(4, 0, -4, 0);

			var splitter = new Files.App.Controls.GridSplitter
			{
				Style = (Style)Resources["HeaderGridSplitterStyle"],
			};
			DetailsLayoutColumnExtensions.SetId(splitter, id);
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(splitter, $"{id}HeaderGridSplitter");
			splitter.DoubleTapped += GridSplitter_DoubleTapped;
			splitter.Loaded += GridSplitter_Loaded;
			splitter.ManipulationStarted += GridSplitter_ManipulationStarted;
			splitter.ManipulationDelta += GridSplitter_ManipulationDelta;
			splitter.ManipulationCompleted += GridSplitter_ManipulationCompleted;
			splitter.PreviewKeyUp += GridSplitter_PreviewKeyUp;

			return new(new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto }, header, splitter, ColumnsViewModel.NotifyLayoutChanged);
		}

		private string GetColumnDisplayName(string id)
		{
			return DetailsLayoutColumnDefaults.GetDisplayName(id, _shellPropertyCatalog);
		}

		private async Task LoadShellPropertyCatalogAsync()
		{
			_shellPropertyCatalog = await ShellPropertyCatalog.GetAsync();
			foreach (var (id, columnHeader) in _columnHeaders)
				columnHeader.Header.Header = GetColumnDisplayName(id);
		}

		private void SetColumnVisibility(string id, bool isVisible)
		{
			if (isVisible == ColumnsViewModel.IsColumnVisible(id))
				return;

			var width = _shellPropertyCatalog is not null && _shellPropertyCatalog.TryGetValue(id, out var info) ? info.DefaultWidth : 150;
			ColumnsViewModel.SetColumnVisibility(id, isVisible, width);
			if (isVisible)
				LoadShellPropertiesForRealizedItems();

			SaveColumnVisibility();
		}

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
		private void LoadShellPropertiesForRealizedItems()
		{
			if (FileList.ItemsPanelRoot is not { } panel)
				return;

			foreach (var child in panel.Children)
			{
				if (child is SelectorItem container && FileList.ItemFromContainer(container) is ListedItem item)
					_ = LoadShellPropertiesAsync(item);
			}
		}

		private void FileList_ShellPropertyContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (!args.InRecycleQueue && args.Item is ListedItem item)
				_ = LoadShellPropertiesAsync(item);
		}

		private async Task LoadShellPropertiesAsync(ListedItem item)
		{
			var propertyColumnIds = ColumnsViewModel.PropertyColumnIds;
			if (propertyColumnIds.Count == 0 || string.IsNullOrEmpty(item.ItemPath))
				return;

			var existing = item.ShellPropertyValues;
			var missing = propertyColumnIds.Where(id => existing is null || !existing.ContainsKey(id)).ToArray();
			if (missing.Length == 0 || !_shellPropertyLoads.Add(item))
				return;

			try
			{
				var values = await ShellPropertyReader.ReadAsync(item.ItemPath, missing);
				if (item.ShellPropertyValues is { } current)
				{
					foreach (var (id, value) in current)
						values.TryAdd(id, value);
				}

				item.ShellPropertyValues = values;
				RefreshGeneratedCells(item);
			}
			finally
			{
				_shellPropertyLoads.Remove(item);
			}
		}

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
		[DynamicWindowsRuntimeCast(typeof(UserControl))]
		private void RefreshGeneratedCells(ListedItem item)
		{
			if (FileList.ContainerFromItem(item) is SelectorItem container &&
				container.ContentTemplateRoot is UserControl { Content: DetailsLayoutRowPanel panel })
			{
				panel.RefreshGeneratedCells();
			}
		}

		private sealed class ColumnHeader(ColumnDefinition column, ColumnDefinition divider, DataGridHeader header, Files.App.Controls.GridSplitter splitter, Action layoutChanged)
		{
			private DetailsLayoutColumnItem? _item;

			public ColumnDefinition Column { get; } = column;

			public ColumnDefinition Divider { get; } = divider;

			public DataGridHeader Header { get; } = header;

			public Files.App.Controls.GridSplitter Splitter { get; } = splitter;

			public void Attach(DetailsLayoutColumnItem? item)
			{
				if (ReferenceEquals(_item, item))
					return;

				if (_item is not null)
					_item.PropertyChanged -= Item_PropertyChanged;

				_item = item;
				if (_item is null)
					return;

				_item.PropertyChanged += Item_PropertyChanged;
				UpdateLayout();
			}

			private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
			{
				if (e.PropertyName is nameof(DetailsLayoutColumnItem.Length) or nameof(DetailsLayoutColumnItem.MaxLength) or nameof(DetailsLayoutColumnItem.Visibility))
					UpdateLayout();
				else if (e.PropertyName is nameof(DetailsLayoutColumnItem.LengthIncludingGridSplitter))
					layoutChanged();
			}

			private void UpdateLayout()
			{
				if (_item is null)
					return;

				Column.Width = _item.Length;
				Column.MaxWidth = _item.MaxLength;
				Column.MinWidth = _item.MinLength;
				Header.Visibility = _item.Visibility;
				Splitter.Visibility = _item.IsResizable ? _item.Visibility : Visibility.Collapsed;
			}
		}

		private void SaveColumnLayout()
		{
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout does not have folder settings.");
			folderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private void HeaderGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
		{
			_contextColumnId = null;
			for (var element = args.OriginalSource as DependencyObject; element is not null && element != sender; element = VisualTreeHelper.GetParent(element))
			{
				if (DetailsLayoutColumnExtensions.GetId(element) is { } id)
				{
					_contextColumnId = id;
					break;
				}
			}
		}

		private void HeaderGridMenuFlyout_Opening(object sender, object e)
		{
			var contextColumn = _contextColumnId is { } contextColumnId ? ColumnsViewModel.FindColumn(contextColumnId) : null;
			var moveVisibility = _contextColumnId is not null && !DetailsLayoutColumnOrder.IsFixed(_contextColumnId) ? Visibility.Visible : Visibility.Collapsed;
			ColumnToFitMenuFlyoutItem.Visibility = contextColumn is { IsResizable: true } ? Visibility.Visible : Visibility.Collapsed;
			MoveColumnLeftMenuFlyoutItem.Visibility = moveVisibility;
			MoveColumnRightMenuFlyoutItem.Visibility = moveVisibility;
			GroupByColumnMenuFlyoutItem.Visibility = _contextColumnId is not null ? Visibility.Visible : Visibility.Collapsed;

			if (_contextColumnId is { } id)
			{
				MoveColumnLeftMenuFlyoutItem.IsEnabled = ColumnsViewModel.CanMoveColumn(id, -1);
				MoveColumnRightMenuFlyoutItem.IsEnabled = ColumnsViewModel.CanMoveColumn(id, 1);
				if (FolderSettings is { } folderSettings)
					GroupByColumnMenuFlyoutItem.IsChecked = IsGroupedByColumn(folderSettings, id);
			}

			if (sender is MenuFlyout menuFlyout)
				UpdateColumnMenuItems(menuFlyout);
		}

		private void UpdateColumnMenuItems(MenuFlyout menuFlyout)
		{
			var items = menuFlyout.Items;
			var startIndex = items.IndexOf(ColumnTogglesStartSeparator) + 1;
			for (var index = items.IndexOf(ColumnTogglesEndSeparator) - 1; index >= startIndex; index--)
				items.RemoveAt(index);

			// Lists the columns that apply to this folder in display order, like the column menu of File Explorer.
			var columnIds = ColumnsViewModel.OrderedColumns.Ids.Where(id =>
				!DetailsLayoutColumnOrder.IsFixed(id) &&
				ColumnsViewModel.GetColumn(id) is { IsHidden: false } column &&
				(DetailsLayoutColumnDefinitions.IsBuiltIn(id) || !column.UserCollapsed));

			foreach (var id in columnIds)
			{
				var item = new ToggleMenuFlyoutItem
				{
					Text = GetColumnDisplayName(id),
					Tag = id,
					IsChecked = ColumnsViewModel.IsColumnVisible(id),
				};
				item.Click += ColumnMenuItem_Click;
				items.Insert(startIndex++, item);
			}
		}

		private async void MoreColumns_Click(object sender, RoutedEventArgs e)
		{
			var catalog = _shellPropertyCatalog ?? await ShellPropertyCatalog.GetAsync();
			var availableColumns = DetailsLayoutColumnIds.All
				.Where(id => !DetailsLayoutColumnOrder.IsFixed(id) && !ColumnsViewModel.GetColumn(id).IsHidden)
				.Concat(catalog.Keys.Where(id => !DetailsLayoutColumnDefinitions.IsBuiltIn(id)))
				.ToArray();

			var shownColumns = ColumnsViewModel.OrderedColumns.Ids.Where(id => ColumnsViewModel.IsColumnVisible(id) && !ColumnsViewModel.GetColumn(id).IsHidden);
			var template = LayoutTemplatesService.GetTemplate(FolderSettings?.TemplateId) ?? LayoutTemplatesService.DefaultTemplate;
			var defaultColumns = DetailsLayoutColumnDefaults.GetColumns(template).Where(availableColumns.Contains);

			var viewModel = new ColumnCustomizationViewModel(shownColumns, availableColumns, defaultColumns, GetColumnDisplayName, Strings.CustomizeColumns.GetLocalizedResource());
			viewModel.Saved += (_, columns) =>
			{
				if (!IsLoaded)
					return;

				SetColumnsVisibility(availableColumns.Select(id => (id, columns.Contains(id))));
				ColumnsViewModel.ColumnOrder = [.. columns, .. ColumnsViewModel.OrderedColumns.Ids.Where(id => !columns.Contains(id) && !DetailsLayoutColumnOrder.IsFixed(id))];
				SaveColumnLayout();
			};

			ColumnCustomizationWindow.Show(viewModel);
		}

		private void SetColumnsVisibility(IEnumerable<(string Id, bool IsVisible)> changes)
		{
			var hasNewColumns = false;
			foreach (var (id, isVisible) in changes)
			{
				if (isVisible == ColumnsViewModel.IsColumnVisible(id))
					continue;

				var width = _shellPropertyCatalog is not null && _shellPropertyCatalog.TryGetValue(id, out var info) ? info.DefaultWidth : 150;
				ColumnsViewModel.SetColumnVisibility(id, isVisible, width);
				hasNewColumns |= isVisible;
			}

			if (hasNewColumns)
				LoadShellPropertiesForRealizedItems();

			SaveColumnVisibility();
		}

		[DynamicWindowsRuntimeCast(typeof(ToggleMenuFlyoutItem))]
		private void ColumnMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleMenuFlyoutItem { Tag: string name } item)
				SetColumnVisibility(name, item.IsChecked);
		}

		private void SizeColumnToFit_Click(object sender, RoutedEventArgs e)
		{
			if (_contextColumnId is { } id)
				ResizeColumnToFit(id);
		}

		private void GroupByColumn_Click(object sender, RoutedEventArgs e)
		{
			if (_contextColumnId is not { } id || FolderSettings is not { } folderSettings)
				return;

			if (IsGroupedByColumn(folderSettings, id))
				folderSettings.DirectoryGroupOption = GroupOption.None;
			else if (GetBuiltInGroupOption(id) is { } groupOption)
				folderSettings.DirectoryGroupOption = groupOption;
			else
				folderSettings.SetGroupColumn(id);
		}

		private static bool IsGroupedByColumn(LayoutPreferencesManager folderSettings, string id)
		{
			return GetBuiltInGroupOption(id) is { } groupOption
				? folderSettings.DirectoryGroupOption == groupOption
				: folderSettings.DirectoryGroupOption == GroupOption.Column && folderSettings.DirectoryGroupColumnId == id;
		}

		private static GroupOption? GetBuiltInGroupOption(string id)
		{
			return id switch
			{
				DetailsLayoutColumnIds.Name => GroupOption.Name,
				DetailsLayoutColumnIds.DateModified => GroupOption.DateModified,
				DetailsLayoutColumnIds.DateCreated => GroupOption.DateCreated,
				DetailsLayoutColumnIds.ItemType => GroupOption.FileType,
				DetailsLayoutColumnIds.Size => GroupOption.Size,
				DetailsLayoutColumnIds.Status => GroupOption.SyncStatus,
				DetailsLayoutColumnIds.Tag => GroupOption.FileTag,
				DetailsLayoutColumnIds.OriginalPath => GroupOption.OriginalFolder,
				DetailsLayoutColumnIds.DateDeleted => GroupOption.DateDeleted,
				DetailsLayoutColumnIds.Path => GroupOption.FolderPath,
				_ => null,
			};
		}

		private void MoveColumnLeft_Click(object sender, RoutedEventArgs e)
		{
			if (_contextColumnId is { } id)
			{
				ColumnsViewModel.MoveColumn(id, -1);
				SaveColumnLayout();
			}
		}

		private void MoveColumnRight_Click(object sender, RoutedEventArgs e)
		{
			if (_contextColumnId is { } id)
			{
				ColumnsViewModel.MoveColumn(id, 1);
				SaveColumnLayout();
			}
		}

		private List<(string Id, double Left, double Right)> GetHeaderColumnBounds()
		{
			if (_headerColumnIndices is null)
				ApplyHeaderColumnOrder();

			var bounds = new List<(string Id, double Left, double Right)>();
			if (_headerColumnIndices is null)
				return bounds;

			var definitions = HeaderGrid.ColumnDefinitions;
			var offsets = new double[definitions.Count + 1];
			for (int i = 0; i < definitions.Count; i++)
				offsets[i + 1] = offsets[i] + definitions[i].ActualWidth;

			foreach (var id in ColumnsViewModel.OrderedColumns.Ids)
			{
				if (!DetailsLayoutColumnOrder.IsFixed(id) && _headerColumnIndices.TryGetValue(id, out var index) && ColumnsViewModel.GetColumn(id).Visibility == Visibility.Visible)
					bounds.Add((id, offsets[index], offsets[index + 2]));
			}

			return bounds;
		}

		private double GetHeaderContentX(PointerRoutedEventArgs e)
		{
			return e.GetCurrentPoint(HeaderGrid).Position.X - HeaderGrid.Padding.Left;
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void HeaderGrid_ReorderPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			EndColumnReorder();
			_suppressHeaderSort = false;

			if (!e.GetCurrentPoint(HeaderGrid).Properties.IsLeftButtonPressed)
				return;

			for (var element = e.OriginalSource as DependencyObject; element is not null && element != HeaderGrid; element = VisualTreeHelper.GetParent(element))
			{
				if (element is Files.App.Controls.GridSplitter)
					return;
			}

			var bounds = GetHeaderColumnBounds();
			var x = GetHeaderContentX(e);
			foreach (var (id, left, right) in bounds)
			{
				if (x < left || x >= right)
					continue;

				foreach (var child in HeaderGrid.Children)
				{
					if (child is FrameworkElement element && element is not Files.App.Controls.GridSplitter && DetailsLayoutColumnExtensions.GetId(element) == id)
						_reorderHeader = element;
				}

				_reorderColumnBounds = bounds;
				_reorderColumnId = id;
				_reorderPointerId = e.Pointer.PointerId;
				_reorderStartX = x;
				return;
			}
		}

		private void HeaderGrid_ReorderPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (_reorderHeader is null || _reorderColumnBounds is null || e.Pointer.PointerId != _reorderPointerId)
				return;

			var x = GetHeaderContentX(e);
			if (!_isReorderingColumn)
			{
				if (Math.Abs(x - _reorderStartX) < (e.Pointer.PointerDeviceType == PointerDeviceType.Touch ? ColumnReorderTouchDragThreshold : ColumnReorderDragThreshold))
					return;

				for (var element = e.OriginalSource as DependencyObject; element is not null && element != HeaderGrid; element = VisualTreeHelper.GetParent(element))
					(element as UIElement)?.ReleasePointerCapture(e.Pointer);

				HeaderGrid.CapturePointer(e.Pointer);
				_isReorderingColumn = true;
				_suppressHeaderSort = true;

				var draggedBound = _reorderColumnBounds.First(bound => bound.Id == _reorderColumnId);
				_reorderGhostLeft = draggedBound.Left;
				ColumnReorderGhost.Width = draggedBound.Right - draggedBound.Left;
				ColumnReorderGhostImage.Source = null;
				ColumnReorderGhost.Visibility = Visibility.Visible;
				_ = CaptureColumnReorderGhostAsync(_reorderHeader);
			}

			var maxGhostLeft = Math.Max(0, _reorderColumnBounds[^1].Right - ColumnReorderGhost.Width);
			ColumnReorderGhostTransform.X = Math.Clamp(_reorderGhostLeft + x - _reorderStartX, 0, maxGhostLeft);
			ScrollHeaderWhileReordering(e);

			ReflowHeadersWhileReordering(x);
			e.Handled = true;
		}

		private void ReflowHeadersWhileReordering(double x)
		{
			if (_reorderColumnBounds is not { } bounds)
				return;

			var draggedIndex = bounds.FindIndex(bound => bound.Id == _reorderColumnId);
			if (draggedIndex < 0)
				return;

			var draggedWidth = bounds[draggedIndex].Right - bounds[draggedIndex].Left;
			_reorderTargetId = null;
			for (int i = 0; i < bounds.Count; i++)
			{
				if (i == draggedIndex)
					continue;

				var (id, left, right) = bounds[i];
				var center = (left + right) / 2;
				var offset = 0d;
				if (i > draggedIndex && x > center)
				{
					offset = -draggedWidth;
					(_reorderTargetId, _reorderAfterTarget) = (id, true);
				}
				else if (i < draggedIndex && x < center)
				{
					offset = draggedWidth;
					if (_reorderTargetId is null)
						(_reorderTargetId, _reorderAfterTarget) = (id, false);
				}

				SetColumnHeaderOffset(id, offset, animate: true);
			}
		}

		private void SetColumnHeaderOffset(string id, double offset, bool animate)
		{
			if (!_columnHeaders.TryGetValue(id, out var columnHeader))
				return;

			foreach (var element in (UIElement[])[columnHeader.Header, columnHeader.Splitter])
			{
				element.TranslationTransition = animate ? element.TranslationTransition ?? new Vector3Transition { Duration = ColumnReorderAnimationDuration } : null;
				element.Translation = new((float)offset, 0, 0);
			}
		}

		private void HeaderGrid_ReorderPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_reorderHeader is null || e.Pointer.PointerId != _reorderPointerId)
				return;

			var isCommitted = false;
			if (_isReorderingColumn)
			{
				if (_reorderColumnId is { } columnId && _reorderTargetId is { } targetId)
				{
					ColumnsViewModel.MoveColumn(columnId, targetId, _reorderAfterTarget);
					SaveColumnLayout();
					isCommitted = true;
				}

				e.Handled = true;
			}

			EndColumnReorder(isCommitted);
			HeaderGrid.ReleasePointerCapture(e.Pointer);
			_suppressHeaderSort = false;
		}

		private void HeaderGrid_ReorderPointerCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (_isReorderingColumn && e.Pointer.PointerId == _reorderPointerId && e.OriginalSource == HeaderGrid)
				EndColumnReorder();
		}

		private void HeaderGrid_ReorderKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (_isReorderingColumn && e.Key == VirtualKey.Escape)
			{
				EndColumnReorder();
				HeaderGrid.ReleasePointerCaptures();
				e.Handled = true;
			}
		}

		private void EndColumnReorder(bool isCommitted = false)
		{
			if (_reorderHeader is not null && _isReorderingColumn)
			{
				// Headers that already moved to their new columns must not animate back.
				var animate = !isCommitted;
				foreach (var id in _columnHeaders.Keys)
					SetColumnHeaderOffset(id, 0, animate);

				SetReorderedHeaderOpacity(1);
				ColumnReorderGhost.Visibility = Visibility.Collapsed;
			}

			_reorderHeader = null;
			_reorderColumnBounds = null;
			_reorderColumnId = null;
			_reorderTargetId = null;
			_isReorderingColumn = false;
		}

		private async Task CaptureColumnReorderGhostAsync(FrameworkElement header)
		{
			var bitmap = new RenderTargetBitmap();
			await bitmap.RenderAsync(header);
			if (!_isReorderingColumn || _reorderHeader != header)
				return;

			ColumnReorderGhostImage.Width = header.ActualWidth;
			ColumnReorderGhostImage.Height = header.ActualHeight;
			ColumnReorderGhostImage.Source = bitmap;
			SetReorderedHeaderOpacity(0);
		}

		private void SetReorderedHeaderOpacity(double opacity)
		{
			if (_reorderColumnId is not null && _columnHeaders.TryGetValue(_reorderColumnId, out var columnHeader))
			{
				columnHeader.Header.Opacity = opacity;
				columnHeader.Splitter.Opacity = opacity;
			}
		}

		private void ScrollHeaderWhileReordering(PointerRoutedEventArgs e)
		{
			if (ContentScroller is not { } scroller || scroller.ScrollableWidth <= 0)
				return;

			var x = e.GetCurrentPoint(scroller).Position.X;
			var offset = x < ColumnReorderScrollMargin
				? -ColumnReorderScrollStep
				: x > scroller.ViewportWidth - ColumnReorderScrollMargin ? ColumnReorderScrollStep : 0;

			if (offset != 0)
				scroller.ChangeView(Math.Clamp(scroller.HorizontalOffset + offset, 0, scroller.ScrollableWidth), null, null, true);
		}

		private void SetDetailsColumnsAsDefault_Click(object sender, RoutedEventArgs e)
		{
			FolderSettings?.SaveColumnsToTemplate();
		}

		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		private void ItemSelected_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox && checkBox.DataContext is ListedItem item && !FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Add(item);
		}

		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		private void ItemSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			if (sender is not CheckBox checkBox)
				return;

			if (checkBox.DataContext is ListedItem item && FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Remove(item);

			// Workaround for #17298
			checkBox.IsTabStop = false;
			checkBox.IsEnabled = false;
			checkBox.IsEnabled = true;
			checkBox.IsTabStop = true;
			FileList.Focus(FocusState.Programmatic);
		}

		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		private new void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			var selectionCheckbox = GetSelectionCheckbox(args.ItemContainer);

			selectionCheckbox.PointerEntered -= SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited -= SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled -= SelectionCheckbox_PointerCanceled;
			selectionCheckbox.Checked -= ItemSelected_Checked;
			selectionCheckbox.Unchecked -= ItemSelected_Unchecked;

			base.FileList_ContainerContentChanging(sender, args);
			if (args.InRecycleQueue)
				return;

			SetCheckboxSelectionState(args.Item, args.ItemContainer as ListViewItem);

			selectionCheckbox.PointerEntered += SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited += SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled += SelectionCheckbox_PointerCanceled;
		}

		private readonly ConditionalWeakTable<SelectorItem, Tuple<object?, CheckBox>> selectionCheckboxCache = new();

		// The template-root identity check invalidates the cache when a container is re-templated
		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		private CheckBox GetSelectionCheckbox(SelectorItem container)
		{
			var root = container.ContentTemplateRoot;
			if (selectionCheckboxCache.TryGetValue(container, out var cached) && ReferenceEquals(cached.Item1, root))
				return cached.Item2;

			var checkbox = (CheckBox)container.FindDescendant("SelectionCheckbox")!;
			selectionCheckboxCache.AddOrUpdate(container, new Tuple<object?, CheckBox>(root, checkbox));
			return checkbox;
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
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

		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		[DynamicWindowsRuntimeCast(typeof(StackPanel))]
		private void TagItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var tagName = ((sender as StackPanel)?.Children[TAG_TEXT_BLOCK] as TextBlock)?.Text;
			if (tagName is null)
				return;

			ParentShellPageInstance?.SubmitSearch(FolderSearch.FormatTagQuery(tagName));
		}

		[DynamicWindowsRuntimeCast(typeof(UserControl))]
		private void FileTag_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "PointerOver", true);
		}

		[DynamicWindowsRuntimeCast(typeof(UserControl))]
		private void FileTag_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "Normal", true);
		}

		[DynamicWindowsRuntimeCast(typeof(StackPanel))]
		[DynamicWindowsRuntimeCast(typeof(FontIcon))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		private async void RemoveTagIcon_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var parent = (sender as FontIcon)?.Parent as StackPanel;
			var tagName = (parent?.Children[TAG_TEXT_BLOCK] as TextBlock)?.Text;

			if (tagName is null || parent?.DataContext is not ListedItem item)
				return;

			var tagId = FileTagsSettingsService.GetTagsByName(tagName).FirstOrDefault()?.Uid;

			if (tagId is not null)
			{
				var fileTags = item.FileTags
					?? throw new InvalidOperationException("The selected item does not have initialized tags.");
				item.FileTags = fileTags
					.Except((string[])[tagId])
					.ToArray();

				if (ParentShellPageInstance is not null)
				{
					var shellViewModel = ParentShellPageInstance.GetRequiredShellViewModel();
					await shellViewModel.RefreshTagGroups();
				}
			}

			e.Handled = true;
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void SelectionCheckbox_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, true);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void SelectionCheckbox_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, false);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void SelectionCheckbox_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, false);
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewItem))]
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

		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		private void TextBlock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
		{
			if (sender is TextBlock textBlock)
				SetToolTip(textBlock);
		}

		private void SetToolTip(TextBlock textBlock)
		{
			ToolTipService.SetToolTip(textBlock, textBlock.IsTextTrimmed ? textBlock.Text : null);
		}

		private async void ViewSizeButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not FrameworkElement { DataContext: ListedItem item })
				return;

			item.IsCalculatingSize = true;
			var sizeProvider = Ioc.Default.GetRequiredService<Services.SizeProvider.ISizeProvider>();
			var updateTask = Task.Run(() => sizeProvider.UpdateAsync(item.ItemPath!, default));

			try
			{
				if (await Task.WhenAny(updateTask, Task.Delay(300)) != updateTask)
					item.ShowCalculatingText = true;
				await updateTask;
			}
			finally
			{
				item.ShowCalculatingText = false;
				item.IsCalculatingSize = false;
			}
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
			bool IsShown(string id) => columnsViewModel.GetColumn(id) is { IsHidden: false, UserCollapsed: false };

			var enableStatus = IsShown(DetailsLayoutColumnIds.GitStatus);
			var enableCommit = IsShown(DetailsLayoutColumnIds.GitLastCommitDate)
				|| IsShown(DetailsLayoutColumnIds.GitLastCommitMessage)
				|| IsShown(DetailsLayoutColumnIds.GitCommitAuthor)
				|| IsShown(DetailsLayoutColumnIds.GitLastCommitSha);
			return (enableStatus, enableCommit) switch
			{
				(true, true) => GitProperties.All,
				(true, false) => GitProperties.Status,
				(false, true) => GitProperties.Commit,
				(false, false) => GitProperties.None
			};
		}
	}
}
