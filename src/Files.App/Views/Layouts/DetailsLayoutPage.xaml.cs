// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.UserControls.Selection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using SortDirection = Files.Core.Data.Enums.SortDirection;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the browser page of Details View
	/// </summary>
	public sealed partial class DetailsLayoutPage : BaseGroupableLayoutPage
	{
		// Constants

		private const int TAG_TEXT_BLOCK = 1;

		// Fields

		private uint currentIconSize;

		private ListedItem? _nextItemToSelect;

		// Properties

		protected override uint IconSize => currentIconSize;
		protected override ListViewBase ListViewBase => FileList;
		protected override SemanticZoom RootZoom => RootGridZoom;

		public DetailsLayoutViewModel ViewModel { get; } = new();

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

		// Constructor

		public DetailsLayoutPage() : base()
		{
			InitializeComponent();
			DataContext = this;
			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
		}

		// Methods

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
			ContentScroller?.ChangeView(null, FileList.Items.IndexOf(e) * Convert.ToInt32(Application.Current.Resources["ListItemHeight"]), null, true); // Scroll to index * item height
		}

		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems?.Any() ?? false)
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				ContentScroller?.ChangeView(null, FileList.Items.IndexOf(SelectedItems.Last()) * Convert.ToInt32(Application.Current.Resources["ListItemHeight"]), null, false);
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

			currentIconSize = LayoutPreferencesManager.GetIconSize();
			LayoutPreferencesManager.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			LayoutPreferencesManager.GridViewSizeChangeRequested += FolderSettings_GridViewSizeChangeRequested;
			LayoutPreferencesManager.GroupOptionPreferenceUpdated += ZoomIn;
			LayoutPreferencesManager.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
			LayoutPreferencesManager.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
			ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;

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
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			LayoutPreferencesManager.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			LayoutPreferencesManager.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
			LayoutPreferencesManager.GroupOptionPreferenceUpdated -= ZoomIn;
			LayoutPreferencesManager.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
			LayoutPreferencesManager.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
			ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
		}

		private void FileList_LayoutUpdated(object? sender, object e)
		{
			FileList.LayoutUpdated -= FileList_LayoutUpdated;
			TryStartRenameNextItem(_nextItemToSelect!);
			_nextItemToSelect = null;
		}

		private void FolderSettings_SortOptionPreferenceUpdated(object? sender, SortOption e)
		{
			ViewModel.UpdateSortIndicator();
		}

		private void FolderSettings_SortDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			ViewModel.UpdateSortIndicator();
		}

		private void FilesystemViewModel_PageTypeUpdated(object? sender, PageTypeUpdatedEventArgs e)
		{
			ViewModel.UpdateDetailsLayoutColumnsVisibilities(e);
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

		private async void FolderSettings_GridViewSizeChangeRequested(object? sender, EventArgs e)
		{
			var requestedIconSize = LayoutPreferencesManager.GetIconSize(); // Get new icon size

			// Prevents reloading icons when the icon size hasn't changed
			if (requestedIconSize != currentIconSize)
			{
				currentIconSize = requestedIconSize; // Update icon size before refreshing
				await ReloadItemIconsAsync();
			}
		}

		private async Task ReloadItemIconsAsync()
		{
			if (ParentShellPageInstance is null)
				return;

			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			var filesAndFolders = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList();
			foreach (ListedItem listedItem in filesAndFolders)
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemPropertiesAsync(listedItem, currentIconSize);
			}

			if (ParentShellPageInstance.FilesystemViewModel.EnabledGitProperties is not GitProperties.None)
			{
				await Task.WhenAll(filesAndFolders.Select(item =>
				{
					if (item is GitItem gitItem)
						return ParentShellPageInstance.FilesystemViewModel.LoadGitPropertiesAsync(gitItem);

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
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item
				 && !UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				await Commands.OpenItem.ExecuteAsync();
			}
			else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
			{
				await Commands.NavigateUp.ExecuteAsync();
			}
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
				LayoutPreferencesManager.ColumnItems = ViewModel.ColumnItems;
			}
		}

		private void UpdateColumnLayout()
		{
			ViewModel.IconColumn.Width = IconColumnDefinition.ActualWidth;
			ViewModel.NameColumn.Width = NameColumnDefinition.ActualWidth;
			ViewModel.GitStatusColumn.Width = GitStatusColumnDefinition.ActualWidth;
			ViewModel.GitLastCommitDateColumn.Width = GitLastCommitDateColumnDefinition.ActualWidth;
			ViewModel.GitLastCommitMessageColumn.Width = GitLastCommitMessageColumnDefinition.ActualWidth;
			ViewModel.GitCommitAuthorColumn.Width = GitCommitAuthorColumnDefinition.ActualWidth;
			ViewModel.GitLastCommitShaColumn.Width = GitLastCommitShaColumnDefinition.ActualWidth;
			ViewModel.TagColumn.Width = TagsColumnDefinition.ActualWidth;
			ViewModel.PathColumn.Width = PathColumnDefinition.ActualWidth;
			ViewModel.OriginalPathColumn.Width = OriginalPathColumnDefinition.ActualWidth;
			ViewModel.DateDeletedColumn.Width = DateDeletedColumnDefinition.ActualWidth;
			ViewModel.DateModifiedColumn.Width = DateModifiedColumnDefinition.ActualWidth;
			ViewModel.DateCreatedColumn.Width = DateCreatedColumnDefinition.ActualWidth;
			ViewModel.ItemTypeColumn.Width = TypeColumnDefinition.ActualWidth;
			ViewModel.SizeColumn.Width = SizeColumnDefinition.ActualWidth;
			ViewModel.StatusColumn.Width = CloudSyncStatusColumnDefinition.ActualWidth;
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
			LayoutPreferencesManager.ColumnItems = ViewModel.ColumnItems;
			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void GridSplitter_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as UIElement)?.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
		}

		private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			LayoutPreferencesManager.ColumnItems = ViewModel.ColumnItems;
			ParentShellPageInstance.FilesystemViewModel.EnabledGitProperties = ViewModel.GetEnabledGitColumns();
		}

		private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var columnToResize = Grid.GetColumn(sender as CommunityToolkit.WinUI.UI.Controls.GridSplitter) / 2 + 1;
			ResizeColumnToFit(columnToResize);

			e.Handled = true;
		}

		private void SizeAllColumnsToFit_Click(object sender, RoutedEventArgs e)
		{
			// If there aren't items, do not make columns fit
			if (!FileList.Items.Any())
				return;

			// For scalability, just count the # of public `ColumnViewModel` properties in ViewModel
			int totalColumnCount = ViewModel.ColumnItems.Count;

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
				4 => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitDateHumanized?.Length ?? 0).Max(), // git
				5 => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitMessage?.Length ?? 0).Max(), // git
				6 => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitAuthor?.Length ?? 0).Max(), // git
				7 => FileList.Items.Cast<ListedItem>().Select(x => (x as GitItem)?.GitLastCommitSha?.Length ?? 0).Max(), // git
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
					2 => ViewModel.NameColumn,
					3 => ViewModel.GitStatusColumn,
					4 => ViewModel.GitLastCommitDateColumn,
					5 => ViewModel.GitLastCommitMessageColumn,
					6 => ViewModel.GitCommitAuthorColumn,
					7 => ViewModel.GitLastCommitShaColumn,
					8 => ViewModel.TagColumn,
					9 => ViewModel.PathColumn,
					10 => ViewModel.OriginalPathColumn,
					11 => ViewModel.DateDeletedColumn,
					12 => ViewModel.DateModifiedColumn,
					13 => ViewModel.DateCreatedColumn,
					14 => ViewModel.ItemTypeColumn,
					15 => ViewModel.SizeColumn,
					_ => ViewModel.StatusColumn
				};

				if (columnToResize == 2) // file name column
					columnSizeToFit += 20;

				var minFitLength = Math.Max(columnSizeToFit, column.MinWidth);
				var maxFitLength = Math.Min(minFitLength + 36, column.MaxWidth); // 36 to account for SortIcon & padding

				column.Width = maxFitLength;
			}

			LayoutPreferencesManager.ColumnItems = ViewModel.ColumnItems;
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

		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
		}

		private void SetDetailsColumnsAsDefault_Click(object sender, RoutedEventArgs e)
		{
			LayoutPreferencesManager.SetDefaultLayoutPreferences(ViewModel.ColumnItems);
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

		private void RemoveTagIcon_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var parent = (sender as FontIcon)?.Parent as StackPanel;
			var tagName = (parent?.Children[TAG_TEXT_BLOCK] as TextBlock)?.Text;

			if (tagName is null || parent?.DataContext is not ListedItem item)
				return;

			var tagId = FileTagsSettingsService.GetTagsByName(tagName).FirstOrDefault()?.Uid;

			if (tagId is not null)
			{
				item.FileTags = item.FileTags
					.Except(new string[] { tagId })
					.ToArray();
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
	}
}
