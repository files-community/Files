using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels
{
    public class ContextFlyoutViewModel : ObservableObject
    {
        private List<MenuFlyoutItemBase> baseMenuFlyoutItems;
        public List<MenuFlyoutItemBase> BaseMenuFlyoutItems
        {
            get => baseMenuFlyoutItems;
            set => SetProperty(ref baseMenuFlyoutItems, value);
        }
        
        private List<MenuFlyoutItemBase> menuFlyoutItems;
        public List<MenuFlyoutItemBase> MenuFlyoutItems
        {
            get => menuFlyoutItems;
            set => SetProperty(ref menuFlyoutItems, value);
        }

        private List<ListedItem> selectedItems;
        public List<ListedItem> SelectedItems
        {
            get => selectedItems;
            set => SetProperty(ref selectedItems, value);
        }

        public string CurrentDirectoryPath { get; set; }

        public NamedPipeAsAppServiceConnection Connection { get; set; }

        public bool IsItemSelected => selectedItems?.Count > 0;

        public void SetShellContextmenu(List<ContextMenuFlyoutItemViewModel> baseItems, bool shiftPressed, bool showOpenMenu)
        {
            var currentBaseLayoutItemCount = baseItems.Count;
            var maxItems = !App.AppSettings.MoveOverflowMenuItemsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 4;

            if (Connection != null)
            {
                var (status, response) = Task.Run(() => Connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "LoadContextMenu" },
                    { "FilePath", IsItemSelected ?
                        string.Join('|', selectedItems.Select(x => x.ItemPath)) :
                        CurrentDirectoryPath},
                    { "ExtendedMenu", shiftPressed },
                    { "ShowOpenMenu", showOpenMenu }
                })).Result;
                if (status == AppServiceResponseStatus.Success
                    && response.ContainsKey("Handle"))
                {
                    var contextMenu = JsonConvert.DeserializeObject<Win32ContextMenu>((string)response["ContextMenu"]);
                    if (contextMenu != null)
                    {
                        LoadMenuFlyoutItem(baseItems, contextMenu.Items, (string)response["Handle"], true, maxItems);
                    }
                }
            }
            var totalFlyoutItems = baseItems.Count - currentBaseLayoutItemCount;
            if (totalFlyoutItems > 0 && !(baseItems[totalFlyoutItems].IsSeparator))
            {
                MenuFlyoutItems.Insert(totalFlyoutItems, new MenuFlyoutSeparator());
            }
        }

        private void LoadMenuFlyoutItem(IList<ContextMenuFlyoutItemViewModel> MenuItemsList,
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
                var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Tag = ((Win32ContextMenuItem)null, menuHandle),
                    Glyph = "\xE712",
                };
                LoadMenuFlyoutItem(menuLayoutSubItem.Items, overflowItems, menuHandle, false);
                MenuItemsList.Insert(0, menuLayoutSubItem);
            }
            foreach (var menuFlyoutItem in menuItems
                .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR) // Remove leading separators
                .Reverse()
                .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR)) // Remove trailing separators
            {
                if ((menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR) && (MenuItemsList.FirstOrDefault() is MenuFlyoutSeparator))
                {
                    // Avoid duplicate separators
                    continue;
                }

                BitmapImage image = null;
                if (showIcons)
                {
                    image = new BitmapImage();
                    if (!string.IsNullOrEmpty(menuFlyoutItem.IconBase64))
                    {
                        byte[] bitmapData = Convert.FromBase64String(menuFlyoutItem.IconBase64);
                        using var ms = new MemoryStream(bitmapData);
#pragma warning disable CS4014
                        image.SetSourceAsync(ms.AsRandomAccessStream());
#pragma warning restore CS4014
                    }
                }

                if (menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR)
                {
                    var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
                    {
                        IsSeparator = true,
                        Tag = (menuFlyoutItem, menuHandle)
                    };
                    MenuItemsList.Insert(0, menuLayoutItem);
                }
                else if (menuFlyoutItem.SubItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any()
                    && !string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                    };
                    LoadMenuFlyoutItem(menuLayoutSubItem.Items, menuFlyoutItem.SubItems, menuHandle, false);
                    MenuItemsList.Insert(0, menuLayoutSubItem);
                }
                else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                        BitmapIcon = image
                    };
                    menuLayoutItem.Click = MenuLayoutItem_Click;
                    MenuItemsList.Insert(0, menuLayoutItem);
                }
            }
        }

        private async void MenuLayoutItem_Click(object sender, RoutedEventArgs e)
        {
            var currentMenuLayoutItem = (MenuFlyoutItem)sender;
            if (currentMenuLayoutItem != null)
            {
                var (menuItem, menuHandle) = ParseContextMenuTag(currentMenuLayoutItem.Tag);
                if (Connection != null)
                {
                    await Connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "ExecAndCloseContextMenu" },
                        { "Handle", menuHandle },
                        { "ItemID", menuItem.ID },
                        { "CommandString", menuItem.CommandString }
                    });
                }
            }
        }

        private (Win32ContextMenuItem menuItem, string menuHandle) ParseContextMenuTag(object tag)
        {
            if (tag is ValueTuple<Win32ContextMenuItem, string> tuple)
            {
                (Win32ContextMenuItem menuItem, string menuHandle) = tuple;
                return (menuItem, menuHandle);
            }

            return (null, null);
        }
    }

    public static class BaseItems
    {
        public static List<ContextMenuFlyoutItemViewModel> ItemContextFlyoutItems { get; } = new List<ContextMenuFlyoutItemViewModel>()
        {

        };
    } 
}
