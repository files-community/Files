using System.Collections.Generic;

namespace Files.UserControls.Selection
{
    public class GenericSelectedItems : ISelectedItems
    {
        private readonly IList<object> selectedItems;

        public GenericSelectedItems(IList<object> selectedItems)
        {
            this.selectedItems = selectedItems;
        }

        public void Add(object item)
        {
            selectedItems.Add(item);
        }
        public void Clear()
        {
            selectedItems.Clear();
        }

        public bool Contains(object item)
        {
            return selectedItems.Contains(item);
        }

        public void Remove(object item)
        {
            selectedItems.Remove(item);
        }
    }
}