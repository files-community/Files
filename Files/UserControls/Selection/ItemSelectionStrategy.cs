using System.Collections.Generic;

namespace Files.UserControls.Selection
{

    public abstract class ItemSelectionStrategy
    {
        protected readonly IList<object> selectedItems;

        protected ItemSelectionStrategy(IList<object> selectedItems)
        {
            this.selectedItems = selectedItems;
        }

        public abstract void HandleIntersectionWithItem(object item);

        public abstract void HandleNoIntersectionWithItem(object item);

        public virtual void StartSelection()
        {
        }
    }
}