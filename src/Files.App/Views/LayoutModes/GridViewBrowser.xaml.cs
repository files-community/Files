using CommunityToolkit.WinUI.UI;
using Files.App.EventArguments;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.Interacts;
using Files.App.UserControls.Selection;
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
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Views.LayoutModes
{
	public sealed partial class GridViewBrowser : BaseLayout
	{
		private uint currentIconSize;

		protected override uint IconSize => currentIconSize;

		protected override ItemsControl ItemsControl => FileList;

		/// <summary>
		/// The minimum item width for items. Used in the StretchedGridViewItems behavior.
		/// </summary>
		public int GridViewItemMinWidth => FolderSettings.LayoutMode == FolderLayoutModes.TilesView ? Constants.Browser.GridViewBrowser.TilesView : FolderSettings.GridViewSize;

		public GridViewBrowser()
			: base()
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
			ItemManipulationModel.RefreshItemThumbnailInvoked += ItemManipulationModel_RefreshItemThumbnail;
			ItemManipulationModel.RefreshItemsThumbnailInvoked += ItemManipulationModel_RefreshItemsThumbnail;

		}

		private void ItemManipulationModel_RefreshItemsThumbnail(object? sender, EventArgs e)
		{
			ReloadSelectedItemsIcon();
		}

		private void ItemManipulationModel_RefreshItemThumbnail(object? sender, EventArgs args)
		{
			ReloadSelectedItemIcon();
		}

		private void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
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
				(FileList.ContainerFromItem(SelectedItems.Last()) as GridViewItem)?.Focus(FocusState.Keyboard);
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
			ItemManipulationModel.RefreshItemThumbnailInvoked -= ItemManipulationModel_RefreshItemThumbnail;
			ItemManipulationModel.RefreshItemsThumbnailInvoked -= ItemManipulationModel_RefreshItemsThumbnail;
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

			currentIconSize = FolderSettings.GetIconSize();
			FolderSettings.GroupOptionPreferenceUpdated -= ZoomIn;
			FolderSettings.GroupOptionPreferenceUpdated += ZoomIn;
			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			SetItemTemplate(); // Set ItemTemplate
			FileList.ItemsSource ??= ParentShellPageInstance.FilesystemViewModel.FilesAndFolders;
			var parameters = (NavigationArguments)eventArgs.Parameter;
			if (parameters.IsLayoutSwitch)
				ReloadItemIcons();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
		}

		private void SelectionRectangle_SelectionEnded(object? sender, EventArgs e)
		{
			FileList.Focus(FocusState.Programmatic);
		}

		private void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			if (FolderSettings.LayoutMode == FolderLayoutModes.GridView || FolderSettings.LayoutMode == FolderLayoutModes.TilesView)
			{
				SetItemTemplate(); // Set ItemTemplate
				var requestedIconSize = FolderSettings.GetIconSize();
				if (requestedIconSize != currentIconSize)
				{
					currentIconSize = requestedIconSize;
					ReloadItemIcons();
				}
			}
		}

		private void SetItemTemplate()
		{
			FileList.ItemTemplate = (FolderSettings.LayoutMode == FolderLayoutModes.TilesView) ? TilesBrowserTemplate : GridViewBrowserTemplate; // Choose Template
			SetItemMinWidth();

			// Set GridViewSize event handlers
			if (FolderSettings.LayoutMode == FolderLayoutModes.TilesView)
			{
				FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
				FolderSettings.GridViewSizeChangeRequested += FolderSettings_GridViewSizeChangeRequested;
			}
		}

		/// <summary>
		/// Updates the min size for the item containers
		/// </summary>
		private void SetItemMinWidth()
		{
			NotifyPropertyChanged(nameof(GridViewItemMinWidth));
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

			GridViewItem gridViewItem = FileList.ContainerFromItem(RenamingItem) as GridViewItem;
			if (gridViewItem is null)
			{
				return;
			}

			TextBox textBox = null;

			// Handle layout differences between tiles browser and photo album
			if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				Popup popup = gridViewItem.FindDescendant("EditPopup") as Popup;
				TextBlock textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
				textBox = popup.Child as TextBox;
				textBox.Text = textBlock.Text;
				popup.IsOpen = true;
				OldItemName = textBlock.Text;
			}
			else
			{
				TextBlock textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
				textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;
				textBox.Text = textBlock.Text;
				OldItemName = textBlock.Text;
				textBlock.Visibility = Visibility.Collapsed;
				textBox.Visibility = Visibility.Visible;
			}

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;

			int selectedTextLength = SelectedItem.Name.Length;
			if (!SelectedItem.IsShortcut && UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
				selectedTextLength -= extensionLength;
			textBox.Select(0, selectedTextLength);
			IsRenamingItem = true;
		}

		private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (!IsRenamingItem)
				return;

			ValidateItemNameInputText(textBox, args, (showError) =>
			{
				FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
				FileNameTeachingTip.IsOpen = showError;
			});
		}

		private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Escape)
			{
				TextBox textBox = (TextBox)sender;
				textBox.LostFocus -= RenameTextBox_LostFocus;
				textBox.Text = OldItemName;
				EndRename(textBox);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Enter)
			{
				TextBox textBox = (TextBox)sender;
				textBox.LostFocus -= RenameTextBox_LostFocus;
				CommitRename(textBox);
				e.Handled = true;
			}
		}

		private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// This check allows the user to use the text box context menu without ending the rename
			if ((FocusManager.GetFocusedElement() is AppBarButton or Popup))
				return;

			TextBox textBox = (TextBox)e.OriginalSource;
			CommitRename(textBox);
		}

		private async void CommitRename(TextBox textBox)
		{
			EndRename(textBox);
			string newItemName = textBox.Text.Trim().TrimEnd('.');
			await UIFilesystemHelpers.RenameFileItemAsync(RenamingItem, newItemName, ParentShellPageInstance);
		}

		private void EndRename(TextBox textBox)
		{
			GridViewItem? gridViewItem = FileList.ContainerFromItem(RenamingItem) as GridViewItem;

			if (textBox is null || gridViewItem is null)
			{
				// Navigating away, do nothing
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				Popup? popup = gridViewItem.FindDescendant("EditPopup") as Popup;
				popup!.IsOpen = false;
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.TilesView)
			{
				TextBlock? textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
				textBox.Visibility = Visibility.Collapsed;
				textBlock!.Visibility = Visibility.Visible;
			}

			textBox!.LostFocus -= RenameTextBox_LostFocus;
			textBox.KeyDown -= RenameTextBox_KeyDown;
			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;

			// Re-focus selected list item
			gridViewItem?.Focus(FocusState.Programmatic);
		}

		private async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
			var isFooterFocused = focusedElement is HyperlinkButton;

			if (e.Key == VirtualKey.Enter && !isFooterFocused && !e.KeyStatus.IsMenuKeyDown)
			{
				if (IsRenamingItem)
					return;

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
					NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
				}
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				if (!IsRenamingItem && !isFooterFocused && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
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
				if (IsItemSelected)
					return;

				FileList.SelectedIndex = 0;
				e.Handled = true;
			}
		}

		protected override void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance is null)
				return;

			if (ParentShellPageInstance.CurrentPageType == typeof(GridViewBrowser) && !IsRenamingItem)
			{
				// Don't block the various uses of enter key (key 13)
				var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement();
				if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Enter) == CoreVirtualKeyStates.Down
					|| focusedElement is Button
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
			=> element is GridViewItem;

		private void FolderSettings_GridViewSizeChangeRequested(object? sender, EventArgs e)
		{
			SetItemMinWidth();
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
				if (FileList.ContainerFromItem(listedItem) is null)
					return;

				await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, currentIconSize);
			}
		}

		private async void ReloadSelectedItemIcon()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			ParentShellPageInstance.SlimContentPage.SelectedItem.ItemPropertiesInitialized = false;
			await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(ParentShellPageInstance.SlimContentPage.SelectedItem, currentIconSize);
		}

		private async void ReloadSelectedItemsIcon()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();

			foreach (var selectedItem in ParentShellPageInstance.SlimContentPage.SelectedItems)
			{
				selectedItem.ItemPropertiesInitialized = false;
				await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(selectedItem, currentIconSize);
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
				NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else
			{
				var clickedItem = e.OriginalSource as FrameworkElement;
				if (clickedItem is TextBlock textBlock && textBlock.Name == "ItemName")
				{
					CheckRenameDoubleClick(clickedItem?.DataContext);
				}
				else if (IsRenamingItem)
				{
					if (FileList.ContainerFromItem(RenamingItem) is GridViewItem gridViewItem)
					{
						if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
						{
							Popup popup = gridViewItem.FindDescendant("EditPopup") as Popup;
							var textBox = popup.Child as TextBox;
							CommitRename(textBox);
						}
						else
						{
							var textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;
							CommitRename(textBox);
						}
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
				NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
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

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			// This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
			// because you can't use bindings in the setters
			DependencyObject item = VisualTreeHelper.GetParent(sender as Grid);
			while (item is not GridViewItem)
				item = VisualTreeHelper.GetParent(item);
			if (item is GridViewItem itemContainer)
				itemContainer.ContextFlyout = ItemContextMenuFlyout;
		}
	}
}