using Files.UserControls;
using Files.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Files.Helpers.ContextFlyouts
{
    public static class ItemModelListToContextFlyoutHelper
    {
        public static List<MenuFlyoutItemBase> GetMenuFlyoutItemsFromModel(List<ContextMenuFlyoutItemViewModel> items)
        {
            var flyout = new List<MenuFlyoutItemBase>();
            items.ForEach(i =>
            {
                flyout.Add(GetMenuItem(i));
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

        private static MenuFlyoutItemBase GetMenuFlyoutItem(ContextMenuFlyoutItemViewModel item, bool isToggle = false)
        {
            if(item.Items?.Count > 0)
            {
                var flyoutSubItem = new MenuFlyoutSubItem()
                {
                    Text = item.Text,
                    Tag = item.Tag,
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

            if (i.BitmapIcon != null)
            {
                var item = new MenuFlyoutItemWithImage()
                {
                    Text = i.Text,
                    Tag = i.Tag,
                    Command = i.Command,
                    CommandParameter = i.CommandParameter,
                };
                try
                {
                    item.BitmapIcon = i.BitmapIcon;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                return item;
            }
            MenuFlyoutItemBase flyoutItem;

            if(i.ItemType == ItemType.Toggle)
            {
                flyoutItem = new ToggleMenuFlyoutItem()
                {
                    Text = i.Text,
                    Tag = i.Tag,
                    Command = i.Command,
                    CommandParameter = i.CommandParameter,
                    IsChecked = i.IsChecked,
                };
            } else
            {
                var icon = new FontIcon
                {
                    Glyph = !string.IsNullOrEmpty(i.Glyph) ? i.Glyph : "",
                };

                if(!string.IsNullOrEmpty(i.GlyphFontFamilyName))
                {
                    var fontFamily = App.Current.Resources[i.GlyphFontFamilyName] as FontFamily;
                    icon.FontFamily = fontFamily;
                }

                flyoutItem = new MenuFlyoutItem()
                {
                    Text = i.Text,
                    Tag = i.Tag,
                    Command = i.Command,
                    CommandParameter = i.CommandParameter,
                    Icon = icon,
                };
            }

            if(i.KeyboardAccelerator != null)
            {
                flyoutItem.KeyboardAccelerators.Add(i.KeyboardAccelerator);
            }
            flyoutItem.IsEnabled = i.IsEnabled;

            return flyoutItem;
        }
    }
}
