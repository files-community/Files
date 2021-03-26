using Files.Filesystem;
using System;
using System.Collections.Generic;

namespace Files
{
    public interface IBaseLayout : IDisposable
    {
        bool IsRenamingItem { get; }

        bool IsItemSelected { get; }

        public List<ListedItem> SelectedItems { get; }

        public ListedItem SelectedItem { get; }

        void SetItemOpacity(ListedItem item); // TODO: Add opactiy value here

        void ResetItemOpacity();

        void ClearSelection();

        void SelectAllItems();

        void InvertSelection();

        void SetDragModeForItems();

        void ScrollIntoView(ListedItem item);

        void SetSelectedItemOnUi(ListedItem item);

        void SetSelectedItemsOnUi(List<ListedItem> selectedItems);

        void AddSelectedItemsOnUi(List<ListedItem> selectedItems);

        void FocusSelectedItems();

        void StartRenameItem();
    }
}