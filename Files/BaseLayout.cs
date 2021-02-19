using Files.Common;
using Files.DataModels;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Files.Helpers.PathNormalization;

namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public abstract class BaseLayout : Page, INotifyPropertyChanged
    {
        private AppServiceConnection Connection => ParentShellPageInstance?.ServiceConnection;

        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

        public SettingsViewModel AppSettings => App.AppSettings;

        public FolderSettingsViewModel FolderSettings => ParentShellPageInstance.InstanceViewModel.FolderSettings;

        public CurrentInstanceViewModel InstanceViewModel => ParentShellPageInstance.InstanceViewModel;

        public InteractionViewModel InteractionViewModel => App.InteractionViewModel;

        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

        public bool IsQuickLookEnabled { get; set; } = false;

        public MenuFlyout BaseLayoutContextFlyout { get; set; }

        public MenuFlyout BaseLayoutItemContextFlyout { get; set; }

        public IShellPage ParentShellPageInstance { get; private set; } = null;

        public bool IsRenamingItem { get; set; } = false;

        private NavigationArguments navigationArguments;

        private bool isItemSelected = false;

        public bool IsItemSelected
        {
            get
            {
                return isItemSelected;
            }
            internal set
            {
                if (value != isItemSelected)
                {
                    isItemSelected = value;
                    NotifyPropertyChanged(nameof(IsItemSelected));
                }
            }
        }

        private List<ListedItem> selectedItems = new List<ListedItem>();

        public List<ListedItem> SelectedItems
        {
            get
            {
                return selectedItems;
            }
            internal set
            {
                if (value != selectedItems)
                {
                    selectedItems = value;
                    if (selectedItems.Count == 0 || selectedItems[0] == null)
                    {
                        IsItemSelected = false;
                        SelectedItem = null;
                        SelectedItemsPropertiesViewModel.IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                        SelectedItem = selectedItems.First();
                        SelectedItemsPropertiesViewModel.IsItemSelected = true;

                        if (SelectedItems.Count >= 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCount = SelectedItems.Count;
                        }

                        if (SelectedItems.Count == 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{SelectedItems.Count} {"ItemSelected/Text".GetLocalized()}";
                            SelectedItemsPropertiesViewModel.ItemSize = SelectedItem.FileSize;
                        }
                        else
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{SelectedItems.Count} {"ItemsSelected/Text".GetLocalized()}";

                            if (SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File))
                            {
                                long size = 0;
                                foreach (var item in SelectedItems)
                                {
                                    size += item.FileSizeBytes;
                                }
                                SelectedItemsPropertiesViewModel.ItemSize = ByteSizeLib.ByteSize.FromBytes(size).ToBinaryString().ConvertSizeAbbreviation();
                            }
                            else
                            {
                                SelectedItemsPropertiesViewModel.ItemSize = string.Empty;
                            }
                        }
                    }
                    NotifyPropertyChanged(nameof(SelectedItems));
                    SetDragModeForItems();
                }
            }
        }

        public ListedItem SelectedItem { get; private set; }

        private List<ShellNewEntry> cachedNewContextMenuEntries { get; set; }

        public BaseLayout()
        {
            SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel(this);
            DirectoryPropertiesViewModel = new DirectoryPropertiesViewModel();

            // QuickLook Integration
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isQuickLookIntegrationEnabled = localSettings.Values["quicklook_enabled"];

            if (isQuickLookIntegrationEnabled != null && isQuickLookIntegrationEnabled.Equals(true))
            {
                IsQuickLookEnabled = true;
            }
        }

        public abstract void FocusFileList();

        public abstract void SelectAllItems();

        public virtual void InvertSelection()
        {
            List<ListedItem> newSelectedItems = GetAllItems()
                .Cast<ListedItem>()
                .Except(SelectedItems)
                .ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public abstract void ClearSelection();

        public abstract void SetDragModeForItems();

        public abstract void ScrollIntoView(ListedItem item);

        protected abstract void AddSelectedItem(ListedItem item);

        protected abstract IEnumerable GetAllItems();

        public virtual void SetSelectedItemOnUi(ListedItem selectedItem)
        {
            ClearSelection();
            AddSelectedItem(selectedItem);
        }

        public virtual void SetSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            ClearSelection();
            AddSelectedItemsOnUi(selectedItems);
        }

        public virtual void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            foreach (ListedItem selectedItem in selectedItems)
            {
                AddSelectedItem(selectedItem);
            }
        }

        private void ClearShellContextMenus(MenuFlyout menuFlyout)
        {
            var contextMenuItems = menuFlyout.Items.Where(c => c.Tag != null && ParseContextMenuTag(c.Tag).menuHandle != null).ToList();
            for (int i = 0; i < contextMenuItems.Count; i++)
            {
                menuFlyout.Items.RemoveAt(menuFlyout.Items.IndexOf(contextMenuItems[i]));
            }

            if (menuFlyout.Items[0] is MenuFlyoutSeparator flyoutSeperator)
            {
                menuFlyout.Items.RemoveAt(menuFlyout.Items.IndexOf(flyoutSeperator));
            }
        }

        public virtual void SetShellContextmenu(MenuFlyout menuFlyout, bool shiftPressed, bool showOpenMenu)
        {
            ClearShellContextMenus(menuFlyout);
            var currentBaseLayoutItemCount = menuFlyout.Items.Count;
            var maxItems = !AppSettings.MoveOverflowMenuItemsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 4;
            if (Connection != null)
            {
                var response = Task.Run(() => Connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "LoadContextMenu" },
                    { "FilePath", IsItemSelected ?
                        string.Join('|', selectedItems.Select(x => x.ItemPath)) :
                        ParentShellPageInstance.FilesystemViewModel.CurrentFolder.ItemPath},
                    { "ExtendedMenu", shiftPressed },
                    { "ShowOpenMenu", showOpenMenu }
                }).AsTask()).Result;
                if (response.Status == AppServiceResponseStatus.Success
                    && response.Message.ContainsKey("Handle"))
                {
                    var contextMenu = JsonConvert.DeserializeObject<Win32ContextMenu>((string)response.Message["ContextMenu"]);
                    if (contextMenu != null)
                    {
                        LoadMenuFlyoutItem(menuFlyout.Items, contextMenu.Items, (string)response.Message["Handle"], true, maxItems);
                    }
                }
            }
            var totalFlyoutItems = menuFlyout.Items.Count - currentBaseLayoutItemCount;
            if (totalFlyoutItems > 0 && !(menuFlyout.Items[totalFlyoutItems] is MenuFlyoutSeparator))
            {
                menuFlyout.Items.Insert(totalFlyoutItems, new MenuFlyoutSeparator());
            }
        }

        public abstract void FocusSelectedItems();

        public abstract void StartRenameItem();

        public virtual void ResetItemOpacity()
        {
            IEnumerable items = GetAllItems();
            if (items == null)
            {
                return;
            }

            foreach (ListedItem listedItem in items)
            {
                if (listedItem.IsHiddenItem)
                {
                    listedItem.Opacity = 0.4;
                }
                else
                {
                    listedItem.Opacity = 1;
                }
            }
        }

        public virtual void SetItemOpacity(ListedItem item)
        {
            item.Opacity = 0.4;
        }

        protected abstract ListedItem GetItemFromElement(object element);

        private void FolderSettings_LayoutModeChangeRequested(object sender, EventArgs e)
        {
            if (ParentShellPageInstance.ContentPage != null)
            {
                var layoutType = FolderSettings.GetLayoutType(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory);

                if (layoutType != ParentShellPageInstance.CurrentPageType)
                {
                    FolderSettings.IsLayoutModeChanging = true;
                    ParentShellPageInstance.ContentFrame.Navigate(layoutType, new NavigationArguments()
                    {
                        NavPathParam = navigationArguments.NavPathParam,
                        IsSearchResultPage = navigationArguments.IsSearchResultPage,
                        SearchPathParam = navigationArguments.SearchPathParam,
                        SearchResults = navigationArguments.SearchResults,
                        IsLayoutSwitch = true,
                        AssociatedTabInstance = ParentShellPageInstance
                    }, null);

                    // Remove old layout from back stack
                    ParentShellPageInstance.ContentFrame.BackStack.RemoveAt(ParentShellPageInstance.ContentFrame.BackStack.Count - 1);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            // Add item jumping handler
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
            navigationArguments = (NavigationArguments)eventArgs.Parameter;
            ParentShellPageInstance = navigationArguments.AssociatedTabInstance;
            IsItemSelected = false;
            FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
            ParentShellPageInstance.FilesystemViewModel.IsFolderEmptyTextDisplayed = false;
            FolderSettings.SetLayoutInformation();

            if (!navigationArguments.IsSearchResultPage)
            {
                ParentShellPageInstance.NavigationToolbar.CanRefresh = true;
                string previousDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
                await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

                // pathRoot will be empty on recycle bin path
                var workingDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
                string pathRoot = GetPathRoot(workingDir);
                if (string.IsNullOrEmpty(pathRoot) || NormalizePath(workingDir) == NormalizePath(pathRoot)
                    || workingDir.StartsWith(AppSettings.RecycleBinPath)) // Can't go up from recycle bin
                {
                    ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = false;
                }
                else
                {
                    ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = true;
                }

                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(App.AppSettings.RecycleBinPath);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\");
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
                ParentShellPageInstance.NavigationToolbar.PathControlDisplayText = navigationArguments.NavPathParam;
                if (!navigationArguments.IsLayoutSwitch)
                {
                    ParentShellPageInstance.FilesystemViewModel.RefreshItems(previousDir);
                }
                else
                {
                    ParentShellPageInstance.NavigationToolbar.CanGoForward = false;
                }
            }
            else
            {
                ParentShellPageInstance.NavigationToolbar.CanRefresh = false;
                ParentShellPageInstance.NavigationToolbar.CanGoForward = false;
                ParentShellPageInstance.NavigationToolbar.CanGoBack = true;  // Impose no artificial restrictions on back navigation. Even in a search results page.
                ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;
                if (!navigationArguments.IsLayoutSwitch)
                {
                    await ParentShellPageInstance.FilesystemViewModel.AddSearchResultsToCollection(navigationArguments.SearchResults, navigationArguments.SearchPathParam);
                }
            }

            ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page

            FolderSettings.IsLayoutModeChanging = false;

            cachedNewContextMenuEntries = await RegistryHelper.GetNewContextMenuEntries();

            FocusFileList(); // Set focus on layout specific file list control
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;

            var parameter = e.Parameter as NavigationArguments;
            if (!parameter.IsLayoutSwitch)
            {
                ParentShellPageInstance.FilesystemViewModel.CancelLoadAndClearFiles();
            }
        }

        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            if (FindName(nameToUnload) is MenuFlyoutItemBase menuItem) // Prevent crash if the MenuFlyoutItem is missing
            {
                menuItem.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadMenuFlyoutItemByName(string nameToUnload)
        {
            if (FindName(nameToUnload) is MenuFlyoutItemBase menuItem) // Prevent crash if the MenuFlyoutItem is missing
            {
                menuItem.Visibility = Visibility.Visible;
            }
        }

        private void LoadMenuFlyoutItem(IList<MenuFlyoutItemBase> MenuItemsList,
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
                var menuLayoutSubItem = new MenuFlyoutSubItem()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Tag = ((Win32ContextMenuItem)null, menuHandle),
                    Icon = new FontIcon()
                    {
                        FontFamily = App.Current.Resources["FluentUIGlyphs"] as Windows.UI.Xaml.Media.FontFamily,
                        Glyph = "\xEAD0"
                    }
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
                        using (var ms = new MemoryStream(bitmapData))
                        {
#pragma warning disable CS4014
                            image.SetSourceAsync(ms.AsRandomAccessStream());
#pragma warning restore CS4014
                        }
                    }
                }

                if (menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR)
                {
                    var menuLayoutItem = new MenuFlyoutSeparator()
                    {
                        Tag = (menuFlyoutItem, menuHandle)
                    };
                    MenuItemsList.Insert(0, menuLayoutItem);
                }
                else if (menuFlyoutItem.SubItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any()
                    && !string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutSubItem = new MenuFlyoutSubItem()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                    };
                    LoadMenuFlyoutItem(menuLayoutSubItem.Items, menuFlyoutItem.SubItems, menuHandle, false);
                    MenuItemsList.Insert(0, menuLayoutSubItem);
                }
                else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutItem = new MenuFlyoutItemWithImage()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                        BitmapIcon = image
                    };
                    menuLayoutItem.Click += MenuLayoutItem_Click;
                    MenuItemsList.Insert(0, menuLayoutItem);
                }
            }
        }

        private (Win32ContextMenuItem menuItem, string menuHandle) ParseContextMenuTag(object tag)
        {
            if (tag is ValueTuple<Win32ContextMenuItem, string>)
            {
                (Win32ContextMenuItem menuItem, string menuHandle) = (ValueTuple<Win32ContextMenuItem, string>)tag;
                return (menuItem, menuHandle);
            }

            return (null, null);
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

        public async void RightClickItemContextMenu_Closing(object sender, object e)
        {
            var shellContextMenuTag = (sender as MenuFlyout).Items.Where(x => x.Tag != null)
                .Select(x => ParseContextMenuTag(x.Tag)).FirstOrDefault(x => x.menuItem != null);
            if (shellContextMenuTag.menuItem != null && Connection != null)
            {
                await Connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "ExecAndCloseContextMenu" },
                    { "Handle", shellContextMenuTag.menuHandle }
                });
            }
        }

        public void RightClickContextMenu_Opening(object sender, object e)
        {
            ClearSelection();
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            SetShellContextmenu(BaseLayoutContextFlyout, shiftPressed, false);
            var newItemMenu = (MenuFlyoutSubItem)BaseLayoutContextFlyout.Items.SingleOrDefault(x => x.Name == "NewEmptySpace");
            if (newItemMenu == null || cachedNewContextMenuEntries == null)
            {
                return;
            }
            if (!newItemMenu.Items.Any(x => (x.Tag as string) == "CreateNewFile"))
            {
                var separatorIndex = newItemMenu.Items.IndexOf(newItemMenu.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));
                foreach (var newEntry in Enumerable.Reverse(cachedNewContextMenuEntries))
                {
                    MenuFlyoutItem menuLayoutItem;
                    if (newEntry.Icon != null)
                    {
                        var image = new BitmapImage();
#pragma warning disable CS4014
                        image.SetSourceAsync(newEntry.Icon);
#pragma warning restore CS4014
                        menuLayoutItem = new MenuFlyoutItemWithImage()
                        {
                            Text = newEntry.Name,
                            BitmapIcon = image,
                            Tag = "CreateNewFile"
                        };
                    }
                    else
                    {
                        menuLayoutItem = new MenuFlyoutItem()
                        {
                            Text = newEntry.Name,
                            Icon = new FontIcon()
                            {
                                FontFamily = App.Current.Resources["FluentUIGlyphs"] as Windows.UI.Xaml.Media.FontFamily,
                                Glyph = "\xea00"
                            },
                            Tag = "CreateNewFile"
                        };
                    }
                    menuLayoutItem.Command = ParentShellPageInstance.InteractionOperations.CreateNewFile;
                    menuLayoutItem.CommandParameter = newEntry;
                    newItemMenu.Items.Insert(separatorIndex + 1, menuLayoutItem);
                }
            }
            var isPinned = App.SidebarPinnedController.Model.Items.Contains(
                ParentShellPageInstance.FilesystemViewModel.WorkingDirectory);
            if (isPinned)
            {
                LoadMenuFlyoutItemByName("UnpinDirectoryFromSidebar");
                UnloadMenuFlyoutItemByName("PinDirectoryToSidebar");
            }
            else
            {
                LoadMenuFlyoutItemByName("PinDirectoryToSidebar");
                UnloadMenuFlyoutItemByName("UnpinDirectoryFromSidebar");
            }
        }

        public void RightClickItemContextMenu_Opening(object sender, object e)
        {
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var showOpenMenu = (SelectedItems.Count == 1)
                && SelectedItem.PrimaryItemAttribute == StorageItemTypes.File
                && !string.IsNullOrEmpty(SelectedItem.FileExtension)
                && SelectedItem.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase);
            SetShellContextmenu(BaseLayoutItemContextFlyout, shiftPressed, showOpenMenu);

            if (!AppSettings.ShowCopyLocationMenuItem)
            {
                UnloadMenuFlyoutItemByName("CopyLocationItem");
            }

            if (!AppSettings.ShowOpenInNewTabMenuItem)
            {
                UnloadMenuFlyoutItemByName("OpenInNewTab");
            }

            if (!DataTransferManager.IsSupported())
            {
                UnloadMenuFlyoutItemByName("ShareItem");
            }

            // Find selected items that are not folders
            if (SelectedItems.Any(x => x.PrimaryItemAttribute != StorageItemTypes.Folder))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("SidebarUnpinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");
                UnloadMenuFlyoutItemByName("OpenInNewPane");

                if (SelectedItems.Count == 1)
                {
                    if (!string.IsNullOrEmpty(SelectedItem.FileExtension))
                    {
                        if (SelectedItem.IsShortcutItem)
                        {
                            LoadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            UnloadMenuFlyoutItemByName("CreateShortcut");
                        }
                        else if (SelectedItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            LoadMenuFlyoutItemByName("CreateShortcut");
                        }
                        else if (SelectedItem.FileExtension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".bat", StringComparison.OrdinalIgnoreCase) || SelectedItem.FileExtension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
                        {
                            LoadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            LoadMenuFlyoutItemByName("RunAsAdmin");
                            LoadMenuFlyoutItemByName("RunAsAnotherUser");
                            LoadMenuFlyoutItemByName("CreateShortcut");
                        }
                        else if (SelectedItem.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            LoadMenuFlyoutItemByName("RunAsAnotherUser");
                            LoadMenuFlyoutItemByName("CreateShortcut");
                        }
                        else if (SelectedItem.FileExtension.Equals(".appx", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase))
                        {
                            LoadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            LoadMenuFlyoutItemByName("CreateShortcut");
                        }
                        else
                        {
                            LoadMenuFlyoutItemByName("OpenItem");
                            LoadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            LoadMenuFlyoutItemByName("CreateShortcut");
                        }
                    }
                }
                else if (SelectedItems.Count > 1)
                {
                    UnloadMenuFlyoutItemByName("OpenItem");
                    UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }
            }
            else  // All are folders or shortcuts to folders
            {
                UnloadMenuFlyoutItemByName("OpenItem");
                UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");

                if (SelectedItems.Any(x => x.IsShortcutItem))
                {
                    UnloadMenuFlyoutItemByName("SidebarPinItem");
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }
                else if (SelectedItems.Count == 1)
                {
                    LoadMenuFlyoutItemByName("CreateShortcut");
                    LoadMenuFlyoutItemByName("OpenItem");

                    if (AppSettings.ShowCopyLocationMenuItem)
                    {
                        LoadMenuFlyoutItemByName("CopyLocationItem");
                    }
                }
                else
                {
                    LoadMenuFlyoutItemByName("SidebarPinItem");
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }

                if (selectedItems.All(x => !x.IsShortcutItem))
                {
                    if (selectedItems.All(x => x.IsPinned))
                    {
                        LoadMenuFlyoutItemByName("SidebarUnpinItem");
                        UnloadMenuFlyoutItemByName("SidebarPinItem");
                    }
                    else
                    {
                        LoadMenuFlyoutItemByName("SidebarPinItem");
                        UnloadMenuFlyoutItemByName("SidebarUnpinItem");
                    }
                }

                if (SelectedItems.Count <= 5 && SelectedItems.Count > 0)
                {
                    if (AppSettings.ShowOpenInNewTabMenuItem)
                    {
                        LoadMenuFlyoutItemByName("OpenInNewTab");
                    }
                    LoadMenuFlyoutItemByName("OpenInNewWindowItem");
                }
                else if (SelectedItems.Count > 5)
                {
                    UnloadMenuFlyoutItemByName("OpenInNewTab");
                    UnloadMenuFlyoutItemByName("OpenInNewWindowItem");
                }

                if (SelectedItems.Count == 1 && ParentShellPageInstance.IsMultiPaneEnabled && ParentShellPageInstance.IsPageMainPane)
                {
                    LoadMenuFlyoutItemByName("OpenInNewPane");
                }
                else
                {
                    UnloadMenuFlyoutItemByName("OpenInNewPane");
                }
            }

            //check the file extension of the selected item
            ParentShellPageInstance.ContentPage.SelectedItemsPropertiesViewModel.CheckFileExtension();
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance.IsCurrentInstance)
            {
                char letterPressed = Convert.ToChar(args.KeyCode);
                ParentShellPageInstance.InteractionOperations.PushJumpChar(letterPressed);
            }
        }

        protected async void List_DragEnter(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            ClearSelection();
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.Handled = true;
                e.DragUIOverride.IsCaptionVisible = true;
                IEnumerable<IStorageItem> draggedItems = new List<IStorageItem>();
                try
                {
                    draggedItems = await e.DataView.GetStorageItemsAsync();
                }
                catch (Exception dropEx) when ((uint)dropEx.HResult == 0x80040064)
                {
                    if (Connection != null)
                    {
                        await Connection.SendMessageAsync(new ValueSet() {
                            { "Arguments", "FileOperation" },
                            { "fileop", "DragDrop" },
                            { "droptext", "DragDropWindowText".GetLocalized() },
                            { "droppath", ParentShellPageInstance.FilesystemViewModel.WorkingDirectory } });
                    }
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                }
                if (!draggedItems.Any())
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }

                var folderName = Path.GetFileName(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory);
                // As long as one file doesn't already belong to this folder
                if (InstanceViewModel.IsPageTypeSearchResults || draggedItems.AreItemsAlreadyInFolder(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else if (draggedItems.AreItemsInSameDrive(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            deferral.Complete();
        }

        protected async void List_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                await ParentShellPageInstance.InteractionOperations.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, true);
                e.Handled = true;
            }

            deferral.Complete();
        }

        protected async void Item_DragStarting(object sender, DragStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();

            foreach (ListedItem item in ParentShellPageInstance.ContentPage.SelectedItems)
            {
                if (item is ShortcutItem)
                {
                    // Can't drag shortcut items
                    continue;
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    await ParentShellPageInstance.FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    await ParentShellPageInstance.FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
            }

            if (selectedStorageItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Data.SetStorageItems(selectedStorageItems, false);
            e.DragUI.SetContentFromDataPackage();
        }

        private ListedItem dragOverItem = null;
        private DispatcherTimer dragOverTimer = new DispatcherTimer();

        private void Item_DragLeave(object sender, DragEventArgs e)
        {
            ListedItem item = GetItemFromElement(sender);
            if (item == dragOverItem)
            {
                // Reset dragged over item
                dragOverItem = null;
            }
        }

        protected async void Item_DragOver(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            ListedItem item = GetItemFromElement(sender);

            if (item is null && sender is GridViewItem gvi)
            {
                item = gvi.Content as ListedItem;
            }

            SetSelectedItemOnUi(item);

            if (dragOverItem != item)
            {
                dragOverItem = item;
                dragOverTimer.Stop();
                dragOverTimer.Debounce(() =>
                {
                    if (dragOverItem != null && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        dragOverItem = null;
                        dragOverTimer.Stop();
                        ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                    }
                }, TimeSpan.FromMilliseconds(1000), false);
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> draggedItems;
                try
                {
                    draggedItems = await e.DataView.GetStorageItemsAsync();
                }
                catch (Exception ex) when ((uint)ex.HResult == 0x80040064)
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }

                e.Handled = true;
                e.DragUIOverride.IsCaptionVisible = true;

                if (InstanceViewModel.IsPageTypeSearchResults || draggedItems.AreItemsAlreadyInFolder(item.ItemPath) || draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                // Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
                else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            deferral.Complete();
        }

        protected async void Item_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            e.Handled = true;
            dragOverItem = null; // Reset dragged over item

            ListedItem rowItem = GetItemFromElement(sender);
            if (rowItem != null)
            {
                await ParentShellPageInstance.InteractionOperations.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (rowItem as ShortcutItem)?.TargetPath ?? rowItem.ItemPath, true);
            }
            deferral.Complete();
        }

        protected void InitializeDrag(UIElement element)
        {
            ListedItem item = GetItemFromElement(element);
            if (item != null)
            {
                element.AllowDrop = false;
                element.DragStarting -= Item_DragStarting;
                element.DragStarting += Item_DragStarting;
                element.DragOver -= Item_DragOver;
                element.DragLeave -= Item_DragLeave;
                element.Drop -= Item_Drop;
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    element.AllowDrop = true;
                    element.DragOver += Item_DragOver;
                    element.DragLeave += Item_DragLeave;
                    element.Drop += Item_Drop;
                }
            }
        }

        protected void UninitializeDrag(UIElement element)
        {
            element.AllowDrop = false;
            element.DragStarting -= Item_DragStarting;
            element.DragOver -= Item_DragOver;
            element.DragLeave -= Item_DragLeave;
            element.Drop -= Item_Drop;
        }

        // VirtualKey doesn't support / accept plus and minus by default.
        public readonly VirtualKey PlusKey = (VirtualKey)187;

        public readonly VirtualKey MinusKey = (VirtualKey)189;

        public void GridViewSizeIncrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            FolderSettings.GridViewSize = FolderSettings.GridViewSize + Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Larger
            if (args != null)
            {
                args.Handled = true;
            }
        }

        public void GridViewSizeDecrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            FolderSettings.GridViewSize = FolderSettings.GridViewSize - Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Smaller
            if (args != null)
            {
                args.Handled = true;
            }
        }

        public void BaseLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers == VirtualKeyModifiers.Control)
            {
                if (e.GetCurrentPoint(null).Properties.MouseWheelDelta < 0) // Mouse wheel down
                {
                    GridViewSizeDecrease(null, null);
                }
                else // Mouse wheel up
                {
                    GridViewSizeIncrease(null, null);
                }

                e.Handled = true;
            }
        }
    }
}