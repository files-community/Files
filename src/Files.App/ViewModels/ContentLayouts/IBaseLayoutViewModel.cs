// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.ContentLayouts
{
	/// <summary>
	/// Represents an interface for <see cref="BaseLayoutViewModel"/>.
	/// </summary>
	public interface IBaseLayoutViewModel
	{
		ItemManipulationModel ItemManipulationModel { get; }

		PreviewPaneViewModel PreviewPaneViewModel { get; }

		SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		bool IsRenamingItem { get; }

		bool IsItemSelected { get; }

		bool IsMiddleClickToScrollEnabled { get; set; }

		/// <summary>
		/// If true, the preview pane is not updated when the selected item is changed.
		/// </summary>
		bool LockPreviewPaneContent { get; set; }

		List<ListedItem>? SelectedItems { get; }

		ListedItem? SelectedItem { get; }

		CommandBarFlyout ItemContextMenuFlyout { get; set; }

		CommandBarFlyout BaseContextMenuFlyout { get; set; }
	}
}
