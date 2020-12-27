using System.Collections;

namespace Files.UserControls.Selection
{
    public class RawSelectedItems : ISelectedItems
    {
        private readonly IList selectedItems;

        public RawSelectedItems(System.Collections.IList selectedItems)
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