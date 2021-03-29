using Files.DataModels;
using Files.Filesystem;
using Files.Interacts;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    public static class ContextFlyoutItemHelper
    {
        static List<ShellNewEntry> cachedNewContextMenuEntries; 
        public static List<ShellNewEntry> CachedNewContextMenuEntries { 
            get
            {
                cachedNewContextMenuEntries ??= Task.Run(() => RegistryHelper.GetNewContextMenuEntries()).Result;
                return cachedNewContextMenuEntries;
            }
        }

        public static List<ContextMenuFlyoutItemViewModel> GetItemContextCommands(NamedPipeAsAppServiceConnection connection, CurrentInstanceViewModel currentInstanceViewModel, string workingDir, List<ListedItem> selectedItems, BaseLayoutCommandsViewModel commandsViewModel, bool shiftPressed, bool showOpenMenu, SelectedItemsPropertiesViewModel selectedItemsPropertiesViewModel)
        {
            var menuItemsList = ShellContextmenuHelper.SetShellContextmenu(GetBaseItemMenuItems(commandsViewModel: commandsViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel), shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, connection: connection, workingDirectory: workingDir, selectedItems: selectedItems);
            menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems);
            return menuItemsList;
        }

        public static List<ContextMenuFlyoutItemViewModel> GetBaseContextCommands(NamedPipeAsAppServiceConnection connection, CurrentInstanceViewModel currentInstanceViewModel, ItemViewModel itemViewModel, BaseLayoutCommandsViewModel commandsViewModel, bool shiftPressed, bool showOpenMenu)
        {
            var menuItemsList = ShellContextmenuHelper.SetShellContextmenu(GetBaseLayoutMenuItems(currentInstanceViewModel, itemViewModel, commandsViewModel), shiftPressed, showOpenMenu, connection, itemViewModel.WorkingDirectory, new List<ListedItem>());
            menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: new List<ListedItem>());
            return menuItemsList;
        }
        public static List<ContextMenuFlyoutItemViewModel> Filter(List<ContextMenuFlyoutItemViewModel> items, List<ListedItem> selectedItems, bool shiftPressed, CurrentInstanceViewModel currentInstanceViewModel)
        {
            items = items.Where(x => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, shiftPressed: shiftPressed)).ToList();
            items.ForEach(x => x.Items = x.Items.Where(y => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, shiftPressed: shiftPressed)).ToList());
            var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
            if(overflow != null && !shiftPressed)
            {
                var overflowItems = items.Where(x => x.ShowOnShift).ToList();

                // Adds a separator between items already there and the new ones
                if(overflow.Items.Count != 0 && overflow.Items.Last().ItemType != ItemType.Separator && overflowItems.Count > 0)
                {
                    overflow.Items.Add(new ContextMenuFlyoutItemViewModel()
                    {
                        ItemType = ItemType.Separator,
                    });
                }
                overflowItems.ForEach(x => overflow.Items.Add(x));
                items = items.Except(overflowItems).ToList();
            }

            // remove the overflow if it has no child items
            if (overflow != null && overflow.Items.Count == 0)
            {
                items.Remove(overflow);
            }
            return items;
        }

        private static bool Check(ContextMenuFlyoutItemViewModel item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, bool shiftPressed)
        {
            return (item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin) // Hide non-recycle bin items
                && (!item.SingleItemOnly || selectedItems.Count == 1)
                && item.ShowItem;
        }

        public static List<ContextMenuFlyoutItemViewModel> GetBaseLayoutMenuItems(CurrentInstanceViewModel currentInstanceViewModel, ItemViewModel itemViewModel, BaseLayoutCommandsViewModel commandsViewModel)
        {
            return new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Glyph = "\xE712",
                    ID = "ItemOverflow"
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutLayoutMode2".GetLocalized(),
                    Glyph = "\uE152",
                    ShowInRecycleBin = true,
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        // Grid view large
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutGridViewLarge2".GetLocalized(),
                            Glyph = "\uE739",
                            ShowInRecycleBin = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge,
                        },
                        // Grid view medium
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutGridViewMedium2".GetLocalized(),
                            Glyph = "\uF0E2",
                            ShowInRecycleBin = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium,
                        },
                        // Grid view small
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutGridViewSmall2".GetLocalized(),
                            Glyph = "\uE80A",
                            ShowInRecycleBin = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall,
                        },
                        // Tiles view
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutTilesView2".GetLocalized(),
                            Glyph = "\uE15C",
                            ShowInRecycleBin = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeTiles,
                        },
                        // Details view
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutDetails2".GetLocalized(),
                            Glyph = "\uE179",
                            ShowInRecycleBin = true,
                            Command = currentInstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView,
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutSortBy2".GetLocalized(),
                    Glyph = "\uE8CB",
                    ShowInRecycleBin = true,
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByName2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByName,
                            ShowInRecycleBin = true,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByName = true),
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByOriginalPath2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByOriginalPath,
                            ShowInRecycleBin = true,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByOriginalPath = true),
                            ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDateDeleted2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByDateDeleted,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByDateDeleted = true),
                            ShowInRecycleBin = true,
                            ShowItem =currentInstanceViewModel.IsPageTypeRecycleBin,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByType2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByType,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByType = true),
                            ShowInRecycleBin = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortBySize2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedBySize,
                            Command = new RelayCommand(() => itemViewModel.IsSortedBySize = true),
                            ShowInRecycleBin = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDate2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByDate,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByDate = true),
                            ShowInRecycleBin = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDateCreated2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByDateCreated,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByDateCreated = true),
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
                            Text = "BaseLayoutContextFlyoutSortByAscending2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedAscending,
                            Command = new RelayCommand(() => itemViewModel.IsSortedAscending = true),
                            ShowInRecycleBin = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDescending2".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedDescending,
                            Command = new RelayCommand(() => itemViewModel.IsSortedDescending = true),
                            ShowInRecycleBin = true,
                            ItemType = ItemType.Toggle
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRefresh2".GetLocalized(),
                    Glyph = "\uE72C",
                    ShowInRecycleBin = true,
                    Command = commandsViewModel.RefreshCommand,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.F5,
                        IsEnabled = false,
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutPaste2".GetLocalized(),
                    Glyph = "\uE16D",
                    Command = commandsViewModel.PasteItemsFromClipboardCommand,
                    IsEnabled = currentInstanceViewModel.CanPasteInPage && App.InteractionViewModel.IsPasteEnabled,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.V,
                        Modifiers = Windows.System.VirtualKeyModifiers.Control,
                        IsEnabled = false,
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutOpenInTerminal2".GetLocalized(),
                    Glyph = "\uE756",
                    Command = commandsViewModel.OpenDirectoryInDefaultTerminalCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ItemType.Separator,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutNew2".GetLocalized(),
                    Glyph = "\uE710",
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.N,
                        Modifiers = Windows.System.VirtualKeyModifiers.Control,
                        IsEnabled = false,
                    },
                    Items = GetNewItemItems(commandsViewModel),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutPinDirectoryToSidebar2".GetLocalized(),
                    Glyph = "\uE840",
                    Command = commandsViewModel.SidebarPinItemCommand,
                    ShowItem =!itemViewModel.CurrentFolder.IsPinned
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutUnpinDirectoryFromSidebar2".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = commandsViewModel.SidebarUnpinItemCommand,
                    ShowItem =itemViewModel.CurrentFolder.IsPinned
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutPropertiesFolder2".GetLocalized(),
                    Glyph = "\uE946",
                    Command = commandsViewModel.ShowFolderPropertiesCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutEmptyRecycleBin2".GetLocalized(),
                    Glyph = "\uEF88",
                    Command = commandsViewModel.EmptyRecycleBinCommand,
                    ShowItem =currentInstanceViewModel.IsPageTypeRecycleBin
                },
            };
        }

        public static List<ContextMenuFlyoutItemViewModel> GetBaseItemMenuItems(BaseLayoutCommandsViewModel commandsViewModel, List<ListedItem> selectedItems, SelectedItemsPropertiesViewModel selectedItemsPropertiesViewModel)
        {
            return new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutRestore2".GetLocalized(),
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.RestoreItemCommand,
                    ShowInRecycleBin = true,
                    ShowItem = selectedItems.All(x => x.IsRecycleBinItem)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenItem2".GetLocalized(),
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.OpenItemCommand,
                    ShowItem = selectedItems.Count <= 10
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenItemWith2".GetLocalized(),
                    Glyph = "\uE17D",
                    Command = commandsViewModel.OpenItemWithApplicationPickerCommand,
                    ShowItem = selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.File && !i.IsShortcutItem),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenFileLocation2".GetLocalized(),
                    Glyph = "\uE8DA",
                    Command = commandsViewModel.OpenFileLocationCommand,
                    ShowItem = selectedItems.All(i => i.IsShortcutItem),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewPane2".GetLocalized(),
                    Glyph = "\uF57C",
                    Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
                    ShowItem = App.AppSettings.IsDualPaneEnabled && selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewTab2".GetLocalized(),
                    Glyph = "\uEC6C",
                    Command = commandsViewModel.OpenDirectoryInNewTabCommand,
                    ShowItem = selectedItems.Count < 5 && selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewWindow2".GetLocalized(),
                    Glyph = "\uE737",
                    Command = commandsViewModel.OpenInNewWindowItemCommand,
                    ShowItem = selectedItems.Count < 5 && selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutSetAs2".GetLocalized(),
                    ShowItem = selectedItemsPropertiesViewModel.IsSelectedItemImage,
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutItemContextFlyoutSetAsDesktopBackground2".GetLocalized(),
                            Glyph = "\uE91B",
                            Command = commandsViewModel.SetAsDesktopBackgroundItemCommand,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutItemContextFlyoutSetAsLockscreenBackground2".GetLocalized(),
                            Glyph = "\uF114",
                            GlyphFontFamilyName = "CustomGlyph",
                            Command = commandsViewModel.SetAsLockscreenBackgroundItemCommand,
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRunAsAdmin2".GetLocalized(),
                    Glyph = "\uE7EF",
                    Command = commandsViewModel.RunAsAdminCommand,
                    ShowItem = new string[]{".bat", ".exe", "cmd" }.Contains(selectedItems.FirstOrDefault().FileExtension)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRunAsAnotherUser2".GetLocalized(),
                    Glyph = "\uE7EE",
                    Command = commandsViewModel.RunAsAnotherUserCommand,
                    ShowItem = new string[]{".bat", ".exe", "cmd" }.Contains(selectedItems.FirstOrDefault().FileExtension)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutShare2".GetLocalized(),
                    Glyph = "\uE72D",
                    Command = commandsViewModel.ShareItemCommand,
                    ShowItem = DataTransferManager.IsSupported() && !selectedItems.Any(i => i.IsHiddenItem),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Glyph = "\xE712",
                    ID = "ItemOverflow"
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ItemType.Separator,
                    ShowInRecycleBin = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutCut2".GetLocalized(),
                    Glyph = "\uE8C6",
                    Command = commandsViewModel.CutItemCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutCopy2".GetLocalized(),
                    Glyph = "\uE8C8",
                    Command = commandsViewModel.CopyItemCommand,
                    ShowInRecycleBin = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutCopyLocation2".GetLocalized(),
                    Glyph = "\uE167",
                    Command = commandsViewModel.CopyPathOfSelectedItemCommand,
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutShortcut2".GetLocalized(),
                    Glyph = "\uF10A",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = commandsViewModel.CreateShortcutCommand,
                    ShowItem = !selectedItems.FirstOrDefault().IsShortcutItem,
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutDelete2".GetLocalized(),
                    Glyph = "\uE74D",
                    Command = commandsViewModel.DeleteItemCommand,
                    KeyboardAccelerator = new KeyboardAccelerator() { Key = Windows.System.VirtualKey.Delete },
                    ShowInRecycleBin = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutRename2".GetLocalized(),
                    Glyph = "\uE8AC",
                    Command = commandsViewModel.RenameItemCommand,
                    KeyboardAccelerator = new KeyboardAccelerator() { Key = Windows.System.VirtualKey.F2 },
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutPinToSidebar2".GetLocalized(),
                    Glyph = "\uE840",
                    Command = commandsViewModel.SidebarPinItemCommand,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsPinned)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutUnpinDirectoryFromSidebar2".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = commandsViewModel.SidebarUnpinItemCommand,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsPinned)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "PinItemToStart2".GetLocalized(),
                    Glyph = "\uE840",
                    // TODO: Add command
                    ShowOnShift = true,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsItemPinnedToStart),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "UnpinItemFromStart2".GetLocalized(),
                    Glyph = "\uE77A",
                    // TODO: Add command
                    ShowOnShift = true,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsItemPinnedToStart),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutProperties2".GetLocalized(),
                    Glyph = "\uE946",
                    Command = commandsViewModel.ShowPropertiesCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutQuickLook2".GetLocalized(),
                    BitmapIcon = new BitmapImage(new Uri("ms-appx:///Assets/QuickLook/quicklook_icon_black.png")),
                    Command = commandsViewModel.QuickLookCommand,
                    ShowItem = App.InteractionViewModel.IsQuickLookEnabled
                },
            };
        }

        public static List<ContextMenuFlyoutItemViewModel> GetNewItemItems(BaseLayoutCommandsViewModel commandsViewModel)
        {
            var list = new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutNewFolder2".GetLocalized(),
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
                        CommandParameter = i,
                    });
                }
                else
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
}

