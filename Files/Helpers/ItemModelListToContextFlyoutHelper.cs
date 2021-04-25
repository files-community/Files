using Files.UserControls;
using Files.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml.Controls;
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

        public static (List<ICommandBarElement> primaryElements, List<ICommandBarElement> secondaryElements) GetAppBarItemsFromModel(List<ContextMenuFlyoutItemViewModel> items)
        {
            var primaryModels = items.Where(i => i.IsPrimary).ToList();
            var secondaryModels = items.Except(primaryModels).ToList();

            if (secondaryModels.Last().ItemType == ItemType.Separator)
            {
                secondaryModels.RemoveAt(secondaryModels.Count - 1);
            }

            var primary = new List<ICommandBarElement>();
            primaryModels.ForEach(i => primary.Add(GetCommandBarItem(i)));
            var secondary = new List<ICommandBarElement>();
            secondaryModels.ForEach(i => secondary.Add(GetCommandBarItem(i)));

            return (primary, secondary);
        }

        private static MenuFlyoutItemBase GetMenuItem(ContextMenuFlyoutItemViewModel item)
        {
            return item.ItemType switch
            {
                ItemType.Separator => new MenuFlyoutSeparator(),
                _ => GetMenuFlyoutItem(item),
            };
        }

        private static MenuFlyoutItemBase GetMenuFlyoutItem(ContextMenuFlyoutItemViewModel item, bool isToggle = false)
        {
            if (item.Items?.Count > 0)
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
            }
            else
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

            if (i.ItemType == ItemType.Toggle)
            {
                flyoutItem = new ToggleMenuFlyoutItem()
                {
                    Text = i.Text,
                    Tag = i.Tag,
                    Command = i.Command,
                    CommandParameter = i.CommandParameter,
                    IsChecked = i.IsChecked,
                };
            }
            else
            {
                var icon = new FontIcon
                {
                    Glyph = !string.IsNullOrEmpty(i.Glyph) ? i.Glyph : "",
                };

                if (!string.IsNullOrEmpty(i.GlyphFontFamilyName))
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

            if (i.KeyboardAccelerator != null)
            {
                flyoutItem.KeyboardAccelerators.Add(i.KeyboardAccelerator);
            }
            flyoutItem.IsEnabled = i.IsEnabled;

            return flyoutItem;
        }

        private static ICommandBarElement GetCommandBarItem(ContextMenuFlyoutItemViewModel item)
        {
            return item.ItemType switch
            {
                ItemType.Separator => new AppBarSeparator(),
                _ => GetCommandBarButton(item),
            };
        }

        private static ICommandBarElement GetCommandBarButton(ContextMenuFlyoutItemViewModel item)
        {
            ICommandBarElement element;
            FontIcon icon = null;
            if (!string.IsNullOrEmpty(item.Glyph))
            {
                icon = new FontIcon
                {
                    Glyph = item.Glyph,
                };
            }

            if (!string.IsNullOrEmpty(item.GlyphFontFamilyName))
            {
                var fontFamily = App.Current.Resources[item.GlyphFontFamilyName] as FontFamily;
                icon.FontFamily = fontFamily;
            }
            MenuFlyout ctxFlyout = null;
            if (item.Items.Count > 0)
            {
                ctxFlyout = new MenuFlyout();
                GetMenuFlyoutItemsFromModel(item.Items).ForEach(i => ctxFlyout.Items.Add(i));
            }

            Image content = null;
            if (item.BitmapIcon != null)
            {
                content = new Image()
                {
                    Source = item.BitmapIcon,
                };
            }

            if (item.ItemType == ItemType.Toggle)
            {
                element = new AppBarToggleButton()
                {
                    Label = item.Text,
                    Tag = item.Tag,
                    Command = item.Command,
                    CommandParameter = item.CommandParameter,
                    IsChecked = item.IsChecked,
                    Content = content,
                    IsEnabled = item.IsEnabled
                };

                if (icon != null)
                {
                    (element as AppBarToggleButton).Icon = icon;
                }

                if (item.IsPrimary)
                {
                    (element as AppBarToggleButton).SetValue(ToolTipService.ToolTipProperty, item.Text);
                }
            }
            else
            {
                element = new AppBarButton()
                {
                    Label = item.Text,
                    Tag = item.Tag,
                    Command = item.Command,
                    CommandParameter = item.CommandParameter,
                    Flyout = ctxFlyout,
                    Content = content,
                    IsEnabled = item.IsEnabled
                };

                if (icon != null)
                {
                    (element as AppBarButton).Icon = icon;
                }

                if (item.IsPrimary)
                {
                    (element as AppBarButton).SetValue(ToolTipService.ToolTipProperty, item.Text);
                }
            }

            return element;
        }
    }
}