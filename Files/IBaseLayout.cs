using Files.Filesystem;
using Files.Interacts;
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

        ItemManipulationModel ItemManipulationModel { get; }
    }
}