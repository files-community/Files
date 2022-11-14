using Files.App.Filesystem;
using Files.App.Interacts;
using Files.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

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

		IPaneViewModel PaneViewModel { get; }

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }
		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }
		public BaseLayoutCommandsViewModel? CommandsViewModel { get; }
		public CommandBarFlyout ItemContextMenuFlyout { get; set; }
	}
}
