using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using Files.App.EventArguments;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.Interacts;
using Files.App.UserControls.Selection;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using static Files.App.Constants;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.LayoutModes
{
	public sealed partial class ColumnViewBase : BaseLayout
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		protected override uint IconSize => Browser.ColumnViewBrowser.ColumnViewSizeSmall;

		protected override ItemsControl ItemsControl => FileList;

		public ColumnViewBase() : base()
		{
			this.InitializeComponent();
			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
			tapDebounceTimer = DispatcherQueue.CreateTimer();
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

		private void ItemManipulationModel_ScrollIntoViewInvoked(object sender, ListedItem e)
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

		private void ItemManipulationModel_StartRenameItemInvoked(object sender, EventArgs e)
		{
			StartRenameItem();
		}

		private void ItemManipulationModel_FocusSelectedItemsInvoked(object sender, EventArgs e)
		{
			FileList.ScrollIntoView(FileList.Items.Last());
		}

		private void ItemManipulationModel_AddSelectedItemInvoked(object sender, ListedItem e)
		{
			FileList?.SelectedItems.Add(e);
		}

		private void ItemManipulationModel_RemoveSelectedItemInvoked(object sender, ListedItem e)
		{
			FileList?.SelectedItems.Remove(e);
		}

		private void ItemManipulationModel_InvertSelectionInvoked(object sender, EventArgs e)
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

		private void ItemManipulationModel_ClearSelectionInvoked(object sender, EventArgs e)
		{
			FileList.SelectedItems.Clear();
		}

		private void ItemManipulationModel_SelectAllItemsInvoked(object sender, EventArgs e)
		{
			FileList.SelectAll();
		}

		private void ItemManipulationModel_FocusFileListInvoked(object sender, EventArgs e)
		{
			FileList.Focus(FocusState.Programmatic);
		}

		protected override void UnhookEvents()
		{
			if (ItemManipulationModel != null)
			{
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
		}

		public event EventHandler ItemInvoked;

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
			{
				// Focus filelist only if first column
				navArgs.FocusOnNavigation = (navArgs.AssociatedTabInstance as ColumnShellPage)?.ColumnParams?.Column == 0;
			}
			base.OnNavigatedTo(eventArgs);
		}

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
		}

		private void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
		{
			FileList.Focus(FocusState.Programmatic);
		}

		private async void ReloadItemIcons()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) != null)
				{
					await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, 24);
				}
			}
		}

		override public void StartRenameItem()
		{
			RenamingItem = FileList.SelectedItem as ListedItem;

			if (RenamingItem == null)
				return;

			int extensionLength = RenamingItem.FileExtension?.Length ?? 0;
			ListViewItem listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
			TextBox textBox = null;

			if (listViewItem == null)
				return;

			TextBlock textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
			textBox = listViewItem.FindDescendant("ListViewTextBoxItemName") as TextBox;
			textBox.Text = textBlock.Text;
			OldItemName = textBlock.Text;
			textBlock.Visibility = Visibility.Collapsed;
			textBox.Visibility = Visibility.Visible;

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;

			int selectedTextLength = SelectedItem.Name.Length;
			if (!SelectedItem.IsShortcut && UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
			{
				selectedTextLength -= extensionLength;
			}
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
				TextBox textBox = sender as TextBox;
				textBox.LostFocus -= RenameTextBox_LostFocus;
				textBox.Text = OldItemName;
				EndRename(textBox);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Enter)
			{
				TextBox textBox = sender as TextBox;
				textBox.LostFocus -= RenameTextBox_LostFocus;
				CommitRename(textBox);
				e.Handled = true;
			}
		}

		private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// This check allows the user to use the text box context menu without ending the rename
			if (!(FocusManager.GetFocusedElement() is AppBarButton or Popup))
			{
				TextBox textBox = e.OriginalSource as TextBox;
				CommitRename(textBox);
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
			if (textBox == null || textBox.Parent == null)
			{
				// Navigating away, do nothing
			}
			else
			{
				// Re-focus selected list item
				ListViewItem listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
				listViewItem?.Focus(FocusState.Programmatic);

				TextBlock textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
				textBox.Visibility = Visibility.Collapsed;
				textBlock.Visibility = Visibility.Visible;
			}

			textBox.LostFocus -= RenameTextBox_LostFocus;
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

		#region IDisposable

		public override void Dispose()
		{
			base.Dispose();
			UnhookEvents();
			CommandsViewModel?.Dispose();
		}

		#endregion IDisposable

		private async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItems = FileList.SelectedItems.Cast<ListedItem>().Where(x => x != null).ToList();

			if (SelectedItems.Count == 1 && App.AppModel.IsQuickLookAvailable)
				await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance, true);
		}

		private void FileList_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (IsRenamingItem)
				return;

			HandleRightClick(sender, e);
		}

		private void HandleRightClick(object sender, RightTappedRoutedEventArgs e)
		{
			var objectPressed = ((FrameworkElement)e.OriginalSource).DataContext as ListedItem;

			if (objectPressed != null)
				return;

			// Check if RightTapped row is currently selected
			if (IsItemSelected)
			{
				if (SelectedItems.Contains(objectPressed))
					return;
			}

			// The following code is only reachable when a user RightTapped an unselected row
			ItemManipulationModel.SetSelectedItem(objectPressed);
		}

		private DispatcherQueueTimer tapDebounceTimer;

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
					{
						ItemInvoked?.Invoke(new ColumnParam { NavPathParam = (SelectedItem is ShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
					}
					tapDebounceTimer.Stop();
				}, TimeSpan.FromMilliseconds(200));
			}
		}

		private async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

			if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
			{
				if (!IsRenamingItem)
				{
					if (IsItemSelected && SelectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
					{
						ItemInvoked?.Invoke(new ColumnParam { NavPathParam = (SelectedItem is ShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
					}
					else
					{
						NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
					}
					e.Handled = true;
				}
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				if (!IsRenamingItem && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
				{
					e.Handled = true;
					await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance);
				}
			}
			else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
			{
				// Unfocus the GridView so keyboard shortcut can be handled
				NavToolbar?.Focus(FocusState.Pointer);
			}
			else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
			{
				// Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
				NavToolbar?.Focus(FocusState.Pointer);
			}
			else if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
			{
				// If list has only one item, select it on arrow down/up (#5681)
				if (!IsItemSelected)
				{
					FileList.SelectedIndex = 0;
					e.Handled = true;
				}
			}
			else if (e.Key == VirtualKey.Left) // Left arrow: select parent folder (previous column)
			{
				if (!IsRenamingItem && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
				{
					if ((ParentShellPageInstance as ColumnShellPage).ColumnParams.Column > 0)
					{
						FocusManager.TryMoveFocus(FocusNavigationDirection.Previous);
					}
					e.Handled = true;
				}
			}
			else if (e.Key == VirtualKey.Right) // Right arrow: switch focus to next column
			{
				if (!IsRenamingItem && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
				{
					FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
					e.Handled = true;
				}
			}
		}

		protected override void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance == null)
				return;

			if (ParentShellPageInstance.CurrentPageType != typeof(ColumnViewBase) || IsRenamingItem)
				return;

			// Don't block the various uses of enter key (key 13)
			var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;

			if
			(
				Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Enter) == CoreVirtualKeyStates.Down ||
				focusedElement is Button ||
				focusedElement is TextBox ||
				focusedElement is PasswordBox ||
				DependencyObjectHelpers.FindParent<ContentDialog>(focusedElement) != null
			)
				return;

			base.Page_CharacterReceived(sender, args);
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
						{
							NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
						}
						break;
					case StorageItemTypes.Folder:
						if (!UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
						{
							ItemInvoked?.Invoke(new ColumnParam { NavPathParam = (item is ShortcutItem sht ? sht.TargetPath : item.ItemPath), ListView = FileList }, EventArgs.Empty);
						}
						break;
					default:
						ParentShellPageInstance.Up_Click();
						break;
				}
			}
			else
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
			var objectPressed = ((FrameworkElement)e.OriginalSource).DataContext as ListedItem;

			if (objectPressed != null)
				return;

			// Check if RightTapped row is currently selected
			if (IsItemSelected)
			{
				if (SelectedItems.Contains(objectPressed))
					return;
			}

			// The following code is only reachable when a user RightTapped an unselected row
			ItemManipulationModel.SetSelectedItem(objectPressed);
		}

		private void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;

			// Allow for Ctrl+Shift selection
			if (ctrlPressed || shiftPressed)
				return;

			// Check if the setting to open items with a single click is turned on
			if (item != null
				&& (UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick && item.PrimaryItemAttribute == StorageItemTypes.File))
			{
				ResetRenameDoubleClick();
				NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else
			{
				var clickedItem = e.OriginalSource as FrameworkElement;
				if (clickedItem is TextBlock textBlock && textBlock.Name == "Name")
				{
					CheckRenameDoubleClick(clickedItem.DataContext);
				}
				else if (IsRenamingItem)
				{
					if (FileList.ContainerFromItem(RenamingItem) is ListViewItem listViewItem
						&& listViewItem.FindDescendant("ListViewTextBoxItemName") is TextBox textBox)
					{
						CommitRename(textBox);
					}
				}
				if (item != null && item.PrimaryItemAttribute == StorageItemTypes.Folder &&
					UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
				{
					ItemInvoked?.Invoke(new ColumnParam { NavPathParam = (item is ShortcutItem sht ? sht.TargetPath : item.ItemPath), ListView = FileList }, EventArgs.Empty);
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

		protected override void BaseFolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
		{
			var parent = this.FindAscendant<ModernShellPage>();

			if (parent == null)
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
	}
}