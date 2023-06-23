// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents a base class for file content view layout.
	/// </summary>
	public interface IBaseLayout : IDisposable
	{
		/// <summary>
		/// Gets if the selected item is in renaming.
		/// </summary>
		bool IsRenamingItem { get; }

		/// <summary>
		/// Gets if an item is selected or multiple items are selected.
		/// </summary>
		bool IsItemSelected { get; }

		/// <summary>
		/// Gets or sets if middle click is enabled.
		/// </summary>
		bool IsMiddleClickToScrollEnabled { get; set; }

		/// <summary>
		/// Gets or sets if preview pane content is locked.
		/// </summary>
		/// <remarks>
		/// If it's true, the preview pane won't be updated when the selected item(s) is changed.
		/// </remarks>
		bool LockPreviewPaneContent { get; set; }

		/// <summary>
		/// Gets a collection of multiply selected items.
		/// </summary>
		List<ListedItem>? SelectedItems { get; }

		/// <summary>
		/// Gets an item selected.
		/// </summary>
		ListedItem? SelectedItem { get; }

		/// <summary>
		/// Gets a ParentShellPageInstance instance.
		/// </summary>
		public IShellPage? ParentShellPageInstance { get; }

		/// <summary>
		/// Gets an item in renaming.
		/// </summary>
		public ListedItem? RenamingItem { get; }

		/// <summary>
		/// Gets old name of the renamed item.
		/// </summary>
		public string? OldItemName { get; }

		/// <summary>
		/// Gets an ItemManipulationModel instance.
		/// </summary>
		ItemManipulationModel ItemManipulationModel { get; }

		/// <summary>
		/// Gets a PreviewPaneViewModel instance.
		/// </summary>
		PreviewPaneViewModel PreviewPaneViewModel { get; }

		/// <summary>
		/// Gets a SelectedItemsPropertiesViewModel instance.
		/// </summary>
		SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		/// <summary>
		/// Gets a DirectoryPropertiesViewModel instance.
		/// </summary>
		DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		/// <summary>
		/// Gets a BaseLayoutCommandsViewModel instance.
		/// </summary>
		BaseLayoutCommandsViewModel? CommandsViewModel { get; }

		/// <summary>
		/// Gets or sets a CommandBarFlyout instance for the selected item(s).
		/// </summary>
		CommandBarFlyout ItemContextMenuFlyout { get; set; }

		/// <summary>
		/// Gets or sets a base CommandBarFlyout instance.
		/// </summary>
		CommandBarFlyout BaseContextMenuFlyout { get; set; }
	}
}
