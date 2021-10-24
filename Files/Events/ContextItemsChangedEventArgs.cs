using Files.ViewModels;
using System;
using System.Collections.Generic;

namespace Files.Events
{
    public class ContextItemsChangedEventArgs : EventArgs
    {
        public ContextItemsChangedEventArgs(List<ContextMenuFlyoutItemViewModel> items)
        {
            Items = items;
        }

        public List<ContextMenuFlyoutItemViewModel> Items { get; }
    }
}