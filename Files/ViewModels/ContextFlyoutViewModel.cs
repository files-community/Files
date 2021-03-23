using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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
        private List<ListedItem> selectedItems;
        public List<ListedItem> SelectedItems
        {
            get => selectedItems;
            set => SetProperty(ref selectedItems, value);
        }

        private List<ContextMenuFlyoutItemViewModel> menuItemsList = new List<ContextMenuFlyoutItemViewModel>();
        public List<ContextMenuFlyoutItemViewModel> MenuItemsList
        {
            get => menuItemsList;
            set => SetProperty(ref menuItemsList, value);
        }

        public void Filter()
        {
            MenuItemsList = MenuItemsList.Where(x => x.CheckShowItem?.Invoke(SelectedItems) ?? true).ToList();
        }

        public string CurrentDirectoryPath { get; set; }

        public NamedPipeAsAppServiceConnection Connection { get; set; }

        public bool IsItemSelected => selectedItems?.Count > 0;

        public async Task SetShellContextmenu(List<ContextMenuFlyoutItemViewModel> baseItems, bool shiftPressed, bool showOpenMenu)
        {
            MenuItemsList = new List<ContextMenuFlyoutItemViewModel>(baseItems);
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
                        await LoadMenuFlyoutItem(MenuItemsList, contextMenu.Items, (string)response["Handle"], true, maxItems);
                    }
                }
            }
            var totalFlyoutItems = baseItems.Count - currentBaseLayoutItemCount;
            if (totalFlyoutItems > 0 && !(baseItems[totalFlyoutItems].ItemType == ItemType.Separator))
            {
                MenuItemsList.Insert(totalFlyoutItems, new ContextMenuFlyoutItemViewModel() { ItemType = ItemType.Separator });
            }
        }

        private async Task LoadMenuFlyoutItem(IList<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
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
                await LoadMenuFlyoutItem(menuLayoutSubItem.Items, overflowItems, menuHandle, false);
                menuItemsListLocal.Insert(0, menuLayoutSubItem);
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
                    image = new BitmapImage();
                    if (!string.IsNullOrEmpty(menuFlyoutItem.IconBase64))
                    {
                        byte[] bitmapData = Convert.FromBase64String(menuFlyoutItem.IconBase64);
                        using var ms = new MemoryStream(bitmapData);
                        await image.SetSourceAsync(ms.AsRandomAccessStream());
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
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                    };
                    await LoadMenuFlyoutItem(menuLayoutSubItem.Items, menuFlyoutItem.SubItems, menuHandle, false);
                    menuItemsListLocal.Insert(0, menuLayoutSubItem);
                }
                else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                        BitmapIcon = image
                    };
                    menuLayoutItem.Command = new RelayCommand<object>(x => InvokeShellMenuItem(x));
                    menuLayoutItem.CommandParameter = (menuFlyoutItem, menuHandle);
                    menuItemsListLocal.Insert(0, menuLayoutItem);
                }
            }
        }

        private async void InvokeShellMenuItem(object tag)
        {
            var (menuItem, menuHandle) = ParseContextMenuTag(tag);
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

        private (Win32ContextMenuItem menuItem, string menuHandle) ParseContextMenuTag(object tag)
        {
            if (tag is ValueTuple<Win32ContextMenuItem, string> tuple)
            {
                (Win32ContextMenuItem menuItem, string menuHandle) = tuple;
                return (menuItem, menuHandle);
            }

            return (null, null);
        }

        public static class BaseItems
        {
            public static List<ContextMenuFlyoutItemViewModel> GetItemContextFlyoutItems(BaseLayoutCommandsViewModel commandsViewModel)
            {
                return new List<ContextMenuFlyoutItemViewModel>()
                {
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Open Item",
                        Glyph = "\uE8E5",
                        Command = commandsViewModel.OpenItemCommand,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Open with",
                        Glyph = "\uE17D",
                        Command = commandsViewModel.OpenItemWithApplicationPickerCommand,
                        CheckShowItem = new Func<List<ListedItem>, bool>(x => x.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.File && !i.IsShortcutItem)),
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Open file location",
                        Glyph = "\uE8DA",
                        Command = commandsViewModel.OpenFileLocationCommand,
                        CheckShowItem = new Func<List<ListedItem>, bool>(x => x.All(i => i.IsShortcutItem)),
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Open in new pane",
                        Glyph = "\uF57C",
                        Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
                        CheckShowItem = new Func<List<ListedItem>, bool>(x => App.AppSettings.IsDualPaneEnabled && x.Count == 1 && x.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Open in new tab",
                        Glyph = "\uF113",
                        Command = commandsViewModel.OpenDirectoryInNewTabCommand,
                        CheckShowItem = new Func<List<ListedItem>, bool>(x => x.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Open in new window",
                        Glyph = "\uE737",
                        Command = commandsViewModel.OpenInNewWindowItemCommand,
                        CheckShowItem = new Func<List<ListedItem>, bool>(x => x.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Set as",
                        CheckShowItem = new Func<List<ListedItem>, bool>(x => x.All(i => i.FileExtension == ".png")),
                        Items = new List<ContextMenuFlyoutItemViewModel>()
                        {
                            new ContextMenuFlyoutItemViewModel()
                            {
                                Text = "Set as desktop background",
                                Glyph = "\uE91B",
                                Command = commandsViewModel.SetAsDesktopBackgroundItemCommand,
                            },
                            new ContextMenuFlyoutItemViewModel()
                            {
                                Text = "Set as lock screen background",
                                Glyph = "\uF114",
                                Command = commandsViewModel.SetAsLockscreenBackgroundItemCommand,
                            },
                        }
                    },
                  new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Share",
                        Glyph = "\uE72D",
                        Command = commandsViewModel.ShareItemCommand,
                        CheckShowItem = new Func<List<ListedItem>, bool>(x => !x.Any(i => i.IsHiddenItem || i.IsShortcutItem)),
                    },
                };
            }
        }
    }
}
