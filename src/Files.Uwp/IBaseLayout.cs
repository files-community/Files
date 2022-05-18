using Files.Uwp.Filesystem;
using Files.Uwp.Interacts;
using Files.Uwp.ViewModels;
using System;
using System.Collections.Generic;

namespace Files.Uwp
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