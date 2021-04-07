using Files.Filesystem;
using System;
using System.Collections.Generic;

namespace Files.Interacts
{
    public class ItemManipulationModel
    {
        public event EventHandler<ListedItem> AddSelectedItemInvoked;

        public event EventHandler ClearSelectionInvoked;

        public event EventHandler FocusFileListInvoked;

        public event EventHandler FocusSelectedItemsInvoked;

        public event EventHandler InvertSelectionInvoked;

        public event EventHandler RefreshItemsOpacityInvoked;

        public event EventHandler<ListedItem> ScrollIntoViewInvoked;

        public event EventHandler SelectAllItemsInvoked;

        public event EventHandler SetDragModeForItemsInvoked;

        public event EventHandler StartRenameItemInvoked;

        public void AddSelectedItem(ListedItem item)
        {
            AddSelectedItemInvoked?.Invoke(this, item);
        }

        public void AddSelectedItems(List<ListedItem> items)
        {
            foreach (ListedItem item in items)
            {
                AddSelectedItem(item);
            }
        }

        public void ClearSelection()
        {
            ClearSelectionInvoked?.Invoke(this, EventArgs.Empty);
        }

        public void FocusFileList()
        {
            FocusFileListInvoked?.Invoke(this, EventArgs.Empty);
        }

        public void FocusSelectedItems()
        {
            FocusSelectedItemsInvoked?.Invoke(this, EventArgs.Empty);
        }

        public void InvertSelection()
        {
            InvertSelectionInvoked?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshItemsOpacity()
        {
            RefreshItemsOpacityInvoked?.Invoke(this, EventArgs.Empty);
        }

        public void ScrollIntoView(ListedItem item)
        {
            ScrollIntoViewInvoked?.Invoke(this, item);
        }

        public void SelectAllItems()
        {
            SelectAllItemsInvoked?.Invoke(this, EventArgs.Empty);
        }

        public void SetDragModeForItems()
        {
            SetDragModeForItemsInvoked?.Invoke(this, EventArgs.Empty);
        }

        public void SetSelectedItem(ListedItem item)
        {
            ClearSelection();
            AddSelectedItem(item);
        }

        public void SetSelectedItems(List<ListedItem> items)
        {
            ClearSelection();
            AddSelectedItems(items);
        }

        public void StartRenameItem()
        {
            StartRenameItemInvoked?.Invoke(this, EventArgs.Empty);
        }
    }
}