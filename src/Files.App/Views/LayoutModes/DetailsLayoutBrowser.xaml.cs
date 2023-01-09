using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Files.App.EventArguments;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.Interacts;
using Files.App.UserControls;
using Files.App.UserControls.Selection;
using Files.App.ViewModels;
using Files.Shared.Enums;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using UWPToWinAppSDKUpgradeHelpers;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

using SortDirection = Files.Shared.Enums.SortDirection;

namespace Files.App.Views.LayoutModes
{
	public sealed partial class DetailsLayoutBrowser : BaseLayout
	{
		private uint currentIconSize;

		private InputCursor arrowCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));

		private InputCursor resizeCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.SizeWestEast, 1));

		protected override uint IconSize => currentIconSize;

		protected override ItemsControl ItemsControl => FileList;

		public ColumnsViewModel ColumnsViewModel { get; } = new();

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

		private RelayCommand<string>? UpdateSortOptionsCommand { get; set; }

		public ScrollViewer? ContentScroller { get; private set; }

		public DetailsLayoutBrowser() : base()
		{
			InitializeComponent();
			this.DataContext = this;

			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
		}

		protected override void HookEvents()
		{
			UnhookEvents();
			ItemManipulationModel.FocusFileListInvoked += ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked += ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked += ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked += ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked += ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked += ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked += ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked += ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked += ItemManipulationModel_ScrollIntoViewInvoked;
		}

		private void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
			ContentScroller?.ChangeView(null, FileList.Items.IndexOf(e) * Convert.ToInt32(Application.Current.Resources["ListItemHeight"]), null, true); // Scroll to index * item height
		}

		private void ItemManipulationModel_StartRenameItemInvoked(object? sender, EventArgs e)
		{
			StartRenameItem();
		}

		private void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Any())
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				(FileList.ContainerFromItem(SelectedItems.Last()) as ListViewItem)?.Focus(FocusState.Keyboard);
			}
		}

		private void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (FileList?.Items.Contains(e) ?? false)
				FileList.SelectedItems.Add(e);
		}

		private void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (FileList?.Items.Contains(e) ?? false)
				FileList.SelectedItems.Remove(e);
		}

		private void ItemManipulationModel_InvertSelectionInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Count < GetAllItems().Count() / 2)
			{
				var oldSelectedItems = SelectedItems.ToList();
				ItemManipulationModel.SelectAllItems();
				ItemManipulationModel.RemoveSelectedItems(oldSelectedItems);
			}
			else
			{
				List<ListedItem> newSelectedItems = GetAllItems()
					.Cast<ListedItem>()
					.Except(SelectedItems)
					.ToList();

				ItemManipulationModel.SetSelectedItems(newSelectedItems);
			}
		}

		private void ItemManipulationModel_ClearSelectionInvoked(object? sender, EventArgs e)
		{
			FileList.SelectedItems.Clear();
		}

		private void ItemManipulationModel_SelectAllItemsInvoked(object? sender, EventArgs e)
		{
			FileList.SelectAll();
		}

		private void ItemManipulationModel_FocusFileListInvoked(object? sender, EventArgs e)
		{
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
			var isFileListFocused = DependencyObjectHelpers.FindParent<ListViewBase>(focusedElement) == FileList;
			if (!isFileListFocused)
				FileList.Focus(FocusState.Programmatic);
		}

		private void ZoomIn(object? sender, GroupOption option)
		{
			if (option == GroupOption.None)
				RootGridZoom.IsZoomedInViewActive = true;
		}

		protected override void UnhookEvents()
		{
			if (ItemManipulationModel is null)
				return;

			ItemManipulationModel.FocusFileListInvoked -= ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked -= ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked -= ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked -= ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked -= ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked -= ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked -= ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked -= ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked -= ItemManipulationModel_ScrollIntoViewInvoked;
		}

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
				navArgs.FocusOnNavigation = true;

			base.OnNavigatedTo(eventArgs);

			if (FolderSettings.ColumnsViewModel is not null)
			{
				ColumnsViewModel.DateCreatedColumn = FolderSettings.ColumnsViewModel.DateCreatedColumn;
				ColumnsViewModel.DateDeletedColumn = FolderSettings.ColumnsViewModel.DateDeletedColumn;
				ColumnsViewModel.DateModifiedColumn = FolderSettings.ColumnsViewModel.DateModifiedColumn;
				ColumnsViewModel.IconColumn = FolderSettings.ColumnsViewModel.IconColumn;
				ColumnsViewModel.ItemTypeColumn = FolderSettings.ColumnsViewModel.ItemTypeColumn;
				ColumnsViewModel.NameColumn = FolderSettings.ColumnsViewModel.NameColumn;
				ColumnsViewModel.OriginalPathColumn = FolderSettings.ColumnsViewModel.OriginalPathColumn;
				ColumnsViewModel.SizeColumn = FolderSettings.ColumnsViewModel.SizeColumn;
				ColumnsViewModel.StatusColumn = FolderSettings.ColumnsViewModel.StatusColumn;
				ColumnsViewModel.TagColumn = FolderSettings.ColumnsViewModel.TagColumn;
			}

			currentIconSize = FolderSettings.GetIconSize();
			FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			FolderSettings.GridViewSizeChangeRequested += FolderSettings_GridViewSizeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated += ZoomIn;
			FolderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
			FolderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
			ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;

			var parameters = (NavigationArguments)eventArgs.Parameter;
			if (parameters.IsLayoutSwitch)
				ReloadItemIcons();

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

			FilesystemViewModel_PageTypeUpdated(null, new PageTypeUpdatedEventArgs()
			{
				IsTypeCloudDrive = InstanceViewModel.IsPageTypeCloudDrive,
				IsTypeRecycleBin = InstanceViewModel.IsPageTypeRecycleBin
			});

			RootGrid_SizeChanged(null, null);
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
			// This code updates which columns are hidden and which ones are shwn
			if (!e.IsTypeRecycleBin)
			{
				ColumnsViewModel.DateDeletedColumn.Hide();
				ColumnsViewModel.OriginalPathColumn.Hide();
			}
			else
			{
				ColumnsViewModel.OriginalPathColumn.Show();
				ColumnsViewModel.DateDeletedColumn.Show();
			}

			if (!e.IsTypeCloudDrive)
				ColumnsViewModel.StatusColumn.Hide();
			else
				ColumnsViewModel.StatusColumn.Show();

			UpdateSortIndicator();
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

		private void SelectionRectangle_SelectionEnded(object? sender, EventArgs e)
		{
			FileList.Focus(FocusState.Programmatic);
		}

		private void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
		}

		private async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItems = FileList.SelectedItems.Cast<ListedItem>().Where(x => x is not null).ToList();
			if (SelectedItems.Count == 1 && App.AppModel.IsQuickLookAvailable)
			{
				await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance, true);
			}
		}

		override public void StartRenameItem()
		{
			RenamingItem = SelectedItem;
			if (RenamingItem is null)
				return;
			int extensionLength = RenamingItem.FileExtension?.Length ?? 0;
			ListViewItem? listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
			TextBox? textBox = null;
			if (listViewItem is null)
				return;
			TextBlock? textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
			textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
			textBox!.Text = textBlock!.Text;
			OldItemName = textBlock.Text;
			textBlock.Visibility = Visibility.Collapsed;
			textBox.Visibility = Visibility.Visible;
			Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;

			int selectedTextLength = SelectedItem.Name.Length;
			if (!SelectedItem.IsShortcut && UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				selectedTextLength -= extensionLength;
			textBox.Select(0, selectedTextLength);
			IsRenamingItem = true;
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

		private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Escape)
			{
				TextBox? textBox = sender as TextBox;
				textBox!.LostFocus -= RenameTextBox_LostFocus;
				textBox.Text = OldItemName;
				EndRename(textBox);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Enter)
			{
				TextBox? textBox = sender as TextBox;
				if (textBox is null)
					return;
				textBox.LostFocus -= RenameTextBox_LostFocus;
				CommitRename(textBox);
				e.Handled = true;
			}
		}

		private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// This check allows the user to use the text box context menu without ending the rename
			if (!(FocusManager.GetFocusedElement(XamlRoot) is AppBarButton or Popup))
			{
				TextBox? textBox = e.OriginalSource as TextBox;
				CommitRename(textBox!);
			}
		}

		private async void CommitRename(TextBox textBox)
		{
			EndRename(textBox);
			string newItemName = textBox.Text.Trim().TrimEnd('.');
			await UIFilesystemHelpers.RenameFileItemAsync(RenamingItem, newItemName, ParentShellPageInstance);
		}

		private void EndRename(TextBox textBox)
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

		private async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
			var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) is not null;
			var isFooterFocused = focusedElement is HyperlinkButton;

			if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
			{
				if (IsRenamingItem)
					return;

				e.Handled = true;

				if (ctrlPressed)
				{
					var folders = ParentShellPageInstance?.SlimContentPage.SelectedItems?.Where(file => file.PrimaryItemAttribute == StorageItemTypes.Folder);
					foreach (ListedItem? folder in folders)
					{
						if (folder is not null)
							await NavigationHelpers.OpenPathInNewTab(folder.ItemPath);
					}
				}
				else
				{
					await NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
					FileList.SelectedIndex = 0;
				}
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				if (!IsRenamingItem && !isHeaderFocused && !isFooterFocused && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
				{
					e.Handled = true;
					await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance);
				}
			}
			else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
			{
				// Unfocus the GridView so keyboard shortcut can be handled
				this.Focus(FocusState.Pointer);
			}
			else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
			{
				// Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
				this.Focus(FocusState.Pointer);
			}
			else if (e.Key == VirtualKey.Down)
			{
				if (!IsRenamingItem && isHeaderFocused && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
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

		protected override void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance is null)
				return;
			if (ParentShellPageInstance.CurrentPageType == typeof(DetailsLayoutBrowser) && !IsRenamingItem)
			{
				// Don't block the various uses of enter key (key 13)
				var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
				var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) is not null;
				if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Enter) == CoreVirtualKeyStates.Down
					|| (focusedElement is Button && !isHeaderFocused) // Allow jumpstring when header is focused
					|| focusedElement is TextBox
					|| focusedElement is PasswordBox
					|| DependencyObjectHelpers.FindParent<ContentDialog>(focusedElement) is not null)
				{
					return;
				}

				base.Page_CharacterReceived(sender, args);
			}
		}

		protected override bool CanGetItemFromElement(object element)
			=> element is ListViewItem;

		private void FolderSettings_GridViewSizeChangeRequested(object? sender, EventArgs e)
		{
			var requestedIconSize = FolderSettings.GetIconSize(); // Get new icon size

			// Prevents reloading icons when the icon size hasn't changed
			if (requestedIconSize != currentIconSize)
			{
				currentIconSize = requestedIconSize; // Update icon size before refreshing
				ReloadItemIcons();
			}
		}

		private async void ReloadItemIcons()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, currentIconSize);
			}
		}

		private void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;
			if (item is null)
				return;
			// Skip code if the control or shift key is pressed or if the user is using multiselect
			if (ctrlPressed || shiftPressed || AppModel.MultiselectEnabled)
				return;

			// Check if the setting to open items with a single click is turned on
			if (item is not null
				&& UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				ResetRenameDoubleClick();
				_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else
			{
				var clickedItem = e.OriginalSource as FrameworkElement;
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
						CommitRename(textBox);
					}
				}
			}
		}

		private void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item
				 && !UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else
			{
				ParentShellPageInstance.Up_Click();
			}
			ResetRenameDoubleClick();
		}

		#region IDisposable

		public override void Dispose()
		{
			base.Dispose();
			UnhookEvents();
			CommandsViewModel?.Dispose();
		}

		#endregion IDisposable

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
			ColumnsViewModel.IconColumn.UserLength = new GridLength(Column1.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.NameColumn.UserLength = new GridLength(Column2.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.TagColumn.UserLength = new GridLength(Column3.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.OriginalPathColumn.UserLength = new GridLength(Column4.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.DateDeletedColumn.UserLength = new GridLength(Column5.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.DateModifiedColumn.UserLength = new GridLength(Column6.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.DateCreatedColumn.UserLength = new GridLength(Column7.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.ItemTypeColumn.UserLength = new GridLength(Column8.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.SizeColumn.UserLength = new GridLength(Column9.ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.StatusColumn.UserLength = new GridLength(Column10.ActualWidth, GridUnitType.Pixel);
		}

		private void RootGrid_SizeChanged(object? sender, SizeChangedEventArgs? e)
		{
			ColumnsViewModel.SetDesiredSize(Math.Max(0, RootGrid.ActualWidth - 80));
			MaxWidthForRenameTextbox = Math.Max(0, RootGrid.ActualWidth - 80);
		}

		private void GridSplitter_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
		{
			this.ChangeCursor(resizeCursor);
		}

		private void GridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			FolderSettings.ColumnsViewModel = ColumnsViewModel;
			this.ChangeCursor(arrowCursor);
		}

		private void GridSplitter_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as UIElement).ChangeCursor(resizeCursor);
		}

		private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			FolderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var columnToResize = (Grid.GetColumn(sender as CommunityToolkit.WinUI.UI.Controls.GridSplitter) - 1) / 2;
			ResizeColumnToFit(columnToResize);
			e.Handled = true;
		}

		private void SizeAllColumnsToFit_Click(object sender, RoutedEventArgs e)
		{
			if (!FileList.Items.Any())
				return;

			// for scalability, just count the # of public `ColumnViewModel` properties in ColumnsViewModel
			int totalColumnCount = ColumnsViewModel.GetType().GetProperties().Count(prop => prop.PropertyType == typeof(ColumnViewModel));
			for (int columnIndex = 1; columnIndex <= totalColumnCount; columnIndex++)
				ResizeColumnToFit(columnIndex);
		}

		private void ResizeColumnToFit(int columnToResize)
		{
			if (!FileList.Items.Any())
				return;

			var maxItemLength = columnToResize switch
			{
				1 => FileList.Items.Cast<ListedItem>().Select(x => x.Name?.Length ?? 0).Max(), // file name column
				2 => FileList.Items.Cast<ListedItem>().Select(x => x.FileTagsUI?.FirstOrDefault()?.TagName?.Length ?? 0).Max(), // file tag column
				3 => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemOriginalPath?.Length ?? 0).Max(), // original path column
				4 => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemDateDeleted?.Length ?? 0).Max(), // date deleted column
				5 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateModified?.Length ?? 0).Max(), // date modified column
				6 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateCreated?.Length ?? 0).Max(), // date created column
				7 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemType?.Length ?? 0).Max(), // item type column
				8 => FileList.Items.Cast<ListedItem>().Select(x => x.FileSize?.Length ?? 0).Max(), // item size column
				_ => 20 // cloud status column
			};

			// if called programmatically, the column could be hidden
			// in this case, resizing doesn't need to be done at all
			if (maxItemLength == 0)
				return;

			var columnSizeToFit = new[] { 9 }.Contains(columnToResize) ? maxItemLength : MeasureTextColumnEstimate(columnToResize, 5, maxItemLength);
			if (columnSizeToFit > 0)
			{
				var column = columnToResize switch
				{
					1 => ColumnsViewModel.NameColumn,
					2 => ColumnsViewModel.TagColumn,
					3 => ColumnsViewModel.OriginalPathColumn,
					4 => ColumnsViewModel.DateDeletedColumn,
					5 => ColumnsViewModel.DateModifiedColumn,
					6 => ColumnsViewModel.DateCreatedColumn,
					7 => ColumnsViewModel.ItemTypeColumn,
					8 => ColumnsViewModel.SizeColumn,
					_ => ColumnsViewModel.StatusColumn
				};

				if (columnToResize == 1) // file name column
					columnSizeToFit += 20;

				var minFitLength = Math.Max(columnSizeToFit, column.NormalMinLength);
				var maxFitLength = Math.Min(minFitLength + 36, column.NormalMaxLength); // 36 to account for SortIcon & padding

				column.UserLength = new GridLength(maxFitLength, GridUnitType.Pixel);
			}

			FolderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private double MeasureTextColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			var tbs = DependencyObjectHelpers.FindChildren<TextBlock>(FileList.ItemsPanelRoot).Where(tb =>
			{
				int columnIndexFromName = tb.Name switch
				{
					"ItemName" => 1,
					"ItemTag" => 2,
					"ItemOriginalPath" => 3,
					"ItemDateDeleted" => 4,
					"ItemDateModified" => 5,
					"ItemDateCreated" => 6,
					"ItemType" => 7,
					"ItemSize" => 8,
					"ItemStatus" => 9,
					_ => -1,
				};

				if (columnIndexFromName == -1)
					return false;

				return columnIndexFromName == columnIndex;
			});

			// heuristic: usually, text with more letters are wider than shorter text with wider letters
			// with this, we can calculate avg width using longest text(s) to avoid overshooting the width
			var widthPerLetter = tbs.OrderByDescending(x => x.Text.Length).Where(tb => !string.IsNullOrEmpty(tb.Text)).Take(measureItemsCount).Select(tb =>
			{
				var sampleTb = new TextBlock { Text = tb.Text, FontSize = tb.FontSize, FontFamily = tb.FontFamily };
				sampleTb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

				return sampleTb.DesiredSize.Width / Math.Max(1, tb.Text.Length);
			});

			if (!widthPerLetter.Any())
				return 0;

			// take weighted avg between mean and max since width is an estimate
			var weightedAvg = (widthPerLetter.Average() + widthPerLetter.Max()) / 2;
			return weightedAvg * maxItemLength;
		}

		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
		}

		private void SetDetailsColumnsAsDefault_Click(object sender, RoutedEventArgs e)
		{
			FolderSettings.SetDefaultLayoutPreferences(ColumnsViewModel);
		}
	}
}
