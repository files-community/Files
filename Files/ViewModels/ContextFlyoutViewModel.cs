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
using Windows.ApplicationModel.DataTransfer;
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
            MenuItemsList = ShellContextmenuHelper.SetShellContextmenu(BaseItemMenuItems, shiftPressed, showOpenMenu, Connection, ItemViewModel.WorkingDirectory, SelectedItems);
            Filter(shiftPressed);
        }

        public void LoadBaseContextCommands(bool shiftPressed, bool showOpenMenu)
        {
            MenuItemsList = ShellContextmenuHelper.SetShellContextmenu(BaseLayoutMenuItems, shiftPressed, showOpenMenu, Connection, ItemViewModel.WorkingDirectory, SelectedItems);
            Filter(shiftPressed);
        }

        public ContextFlyoutViewModel()
        {
            InitNewItemItemsAsync();
        }

        private async void InitNewItemItemsAsync()
        {
            CachedNewContextMenuEntries = await RegistryHelper.GetNewContextMenuEntries();
        }

        public List<ContextMenuFlyoutItemViewModel> NewItemItems { 
            get {
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
                    Text = "BaseLayoutItemContextFlyoutRestore2".GetLocalized(),
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.RestoreItemCommand,
                    ShowInRecycleBin = true,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.IsRecycleBinItem))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenItem2".GetLocalized(),
                    Glyph = "\uE8E5",
                    Command = commandsViewModel.OpenItemCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.Count <= 10)
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenItemWith2".GetLocalized(),
                    Glyph = "\uE17D",
                    Command = commandsViewModel.OpenItemWithApplicationPickerCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.File && !i.IsShortcutItem)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenFileLocation2".GetLocalized(),
                    Glyph = "\uE8DA",
                    Command = commandsViewModel.OpenFileLocationCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(i => i.IsShortcutItem)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewPane2".GetLocalized(),
                    Glyph = "\uF57C",
                    Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
                    CheckShowItem = new Func<bool>(() => App.AppSettings.IsDualPaneEnabled && SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewTab2".GetLocalized(),
                    Glyph = "\uEC6C",
                    Command = commandsViewModel.OpenDirectoryInNewTabCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.Count < 5 && SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutOpenInNewWindow2".GetLocalized(),
                    Glyph = "\uE737",
                    Command = commandsViewModel.OpenInNewWindowItemCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.Count < 5 && SelectedItems.All(i => i.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder)),
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutSetAs2".GetLocalized(),
                    CheckShowItem = new Func<bool>(() => SelectedItemsPropertiesViewModel.IsSelectedItemImage),
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
                            Command = commandsViewModel.SetAsLockscreenBackgroundItemCommand,
                        },
                    }
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRunAsAdmin2".GetLocalized(),
                    Glyph = "\uE7EF",
                    Command = CommandsViewModel.RunAsAdminCommand,
                    CheckShowItem = new Func<bool>(() => new string[]{".bat", ".exe", "cmd" }.Contains(SelectedItems.FirstOrDefault().FileExtension))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutRunAsAnotherUser2".GetLocalized(),
                    Glyph = "\uE7EE",
                    Command = CommandsViewModel.RunAsAnotherUserCommand,
                    CheckShowItem = new Func<bool>(() => new string[]{".bat", ".exe", "cmd" }.Contains(SelectedItems.FirstOrDefault().FileExtension))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutItemContextFlyoutShare2".GetLocalized(),
                    Glyph = "\uE72D",
                    Command = commandsViewModel.ShareItemCommand,
                    CheckShowItem = new Func<bool>(() => DataTransferManager.IsSupported() && !SelectedItems.Any(i => i.IsHiddenItem)),
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
                    CheckShowItem = new Func<bool>(() => !SelectedItems.FirstOrDefault().IsShortcutItem),
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
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsPinned))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutUnpinDirectoryFromSidebar2".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = commandsViewModel.SidebarUnpinItemCommand,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsPinned))
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "PinItemToStart2".GetLocalized(),
                    Glyph = "\uE840",
                    // TODO: Add command
                    ShowOnShift = true,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsItemPinnedToStart)),
                    SingleItemOnly = true,
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "UnpinItemFromStart2".GetLocalized(),
                    Glyph = "\uE77A",
                    // TODO: Add command
                    ShowOnShift = true,
                    CheckShowItem = new Func<bool>(() => SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && x.IsItemPinnedToStart)),
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
                    CheckShowItem = new Func<bool>(() => App.InteractionViewModel.IsQuickLookEnabled)
                },
            };
        public List<ContextMenuFlyoutItemViewModel> BaseLayoutMenuItems => new List<ContextMenuFlyoutItemViewModel>()
        {
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
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge,
                    },
                    // Grid view medium
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutGridViewMedium2".GetLocalized(),
                        Glyph = "\uF0E2",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium,
                    },
                    // Grid view small
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutGridViewSmall2".GetLocalized(),
                        Glyph = "\uE80A",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall,
                    },
                    // Tiles view
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutTilesView2".GetLocalized(),
                        Glyph = "\uE15C",
                        ShowInRecycleBin = true,
                        Command =  CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeTiles,
                    },
                    // Details view
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutDetails2".GetLocalized(),
                        Glyph = "\uE179",
                        ShowInRecycleBin = true,
                        Command = CurrentInstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView,
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
                        IsChecked = ItemViewModel.IsSortedByName,
                        ShowInRecycleBin = true,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByName = true),
                        ItemType = ItemType.Toggle,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutSortByOriginalPath2".GetLocalized(),
                        IsChecked = ItemViewModel.IsSortedByOriginalPath,
                        ShowInRecycleBin = true,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByOriginalPath = true),
                        CheckShowItem = new Func<bool>(() => CurrentInstanceViewModel.IsPageTypeRecycleBin),
                        ItemType = ItemType.Toggle,
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutSortByDateDeleted2".GetLocalized(),
                        IsChecked = ItemViewModel.IsSortedByDateDeleted,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByDateDeleted = true),
                        ShowInRecycleBin = true,
                        CheckShowItem = new Func<bool>(() => CurrentInstanceViewModel.IsPageTypeRecycleBin),
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutSortByType2".GetLocalized(),
                        IsChecked = ItemViewModel.IsSortedByType,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByType = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutSortBySize2".GetLocalized(),
                        IsChecked = ItemViewModel.IsSortedBySize,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedBySize = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutSortByDate2".GetLocalized(),
                        IsChecked = ItemViewModel.IsSortedByDate,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedByDate = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutSortByDateCreated2".GetLocalized(),
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
                        Text = "BaseLayoutContextFlyoutSortByAscending2".GetLocalized(),
                        IsChecked = ItemViewModel.IsSortedAscending,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedAscending = true),
                        ShowInRecycleBin = true,
                        ItemType = ItemType.Toggle
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "BaseLayoutContextFlyoutSortByDescending2".GetLocalized(),
                        IsChecked = ItemViewModel.IsSortedDescending,
                        Command = new RelayCommand(() => ItemViewModel.IsSortedDescending = true),
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
                IsEnabled = CurrentInstanceViewModel.CanPasteInPage && App.InteractionViewModel.IsPasteEnabled,
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
                Items = NewItemItems,
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "BaseLayoutContextFlyoutPinDirectoryToSidebar2".GetLocalized(),
                Glyph = "\uE840",
                Command = commandsViewModel.SidebarPinItemCommand,
                CheckShowItem = new Func<bool>(() => !ItemViewModel.CurrentFolder.IsPinned)
            },
            new ContextMenuFlyoutItemViewModel()
            {
                Text = "BaseLayoutContextFlyoutUnpinDirectoryFromSidebar2".GetLocalized(),
                Glyph = "\uE77A",
                Command = commandsViewModel.SidebarUnpinItemCommand,
                CheckShowItem = new Func<bool>(() => ItemViewModel.CurrentFolder.IsPinned)
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
                CheckShowItem = new Func<bool>(() => CurrentInstanceViewModel.IsPageTypeRecycleBin)
            },
        };
    }
}
