using Files.Filesystem;
using Files.Interacts;
using Files.ViewModels;
using System;
using System.Collections.Generic;

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

        PreviewPaneViewModel PreviewPaneViewModel { get; }

        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }
    }
}