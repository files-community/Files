namespace Files.UserControls.Selection
{

    public abstract class ItemSelectionStrategy
    {
        protected readonly ISelectedItems selectedItems;

        protected ItemSelectionStrategy(ISelectedItems selectedItems)
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