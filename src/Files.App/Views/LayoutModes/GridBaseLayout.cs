// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App
{
	public abstract class GridBaseLayout : StandardViewBase
	{
		protected uint currentIconSize;
		protected override uint IconSize => currentIconSize;

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			ListViewBase.ScrollIntoView(e);
		}

		protected override void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (ListViewBase?.Items.Contains(e) ?? false)
				ListViewBase.SelectedItems.Remove(e);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
		}

		protected virtual void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e) { }

		protected virtual void FolderSettings_GridViewSizeChangeRequested(object? sender, EventArgs e)
		{
			var requestedIconSize = FolderSettings.GetIconSize(); // Get new icon size

			// Prevents reloading icons when the icon size hasn't changed
			if (requestedIconSize != currentIconSize)
			{
				currentIconSize = requestedIconSize; // Update icon size before refreshing
				ReloadItemIcons();
			}
		}

		protected virtual async Task ReloadItemIcons()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
			{
				listedItem.ItemPropertiesInitialized = false;
				if (ListViewBase.ContainerFromItem(listedItem) is null)
					return;

				await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, currentIconSize);
			}
		}

		protected virtual void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item &&
				!UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
			{
				ParentShellPageInstance?.Up_Click();
			}

			ResetRenameDoubleClick();
		}

		// QMK - To Rename
		protected bool IsCheckboxChecked(object sender, out ListedItem? item)
		{
			item = null;
			if (sender is not CheckBox checkBox)
				return false;

			if (checkBox.DataContext is not ListedItem listedItem)
				return false;

			item = listedItem;

			return !ListViewBase.SelectedItems.Contains(item);
		}

		protected virtual void ItemSelected_Checked(object sender, RoutedEventArgs e)
		{
			if (IsCheckboxChecked(sender, out var item))
				ListViewBase.SelectedItems.Add(item);
		}

		protected virtual void ItemSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			if (IsCheckboxChecked(sender, out var item))
				ListViewBase.SelectedItems.Remove(item);
		}

		protected void FileList_ContainerContentChanging<T>(ListViewBase sender, ContainerContentChangingEventArgs args, FrameworkElement itemContainer) where T : SelectorItem
		{
			itemContainer.PointerEntered -= Container_PointerEntered;
			itemContainer.PointerExited -= Container_PointerExited;
			itemContainer.PointerCanceled -= Container_PointerCanceled;

			base.FileList_ContainerContentChanging(sender, args);
			SetCheckboxSelectionState(args.Item, args.ItemContainer as T);

			itemContainer.PointerEntered += Container_PointerEntered;
			itemContainer.PointerExited += Container_PointerExited;
			itemContainer.PointerCanceled += Container_PointerCanceled;
		}

		protected virtual void Container_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility(sender, true);
		}

		protected virtual void Container_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility(sender, false);
		}

		protected virtual void Container_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility(sender, false);
		}

		protected abstract void UpdateCheckboxVisibility(object sender, bool? isPointerOver = null);

		protected virtual void SetCheckboxSelectionState<T>(object item, T? lviContainer = null) where T : SelectorItem
		{
			var container = lviContainer ?? ListViewBase.ContainerFromItem(item) as T;
			if (container is not null)
			{
				var checkbox = container.FindDescendant("SelectionCheckbox") as CheckBox;
				if (checkbox is not null)
				{
					// Temporarily disable events to avoid selecting wrong items
					checkbox.Checked -= ItemSelected_Checked;
					checkbox.Unchecked -= ItemSelected_Unchecked;

					checkbox.IsChecked = ListViewBase.SelectedItems.Contains(item);

					checkbox.Checked += ItemSelected_Checked;
					checkbox.Unchecked += ItemSelected_Unchecked;
				}
				UpdateCheckboxVisibility(container, checkbox?.IsPointerOver ?? false);
			}
		}
	}
}