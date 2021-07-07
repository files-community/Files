﻿using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            try
            {
                if (!selectedItems.Contains(item))
                {
                    selectedItems.Add(item);
                }
            }
            catch (COMException) // List is being modified
            {
            }
        }

        public override void HandleNoIntersectionWithItem(object item)
        {
            try
            {
                // Restore selection on items not intersecting with the rectangle
                if (!prevSelectedItems.Contains(item))
                {
                    selectedItems.Remove(item);
                }
            }
            catch (COMException) // List is being modified
            {
            }
        }
    }
}