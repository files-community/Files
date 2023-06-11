// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.LayoutModes
{
	public interface IBaseLayout : IDisposable
	{
		bool IsRenamingItem { get; }

		bool IsItemSelected { get; }

		bool IsMiddleClickToScrollEnabled { get; set; }

		public List<StandardItemViewModel>? SelectedItems { get; }

		public StandardItemViewModel? SelectedItem { get; }

		ItemManipulationModel ItemManipulationModel { get; }

		PreviewPaneViewModel PreviewPaneViewModel { get; }

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		public BaseLayoutCommandsViewModel? CommandsViewModel { get; }

		public CommandBarFlyout ItemContextMenuFlyout { get; set; }

		public CommandBarFlyout BaseContextMenuFlyout { get; set; }
	}
}
