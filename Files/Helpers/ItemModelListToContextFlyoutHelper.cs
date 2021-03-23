using Files.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Files.Helpers.ContextFlyouts
{
    public static class ItemModelListToContextFlyoutHelper
    {
        public static MenuFlyout GetMenuFlyoutFromModel(List<ContextMenuFlyoutItemViewModel> items)
        {
            var flyout = new MenuFlyout();
            items.ForEach(i =>
            {
                flyout.Items.Add(GetMenuItem(i));
            });
            return flyout;
        }

        private static MenuFlyoutItemBase GetMenuItem(ContextMenuFlyoutItemViewModel item)
        {
            switch (item.ItemType)
            {
                case ItemType.Separator:
                    return new MenuFlyoutSeparator();

                default:
                    return GetMenuFlyoutItem(item);
            }
        }

        private static MenuFlyoutItemBase GetMenuFlyoutItem(ContextMenuFlyoutItemViewModel item)
        {
            if(item.Items?.Count > 0)
            {
                var flyoutSubItem = new MenuFlyoutSubItem()
                {
                    Text = item.Text,
                };
                item.Items.ForEach(i =>
                {
                    flyoutSubItem.Items.Add(GetMenuItem(i));
                });
                return flyoutSubItem;
            } else
            {
                return GetItem(item);
            }
        }

        private static MenuFlyoutItemBase GetItem(ContextMenuFlyoutItemViewModel i)
        {
            var flyoutItem = new MenuFlyoutItem()
            {
                Text = i.Text,
            };
            return flyoutItem;
        }
    }
}
