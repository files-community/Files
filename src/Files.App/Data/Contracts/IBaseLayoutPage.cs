// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Layouts;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Contracts
{
	public interface IBaseLayoutPage : IDisposable
	{
		bool IsRenamingItem { get; }

		bool IsItemSelected { get; }

		bool IsMiddleClickToScrollEnabled { get; set; }

		/// <summary>
		/// If true, the preview pane is not updated when the selected item is changed.
		/// </summary>
		bool LockPreviewPaneContent { get; set; }

		List<ListedItem>? SelectedItems { get; }

		ListedItem? SelectedItem { get; }

		ShellManipulationViewModel ItemManipulationModel { get; }

		InfoPaneViewModel InfoPaneViewModel { get; }

		SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		BaseLayoutViewModel? CommandsViewModel { get; }

		CommandBarFlyout ItemContextMenuFlyout { get; set; }

		CommandBarFlyout BaseContextMenuFlyout { get; set; }
	}
}
