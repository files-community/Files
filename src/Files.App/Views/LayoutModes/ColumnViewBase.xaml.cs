using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using Files.App.Commands;
using Files.App.EventArguments;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Interacts;
using Files.App.UserControls.Selection;
using Files.Shared.Enums;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using static Files.App.Constants;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.LayoutModes
{
	public sealed partial class ColumnViewBase : StandardViewBase
	{
		protected override uint IconSize => Browser.ColumnViewBrowser.ColumnViewSizeSmall;

		protected override ListViewBase ListViewBase => FileList;

		protected override SemanticZoom RootZoom => RootGridZoom;

		private ColumnViewBrowser? columnsOwner;
		private ListViewItem? openedFolderPresenter;

		public ColumnViewBase() : base()
		{
			InitializeComponent();
			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
			tapDebounceTimer = DispatcherQueue.CreateTimer();
			ItemInvoked += ColumnViewBase_ItemInvoked;
			GotFocus += ColumnViewBase_GotFocus;
		}

		private void ColumnViewBase_GotFocus(object sender, RoutedEventArgs e)
		{
			if (FileList.SelectedItem == null && openedFolderPresenter != null)
			{
				openedFolderPresenter.Focus(FocusState.Programmatic);
				FileList.SelectedItem = FileList.ItemFromContainer(openedFolderPresenter);
			}
		}

		private void ColumnViewBase_ItemInvoked(object? sender, EventArgs e)
		{
			ClearOpenedFolderSelectionIndicator();
			openedFolderPresenter = FileList.ContainerFromItem(FileList.SelectedItem) as ListViewItem;
		}

		internal void ClearOpenedFolderSelectionIndicator()
		{
			if (openedFolderPresenter is null)
				return;

			openedFolderPresenter.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
			var presenter = openedFolderPresenter.FindDescendant<Grid>()!;
			presenter!.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
			openedFolderPresenter = null;
		}

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			try
			{
				FileList.ScrollIntoView(e, ScrollIntoViewAlignment.Default);
			}
			catch (Exception)
			{
				// Catch error where row index could not be found
			}
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
			if (NextRenameIndex != 0 && TryStartRenameNextItem(e))
				return;

			FileList?.SelectedItems.Add(e);
		}

		protected override void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			FileList?.SelectedItems.Remove(e);
		}

		public event EventHandler? ItemInvoked;

		public event EventHandler? ItemTapped;

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
			{
				columnsOwner = (navArgs.AssociatedTabInstance as FrameworkElement)?.FindAscendant<ColumnViewBrowser>();
				var index = (navArgs.AssociatedTabInstance as ColumnShellPage)?.ColumnParams?.Column;
				navArgs.FocusOnNavigation = index == columnsOwner?.FocusIndex;

				if (index < columnsOwner?.FocusIndex)
					FileList.ContainerContentChanging += HighlightPathDirectory;
			}

			base.OnNavigatedTo(eventArgs);

			FolderSettings.GroupOptionPreferenceUpdated -= ZoomIn;
			FolderSettings.GroupOptionPreferenceUpdated += ZoomIn;
		}

		private void HighlightPathDirectory(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.Item is ListedItem item && columnsOwner?.OwnerPath is string ownerPath
				&& (ownerPath == item.ItemPath || ownerPath.StartsWith(item.ItemPath) && ownerPath[item.ItemPath.Length] is '/' or '\\'))
			{
				var presenter = args.ItemContainer.FindDescendant<Grid>()!;
				presenter!.Background = this.Resources["ListViewItemBackgroundSelected"] as SolidColorBrush;
				FileList.ContainerContentChanging -= HighlightPathDirectory;
			}
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
		}

		private async void ReloadItemIcons()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, 24);
			}
		}

		override public void StartRenameItem()
		{
			StartRenameItem("ListViewTextBoxItemName");
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

		protected override void EndRename(TextBox textBox)
		{
			if (textBox is not null && textBox.Parent is not null)
			{
				// Re-focus selected list item
				var listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
				listViewItem?.Focus(FocusState.Programmatic);

				var textBlock = listViewItem?.FindDescendant("ItemName") as TextBlock;
				textBox!.Visibility = Visibility.Collapsed;
				textBlock!.Visibility = Visibility.Visible;
			}

			textBox!.LostFocus -= RenameTextBox_LostFocus;
			textBox.KeyDown -= RenameTextBox_KeyDown;
			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;
		}

		public override void ResetItemOpacity()
		{
			// throw new NotImplementedException();
		}

		protected override bool CanGetItemFromElement(object element)
			=> element is ListViewItem;

		public override void Dispose()
		{
			base.Dispose();
			columnsOwner = null;
		}

		protected override async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			base.FileList_SelectionChanged(sender, e);
			if (e is null)
				return;

			if (e.AddedItems.Count > 0)
				columnsOwner?.HandleSelectionChange(this);

			if (e.RemovedItems.Count > 0 && openedFolderPresenter != null)
			{
				var presenter = openedFolderPresenter.FindDescendant<Grid>()!;
				presenter!.Background = this.Resources["ListViewItemBackgroundSelected"] as SolidColorBrush;
			}
		}

		private void FileList_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (!IsRenamingItem)
				HandleRightClick(sender, e);
		}

		private void HandleRightClick(object sender, RightTappedRoutedEventArgs e)
		{
			HandleRightClick(e.OriginalSource);
		}

		private readonly DispatcherQueueTimer tapDebounceTimer;

		private void FileList_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
		{
			// Open selected directory
			tapDebounceTimer.Stop();
			if (IsItemSelected && SelectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
			{
				var currItem = SelectedItem;
				tapDebounceTimer.Debounce(() =>
				{
					if (currItem == SelectedItem)
						ItemInvoked?.Invoke(new ColumnParam { NavPathParam = (SelectedItem is ShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
					tapDebounceTimer.Stop();
				}, TimeSpan.FromMilliseconds(200));
			}
		}

		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ParentShellPageInstance is null)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

			if (ctrlPressed && e.Key is VirtualKey.A)
			{
				e.Handled = true;

				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				var hotKey = new HotKey(VirtualKey.A, VirtualKeyModifiers.Control);

				await commands[hotKey].ExecuteAsync();

				return;
			}

			if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
			{
				if (IsRenamingItem)
					return;

				e.Handled = true;

				if (IsItemSelected && SelectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
					ItemInvoked?.Invoke(new ColumnParam { NavPathParam = (SelectedItem is ShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				if (!IsRenamingItem && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
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
			else if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
			{
				ClearOpenedFolderSelectionIndicator();

				// If list has only one item, select it on arrow down/up (#5681)
				if (!IsItemSelected)
				{
					FileList.SelectedIndex = 0;
					e.Handled = true;
				}
			}
			else if (e.Key == VirtualKey.Left) // Left arrow: select parent folder (previous column)
			{
				if (IsRenamingItem || (ParentShellPageInstance is not null && ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled))
					return;

				var currentBladeIndex = (ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;
				this.FindAscendant<ColumnViewBrowser>()?.MoveFocusToPreviousBlade(currentBladeIndex);
				FileList.SelectedItem = null;
				ClearOpenedFolderSelectionIndicator();
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Right) // Right arrow: switch focus to next column
			{
				if (IsRenamingItem || (ParentShellPageInstance is not null && ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled))
					return;

				var currentBladeIndex = (ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;
				this.FindAscendant<ColumnViewBrowser>()?.MoveFocusToNextBlade(currentBladeIndex + 1);
				e.Handled = true;
			}
		}

		private void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var clickedItem = e.OriginalSource as FrameworkElement;

			if (clickedItem?.DataContext is ListedItem item)
			{
				switch (item.PrimaryItemAttribute)
				{
					case StorageItemTypes.File:
						if (!UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
							_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
						break;
					case StorageItemTypes.Folder:
						if (!UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
							ItemInvoked?.Invoke(new ColumnParam { NavPathParam = (item is ShortcutItem sht ? sht.TargetPath : item.ItemPath), ListView = FileList }, EventArgs.Empty);
						break;
					default:
						if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
							ParentShellPageInstance.Up_Click();
						break;
				}
			}
			else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
			{
				ParentShellPageInstance.Up_Click();
			}

			ResetRenameDoubleClick();
		}

		private void FileList_Holding(object sender, HoldingRoutedEventArgs e)
		{
			HandleRightClick(sender, e);
		}

		private void HandleRightClick(object sender, HoldingRoutedEventArgs e)
		{
			HandleRightClick(e.OriginalSource);
		}

		private void HandleRightClick(object pressed)
		{
			var objectPressed = ((FrameworkElement)pressed).DataContext as ListedItem;

			// Check if RightTapped row is currently selected
			if (objectPressed is not null || (IsItemSelected && SelectedItems.Contains(objectPressed)))
				return;

			// The following code is only reachable when a user RightTapped an unselected row
			ItemManipulationModel.SetSelectedItem(objectPressed);
		}

		private async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;


			// Allow for Ctrl+Shift selection
			if (ctrlPressed || shiftPressed)
				return;

			var isItemFile = item?.PrimaryItemAttribute is StorageItemTypes.File;
			var isItemFolder = item?.PrimaryItemAttribute is StorageItemTypes.Folder;

			// Check if the setting to open items with a single click is turned on
			if (UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick && isItemFile)
			{
				ResetRenameDoubleClick();
				_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else
			{
				var clickedItem = e.OriginalSource as FrameworkElement;
				if (clickedItem is TextBlock textBlock && textBlock.Name == "ItemName")
				{
					CheckRenameDoubleClick(clickedItem.DataContext);
				}
				else if (IsRenamingItem &&
					FileList.ContainerFromItem(RenamingItem) is ListViewItem listViewItem &&
					listViewItem.FindDescendant("ListViewTextBoxItemName") is TextBox textBox)
				{
					await CommitRename(textBox);
				}

				if (isItemFolder && UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
				{
					ItemInvoked?.Invoke(
						new ColumnParam
						{
							NavPathParam = (item is ShortcutItem sht ? sht.TargetPath : item!.ItemPath),
							ListView = FileList
						},
						EventArgs.Empty);
				}
				else if (!IsRenamingItem && (isItemFile || isItemFolder))
				{
					ClearOpenedFolderSelectionIndicator();

					var itemPath = item!.ItemPath.EndsWith('\\')
						? item.ItemPath.Substring(0, item.ItemPath.Length - 1)
						: item.ItemPath;

					ItemTapped?.Invoke(new ColumnParam { NavPathParam = Path.GetDirectoryName(itemPath), ListView = FileList }, EventArgs.Empty);
				}
			}
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			var itemContainer = (sender as Grid)?.FindAscendant<ListViewItem>();

			if (itemContainer is null)
				return;

			itemContainer.ContextFlyout = ItemContextMenuFlyout;
		}

		protected override void BaseFolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			var parent = this.FindAscendant<ModernShellPage>();

			if (parent is null)
				return;

			switch (e.LayoutMode)
			{
				case FolderLayoutModes.ColumnView:
					break;
				case FolderLayoutModes.DetailsView:
					parent.FolderSettings.ToggleLayoutModeDetailsView(true);
					break;
				case FolderLayoutModes.TilesView:
					parent.FolderSettings.ToggleLayoutModeTiles(true);
					break;
				case FolderLayoutModes.GridView:
					parent.FolderSettings.ToggleLayoutModeGridView(e.GridViewSize);
					break;
				case FolderLayoutModes.Adaptive:
					parent.FolderSettings.ToggleLayoutModeAdaptive();
					break;
			}
		}

		internal void ClearSelectionIndicator()
		{
			FileList.SelectedItem = null;
		}
	}
}
