// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Controls;
using Files.App.UserControls.Selection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
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

		private Dictionary<string, (ColumnDefinition Column, ColumnDefinition Divider)>? _headerColumnDefinitions;
		private ColumnDefinition[] _fixedHeaderColumnDefinitions = [];
		private Dictionary<string, int>? _headerColumnIndices;
		private readonly Dictionary<string, PropertyColumnHeader> _propertyColumnHeaders = [];
		private IReadOnlyDictionary<string, ShellPropertyColumnInfo>? _shellPropertyCatalog;
		private readonly HashSet<ListedItem> _shellPropertyLoads = [];
		private readonly List<ToggleMenuFlyoutItem> _propertyColumnMenuItems = [];
		private readonly List<(MenuFlyoutItem Item, MenuFlyoutSubItem Group)> _toggleAllMenuItems = [];
		private string? _contextColumnId;

		private const double ColumnReorderDragThreshold = 6;
		private const double ColumnReorderTouchDragThreshold = 16;
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
				foreach (var id in DetailsLayoutColumnIds.All)
					ColumnsViewModel.GetColumn(id).Update(FolderSettings.ColumnsViewModel.GetColumn(id));

				ColumnsViewModel.PropertyColumns = FolderSettings.ColumnsViewModel.PropertyColumns;
				ColumnsViewModel.ColumnOrder = FolderSettings.ColumnsViewModel.ColumnOrder;
			}

			shellViewModel.EnabledGitProperties = GetEnabledGitProperties(ColumnsViewModel);

			folderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			folderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
			folderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
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

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout does not have folder settings.");
			folderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			folderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
			folderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
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
			else
			{
				var settings = sender as ILayoutSettingsService;
				var isDefaultPath = FolderSettings?.IsPathUsingDefaultLayout(ParentShellPageInstance?.ShellViewModel?.CurrentFolder?.ItemPath);
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

			NameHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.Name ? folderSettings.DirectorySortDirection : null;
			TagHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.FileTag ? folderSettings.DirectorySortDirection : null;
			PathHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.Path ? folderSettings.DirectorySortDirection : null;
			OriginalPathHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.OriginalFolder ? folderSettings.DirectorySortDirection : null;
			DateDeletedHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.DateDeleted ? folderSettings.DirectorySortDirection : null;
			DateModifiedHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.DateModified ? folderSettings.DirectorySortDirection : null;
			DateCreatedHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.DateCreated ? folderSettings.DirectorySortDirection : null;
			FileTypeHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.FileType ? folderSettings.DirectorySortDirection : null;
			ItemSizeHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.Size ? folderSettings.DirectorySortDirection : null;
			SyncStatusHeader.ColumnSortOption = folderSettings.DirectorySortOption == SortOption.SyncStatus ? folderSettings.DirectorySortDirection : null;

			var sortColumnId = folderSettings.DirectorySortOption == SortOption.Column ? folderSettings.DirectorySortColumnId : null;
			GitStatusHeader.ColumnSortOption = sortColumnId == DetailsLayoutColumnIds.GitStatus ? folderSettings.DirectorySortDirection : null;
			GitLastCommitDateHeader.ColumnSortOption = sortColumnId == DetailsLayoutColumnIds.GitLastCommitDate ? folderSettings.DirectorySortDirection : null;
			GitLastCommitMessageHeader.ColumnSortOption = sortColumnId == DetailsLayoutColumnIds.GitLastCommitMessage ? folderSettings.DirectorySortDirection : null;
			GitCommitAuthorHeader.ColumnSortOption = sortColumnId == DetailsLayoutColumnIds.GitCommitAuthor ? folderSettings.DirectorySortDirection : null;
			GitLastCommitShaHeader.ColumnSortOption = sortColumnId == DetailsLayoutColumnIds.GitLastCommitSha ? folderSettings.DirectorySortDirection : null;
			foreach (var (id, propertyHeader) in _propertyColumnHeaders)
				propertyHeader.Header.ColumnSortOption = sortColumnId == id ? folderSettings.DirectorySortDirection : null;
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

			foreach (var (id, propertyHeader) in _propertyColumnHeaders)
				ColumnsViewModel.GetColumn(id).UserLength = propertyHeader.Column.Width;
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

		private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The details layout does not have folder settings.");
			folderSettings.ColumnsViewModel = ColumnsViewModel;
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
		{
			if (!FileList.Items.Any())
				return;

			foreach (var columnId in ColumnsViewModel.AllColumnIds.ToArray())
				ResizeColumnToFit(columnId);
		}

		private void AutoFitColumnsIfEnabled()
		{
			if (!UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout)
				return;

			// Trailing debounce so bursts of item adds and extended-property updates during a folder load collapse into one resize.
			_autoFitColumnsTimer ??= DispatcherQueue.CreateTimer();
			_autoFitColumnsTimer.Debounce(
				() => _ = Commands.AutoFitColumns.ExecuteAsync(),
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

		private void ResizeColumnToFit(string columnId)
		{
			if (!FileList.Items.Any())
				return;

			var maxItemLength = columnId switch
			{
				DetailsLayoutColumnIds.Icon => 40, // Check all items columns
				DetailsLayoutColumnIds.Name => FileList.Items.Cast<ListedItem>().Select(x => x.Name?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.GitLastCommitDate => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitDateHumanized?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.GitLastCommitMessage => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitMessage?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.GitCommitAuthor => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitAuthor?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.GitLastCommitSha => FileList.Items.Cast<ListedItem>().Select(x => (x as IGitItem)?.GitLastCommitSha?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.Tag => FileList.Items.Cast<ListedItem>().Select(x => x.FileTagsUI?.Sum(x => x?.Name?.Length ?? 0) ?? 0).Max(),
				DetailsLayoutColumnIds.Path => FileList.Items.Cast<ListedItem>().Select(x => x.ItemPath?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.OriginalPath => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemOriginalPath?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.DateDeleted => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemDateDeleted?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.DateModified => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateModified?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.DateCreated => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateCreated?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.ItemType => FileList.Items.Cast<ListedItem>().Select(x => x.ItemType?.Length ?? 0).Max(),
				DetailsLayoutColumnIds.Size => FileList.Items.Cast<ListedItem>().Select(x => x.FileSize?.Length ?? 0).Max(),
				_ when ColumnsViewModel.HasPropertyColumn(columnId) => FileList.Items.Cast<ListedItem>().Select(x => x.ShellPropertyValues is { } values && values.TryGetValue(columnId, out var value) ? value.Text?.Length ?? 0 : 0).Max(),
				_ => 0 // git status and sync status columns only show icons
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
				"ItemGitStatusTextBlock" => DetailsLayoutColumnIds.GitStatus,
				"ItemGitLastCommitDateTextBlock" => DetailsLayoutColumnIds.GitLastCommitDate,
				"ItemGitLastCommitMessageTextBlock" => DetailsLayoutColumnIds.GitLastCommitMessage,
				"ItemGitCommitAuthorTextBlock" => DetailsLayoutColumnIds.GitCommitAuthor,
				"ItemGitLastCommitShaTextBlock" => DetailsLayoutColumnIds.GitLastCommitSha,
				"ItemTagGrid" => DetailsLayoutColumnIds.Tag,
				"ItemPathTextBlock" => DetailsLayoutColumnIds.Path,
				"ItemOriginalPath" => DetailsLayoutColumnIds.OriginalPath,
				"ItemDateDeleted" => DetailsLayoutColumnIds.DateDeleted,
				"ItemDateModifiedTextBlock" => DetailsLayoutColumnIds.DateModified,
				"ItemDateCreatedTextBlock" => DetailsLayoutColumnIds.DateCreated,
				"ItemTypeTextBlock" => DetailsLayoutColumnIds.ItemType,
				"ItemSizeTextBlock" => DetailsLayoutColumnIds.Size,
				_ => DetailsLayoutColumnExtensions.GetId(element) is { } id && ColumnsViewModel.HasPropertyColumn(id) ? id : null,
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

		private void ColumnsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColumnsViewModel.OrderedColumns))
				ApplyHeaderColumnOrder();
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void ApplyHeaderColumnOrder()
		{
			var definitions = HeaderGrid.ColumnDefinitions;
			if (_headerColumnDefinitions is null)
			{
				var movableIds = DetailsLayoutColumnIds.All.Skip(1).ToArray();
				if (definitions.Count != movableIds.Length * 2 + 3)
					return;

				_headerColumnDefinitions = new(movableIds.Length);
				for (int i = 0; i < movableIds.Length; i++)
					_headerColumnDefinitions[movableIds[i]] = (definitions[i * 2 + 2], definitions[i * 2 + 3]);

				_fixedHeaderColumnDefinitions = [definitions[0], definitions[1], definitions[definitions.Count - 1]];
			}

			SyncPropertyColumnHeaders();

			var columnIndices = new Dictionary<string, int>(_headerColumnDefinitions.Count + _propertyColumnHeaders.Count);
			definitions.Clear();
			definitions.Add(_fixedHeaderColumnDefinitions[0]);
			definitions.Add(_fixedHeaderColumnDefinitions[1]);
			foreach (var id in ColumnsViewModel.OrderedColumns.Ids)
			{
				ColumnDefinition column, divider;
				if (_headerColumnDefinitions.TryGetValue(id, out var pair))
					(column, divider) = pair;
				else if (_propertyColumnHeaders.TryGetValue(id, out var propertyHeader))
					(column, divider) = (propertyHeader.Column, propertyHeader.Divider);
				else
					continue;

				columnIndices[id] = definitions.Count;
				definitions.Add(column);
				definitions.Add(divider);
			}
			definitions.Add(_fixedHeaderColumnDefinitions[2]);
			_headerColumnIndices = columnIndices;
			Grid.SetColumnSpan(ColumnReorderIndicator, definitions.Count);

			foreach (var child in HeaderGrid.Children)
			{
				if (child is FrameworkElement element &&
					DetailsLayoutColumnExtensions.GetId(element) is { } id &&
					columnIndices.TryGetValue(id, out var columnIndex))
				{
					Grid.SetColumn(element, element is Files.App.Controls.GridSplitter ? columnIndex + 1 : columnIndex);
					if (element is DataGridHeader header)
						header.TabIndex = columnIndex;
				}
			}
		}

		private void SyncPropertyColumnHeaders()
		{
			var propertyColumnIds = ColumnsViewModel.PropertyColumnIds;
			foreach (var id in _propertyColumnHeaders.Keys.Where(id => !propertyColumnIds.Contains(id)).ToArray())
			{
				var propertyHeader = _propertyColumnHeaders[id];
				propertyHeader.Attach(null);
				HeaderGrid.Children.Remove(propertyHeader.Header);
				HeaderGrid.Children.Remove(propertyHeader.Splitter);
				_propertyColumnHeaders.Remove(id);
			}

			foreach (var id in propertyColumnIds)
			{
				if (!_propertyColumnHeaders.TryGetValue(id, out var propertyHeader))
				{
					propertyHeader = CreatePropertyColumnHeader(id);
					_propertyColumnHeaders[id] = propertyHeader;
					HeaderGrid.Children.Add(propertyHeader.Header);
					HeaderGrid.Children.Add(propertyHeader.Splitter);
				}

				propertyHeader.Attach(ColumnsViewModel.GetColumn(id));
			}

			if (FolderSettings is not null)
				UpdateSortIndicator();
		}

		private PropertyColumnHeader CreatePropertyColumnHeader(string id)
		{
			var header = new DataGridHeader
			{
				Header = GetPropertyColumnDisplayName(id),
				Command = UpdateSortOptionsCommand,
				CommandParameter = id,
			};
			DetailsLayoutColumnExtensions.SetId(header, id);

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

			return new(new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto }, header, splitter);
		}

		private string GetPropertyColumnDisplayName(string id)
		{
			return _shellPropertyCatalog is not null && _shellPropertyCatalog.TryGetValue(id, out var info) ? info.DisplayName : id;
		}

		private async Task LoadShellPropertyCatalogAsync()
		{
			_shellPropertyCatalog = await ShellPropertyCatalog.GetAsync();
			foreach (var (id, propertyHeader) in _propertyColumnHeaders)
				propertyHeader.Header.Header = GetPropertyColumnDisplayName(id);
		}

		private void SetPropertyColumnVisibility(string id, bool isVisible)
		{
			if (isVisible == ColumnsViewModel.HasPropertyColumn(id))
				return;

			if (isVisible)
			{
				var width = _shellPropertyCatalog is not null && _shellPropertyCatalog.TryGetValue(id, out var info) ? info.DefaultWidth : 150;
				ColumnsViewModel.AddPropertyColumn(id, width);
				LoadShellPropertiesForRealizedItems();
			}
			else
			{
				ColumnsViewModel.RemovePropertyColumn(id);
			}

			SaveColumnLayout();
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
				RefreshShellPropertyCells(item);
			}
			finally
			{
				_shellPropertyLoads.Remove(item);
			}
		}

		[DynamicWindowsRuntimeCast(typeof(SelectorItem))]
		[DynamicWindowsRuntimeCast(typeof(UserControl))]
		private void RefreshShellPropertyCells(ListedItem item)
		{
			if (FileList.ContainerFromItem(item) is SelectorItem container &&
				container.ContentTemplateRoot is UserControl { Content: DetailsLayoutRowPanel panel })
			{
				panel.RefreshPropertyCells();
			}
		}

		private sealed class PropertyColumnHeader(ColumnDefinition column, ColumnDefinition divider, DataGridHeader header, Files.App.Controls.GridSplitter splitter)
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
				UpdateWidth();
			}

			private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
			{
				if (e.PropertyName is nameof(DetailsLayoutColumnItem.Length) or nameof(DetailsLayoutColumnItem.MaxLength))
					UpdateWidth();
			}

			private void UpdateWidth()
			{
				if (_item is null)
					return;

				Column.Width = _item.Length;
				Column.MaxWidth = _item.MaxLength;
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
			var visibility = _contextColumnId is not null ? Visibility.Visible : Visibility.Collapsed;
			MoveColumnLeftMenuFlyoutItem.Visibility = visibility;
			MoveColumnRightMenuFlyoutItem.Visibility = visibility;
			MoveColumnMenuFlyoutSeparator.Visibility = visibility;
			GroupByColumnMenuFlyoutItem.Visibility = visibility;
			if (_contextColumnId is { } groupColumnId && FolderSettings is { } groupSettings)
				GroupByColumnMenuFlyoutItem.IsChecked = IsGroupedByColumn(groupSettings, groupColumnId);

			if (_contextColumnId is { } id)
			{
				MoveColumnLeftMenuFlyoutItem.IsEnabled = ColumnsViewModel.CanMoveColumn(id, -1);
				MoveColumnRightMenuFlyoutItem.IsEnabled = ColumnsViewModel.CanMoveColumn(id, 1);
			}

			if (sender is MenuFlyout menuFlyout)
				PopulatePropertyColumnMenu(menuFlyout);

			foreach (var item in _propertyColumnMenuItems)
				item.IsChecked = item.Tag is string name && ColumnsViewModel.HasPropertyColumn(name);

			UpdateToggleAllMenuItems();
		}

		private void PopulatePropertyColumnMenu(MenuFlyout menuFlyout)
		{
			if (_toggleAllMenuItems.Count == 0)
			{
				AddToggleAllMenuItem(GeneralColumnsMenuFlyoutSubItem);
				AddToggleAllMenuItem(GitColumnsVisibilityMenuFlyoutSubItem);
			}

			MoreColumnsMenuFlyoutItem.IsEnabled = _shellPropertyCatalog is not null;
			if (_shellPropertyCatalog is null || _propertyColumnMenuItems.Count > 0)
				return;

			var insertIndex = menuFlyout.Items.IndexOf(GitColumnsVisibilityMenuFlyoutSubItem) + 1;
			foreach (var (resourceKey, propertyNames) in ShellPropertyCatalog.MenuCategories)
			{
				var infos = propertyNames
					.Select(name => _shellPropertyCatalog.TryGetValue(name, out var info) ? info : null)
					.OfType<ShellPropertyColumnInfo>()
					.ToArray();

				if (infos.Length == 0)
					continue;

				MenuFlyoutSubItem subItem;
				if (resourceKey == Strings.General)
				{
					subItem = GeneralColumnsMenuFlyoutSubItem;
					subItem.Items.Add(new MenuFlyoutSeparator());
				}
				else
				{
					subItem = new MenuFlyoutSubItem { Text = resourceKey.GetLocalizedResource() };
					menuFlyout.Items.Insert(insertIndex++, subItem);
					AddToggleAllMenuItem(subItem);
				}

				foreach (var info in infos)
					subItem.Items.Add(CreatePropertyColumnMenuItem(info));
			}
		}

		private void AddToggleAllMenuItem(MenuFlyoutSubItem group)
		{
			var item = new MenuFlyoutItem();
			item.Click += (_, _) => ToggleAllColumns(group);
			group.Items.Insert(0, item);
			group.Items.Insert(1, new MenuFlyoutSeparator());
			_toggleAllMenuItems.Add((item, group));
		}

		[DynamicWindowsRuntimeCast(typeof(ToggleMenuFlyoutItem))]
		private static ToggleMenuFlyoutItem[] GetVisibleColumnToggles(MenuFlyoutSubItem group)
		{
			return group.Items.OfType<ToggleMenuFlyoutItem>().Where(toggle => toggle.Visibility == Visibility.Visible).ToArray();
		}

		private void UpdateToggleAllMenuItems()
		{
			foreach (var (item, group) in _toggleAllMenuItems)
			{
				item.Text = GetVisibleColumnToggles(group).All(toggle => toggle.IsChecked)
					? Strings.HideAll.GetLocalizedResource()
					: Strings.ShowAll.GetLocalizedResource();
			}
		}

		private void ToggleAllColumns(MenuFlyoutSubItem group)
		{
			var toggles = GetVisibleColumnToggles(group);
			var isVisible = !toggles.All(toggle => toggle.IsChecked);
			foreach (var toggle in toggles)
			{
				if (toggle.IsChecked == isVisible)
					continue;

				toggle.IsChecked = isVisible;
				if (toggle.Tag is string name)
					SetPropertyColumnVisibility(name, isVisible);
			}

			ToggleMenuFlyoutItem_Click(group, new());
		}

		private async void MoreColumns_Click(object sender, RoutedEventArgs e)
		{
			var catalog = _shellPropertyCatalog ?? await ShellPropertyCatalog.GetAsync();
			var groupsPanel = new StackPanel { Spacing = 16 };
			var groups = new List<(StackPanel Panel, CheckBox[] CheckBoxes, Action UpdateToggleAll)>();

			var catalogGroups = catalog.Values
				.GroupBy(info => ShellPropertyCatalog.GetGroupDisplayName(info.Group))
				.OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

			foreach (var catalogGroup in catalogGroups)
			{
				var checkBoxes = catalogGroup
					.OrderBy(info => info.DisplayName, StringComparer.CurrentCultureIgnoreCase)
					.Select(info => new CheckBox { Content = info.DisplayName, Tag = info.Name, IsChecked = ColumnsViewModel.HasPropertyColumn(info.Name) })
					.ToArray();

				var toggleAllButton = new HyperlinkButton { HorizontalAlignment = HorizontalAlignment.Right };
				void UpdateToggleAll()
				{
					toggleAllButton.Content = checkBoxes.Where(checkBox => checkBox.Visibility == Visibility.Visible).All(checkBox => checkBox.IsChecked is true)
						? Strings.HideAll.GetLocalizedResource()
						: Strings.ShowAll.GetLocalizedResource();
				}

				toggleAllButton.Click += (_, _) =>
				{
					var visibleCheckBoxes = checkBoxes.Where(checkBox => checkBox.Visibility == Visibility.Visible).ToArray();
					var isChecked = !visibleCheckBoxes.All(checkBox => checkBox.IsChecked is true);
					foreach (var checkBox in visibleCheckBoxes)
						checkBox.IsChecked = isChecked;

					UpdateToggleAll();
				};

				var header = new Grid();
				header.Children.Add(new TextBlock { Text = catalogGroup.Key, Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"], VerticalAlignment = VerticalAlignment.Center });
				header.Children.Add(toggleAllButton);

				var groupPanel = new StackPanel();
				groupPanel.Children.Add(header);
				foreach (var checkBox in checkBoxes)
				{
					checkBox.Click += (_, _) => UpdateToggleAll();
					groupPanel.Children.Add(checkBox);
				}

				UpdateToggleAll();
				groupsPanel.Children.Add(groupPanel);
				groups.Add((groupPanel, checkBoxes, UpdateToggleAll));
			}

			var searchBox = new TextBox { PlaceholderText = Strings.Search.GetLocalizedResource(), Margin = new(0, 0, 0, 12) };
			searchBox.TextChanged += (_, _) =>
			{
				var query = searchBox.Text.Trim();
				foreach (var (panel, checkBoxes, updateToggleAll) in groups)
				{
					foreach (var checkBox in checkBoxes)
					{
						checkBox.Visibility = query.Length == 0 || (checkBox.Content as string)?.Contains(query, StringComparison.CurrentCultureIgnoreCase) is true
							? Visibility.Visible
							: Visibility.Collapsed;
					}

					panel.Visibility = checkBoxes.Any(checkBox => checkBox.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
					updateToggleAll();
				}
			};

			var scrollViewer = new ScrollViewer { Content = groupsPanel, MaxHeight = 480, Padding = new(0, 0, 16, 0) };
			var content = new Grid { RowDefinitions = { new() { Height = GridLength.Auto }, new() { Height = new(1, GridUnitType.Star) } } };
			Grid.SetRow(scrollViewer, 1);
			content.Children.Add(searchBox);
			content.Children.Add(scrollViewer);

			var dialog = new ContentDialog
			{
				Title = Strings.MoreColumns.GetLocalizedResource(),
				Content = content,
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = XamlRoot,
			};

			if (await dialog.TryShowAsync() != ContentDialogResult.Primary)
				return;

			foreach (var (_, checkBoxes, _) in groups)
			{
				foreach (var checkBox in checkBoxes)
				{
					if (checkBox.Tag is string name)
						SetPropertyColumnVisibility(name, checkBox.IsChecked is true);
				}
			}
		}

		private ToggleMenuFlyoutItem CreatePropertyColumnMenuItem(ShellPropertyColumnInfo info)
		{
			var item = new ToggleMenuFlyoutItem { Text = info.DisplayName, Tag = info.Name };
			item.Click += PropertyColumnMenuItem_Click;
			_propertyColumnMenuItems.Add(item);
			return item;
		}

		[DynamicWindowsRuntimeCast(typeof(ToggleMenuFlyoutItem))]
		private void PropertyColumnMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleMenuFlyoutItem { Tag: string name } item)
				SetPropertyColumnVisibility(name, item.IsChecked);
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
				if (_headerColumnIndices.TryGetValue(id, out var index) && ColumnsViewModel.GetColumn(id).Visibility == Visibility.Visible)
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
				_reorderHeader.RenderTransform = new TranslateTransform();
				_reorderHeader.Opacity = 0.7;
				Canvas.SetZIndex(_reorderHeader, 1);
				ColumnReorderIndicator.Visibility = Visibility.Visible;
			}

			((TranslateTransform)_reorderHeader.RenderTransform).X = x - _reorderStartX;

			var target = _reorderColumnBounds[^1];
			foreach (var bound in _reorderColumnBounds)
			{
				if (x < bound.Right)
				{
					target = bound;
					break;
				}
			}

			_reorderTargetId = target.Id;
			_reorderAfterTarget = x >= (target.Left + target.Right) / 2;
			ColumnReorderIndicatorTransform.X = (_reorderAfterTarget ? target.Right : target.Left) - ColumnReorderIndicator.Width / 2;

			e.Handled = true;
		}

		private void HeaderGrid_ReorderPointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_reorderHeader is null || e.Pointer.PointerId != _reorderPointerId)
				return;

			if (_isReorderingColumn)
			{
				if (_reorderColumnId is { } columnId && _reorderTargetId is { } targetId)
				{
					ColumnsViewModel.MoveColumn(columnId, targetId, _reorderAfterTarget);
					SaveColumnLayout();
				}

				e.Handled = true;
			}

			EndColumnReorder();
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

		private void EndColumnReorder()
		{
			if (_reorderHeader is not null && _isReorderingColumn)
			{
				_reorderHeader.RenderTransform = null;
				_reorderHeader.Opacity = 1;
				_reorderHeader.ClearValue(Canvas.ZIndexProperty);
				ColumnReorderIndicator.Visibility = Visibility.Collapsed;
			}

			_reorderHeader = null;
			_reorderColumnBounds = null;
			_reorderColumnId = null;
			_reorderTargetId = null;
			_isReorderingColumn = false;
		}

		private void SetDetailsColumnsAsDefault_Click(object sender, RoutedEventArgs e)
		{
			LayoutPreferencesManager.SetDefaultLayoutPreferences(ColumnsViewModel);
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
	}
}
