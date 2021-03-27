using Files.Common;
using Files.DataModels;
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
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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

        private SelectedItemsPropertiesViewModel selectedItemsPropertiesViewModel;
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel
        {
            get => selectedItemsPropertiesViewModel;
            set => SetProperty(ref selectedItemsPropertiesViewModel, value);
        }

        private BaseLayoutCommandsViewModel commandsViewModel;
        public BaseLayoutCommandsViewModel CommandsViewModel
        {
            get => commandsViewModel;
            set => SetProperty(ref commandsViewModel, value);
        }

        public CurrentInstanceViewModel CurrentInstanceViewModel { get; set; }
        public ItemViewModel ItemViewModel { get; set; }
        public List<ShellNewEntry> CachedNewContextMenuEntries { get; set; }

        private List<ContextMenuFlyoutItemViewModel> menuItemsList = new List<ContextMenuFlyoutItemViewModel>();
        public List<ContextMenuFlyoutItemViewModel> MenuItemsList
        {
            get => menuItemsList;
            set => SetProperty(ref menuItemsList, value);
        }

        public void Filter(bool shiftPressed)
        {
            MenuItemsList = MenuItemsList.Where(x => Check(x, shiftPressed)).ToList();
            MenuItemsList.ForEach(x => x.Items = x.Items.Where(y => Check(y, shiftPressed)).ToList());
        }

        private bool Check(ContextMenuFlyoutItemViewModel x, bool shiftPressed)
        {
            return (x.ShowInRecycleBin || !CurrentInstanceViewModel.IsPageTypeRecycleBin) // Hide non-recycle bin items
                && (!x.ShowOnShift || shiftPressed)  // Hide items that are only shown on shift
                && (!x.SingleItemOnly || SelectedItems.Count == 1)
                && (x.CheckShowItem?.Invoke() ?? true);
        }

        public NamedPipeAsAppServiceConnection Connection { get; set; }

        public bool IsItemSelected => selectedItems?.Count > 0;

        public void LoadItemContextCommands(bool shiftPressed, bool showOpenMenu)
        {
            SetShellContextmenu(BaseItemMenuItems, shiftPressed, showOpenMenu);
            Filter(shiftPressed);
        }

        public void LoadBaseContextCommands(bool shiftPressed, bool showOpenMenu)
        {
            SetShellContextmenu(BaseLayoutMenuItems, shiftPressed, showOpenMenu);
            Filter(shiftPressed);
        }

        public void SetShellContextmenu(List<ContextMenuFlyoutItemViewModel> baseItems, bool shiftPressed, bool showOpenMenu)
        {
            MenuItemsList = new List<ContextMenuFlyoutItemViewModel>(baseItems);
            if(CurrentInstanceViewModel.IsPageTypeRecycleBin)
            {
                return;
            }

            var currentBaseLayoutItemCount = baseItems.Count;
            var maxItems = !App.AppSettings.MoveOverflowMenuItemsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 4;

            if (Connection != null)
            {
                var (status, response) = Task.Run(() => Connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "LoadContextMenu" },
                    { "FilePath", IsItemSelected ?
                        string.Join('|', selectedItems.Select(x => x.ItemPath)) :
                        ItemViewModel.WorkingDirectory},
                    { "ExtendedMenu", shiftPressed },
                    { "ShowOpenMenu", showOpenMenu }
                })).Result;
                if (status == AppServiceResponseStatus.Success
                    && response.ContainsKey("Handle"))
                {
                    var contextMenu = JsonConvert.DeserializeObject<Win32ContextMenu>((string)response["ContextMenu"]);
                    if (contextMenu != null)
                    {
                        LoadMenuFlyoutItem(MenuItemsList, contextMenu.Items, (string)response["Handle"], true, maxItems);
                    }
                }
            }
            var totalFlyoutItems = baseItems.Count - currentBaseLayoutItemCount;
            if (totalFlyoutItems > 0 && !(baseItems[totalFlyoutItems].ItemType == ItemType.Separator))
            {
                MenuItemsList.Insert(totalFlyoutItems, new ContextMenuFlyoutItemViewModel() { ItemType = ItemType.Separator });
            }
        }

        private void LoadMenuFlyoutItem(IList<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
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
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                    };
                    LoadMenuFlyoutItem(menuLayoutSubItem.Items, menuFlyoutItem.SubItems, menuHandle, false);
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

        public List<ContextMenuFlyoutItemViewModel> NewItemItems { 
            get {
                var list = new List<ContextMenuFlyoutItemViewModel>()
                {
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Folder",
                        Glyph = "\uE8B7",
                        Command = commandsViewModel.CreateNewFolderCommand,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        ItemType = ItemType.Separator,
                    }
                };

                CachedNewContextMenuEntries?.ForEach(i =>
                {
                    if (i.Icon != null)
                    {
                        var bitmap = new BitmapImage();
                        bitmap.SetSourceAsync(i.Icon);
                        list.Add(new ContextMenuFlyoutItemViewModel()
                        {
                            Text = i.Name,
                            BitmapIcon = bitmap,
                            Command = commandsViewModel.CreateNewFileCommand,
                            //CommandParameter = i,
                        });
                    } else
                    {
                        list.Add(new ContextMenuFlyoutItemViewModel()
                        {
                            Text = i.Name,
                            Glyph = "\xE7C3",
                            Command = commandsViewModel.CreateNewFileCommand,
                            //CommandParameter = i,
                        });
                    }

                });

                return list;
            }
        }

        public List<ContextMenuFlyoutItemViewModel> BaseItemMenuItems => new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Restore",
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.RestoreItemCommand,
                    ShowInRecycleBin = true,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.IsRecycleBinItem))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Open Item",
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.OpenItemCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.Count <= 10)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Open with",
                    Glyph = "\uE17D",
                    Command = commandsViewModel.OpenItemWithApplicationPickerCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.File && !i.IsShortcutItem)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Open file location",
                    Glyph = "\uE8DA",
                    Command = commandsViewModel.OpenFileLocationCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(i => i.IsShortcutItem)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Open in new pane",
                    Glyph = "\uF57C",
                    Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
                    CheckShowItem = new Func<bool>(() => App.AppSettings.IsDualPaneEnabled && SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Open in new tab",
                    Glyph = "\uEC6C",
                    Command = commandsViewModel.OpenDirectoryInNewTabCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.Count < 5 && SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Open in new window",
                    Glyph = "\uE737",
                    Command = commandsViewModel.OpenInNewWindowItemCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.Count < 5 && SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Set as",
                    CheckShowItem = new Func<bool>(() => SelectedItemsPropertiesViewModel.IsSelectedItemImage),
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
                    Text = "Run as admin",
                    Glyph = "\uE7EF",
                    Command = CommandsViewModel.RunAsAdminCommand,
                    CheckShowItem = new Func<bool>(() => new string[]{".bat", ".exe", "cmd" }.Contains(SelectedItems.FirstOrDefault().FileExtension))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Run as another user",
                    Glyph = "\uE7EE",
                    Command = CommandsViewModel.RunAsAnotherUserCommand,
                    CheckShowItem = new Func<bool>(() => new string[]{".bat", ".exe", "cmd" }.Contains(SelectedItems.FirstOrDefault().FileExtension))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Share",
                    Glyph = "\uE72D",
                    Command = commandsViewModel.ShareItemCommand,
                    CheckShowItem = new Func<bool>(() => !SelectedItems.Any(i => i.IsHiddenItem || i.IsShortcutItem)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ItemType.Separator,
                    ShowInRecycleBin = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Cut",
                    Glyph = "\uE8C6",
                    Command = commandsViewModel.CutItemCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Copy",
                    Glyph = "\uE8C8",
                    Command = commandsViewModel.CopyItemCommand,
                    ShowInRecycleBin = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Copy item location",
                    Glyph = "\uE167",
                    Command = commandsViewModel.CopyPathOfSelectedItemCommand,
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Create shortcut",
                    Glyph = "\uF10A",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = commandsViewModel.CreateShortcutCommand,
                    CheckShowItem = new Func<bool>(() => !SelectedItems.FirstOrDefault().IsShortcutItem),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Delete",
                    Glyph = "\uE74D",
                    Command = commandsViewModel.DeleteItemCommand,
                    KeyboardAccelerator = new KeyboardAccelerator() { Key = Windows.System.VirtualKey.Delete },
                    ShowInRecycleBin = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Rename",
                    Glyph = "\uE8AC",
                    Command = commandsViewModel.RenameItemCommand,
                    KeyboardAccelerator = new KeyboardAccelerator() { Key = Windows.System.VirtualKey.F2 },
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Pin to sidebar",
                    Glyph = "\uE840",
                    Command = commandsViewModel.SidebarPinItemCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsPinned))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Unpin from sidebar",
                    Glyph = "\uE77A",
                    Command = commandsViewModel.SidebarUnpinItemCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsPinned))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Pin to the Start Menu",
                    Glyph = "\uE840",
                    //Command = commandsViewModel.pin,
                    ShowOnShift = true,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsItemPinnedToStart)),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Unpin from the Start Menu",
                    Glyph = "\uE77A",
                    //Command = commandsViewModel.UnpinDirectoryFromSidebarCommand,
                    ShowOnShift = true,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsItemPinnedToStart)),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "Properties",
                    Glyph = "\uE946",
                    Command = commandsViewModel.ShowPropertiesCommand,
                },
            };
        public List<ContextMenuFlyoutItemViewModel> BaseLayoutMenuItems => new List<ContextMenuFlyoutItemViewModel>()
        {
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Layout mode",
                Glyph = "\uE152",
                ShowInRecycleBin = true,
                Items = new List<ContextMenuFlyoutItemViewModel>()
                {
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Grid View (Large)",
                        Glyph = "\uE739",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Grid View (Medium)",
                        Glyph = "\uF0E2",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Grid View (Small)",
                        Glyph = "\uE80A",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Tiles View",
                        Glyph = "\uE15C",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeTiles,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Details View",
                        Glyph = "\uE179",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView,
                    },
                }
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Sort by",
                Glyph = "\uE8CB",
                ShowInRecycleBin = true,
                Items = new List<ContextMenuFlyoutItemViewModel>()
                {
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Name",
                        IsChecked = ItemViewModel.IsSortedByName,
                        ShowInRecycleBin = true,
                        // TODO: Make these better
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByName = true),
                        ItemType = ItemType.Toggle,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Orginial path",
                        IsChecked = ItemViewModel.IsSortedByOriginalPath,
                        ShowInRecycleBin = true,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByOriginalPath = true),
                        CheckShowItem = new Func<bool>(() => CurrentInstanceViewModel.IsPageTypeRecycleBin),
                        ItemType = ItemType.Toggle,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Date deleted",
                        IsChecked = ItemViewModel.IsSortedByDateDeleted,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByDateDeleted = true),
                        ShowInRecycleBin = true,
                        CheckShowItem = new Func<bool>(() => CurrentInstanceViewModel.IsPageTypeRecycleBin),
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Type",
                        IsChecked = ItemViewModel.IsSortedByType,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByType = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Size",
                        IsChecked = ItemViewModel.IsSortedBySize,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedBySize = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Date modified",
                        IsChecked = ItemViewModel.IsSortedByDate,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByDate = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Date created",
                        IsChecked = ItemViewModel.IsSortedByDateCreated,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByDateCreated = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        ItemType = ItemType.Separator,
                        ShowInRecycleBin = true,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Ascending",
                        IsChecked = ItemViewModel.IsSortedAscending,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedAscending = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "Descending",
                        IsChecked = ItemViewModel.IsSortedDescending,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedDescending = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                }
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Refresh",
                Glyph = "\uE72C",
                ShowInRecycleBin = true,
                KeyboardAccelerator = new KeyboardAccelerator
                {
                    Key = Windows.System.VirtualKey.V,
                    Modifiers = Windows.System.VirtualKeyModifiers.Shift,
                    IsEnabled = false,
                }
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Paste",
                Glyph = "\uE72C",
                Command = commandsViewModel.PasteItemsFromClipboardCommand,
                CheckShowItem = new Func<bool>(() => CurrentInstanceViewModel.CanPasteInPage && App.InteractionViewModel.IsPasteEnabled),
                KeyboardAccelerator = new KeyboardAccelerator
                {
                    Key = Windows.System.VirtualKey.V,
                    Modifiers = Windows.System.VirtualKeyModifiers.Control,
                    IsEnabled = false,
                }
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Open in terminal",
                Glyph = "\uE756",
                Command = commandsViewModel.OpenDirectoryInDefaultTerminalCommand,
            },
            new ContextMenuFlyoutItemViewModel()
            {
                ItemType = ItemType.Separator,
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "New",
                Glyph = "\uE710",
                KeyboardAccelerator = new KeyboardAccelerator
                {
                    Key = Windows.System.VirtualKey.N,
                    Modifiers = Windows.System.VirtualKeyModifiers.Control,
                    IsEnabled = false,
                },
                Items = NewItemItems,
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Pin directory to sidebar",
                Glyph = "\uE840",
                Command = commandsViewModel.SidebarPinItemCommand,
                CheckShowItem = new Func<bool>(() => !ItemViewModel.CurrentFolder.IsPinned)
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Unpin directory from sidebar",
                Glyph = "\uE77A",
                Command = commandsViewModel.SidebarUnpinItemCommand,
                CheckShowItem = new Func<bool>(() => ItemViewModel.CurrentFolder.IsPinned)
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Folder properties",
                Glyph = "\uE946",
                Command = commandsViewModel.ShowFolderPropertiesCommand,
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "Empty recycle bin",
                Glyph = "\uEF88",
                Command = commandsViewModel.EmptyRecycleBinCommand,
                CheckShowItem = new Func<bool>(() => CurrentInstanceViewModel.IsPageTypeRecycleBin)
            },
        };
    }
}
