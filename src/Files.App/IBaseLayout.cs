// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App
{
	public interface IBaseLayout : IDisposable
	{
		bool IsRenamingItem { get; }

		bool IsItemSelected { get; }

		bool IsMiddleClickToScrollEnabled { get; set; }

		public List<ListedItem>? SelectedItems { get; }

		public ListedItem? SelectedItem { get; }

		ItemManipulationModel ItemManipulationModel { get; }

		PreviewPaneViewModel PreviewPaneViewModel { get; }

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		public BaseLayoutCommandsViewModel? CommandsViewModel { get; }

		public CommandBarFlyout ItemContextMenuFlyout { get; set; }

		public CommandBarFlyout BaseContextMenuFlyout { get; set; }
	}
}
