// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.UserControls.Selection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the browser page of Grid View
	/// </summary>
	public sealed partial class GridLayoutPage : BaseGroupableLayoutPage
	{
		// Fields

		private uint currentIconSize;

		// Properties

		protected override uint IconSize => currentIconSize;
		protected override ListViewBase ListViewBase => FileList;
		protected override SemanticZoom RootZoom => RootGridZoom;

		/// <summary>
		/// The minimum item width for items. Used in the StretchedGridViewItems behavior.
		/// </summary>
		public int GridViewItemMinWidth =>
			FolderSettings.LayoutMode == FolderLayoutModes.TilesView
				? Constants.Browser.GridViewBrowser.TilesView
				: FolderSettings.GridViewSize;

		public bool IsPointerOver
		{
			get => (bool)GetValue(IsPointerOverProperty);
			set => SetValue(IsPointerOverProperty, value);
		}

		public static readonly DependencyProperty IsPointerOverProperty =
			DependencyProperty.Register(
				nameof(IsPointerOver),
				typeof(bool),
				typeof(GridLayoutPage),
				new PropertyMetadata(false));

		// Constructor

		public GridLayoutPage() : base()
		{
			InitializeComponent();
			DataContext = this;

			var selectionRectangle = RectangleSelection.Create(ListViewBase, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
		}

		// Methods

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
		}

		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Any())
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				(FileList.ContainerFromItem(SelectedItems.Last()) as GridViewItem)?.Focus(FocusState.Keyboard);
			}
		}

		protected override void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e)
		{
			if ((NextRenameIndex != 0 && TryStartRenameNextItem(e)) || (!FileList?.Items.Contains(e) ?? true))
				return;

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

			currentIconSize = FolderSettings.GetIconSize();
			FolderSettings.GroupOptionPreferenceUpdated -= ZoomIn;
			FolderSettings.GroupOptionPreferenceUpdated += ZoomIn;
			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;

			// Set ItemTemplate
			SetItemTemplate();
			FileList.ItemsSource ??= ParentShellPageInstance.FilesystemViewModel.FilesAndFolders;

			var parameters = (NavigationArguments)eventArgs.Parameter;
			if (parameters.IsLayoutSwitch)
				ReloadItemIconsAsync();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
		}

		private async void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			if (FolderSettings.LayoutMode == FolderLayoutModes.GridView || FolderSettings.LayoutMode == FolderLayoutModes.TilesView)
			{
				// Set ItemTemplate
				SetItemTemplate();

				var requestedIconSize = FolderSettings.GetIconSize();
				if (requestedIconSize != currentIconSize)
				{
					currentIconSize = requestedIconSize;
					await ReloadItemIconsAsync();
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

		private void SetItemMinWidth()
		{
			NotifyPropertyChanged(nameof(GridViewItemMinWidth));
		}

		protected override void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			base.FileList_SelectionChanged(sender, e);

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
			RenamingItem = SelectedItem;
			if (RenamingItem is null)
				return;

			int extensionLength = RenamingItem.FileExtension?.Length ?? 0;

			GridViewItem gridViewItem = FileList.ContainerFromItem(RenamingItem) as GridViewItem;
			if (gridViewItem is null)
				return;

			TextBox textBox = null;

			// Handle layout differences between tiles browser and photo album
			if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				Popup popup = gridViewItem.FindDescendant("EditPopup") as Popup;
				TextBlock textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
				textBox = popup.Child as TextBox;
				textBox.Text = textBlock.Text;
				textBlock.Opacity = 0;
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

				if (textBox.FindParent<Grid>() is null)
				{
					textBlock.Visibility = Visibility.Visible;
					textBox.Visibility = Visibility.Collapsed;
					return;
				}
			}

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
			if (!IsRenamingItem)
				return;

			ValidateItemNameInputTextAsync(textBox, args, (showError) =>
			{
				FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
				FileNameTeachingTip.IsOpen = showError;
			});
		}

		protected override void EndRename(TextBox textBox)
		{
			GridViewItem? gridViewItem = FileList.ContainerFromItem(RenamingItem) as GridViewItem;

			if (textBox is null || gridViewItem is null)
			{
				// NOTE: Navigating away, do nothing
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				Popup? popup = gridViewItem.FindDescendant("EditPopup") as Popup;
				TextBlock? textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
				popup!.IsOpen = false;
				textBlock!.Opacity = (textBlock.DataContext as ListedItem)!.Opacity;
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.TilesView)
			{
				TextBlock? textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;
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
			gridViewItem?.Focus(FocusState.Programmatic);
		}

		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ParentShellPageInstance is null || IsRenamingItem)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot) as FrameworkElement;
			var isFooterFocused = focusedElement is HyperlinkButton;

			if (ctrlPressed && e.Key is VirtualKey.A)
			{
				e.Handled = true;

				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				var hotKey = new HotKey(Keys.A, KeyModifiers.Ctrl);

				await commands[hotKey].ExecuteAsync();
			}
			else if (e.Key == VirtualKey.Enter && !isFooterFocused && !e.KeyStatus.IsMenuKeyDown)
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
			else if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
			{
				// If list has only one item, select it on arrow down/up (#5681)
				if (IsItemSelected)
					return;

				FileList.SelectedIndex = 0;
				e.Handled = true;
			}
		}

		protected override bool CanGetItemFromElement(object element)
			=> element is GridViewItem;

		private async void FolderSettings_GridViewSizeChangeRequested(object? sender, EventArgs e)
		{
			SetItemMinWidth();

			// Get new icon size
			var requestedIconSize = FolderSettings.GetIconSize();

			// Prevents reloading icons when the icon size hasn't changed
			if (requestedIconSize != currentIconSize)
			{
				// Update icon size before refreshing
				currentIconSize = requestedIconSize;
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
				if (FileList.ContainerFromItem(listedItem) is null)
					return;

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
				await Commands.OpenItem.ExecuteAsync();
			}
			else
			{
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

							await CommitRenameAsync(textBox);
						}
						else
						{
							var textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;

							await CommitRenameAsync(textBox);
						}
					}
				}
			}
		}

		private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item &&
				!UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
				await Commands.OpenItem.ExecuteAsync();
			else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
				await Commands.NavigateUp.ExecuteAsync();

			ResetRenameDoubleClick();
		}

		private void ItemSelected_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox &&
				checkBox.DataContext is ListedItem item &&
				!FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Add(item);
		}

		private void ItemSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox &&
				checkBox.DataContext is ListedItem item &&
				FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Remove(item);
		}

		private new void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			var selectionCheckbox = args.ItemContainer.FindDescendant("SelectionCheckbox")!;

			selectionCheckbox.PointerEntered -= SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited -= SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled -= SelectionCheckbox_PointerCanceled;

			base.FileList_ContainerContentChanging(sender, args);
			SetCheckboxSelectionState(args.Item, args.ItemContainer as GridViewItem);

			selectionCheckbox.PointerEntered += SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited += SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled += SelectionCheckbox_PointerCanceled;
		}

		private void SetCheckboxSelectionState(object item, GridViewItem? lviContainer = null)
		{
			var container = lviContainer ?? FileList.ContainerFromItem(item) as GridViewItem;
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

		private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is ListedItem item)
				// Reassign values to update date display
				ToolTipService.SetToolTip(element, item.ItemTooltipText);
		}

		private void SelectionCheckbox_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, true);
		}

		private void SelectionCheckbox_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, false);
		}

		private void SelectionCheckbox_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, false);
		}

		private void UpdateCheckboxVisibility(object sender, bool isPointerOver)
		{
			if (sender is GridViewItem control && control.FindDescendant<UserControl>() is UserControl userControl)
			{
				// Handle visual states
				// Show checkboxes when items are selected (as long as the setting is enabled)
				// Show checkboxes when hovering over the checkbox area (regardless of the setting to hide them)
				if (UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems && control.IsSelected
					|| isPointerOver)
					VisualStateManager.GoToState(userControl, "ShowCheckbox", true);
				else
					VisualStateManager.GoToState(userControl, "HideCheckbox", true);
			}
		}

	}
}
