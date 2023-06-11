// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem;
using System;
using System.Collections.Generic;

namespace Files.App.Interacts
{
	public class ItemManipulationModel
	{
		public event EventHandler FocusFileListInvoked;

		public event EventHandler SelectAllItemsInvoked;

		public event EventHandler ClearSelectionInvoked;

		public event EventHandler InvertSelectionInvoked;

		public event EventHandler<StandardItemViewModel> AddSelectedItemInvoked;

		public event EventHandler<StandardItemViewModel> RemoveSelectedItemInvoked;

		public event EventHandler FocusSelectedItemsInvoked;

		public event EventHandler StartRenameItemInvoked;

		public event EventHandler<StandardItemViewModel> ScrollIntoViewInvoked;

		public event EventHandler SetDragModeForItemsInvoked;

		public event EventHandler RefreshItemsOpacityInvoked;

		public event EventHandler RefreshItemThumbnailInvoked;

		public event EventHandler RefreshItemsThumbnailInvoked;

		public void FocusFileList()
		{
			FocusFileListInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void SelectAllItems()
		{
			SelectAllItemsInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void ClearSelection()
		{
			ClearSelectionInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void InvertSelection()
		{
			InvertSelectionInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void AddSelectedItem(StandardItemViewModel item)
		{
			AddSelectedItemInvoked?.Invoke(this, item);
		}

		public void AddSelectedItems(List<StandardItemViewModel> items)
		{
			foreach (StandardItemViewModel item in items)
			{
				AddSelectedItem(item);
			}
		}

		public void RemoveSelectedItem(StandardItemViewModel item)
		{
			RemoveSelectedItemInvoked?.Invoke(this, item);
		}

		public void RemoveSelectedItems(List<StandardItemViewModel> items)
		{
			foreach (StandardItemViewModel item in items)
			{
				RemoveSelectedItem(item);
			}
		}

		public void SetSelectedItem(StandardItemViewModel item)
		{
			ClearSelection();
			AddSelectedItem(item);
		}

		public void SetSelectedItems(List<StandardItemViewModel> items)
		{
			ClearSelection();
			AddSelectedItems(items);
		}

		public void FocusSelectedItems()
		{
			FocusSelectedItemsInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void StartRenameItem()
		{
			StartRenameItemInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void ScrollIntoView(StandardItemViewModel item)
		{
			ScrollIntoViewInvoked?.Invoke(this, item);
		}

		public void SetDragModeForItems()
		{
			SetDragModeForItemsInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshItemsOpacity()
		{
			RefreshItemsOpacityInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshItemThumbnail()
		{
			RefreshItemThumbnailInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshItemsThumbnail()
		{
			RefreshItemsThumbnailInvoked?.Invoke(this, EventArgs.Empty);
		}
	}
}
