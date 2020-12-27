using System.Collections.Generic;

namespace Files.UserControls.Selection
{
    public class IgnorePreviousItemSelectionStrategy : ItemSelectionStrategy
    {
        public IgnorePreviousItemSelectionStrategy(IList<object> selectedItems) : base(selectedItems)
        {
        }

        public override void HandleIntersectionWithItem(object item)
        {
            // Select item intersection with the rectangle
            if (!selectedItems.Contains(item))
            {
                selectedItems.Add(item);
            }
        }

        public override void HandleNoIntersectionWithItem(object item)
        {
            selectedItems.Remove(item);
        }

        public override void StartSelection()
        {
            selectedItems.Clear();
        }
    }
}