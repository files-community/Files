// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.UserControls.Selection;
using Files.App.ViewModels.LayoutModes;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using SortDirection = Files.Core.Data.Enums.SortDirection;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents the browser page of details page layout.
	/// </summary>
	public sealed partial class DetailsLayoutBrowser : StandardViewBase
	{
		private const int TAG_TEXT_BLOCK = 1;

		private uint _currentIconSize;

		private ListedItem? _nextItemToSelect;

		protected override uint IconSize
			=> _currentIconSize;

		protected override ListViewBase ListViewBase
			=> FileList;

		protected override SemanticZoom RootZoom
			=> RootGridZoom;

		private double _MaxWidthForRenameTextbox;
		public double MaxWidthForRenameTextbox
		{
			get => _MaxWidthForRenameTextbox;
			set
			{
				if (value != _MaxWidthForRenameTextbox)
				{
					_MaxWidthForRenameTextbox = value;
					NotifyPropertyChanged(nameof(MaxWidthForRenameTextbox));
				}
			}
		}

		private readonly DetailsLayoutBrowserViewModel ViewModel;

		public ScrollViewer? ContentScroller { get; private set; }

		#region Overrides
		public DetailsLayoutBrowser() : base()
		{
			InitializeComponent();

			ViewModel = new();
			ViewModel.ListViewBase = FileList;

			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
		}

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
			ContentScroller?.ChangeView(null, FileList.Items.IndexOf(e) * Convert.ToInt32(Application.Current.Resources["ListItemHeight"]), null, true); // Scroll to index * item height
		}

		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Any())
			{
				FileList.ScrollIntoView(SelectedItems.Last());
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

			if (FolderSettings.ColumnsViewModel is not null)
			{
				ViewModel.ColumnsViewModel.DateCreatedColumn = FolderSettings.ColumnsViewModel.DateCreatedColumn;
				ViewModel.ColumnsViewModel.DateDeletedColumn = FolderSettings.ColumnsViewModel.DateDeletedColumn;
				ViewModel.ColumnsViewModel.DateModifiedColumn = FolderSettings.ColumnsViewModel.DateModifiedColumn;
				ViewModel.ColumnsViewModel.IconColumn = FolderSettings.ColumnsViewModel.IconColumn;
				ViewModel.ColumnsViewModel.ItemTypeColumn = FolderSettings.ColumnsViewModel.ItemTypeColumn;
				ViewModel.ColumnsViewModel.NameColumn = FolderSettings.ColumnsViewModel.NameColumn;
				ViewModel.ColumnsViewModel.PathColumn = FolderSettings.ColumnsViewModel.PathColumn;
				ViewModel.ColumnsViewModel.OriginalPathColumn = FolderSettings.ColumnsViewModel.OriginalPathColumn;
				ViewModel.ColumnsViewModel.SizeColumn = FolderSettings.ColumnsViewModel.SizeColumn;
				ViewModel.ColumnsViewModel.StatusColumn = FolderSettings.ColumnsViewModel.StatusColumn;
				ViewModel.ColumnsViewModel.TagColumn = FolderSettings.ColumnsViewModel.TagColumn;
				ViewModel.ColumnsViewModel.GitStatusColumn = FolderSettings.ColumnsViewModel.GitStatusColumn;
				ViewModel.ColumnsViewModel.GitLastCommitDateColumn = FolderSettings.ColumnsViewModel.GitLastCommitDateColumn;
				ViewModel.ColumnsViewModel.GitLastCommitMessageColumn = FolderSettings.ColumnsViewModel.GitLastCommitMessageColumn;
				ViewModel.ColumnsViewModel.GitCommitAuthorColumn = FolderSettings.ColumnsViewModel.GitCommitAuthorColumn;
				ViewModel.ColumnsViewModel.GitLastCommitShaColumn = FolderSettings.ColumnsViewModel.GitLastCommitShaColumn;
			}

			_currentIconSize = FolderSettings.GetIconSize();
			FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			FolderSettings.GridViewSizeChangeRequested += FolderSettings_GridViewSizeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated += ZoomIn;
			FolderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
			FolderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
			ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;

			var parameters = (NavigationArguments)eventArgs.Parameter;
			if (parameters.IsLayoutSwitch)
				ReloadItemIcons();

			FilesystemViewModel_PageTypeUpdated(null, new PageTypeUpdatedEventArgs()
			{
				IsTypeCloudDrive = InstanceViewModel.IsPageTypeCloudDrive,
				IsTypeRecycleBin = InstanceViewModel.IsPageTypeRecycleBin,
				IsTypeGitRepository = InstanceViewModel.IsGitRepository,
				IsTypeSearchResults = InstanceViewModel.IsPageTypeSearchResults
			});

			RootGrid_SizeChanged(null, null);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated -= ZoomIn;
			FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
			FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
			ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
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

			textBox!.LostFocus -= RenameTextBox_LostFocus;
			textBox.KeyDown -= RenameTextBox_KeyDown;
			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;

			// Re-focus selected list item
			listViewItem?.Focus(FocusState.Programmatic);
		}

		protected override void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ParentShellPageInstance is null || IsRenamingItem)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
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
					var folders = ParentShellPageInstance?.SlimContentPage.SelectedItems?.Where(file => file.PrimaryItemAttribute == StorageItemTypes.Folder);
					foreach (ListedItem? folder in folders)
					{
						if (folder is not null)
							await NavigationHelpers.OpenPathInNewTab(folder.ItemPath);
					}
				}
				else if (ctrlPressed && shiftPressed)
				{
					NavigationHelpers.OpenInSecondaryPane(ParentShellPageInstance, SelectedItems.FirstOrDefault(item => item.PrimaryItemAttribute == StorageItemTypes.Folder));
				}
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.OpenPropertiesWindow(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				if (!ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
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
				if (isHeaderFocused && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
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

		public override void StartRenameItem()
		{
			StartRenameItem("ItemNameTextBox");

			if (FileList.ContainerFromItem(RenamingItem) is not ListViewItem listViewItem)
				return;

			var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
			if (textBox is null || textBox.FindParent<Grid>() is null)
				return;

			Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);
		}
		#endregion

		#region Folder settings
		private void FolderSettings_SortOptionPreferenceUpdated(object? sender, SortOption e)
		{
			UpdateSortIndicator();
		}

		private void FolderSettings_SortDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			UpdateSortIndicator();
		}

		private void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
		}

		private void FolderSettings_GridViewSizeChangeRequested(object? sender, EventArgs e)
		{
			var requestedIconSize = FolderSettings.GetIconSize(); // Get new icon size

			// Prevents reloading icons when the icon size hasn't changed
			if (requestedIconSize != _currentIconSize)
			{
				_currentIconSize = requestedIconSize; // Update icon size before refreshing
				ReloadItemIcons();
			}
		}
		#endregion

		#region FileList ListView
		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
		}

		private void FileList_LayoutUpdated(object? sender, object e)
		{
			FileList.LayoutUpdated -= FileList_LayoutUpdated;
			TryStartRenameNextItem(_nextItemToSelect!);
			_nextItemToSelect = null;
		}

		private void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item
				 && !UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
			{
				ParentShellPageInstance?.Up_Click();
			}
			ResetRenameDoubleClick();
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

		private async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var clickedItem = e.OriginalSource as FrameworkElement;
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;
			if (item is null)
				return;

			// Skip code if the control or shift key is pressed or if the user is using multiselect
			if (ctrlPressed ||
				shiftPressed ||
				clickedItem is Microsoft.UI.Xaml.Shapes.Rectangle)
			{
				e.Handled = true;
				return;
			}

			// Check if the setting to open items with a single click is turned on
			if (UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				ResetRenameDoubleClick();
				_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else
			{
				if (clickedItem is TextBlock && ((TextBlock)clickedItem).Name == "ItemName")
				{
					CheckRenameDoubleClick(clickedItem?.DataContext);
				}
				else if (IsRenamingItem)
				{
					ListViewItem listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
					if (listViewItem is not null)
					{
						var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
						await CommitRename(textBox);
					}
				}
			}
		}
		#endregion

		#region GridSplitter
		private void GridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			UpdateColumnLayout();
		}

		private void GridSplitter_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
			{
				UpdateColumnLayout();

				FolderSettings.ColumnsViewModel = ViewModel.ColumnsViewModel;
			}
		}

		private void GridSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void GridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			FolderSettings.ColumnsViewModel = ViewModel.ColumnsViewModel;

			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void GridSplitter_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as UIElement)?.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var columnToResize = Grid.GetColumn(sender as CommunityToolkit.WinUI.UI.Controls.GridSplitter) / 2 + 1;

			ViewModel.ResizeColumnToFit(columnToResize);

			e.Handled = true;
		}
		#endregion

		#region Tags
		private void TagItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var tagName = ((sender as StackPanel)?.Children[TAG_TEXT_BLOCK] as TextBlock)?.Text;
			if (tagName is null)
				return;

			ParentShellPageInstance?.SubmitSearch($"tag:{tagName}", false);
		}

		private void FileTag_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "PointerOver", true);
		}

		private void FileTag_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "Normal", true);
		}

		private void TagIcon_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var parent = (sender as FontIcon)?.Parent as StackPanel;
			var tagName = (parent?.Children[TAG_TEXT_BLOCK] as TextBlock)?.Text;

			if (tagName is null || parent?.DataContext is not ListedItem item)
				return;

			var tagId = FileTagsSettingsService.GetTagsByName(tagName).FirstOrDefault()?.Uid;

			item.FileTags = item.FileTags
				.Except(new string[] { tagId })
				.ToArray();

			e.Handled = true;
		}
		#endregion

		#region Selection CheckBox
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
		#endregion

		#region Name TextBlock
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
		#endregion

		#region Other events
		private void FilesystemViewModel_PageTypeUpdated(object? sender, PageTypeUpdatedEventArgs e)
		{
			// Show original path and date deleted columns in Recycle Bin
			if (e.IsTypeRecycleBin)
			{
				ViewModel.ColumnsViewModel.OriginalPathColumn.Show();
				ViewModel.ColumnsViewModel.DateDeletedColumn.Show();
			}
			else
			{
				ViewModel.ColumnsViewModel.OriginalPathColumn.Hide();
				ViewModel.ColumnsViewModel.DateDeletedColumn.Hide();
			}

			// Show cloud drive item status column
			if (e.IsTypeCloudDrive)
				ViewModel.ColumnsViewModel.StatusColumn.Show();
			else
				ViewModel.ColumnsViewModel.StatusColumn.Hide();

			// Show git columns in git repository
			if (e.IsTypeGitRepository)
			{
				ViewModel.ColumnsViewModel.GitCommitAuthorColumn.Show();
				ViewModel.ColumnsViewModel.GitLastCommitDateColumn.Show();
				ViewModel.ColumnsViewModel.GitLastCommitMessageColumn.Show();
				ViewModel.ColumnsViewModel.GitLastCommitShaColumn.Show();
				ViewModel.ColumnsViewModel.GitStatusColumn.Show();
			}
			else
			{
				ViewModel.ColumnsViewModel.GitCommitAuthorColumn.Hide();
				ViewModel.ColumnsViewModel.GitLastCommitDateColumn.Hide();
				ViewModel.ColumnsViewModel.GitLastCommitMessageColumn.Hide();
				ViewModel.ColumnsViewModel.GitLastCommitShaColumn.Hide();
				ViewModel.ColumnsViewModel.GitStatusColumn.Hide();
			}

			// Show path columns in git repository
			if (e.IsTypeSearchResults)
				ViewModel.ColumnsViewModel.PathColumn.Show();
			else
				ViewModel.ColumnsViewModel.PathColumn.Hide();

			UpdateSortIndicator();
		}

		private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (IsRenamingItem)
			{
				ValidateItemNameInputText(textBox, args, (showError) =>
				{
					FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
					FileNameTeachingTip.IsOpen = showError;
				});
			}
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

		private void RootGrid_SizeChanged(object? sender, SizeChangedEventArgs? e)
		{
			ViewModel.ColumnsViewModel.SetDesiredSize(Math.Max(0, RootGrid.ActualWidth - 80));
			MaxWidthForRenameTextbox = Math.Max(0, RootGrid.ActualWidth - 80);
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
		#endregion

		private void UpdateSortIndicator()
		{
			NameHeader.ColumnSortOption =         FolderSettings.DirectorySortOption == SortOption.Name ?           FolderSettings.DirectorySortDirection : null;
			TagHeader.ColumnSortOption =          FolderSettings.DirectorySortOption == SortOption.FileTag ?        FolderSettings.DirectorySortDirection : null;
			PathHeader.ColumnSortOption =         FolderSettings.DirectorySortOption == SortOption.Path ?           FolderSettings.DirectorySortDirection : null;
			OriginalPathHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.OriginalFolder ? FolderSettings.DirectorySortDirection : null;
			DateDeletedHeader.ColumnSortOption =  FolderSettings.DirectorySortOption == SortOption.DateDeleted ?    FolderSettings.DirectorySortDirection : null;
			DateModifiedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateModified ?   FolderSettings.DirectorySortDirection : null;
			DateCreatedHeader.ColumnSortOption =  FolderSettings.DirectorySortOption == SortOption.DateCreated ?    FolderSettings.DirectorySortDirection : null;
			FileTypeHeader.ColumnSortOption =     FolderSettings.DirectorySortOption == SortOption.FileType ?       FolderSettings.DirectorySortDirection : null;
			ItemSizeHeader.ColumnSortOption =     FolderSettings.DirectorySortOption == SortOption.Size ?           FolderSettings.DirectorySortDirection : null;
			SyncStatusHeader.ColumnSortOption =   FolderSettings.DirectorySortOption == SortOption.SyncStatus ?     FolderSettings.DirectorySortDirection : null;
		}

		public void UpdateColumnLayout()
		{
			ViewModel.ColumnsViewModel.IconColumn.UserLength = new GridLength(Column2.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.NameColumn.UserLength = new GridLength(Column3.ActualWidth, GridUnitType.Pixel);

			// Git
			ViewModel.ColumnsViewModel.GitStatusColumn.UserLength = new GridLength(GitStatusColumnDefinition.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.GitLastCommitDateColumn.UserLength = new GridLength(GitLastCommitDateColumnDefinition.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.GitLastCommitMessageColumn.UserLength = new GridLength(GitLastCommitMessageColumnDefinition.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.GitCommitAuthorColumn.UserLength = new GridLength(GitCommitAuthorColumnDefinition.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.GitLastCommitShaColumn.UserLength = new GridLength(GitLastCommitShaColumnDefinition.ActualWidth, GridUnitType.Pixel);

			ViewModel.ColumnsViewModel.TagColumn.UserLength = new GridLength(Column4.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.PathColumn.UserLength = new GridLength(Column5.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.OriginalPathColumn.UserLength = new GridLength(Column6.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.DateDeletedColumn.UserLength = new GridLength(Column7.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.DateModifiedColumn.UserLength = new GridLength(Column8.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.DateCreatedColumn.UserLength = new GridLength(Column9.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.ItemTypeColumn.UserLength = new GridLength(Column10.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.SizeColumn.UserLength = new GridLength(Column11.ActualWidth, GridUnitType.Pixel);
			ViewModel.ColumnsViewModel.StatusColumn.UserLength = new GridLength(Column12.ActualWidth, GridUnitType.Pixel);
		}

		private async Task ReloadItemIcons()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, _currentIconSize);
			}
		}
	}
}
