using System.Collections;
using System.Collections.Generic;

namespace Files.UserControls.Selection
{
    public interface ISelectedItems
    {
        void Add(object item);

        void Clear();

        bool Contains(object item);

        void Remove(object item);
    }

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

    public class ExtendPreviousItemSelectionStrategy : ItemSelectionStrategy
    {
        private readonly List<object> prevSelectedItems;

        public ExtendPreviousItemSelectionStrategy(ISelectedItems selectedItems, List<object> prevSelectedItems) : base(selectedItems)
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

    public class IgnorePreviousItemSelectionStrategy : ItemSelectionStrategy
    {
        public IgnorePreviousItemSelectionStrategy(ISelectedItems selectedItems) : base(selectedItems)
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