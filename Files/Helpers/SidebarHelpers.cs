using Files.Filesystem;
using System.Collections.Generic;

namespace Files.Helpers
{
    public static class SidebarHelpers
    {
        public static void UnpinItems(List<ListedItem> itemsToUnpin)
        {
            foreach (var item in itemsToUnpin)
            {
                App.SidebarPinnedController.Model.RemoveItem(item.ItemPath);
            }
        }

        public static void PinItems(List<ListedItem> itemsToPin)
        {
            foreach (ListedItem listedItem in itemsToPin)
            {
                App.SidebarPinnedController.Model.AddItem(listedItem.ItemPath);
            }
        }
    }
}