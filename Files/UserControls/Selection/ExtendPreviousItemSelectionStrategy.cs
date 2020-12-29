using System.Collections.Generic;

namespace Files.UserControls.Selection
{
    public class ExtendPreviousItemSelectionStrategy : ItemSelectionStrategy
    {
        private readonly List<object> prevSelectedItems;

        public ExtendPreviousItemSelectionStrategy(ICollection<object> selectedItems, List<object> prevSelectedItems) : base(selectedItems)
        {
            this.prevSelectedItems = prevSelectedItems;
        }

        public override void HandleIntersectionWithItem(object item)
        {
            if (!selectedItems.Contains(item))
            {
                selectedItems.Add(item);
            }
        }

        public override void HandleNoIntersectionWithItem(object item)
        {
            // Restore selection on items not intersecting with the rectangle
            if (!prevSelectedItems.Contains(item))
            {
                selectedItems.Remove(item);
            }
        }
    }
}