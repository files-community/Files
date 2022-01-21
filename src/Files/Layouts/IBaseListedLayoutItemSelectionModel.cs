using System;
using Files.Filesystem;

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

        void AddSelection(ListedItem listedItem);

        void RemoveSelection(ListedItem listedItem);

        void FocusSelectedItems();
    }
}
