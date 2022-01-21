using System;
using Files.Backend.ViewModels.ItemListing;

namespace Files.Layouts
{
    /// <summary>
    /// Provides module for manipulating items in the file list
    /// </summary>
    internal interface IBaseListedLayoutItemSelectionModel : IDisposable
    {
        void FocusFileList();

        void SelectAllItems();

        void ClearSelection();

        void InvertSelection();

        void SetSelection(ListedItemViewModel listedItem);

        void AddSelection(ListedItemViewModel listedItem);

        void RemoveSelection(ListedItemViewModel listedItem);

        void FocusSelectedItems();
    }
}
