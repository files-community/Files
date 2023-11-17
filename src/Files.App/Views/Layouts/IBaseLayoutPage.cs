// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Layouts;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents contract that every layout must define. 
	/// </summary>
	public interface IBaseLayoutPage : IDisposable
	{
		bool IsRenamingItem { get; }

		bool IsItemSelected { get; }

		bool IsMiddleClickToScrollEnabled { get; set; }

		/// <summary>
		/// Gets or sets the value that indicates if the preview pane is not updated when the selected item is changed.
		/// </summary>
		bool LockPreviewPaneContent { get; set; }

		List<ListedItem>? SelectedItems { get; }

		ListedItem? SelectedItem { get; }

		ItemManipulationModel ItemManipulationModel { get; }

		InfoPaneViewModel InfoPaneViewModel { get; }

		SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		BaseLayoutViewModel? CommandsViewModel { get; }

		CommandBarFlyout ItemContextMenuFlyout { get; set; }

		CommandBarFlyout BaseContextMenuFlyout { get; set; }

		void ReloadPreviewPane();
	}
}
