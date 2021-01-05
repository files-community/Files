using System.Collections.Generic;

namespace Files.UserControls.Selection
{
    internal class InvertPreviousItemSelectionStrategy : ItemSelectionStrategy
    {
        private readonly List<object> prevSelectedItems;

        public InvertPreviousItemSelectionStrategy(ICollection<object> selectedItems, List<object> prevSelectedItems) : base(selectedItems)
        {
            this.prevSelectedItems = prevSelectedItems;
        }

        public override void HandleIntersectionWithItem(object item)
        {
            if (prevSelectedItems.Contains(item))
            {
                selectedItems.Remove(item);
            }
            else if (!selectedItems.Contains(item))
            {
                selectedItems.Add(item);
            }
        }

        public override void HandleNoIntersectionWithItem(object item)
        {
            // Restore selection on items not intersecting with the rectangle
            if (prevSelectedItems.Contains(item))
            {
                if (!selectedItems.Contains(item))
                {
                    selectedItems.Add(item);
                }
            }
            else
            {
                selectedItems.Remove(item);
            }
        }
    }
}