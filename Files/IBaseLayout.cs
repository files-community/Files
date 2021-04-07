using Files.Filesystem;
using Files.Interacts;
using System;
using System.Collections.Generic;

namespace Files
{
    public interface IBaseLayout : IDisposable
    {
        bool IsItemSelected { get; }
        bool IsRenamingItem { get; }
        ItemManipulationModel ItemManipulationModel { get; }
        public ListedItem SelectedItem { get; }
        public List<ListedItem> SelectedItems { get; }
    }
}