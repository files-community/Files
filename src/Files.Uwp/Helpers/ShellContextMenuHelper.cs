using Files.Common;
using Files.Filesystem;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    public static class ShellContextmenuHelper
    {
        public static async Task<List<ContextMenuFlyoutItemViewModel>> GetShellContextmenuAsync(bool showOpenMenu, bool shiftPressed, NamedPipeAsAppServiceConnection connection, string workingDirectory, List<ListedItem> selectedItems)
        {
            bool IsItemSelected = selectedItems?.Count > 0;

            var menuItemsList = new List<ContextMenuFlyoutItemViewModel>();

            if (connection != null)
            {
                var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "LoadContextMenu" },
                    { "FilePath", IsItemSelected ?
                        string.Join('|', selectedItems.Select(x => x.ItemPath)) :
                        workingDirectory},
                    { "ExtendedMenu", shiftPressed },
                    { "ShowOpenMenu", showOpenMenu }
                });

                if (status == AppServiceResponseStatus.Success
                    && response.ContainsKey("Handle"))
                {
                    var contextMenu = JsonConvert.DeserializeObject<Win32ContextMenu>((string)response["ContextMenu"]);
                    if (contextMenu != null)
                    {
                        LoadMenuFlyoutItem(menuItemsList, contextMenu.Items, (string)response["Handle"], true);
                    }
                }
            }

            return menuItemsList;
        }

        public static void LoadMenuFlyoutItem(IList<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
                                IEnumerable<Win32ContextMenuItem> menuFlyoutItems,
                                string menuHandle,
                                bool showIcons = true,
                                int itemsBeforeOverflow = int.MaxValue)
        {
            var itemsCount = 0; // Separators do not count for reaching the overflow threshold
            var menuItems = menuFlyoutItems.TakeWhile(x => x.Type == MenuItemType.MFT_SEPARATOR || ++itemsCount <= itemsBeforeOverflow).ToList();
            var overflowItems = menuFlyoutItems.Except(menuItems).ToList();

            if (overflowItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any())
            {
                var moreItem = menuItemsListLocal.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
                if (moreItem == null)
                {
                    var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                        Tag = ((Win32ContextMenuItem)null, menuHandle),
                        Glyph = "\xE712",
                    };
                    LoadMenuFlyoutItem(menuLayoutSubItem.Items, overflowItems, menuHandle, showIcons);
                    menuItemsListLocal.Insert(0, menuLayoutSubItem);
                }
                else
                {
                    LoadMenuFlyoutItem(moreItem.Items, overflowItems, menuHandle, showIcons);
                }
            }
            foreach (var menuFlyoutItem in menuItems
                .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR) // Remove leading separators
                .Reverse()
                .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR)) // Remove trailing separators
            {
                if ((menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR) && (menuItemsListLocal.FirstOrDefault().ItemType == ItemType.Separator))
                {
                    // Avoid duplicate separators
                    continue;
                }

                BitmapImage image = null;
                if (showIcons)
                {
                    if (!string.IsNullOrEmpty(menuFlyoutItem.IconBase64))
                    {
                        image = new BitmapImage();
                        byte[] bitmapData = Convert.FromBase64String(menuFlyoutItem.IconBase64);
                        using var ms = new MemoryStream(bitmapData);
                        image.SetSourceAsync(ms.AsRandomAccessStream()).AsTask().Wait(10);
                    }
                }

                if (menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR)
                {
                    var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
                    {
                        ItemType = ItemType.Separator,
                        Tag = (menuFlyoutItem, menuHandle)
                    };
                    menuItemsListLocal.Insert(0, menuLayoutItem);
                }
                else if (menuFlyoutItem.SubItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any()
                    && !string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
                        Tag = (menuFlyoutItem, menuHandle),
                        Items = new List<ContextMenuFlyoutItemViewModel>(),
                    };
                    LoadMenuFlyoutItem(menuLayoutSubItem.Items, menuFlyoutItem.SubItems, menuHandle, showIcons);
                    menuItemsListLocal.Insert(0, menuLayoutSubItem);
                }
                else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
                        Tag = (menuFlyoutItem, menuHandle),
                        BitmapIcon = image
                    };
                    menuLayoutItem.Command = new RelayCommand<object>(x => InvokeShellMenuItem(x));
                    menuLayoutItem.CommandParameter = (menuFlyoutItem, menuHandle);
                    menuItemsListLocal.Insert(0, menuLayoutItem);
                }
            }

            async void InvokeShellMenuItem(object tag)
            {
                var connection = await AppServiceConnectionHelper.Instance;
                var (menuItem, menuHandle) = ParseContextMenuTag(tag);
                if (connection != null)
                {
                    await connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "ExecAndCloseContextMenu" },
                        { "Handle", menuHandle },
                        { "ItemID", menuItem.ID },
                        { "CommandString", menuItem.CommandString }
                    });
                }
            }

            (Win32ContextMenuItem menuItem, string menuHandle) ParseContextMenuTag(object tag)
            {
                if (tag is ValueTuple<Win32ContextMenuItem, string> tuple)
                {
                    (Win32ContextMenuItem menuItem, string menuHandle) = tuple;
                    return (menuItem, menuHandle);
                }

                return (null, null);
            }
        }

        public static List<ContextMenuFlyoutItemViewModel> GetOpenWithItems(List<ContextMenuFlyoutItemViewModel> flyout)
        {
            var item = flyout.FirstOrDefault(x => x.Tag is ValueTuple<Win32ContextMenuItem, string> vt && vt.Item1.CommandString == "openas");
            flyout.Remove(item);
            return item?.Items;
        }
    }
}