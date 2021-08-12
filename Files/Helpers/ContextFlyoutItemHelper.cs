﻿using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Interacts;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    public static class ContextFlyoutItemHelper
    {
        private static List<ShellNewEntry> cachedNewContextMenuEntries;

        public static List<ShellNewEntry> CachedNewContextMenuEntries
        {
            get
            {
                cachedNewContextMenuEntries ??= Task.Run(() => RegistryHelper.GetNewContextMenuEntries()).Result;
                return cachedNewContextMenuEntries;
            }
        }

        private static List<ContextMenuFlyoutItemViewModel> cachedNewItemItems;

        public static List<ContextMenuFlyoutItemViewModel> GetItemContextCommandsWithoutShellItems(CurrentInstanceViewModel currentInstanceViewModel, string workingDir, List<ListedItem> selectedItems, BaseLayoutCommandsViewModel commandsViewModel, bool shiftPressed, bool showOpenMenu, SelectedItemsPropertiesViewModel selectedItemsPropertiesViewModel)
        {
            var menuItemsList = GetBaseItemMenuItems(commandsViewModel: commandsViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel, currentInstanceViewModel: currentInstanceViewModel);
            menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, removeOverflowMenu: false);
            return menuItemsList;
        }

        public static async Task<List<ContextMenuFlyoutItemViewModel>> GetItemContextShellCommandsAsync(NamedPipeAsAppServiceConnection connection, CurrentInstanceViewModel currentInstanceViewModel, string workingDir, List<ListedItem> selectedItems, bool shiftPressed, bool showOpenMenu)
        {
            return await ShellContextmenuHelper.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, connection: connection, workingDirectory: workingDir, selectedItems: selectedItems);
        }

        public static List<ContextMenuFlyoutItemViewModel> GetBaseContextCommandsWithoutShellItems(NamedPipeAsAppServiceConnection connection, CurrentInstanceViewModel currentInstanceViewModel, ItemViewModel itemViewModel, BaseLayoutCommandsViewModel commandsViewModel, bool shiftPressed, bool showOpenMenu)
        {
            var menuItemsList = GetBaseLayoutMenuItems(currentInstanceViewModel, itemViewModel, commandsViewModel);
            menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: new List<ListedItem>(), removeOverflowMenu: false);
            return menuItemsList;
        }

        public static async Task<List<ContextMenuFlyoutItemViewModel>> GetBaseContextShellCommandsAsync(NamedPipeAsAppServiceConnection connection, CurrentInstanceViewModel currentInstanceViewModel, string workingDir, bool shiftPressed, bool showOpenMenu)
        {
            return await ShellContextmenuHelper.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, connection: connection, workingDirectory: workingDir, selectedItems: new List<ListedItem>());
        }

        public static List<ContextMenuFlyoutItemViewModel> Filter(List<ContextMenuFlyoutItemViewModel> items, List<ListedItem> selectedItems, bool shiftPressed, CurrentInstanceViewModel currentInstanceViewModel, bool removeOverflowMenu = true)
        {
            items = items.Where(x => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, shiftPressed: shiftPressed)).ToList();
            items.ForEach(x => x.Items = x.Items?.Where(y => Check(item: y, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, shiftPressed: shiftPressed)).ToList());

            var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
            if (overflow != null)
            {
                if (!shiftPressed && App.AppSettings.MoveOverflowMenuItemsToSubMenu) // items with ShowOnShift to overflow menu
                {
                    var overflowItems = items.Where(x => x.ShowOnShift).ToList();

                    // Adds a separator between items already there and the new ones
                    if (overflow.Items.Count != 0 && overflow.Items.Last().ItemType != ItemType.Separator && overflowItems.Count > 0)
                    {
                        overflow.Items.Add(new ContextMenuFlyoutItemViewModel { ItemType = ItemType.Separator });
                    }

                    items = items.Except(overflowItems).ToList();
                    overflowItems.ForEach(x => overflow.Items.Add(x));
                }

                // remove the overflow if it has no child items
                if (overflow.Items.Count == 0 && removeOverflowMenu)
                {
                    items.Remove(overflow);
                }
            }

            return items;
        }

        private static bool Check(ContextMenuFlyoutItemViewModel item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, bool shiftPressed)
        {
            return (item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin) // Hide non-recycle bin items
                && (item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults) // Hide non-search items
                && (!item.SingleItemOnly || selectedItems.Count == 1)
                && item.ShowItem;
        }

        public static List<ContextMenuFlyoutItemViewModel> GetBaseLayoutMenuItems(CurrentInstanceViewModel currentInstanceViewModel, ItemViewModel itemViewModel, BaseLayoutCommandsViewModel commandsViewModel)
        {
            return new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutLayoutMode/Text".GetLocalized(),
                    Glyph = "\uE152",
                    ShowInRecycleBin = true,
                    ShowInSearchPage = true,
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        // Details view
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutDetails/Text".GetLocalized(),
                            Glyph = "\uE179",
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView,
                            CommandParameter = true,
                            KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutDetails/KeyboardAcceleratorTextOverride".GetLocalized(),
                            KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number1, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
                        },
                        // Tiles view
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutTilesView/Text".GetLocalized(),
                            Glyph = "\uE15C",
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeTiles,
                            CommandParameter = true,
                            KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutTilesView/KeyboardAcceleratorTextOverride".GetLocalized(),
                            KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number2, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
                        },
                        // Grid view small
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutGridViewSmall/Text".GetLocalized(),
                            Glyph = "\uE80A",
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall,
                            CommandParameter = true,
                            KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutGridViewSmall/KeyboardAcceleratorTextOverride".GetLocalized(),
                            KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number3, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
                        },
                        // Grid view medium
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutGridViewMedium/Text".GetLocalized(),
                            Glyph = "\uF0E2",
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium,
                            CommandParameter = true,
                            KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutGridViewMedium/KeyboardAcceleratorTextOverride".GetLocalized(),
                            KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number4, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
                        },
                        // Grid view large
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutGridViewLarge/Text".GetLocalized(),
                            Glyph = "\uE739",
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge,
                            CommandParameter = true,
                            KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutGridViewLarge/KeyboardAcceleratorTextOverride".GetLocalized(),
                            KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number5, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
                        },
                        // Column view
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutColumn/Text".GetLocalized(),
                            Glyph = "\uF115",
                            GlyphFontFamilyName = "CustomGlyph",
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ToggleLayoutModeColumnView,
                            CommandParameter = true,
                            KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutColumn/KeyboardAcceleratorTextOverride".GetLocalized(),
                            KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number6, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutSortBy/Text".GetLocalized(),
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0029",
                        OverlayLayerGlyph = "\u002A",
                    },
                    ShowInRecycleBin = true,
                    ShowInSearchPage = true,
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByName/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByName,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByName = true),
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDate/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByDate,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByDate = true),
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDateCreated/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByDateCreated,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByDateCreated = true),
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByType/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByType,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByType = true),
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortBySize/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedBySize,
                            Command = new RelayCommand(() => itemViewModel.IsSortedBySize = true),
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortBySyncStatus/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedBySyncStatus,
                            Command = new RelayCommand(() => itemViewModel.IsSortedBySyncStatus = true),
                            ShowItem = currentInstanceViewModel.IsPageTypeCloudDrive,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByFileTag/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByFileTag,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByFileTag = true),
                            ShowItem = App.AppSettings.AreFileTagsEnabled,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByOriginalPath/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByOriginalPath,
                            ShowInRecycleBin = true,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByOriginalPath = true),
                            ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDateDeleted/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedByDateDeleted,
                            Command = new RelayCommand(() => itemViewModel.IsSortedByDateDeleted = true),
                            ShowInRecycleBin = true,
                            ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            ItemType = ItemType.Separator,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByAscending/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedAscending,
                            Command = new RelayCommand(() => itemViewModel.IsSortedAscending = true),
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            ItemType = ItemType.Toggle
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDescending/Text".GetLocalized(),
                            IsChecked = itemViewModel.IsSortedDescending,
                            Command = new RelayCommand(() => itemViewModel.IsSortedDescending = true),
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            ItemType = ItemType.Toggle
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "NavToolbarGroupByRadioButtons/Header".GetLocalized(),
                    Glyph = "\uF168",
                    ShowInRecycleBin = true,
                    ShowInSearchPage = true,
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "NavToolbarGroupByOptionNone/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.None,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.None,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByName/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.Name,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.Name,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDate/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateModified,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.DateModified,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDateCreated/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateCreated,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.DateCreated,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByType/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FileType,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.FileType,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortBySize/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.Size,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.Size,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortBySyncStatus/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.SyncStatus,
                            ShowItem = currentInstanceViewModel.IsPageTypeCloudDrive,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.SyncStatus,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByFileTag/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FileTag,
                            ShowItem = App.AppSettings.AreFileTagsEnabled,
                            ShowInRecycleBin = true,
                            ShowInSearchPage = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.FileTag,
                            ItemType = ItemType.Toggle,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "NavToolbarArrangementOptionOriginalFolder/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.OriginalFolder,
                            ShowInRecycleBin = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.OriginalFolder,
                            ItemType = ItemType.Toggle,
                            ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutContextFlyoutSortByDateDeleted/Text".GetLocalized(),
                            IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateDeleted,
                            ShowInRecycleBin = true,
                            Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
                            CommandParameter = GroupOption.DateDeleted,
                            ItemType = ItemType.Toggle,
                            ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRefresh/Text".GetLocalized(),
                    Glyph = "\uE72C",
                    ShowInRecycleBin = true,
                    ShowInSearchPage = true,
                    Command = commandsViewModel.RefreshCommand,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.F5,
                        IsEnabled = false,
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutPaste/Text".GetLocalized(),
                    //Glyph = "\uE16D",
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0023",
                        OverlayLayerGlyph = "\u0024",
                    },
                    Command = commandsViewModel.PasteItemsFromClipboardCommand,
                    IsEnabled = currentInstanceViewModel.CanPasteInPage && App.MainViewModel.IsPasteEnabled,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.V,
                        Modifiers = Windows.System.VirtualKeyModifiers.Control,
                        IsEnabled = false,
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutOpenInTerminal/Text".GetLocalized(),
                    Glyph = "\uE756",
                    Command = commandsViewModel.OpenDirectoryInDefaultTerminalCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ItemType.Separator,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutNew/Label".GetLocalized(),
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0037",
                        OverlayLayerGlyph = "\u0038"
                    },
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
                    Text = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalized(),
                    Glyph = "\uE840",
                    Command = commandsViewModel.PinDirectoryToFavoritesCommand,
                    ShowItem =!itemViewModel.CurrentFolder.IsPinned & App.AppSettings.ShowFavoritesSection
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutUnpinFromFavorites/Text".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = commandsViewModel.UnpinDirectoryFromFavoritesCommand,
                    ShowItem =itemViewModel.CurrentFolder.IsPinned & App.AppSettings.ShowFavoritesSection
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "PinItemToStart/Text".GetLocalized(),
                    Glyph = "\uE840",
                    Command = commandsViewModel.PinItemToStartCommand,
                    ShowOnShift = true,
                    ShowItem = !itemViewModel.CurrentFolder.IsItemPinnedToStart,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "UnpinItemFromStart/Text".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = commandsViewModel.UnpinItemFromStartCommand,
                    ShowOnShift = true,
                    ShowItem = itemViewModel.CurrentFolder.IsItemPinnedToStart,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutPropertiesFolder/Text".GetLocalized(),
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0031",
                        OverlayLayerGlyph = "\u0032"
                    },
                    Command = commandsViewModel.ShowFolderPropertiesCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutEmptyRecycleBin/Text".GetLocalized(),
                    Glyph = "\uEF88",
                    GlyphFontFamilyName = "RecycleBinIcons",
                    Command = commandsViewModel.EmptyRecycleBinCommand,
                    ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
                    ShowInRecycleBin = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ItemType.Separator,
                    Tag = "OverflowSeparator",
                    IsHidden = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Glyph = "\xE712",
                    Items = new List<ContextMenuFlyoutItemViewModel>(),
                    ID = "ItemOverflow",
                    Tag = "ItemOverflow",
                    IsHidden = true,
                },
            };
        }

        public static List<ContextMenuFlyoutItemViewModel> GetBaseItemMenuItems(BaseLayoutCommandsViewModel commandsViewModel, List<ListedItem> selectedItems, SelectedItemsPropertiesViewModel selectedItemsPropertiesViewModel, CurrentInstanceViewModel currentInstanceViewModel)
        {
            return new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutRestore/Text".GetLocalized(),
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.RestoreItemCommand,
                    ShowInRecycleBin = true,
                    ShowItem = selectedItems.All(x => x.IsRecycleBinItem)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenItem/Text".GetLocalized(),
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.OpenItemCommand,
                    ShowInSearchPage = true,
                    ShowItem = selectedItems.Count <= 10,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalized(),
                    Glyph = "\uE17D",
                    Command = commandsViewModel.OpenItemWithApplicationPickerCommand,
                    Tag = "OpenWith",
                    CollapseLabel = true,
                    ShowInSearchPage = true,
                    ShowItem = selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.File && !i.IsShortcutItem),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalized(),
                    Glyph = "\uE17D",
                    Tag = "OpenWithOverflow",
                    IsHidden = true,
                    CollapseLabel = true,
                    Items = new List<ContextMenuFlyoutItemViewModel>(),
                    ShowItem = selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.File && !i.IsShortcutItem),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenFileLocation/Text".GetLocalized(),
                    Glyph = "\uE8DA",
                    Command = commandsViewModel.OpenFileLocationCommand,
                    ShowItem = selectedItems.All(i => i.IsShortcutItem),
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewPane/Text".GetLocalized(),
                    Glyph = "\uF117",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
                    ShowItem = App.AppSettings.IsDualPaneEnabled && selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder),
                    SingleItemOnly = true,
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewTab/Text".GetLocalized(),
                    Glyph = "\uF113",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = commandsViewModel.OpenDirectoryInNewTabCommand,
                    ShowItem = selectedItems.Count < 5 && selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder),
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewWindow/Text".GetLocalized(),
                    Glyph = "\uE737",
                    Command = commandsViewModel.OpenInNewWindowItemCommand,
                    ShowItem = selectedItems.Count < 5 && selectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder),
                    ShowInSearchPage = true,
                    ShowOnShift = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalized(),
                    ShowItem = selectedItemsPropertiesViewModel.IsSelectedItemImage,
                    ShowInSearchPage = true,
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutItemContextFlyoutSetAsDesktopBackground/Text".GetLocalized(),
                            Glyph = "\uE91B",
                            Command = commandsViewModel.SetAsDesktopBackgroundItemCommand,
                            ShowInSearchPage = true,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutItemContextFlyoutSetAsLockscreenBackground/Text".GetLocalized(),
                            Glyph = "\uF114",
                            GlyphFontFamilyName = "CustomGlyph",
                            Command = commandsViewModel.SetAsLockscreenBackgroundItemCommand,
                            ShowInSearchPage = true,
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRunAsAdmin/Text".GetLocalized(),
                    Glyph = "\uE7EF",
                    Command = commandsViewModel.RunAsAdminCommand,
                    ShowInSearchPage = true,
                    ShowItem = new string[]{".bat", ".exe", "cmd" }.Contains(selectedItems.FirstOrDefault().FileExtension, StringComparer.OrdinalIgnoreCase)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalized(),
                    Glyph = "\uE7EE",
                    Command = commandsViewModel.RunAsAnotherUserCommand,
                    ShowInSearchPage = true,
                    ShowItem = new string[]{".bat", ".exe", "cmd" }.Contains(selectedItems.FirstOrDefault().FileExtension, StringComparer.OrdinalIgnoreCase)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ItemType.Separator,
                    ShowInRecycleBin = true,
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutCut/Text".GetLocalized(),
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u003D",
                        OverlayLayerGlyph = "\u003E",
                    },
                    Command = commandsViewModel.CutItemCommand,
                    IsPrimary = true,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.X,
                        Modifiers = Windows.System.VirtualKeyModifiers.Control,
                        IsEnabled = false,
                    },
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutCopy/Text".GetLocalized(),
                    //Glyph = "\uE8C8",
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0021",
                        OverlayLayerGlyph = "\u0022",
                    },
                    Command = commandsViewModel.CopyItemCommand,
                    ShowInRecycleBin = true,
                    ShowInSearchPage = true,
                    IsPrimary = true,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.C,
                        Modifiers = Windows.System.VirtualKeyModifiers.Control,
                        IsEnabled = false,
                    },
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutCopyLocation/Text".GetLocalized(),
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u002F",
                        OverlayLayerGlyph = "\u0030"
                    },
                    Command = commandsViewModel.CopyPathOfSelectedItemCommand,
                    SingleItemOnly = true,
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutPaste/Text".GetLocalized(),
                    //Glyph = "\uE16D",
                    IsPrimary = true,
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0023",
                        OverlayLayerGlyph = "\u0024",
                    },
                    Command = commandsViewModel.PasteItemsFromClipboardCommand,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder),
                    SingleItemOnly = true,
                    ShowInSearchPage = true,
                    IsEnabled = App.MainViewModel.IsPasteEnabled,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.V,
                        Modifiers = Windows.System.VirtualKeyModifiers.Control,
                        IsEnabled = false,
                    },
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutCreateFolderWithSelection/Text".GetLocalized(),
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0033",
                        OverlayLayerGlyph = "\u0034"
                    },
                    Command = commandsViewModel.CreateFolderWithSelection,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutShortcut/Text".GetLocalized(),
                    Glyph = "\uF10A",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = commandsViewModel.CreateShortcutCommand,
                    ShowItem = !selectedItems.FirstOrDefault().IsShortcutItem,
                    SingleItemOnly = true,
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutRename/Text".GetLocalized(),
                    //Glyph = "\uE8AC",
                    IsPrimary = true,
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0027",
                        OverlayLayerGlyph = "\u0028",
                    },
                    Command = commandsViewModel.RenameItemCommand,
                    SingleItemOnly = true,
                    ShowInSearchPage = true,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.F2,
                        IsEnabled = false,
                    },
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutShare/Text".GetLocalized(),
                    //Glyph = "\uE72D",
                    IsPrimary = true,
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0025",
                        OverlayLayerGlyph = "\u0026",
                    },
                    Command = commandsViewModel.ShareItemCommand,
                    ShowItem = DataTransferManager.IsSupported() && !selectedItems.Any(i => i.IsHiddenItem),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutDelete/Text".GetLocalized(),
                    //Glyph = "\uE74D",
                    IsPrimary = true,
                    ShowInSearchPage = true,
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0035",
                        OverlayLayerGlyph = "\u0036"
                    },
                    Command = commandsViewModel.DeleteItemCommand,
                    ShowInRecycleBin = true,
                    KeyboardAccelerator = new KeyboardAccelerator
                    {
                        Key = Windows.System.VirtualKey.Delete,
                        IsEnabled = false,
                    },
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutProperties/Text".GetLocalized(),
                    //Glyph = "\uE946",
                    IsPrimary = true,
                    ColoredIcon = new ColoredIconModel()
                    {
                        BaseLayerGlyph = "\u0031",
                        OverlayLayerGlyph = "\u0032"
                    },
                    Command = commandsViewModel.ShowPropertiesCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutExtractionOptions".GetLocalized(),
                    Glyph = "\xF11A",
                    ShowItem = selectedItems.Count == 1 && selectedItems.First().PrimaryItemAttribute == StorageItemTypes.File && new [] { ".zip", ".msix", ".msixbundle" }.Contains(selectedItems.First().FileExtension, StringComparer.OrdinalIgnoreCase),
                    ShowInSearchPage = true,
                    GlyphFontFamilyName = "CustomGlyph",
                    Items = new List<ContextMenuFlyoutItemViewModel>()
                    {
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutItemContextFlyoutExtractFilesOption".GetLocalized(),
                            Command = commandsViewModel.DecompressArchiveCommand,
                            Glyph = "\xF11A",
                            GlyphFontFamilyName = "CustomGlyph",
                            ShowInSearchPage = true,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = "BaseLayoutItemContextFlyoutExtractHereOption".GetLocalized(),
                            Command = commandsViewModel.DecompressArchiveHereCommand,
                            Glyph = "\xF11A",
                            GlyphFontFamilyName = "CustomGlyph",
                            ShowInSearchPage = true,
                        },
                        new ContextMenuFlyoutItemViewModel()
                        {
                            Text = string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalized(), Path.GetFileNameWithoutExtension(selectedItems.First().ItemName)),
                            Command = commandsViewModel.DecompressArchiveToChildFolderCommand,
                            Glyph = "\xF11A",
                            GlyphFontFamilyName = "CustomGlyph",
                            ShowInSearchPage = true,
                        }
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenParentFolder/Text".GetLocalized(),
                    Glyph = "\uE197",
                    Command = commandsViewModel.OpenParentFolderCommand,
                    ShowItem = currentInstanceViewModel.IsPageTypeSearchResults,
                    SingleItemOnly = true,
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalized(),
                    Glyph = "\uE840",
                    Command = commandsViewModel.SidebarPinItemCommand,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsPinned) & App.AppSettings.ShowFavoritesSection,
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutUnpinFromFavorites/Text".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = commandsViewModel.SidebarUnpinItemCommand,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsPinned) & App.AppSettings.ShowFavoritesSection,
                    ShowInSearchPage = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "PinItemToStart/Text".GetLocalized(),
                    Glyph = "\uE840",
                    Command = commandsViewModel.PinItemToStartCommand,
                    ShowOnShift = true,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsItemPinnedToStart),
                    ShowInSearchPage = true,
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "UnpinItemFromStart/Text".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = commandsViewModel.UnpinItemFromStartCommand,
                    ShowOnShift = true,
                    ShowItem = selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsItemPinnedToStart),
                    ShowInSearchPage = true,
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ItemType.Separator,
                    Tag = "OverflowSeparator",
                    IsHidden = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Glyph = "\xE712",
                    Items = new List<ContextMenuFlyoutItemViewModel>(),
                    ID = "ItemOverflow",
                    Tag = "ItemOverflow",
                    IsHidden = true,
                },
            };
        }
        public static List<ContextMenuFlyoutItemViewModel> GetNewItemItems(BaseLayoutCommandsViewModel commandsViewModel)
        {
            var list = new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutNewFolder/Text".GetLocalized(),
                    Glyph = "\uE8B7",
                    Command = commandsViewModel.CreateNewFolderCommand,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutNewFile/Text".GetLocalized(),
                    Glyph = "\uE7C3",
                    Command = commandsViewModel.CreateNewFileCommand,
                    CommandParameter = null,
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
                    // loading the bitmaps takes a while, so this caches them
                    var bitmap = cachedNewItemItems?.Where(x => x.Text == i.Name).FirstOrDefault()?.BitmapIcon;
                    if (bitmap == null)
                    {
                        bitmap = new BitmapImage();
                        bitmap.SetSourceAsync(i.Icon).AsTask().Wait(50);
                    }
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
                        CommandParameter = i,
                    });
                }
            });

            cachedNewItemItems = list;
            return list;
        }


    }
}
