using System;
using System.Collections.Generic;
using Files.Filesystem;
using Files.Interacts;
using Files.ViewModels;

namespace Files
{
	public interface IBaseLayout : IDisposable
	{
		bool IsRenamingItem { get; }

		bool IsItemSelected { get; }

		bool IsMiddleClickToScrollEnabled { get; set; }

		public List<ListedItem> SelectedItems { get; }

		public ListedItem SelectedItem { get; }

		ItemManipulationModel ItemManipulationModel { get; }

		IPaneViewModel PaneViewModel { get; }

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }
		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }
		public BaseLayoutCommandsViewModel CommandsViewModel { get; }
	}
}