using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public abstract class BaseLayout : Page, INotifyPropertyChanged
    {
        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }
        public SettingsViewModel AppSettings => App.AppSettings;
        public CurrentInstanceViewModel InstanceViewModel => App.CurrentInstance.InstanceViewModel;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }
        public bool IsQuickLookEnabled { get; set; } = false;
        public MenuFlyout BaseLayoutItemContextFlyout { get; set; }

        public ItemViewModel AssociatedViewModel = null;
        public Interaction AssociatedInteractions = null;
        public bool isRenamingItem = false;

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

        private List<ListedItem> _SelectedItems = new List<ListedItem>();

        public List<ListedItem> SelectedItems
        {
            get
            {
                return _SelectedItems;
            }
            internal set
            {
                if (value != _SelectedItems)
                {
                    _SelectedItems = value;
                    if (_SelectedItems.Count == 0)
                    {
                        IsItemSelected = false;
                        SelectedItem = null;
                        SelectedItemsPropertiesViewModel.IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                        SelectedItem = _SelectedItems.First();
                        SelectedItemsPropertiesViewModel.IsItemSelected = true;

                        if (SelectedItems.Count >= 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCount = SelectedItems.Count;
                        }

                        if (SelectedItems.Count == 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = SelectedItems.Count.ToString() + " " + ResourceController.GetTranslation("ItemSelected/Text");
                            SelectedItemsPropertiesViewModel.ItemSize = SelectedItem.FileSize;
                        }
                        else
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = SelectedItems.Count.ToString() + " " + ResourceController.GetTranslation("ItemsSelected/Text");

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

        public BaseLayout()
        {
            this.Loaded += Page_Loaded;
            Page_Loaded(null, null);
            SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel();
            DirectoryPropertiesViewModel = new DirectoryPropertiesViewModel();
            // QuickLook Integration
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isQuickLookIntegrationEnabled = localSettings.Values["quicklook_enabled"];

            if (isQuickLookIntegrationEnabled != null && isQuickLookIntegrationEnabled.Equals(true))
            {
                IsQuickLookEnabled = true;
            }
        }

        public abstract void SelectAllItems();

        public abstract void InvertSelection();

        public abstract void ClearSelection();

        public abstract void SetDragModeForItems();

        public abstract void ScrollIntoView(ListedItem item);

        public abstract int GetSelectedIndex();

        public abstract void SetSelectedItemOnUi(ListedItem selectedItem);

        public abstract void SetSelectedItemsOnUi(List<ListedItem> selectedItems);

        private void ClearShellContextMenus()
        {
            var contextMenuItems = BaseLayoutItemContextFlyout.Items.Where(c => c.Tag != null && ParseContextMenuTag(c.Tag).menuHandle != null).ToList();
            for (int i = 0; i < contextMenuItems.Count; i++)
            {
                BaseLayoutItemContextFlyout.Items.RemoveAt(BaseLayoutItemContextFlyout.Items.IndexOf(contextMenuItems[i]));
            }
            if (BaseLayoutItemContextFlyout.Items[0] is MenuFlyoutSeparator flyoutSeperator)
            {
                BaseLayoutItemContextFlyout.Items.RemoveAt(BaseLayoutItemContextFlyout.Items.IndexOf(flyoutSeperator));
            }
        }

        public virtual void SetShellContextmenu(bool shiftPressed, bool showOpenMenu)
        {
            ClearShellContextMenus();
            if (_SelectedItems != null && _SelectedItems.Count > 0)
            {
                var currentBaseLayoutItemCount = BaseLayoutItemContextFlyout.Items.Count;
                var isDirectory = !_SelectedItems.Any(c => c.PrimaryItemAttribute == StorageItemTypes.File || c.PrimaryItemAttribute == StorageItemTypes.None);
                var maxItems = AppSettings.ShowAllContextMenuItems ? int.MaxValue : shiftPressed ? 6 : 4;
                if (App.Connection != null)
                {
                    var response = App.Connection.SendMessageAsync(new ValueSet() {
                        { "Arguments", "LoadContextMenu" },
                        { "FilePath", string.Join('|', _SelectedItems.Select(x => x.ItemPath)) },
                        { "ExtendedMenu", shiftPressed },
                        { "ShowOpenMenu", showOpenMenu }}).AsTask().Result;
                    if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success
                        && response.Message.ContainsKey("Handle"))
                    {
                        var contextMenu = JsonConvert.DeserializeObject<Win32ContextMenu>((string)response.Message["ContextMenu"]);
                        if (contextMenu != null)
                        {
                            LoadMenuFlyoutItem(BaseLayoutItemContextFlyout.Items, contextMenu.Items, (string)response.Message["Handle"], true, maxItems);
                        }
                    }
                }
                var totalFlyoutItems = BaseLayoutItemContextFlyout.Items.Count - currentBaseLayoutItemCount;
                if (totalFlyoutItems > 0 && !(BaseLayoutItemContextFlyout.Items[totalFlyoutItems] is MenuFlyoutSeparator))
                {
                    BaseLayoutItemContextFlyout.Items.Insert(totalFlyoutItems, new MenuFlyoutSeparator());
                }
            }
        }

        public abstract void FocusSelectedItems();

        public abstract void StartRenameItem();

        public abstract void ResetItemOpacity();

        public abstract void SetItemOpacity(ListedItem item);

        protected abstract ListedItem GetItemFromElement(object element);

        private void AppSettings_LayoutModeChangeRequested(object sender, EventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                App.CurrentInstance.FilesystemViewModel.CancelLoadAndClearFiles();
                App.CurrentInstance.FilesystemViewModel.IsLoadingItems = true;
                App.CurrentInstance.FilesystemViewModel.IsLoadingItems = false;

                App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), App.CurrentInstance.FilesystemViewModel.WorkingDirectory, null);
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
            AppSettings.LayoutModeChangeRequested += AppSettings_LayoutModeChangeRequested;
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
            var parameters = (string)eventArgs.Parameter;
            App.CurrentInstance.NavigationToolbar.CanRefresh = true;
            IsItemSelected = false;
            AssociatedViewModel.IsFolderEmptyTextDisplayed = false;
            await App.CurrentInstance.FilesystemViewModel.SetWorkingDirectory(parameters);

            // pathRoot will be empty on recycle bin path
            var workingDir = App.CurrentInstance.FilesystemViewModel.WorkingDirectory;
            string pathRoot = Path.GetPathRoot(workingDir);
            if (string.IsNullOrEmpty(pathRoot) || workingDir == pathRoot)
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;
            }
            else
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = true;
            }

            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
            App.CurrentInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(App.AppSettings.RecycleBinPath);
            App.CurrentInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\");

            await App.CurrentInstance.MultitaskingControl?.SetSelectedTabInfo(new DirectoryInfo(workingDir).Name, workingDir);
            App.CurrentInstance.FilesystemViewModel.RefreshItems();

            App.CurrentInstance.MultitaskingControl?.SelectionChanged();
            MainPage.Clipboard_ContentChanged(null, null);
            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = parameters;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            App.CurrentInstance.FilesystemViewModel.CancelLoadAndClearFiles();
            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
            AppSettings.LayoutModeChangeRequested -= AppSettings_LayoutModeChangeRequested;
        }

        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            var menuItem = this.FindName(nameToUnload) as DependencyObject;
            if (menuItem != null) // Prevent crash if the MenuFlyoutItem is missing
                (menuItem as MenuFlyoutItemBase).Visibility = Visibility.Collapsed;
        }

        private void LoadMenuFlyoutItem(IList<MenuFlyoutItemBase> MenuItemsList, IEnumerable<Win32ContextMenuItem> menuFlyoutItems, string menuHandle, bool showIcons = true, int itemsBeforeOverflow = int.MaxValue)
        {
            var items_count = 0; // Separators do not count for reaching the overflow threshold
            var menu_items = menuFlyoutItems.TakeWhile(x => x.Type == MenuItemType.MFT_SEPARATOR || ++items_count <= itemsBeforeOverflow).ToList();
            var overflow_items = menuFlyoutItems.Except(menu_items).ToList();

            if (overflow_items.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any())
            {
                var menuLayoutSubItem = new MenuFlyoutSubItem()
                {
                    Text = ResourceController.GetTranslation("ContextMenuMoreItemsLabel"),
                    Tag = ((Win32ContextMenuItem)null, menuHandle),
                    Icon = new FontIcon()
                    {
                        FontFamily = App.Current.Resources["FluentUIGlyphs"] as Windows.UI.Xaml.Media.FontFamily,
                        Glyph = "\xEAD0"
                    }
                };
                LoadMenuFlyoutItem(menuLayoutSubItem.Items, overflow_items, menuHandle, false);
                MenuItemsList.Insert(0, menuLayoutSubItem);
            }
            foreach (var menuFlyoutItem in menu_items
                .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR) // Remove leading seperators
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
                if (App.Connection != null)
                {
                    await App.Connection.SendMessageAsync(new ValueSet() {
                        { "Arguments", "ExecAndCloseContextMenu" },
                        { "Handle", menuHandle },
                        { "ItemID", menuItem.ID } });
                }
            }
        }

        public async void RightClickItemContextMenu_Closing(object sender, object e)
        {
            var shellContextMenuTag = (sender as MenuFlyout).Items.Where(x => x.Tag != null)
                .Select(x => ParseContextMenuTag(x.Tag)).FirstOrDefault(x => x.menuItem != null);
            if (shellContextMenuTag.menuItem != null && App.Connection != null)
            {
                await App.Connection.SendMessageAsync(new ValueSet() {
                    { "Arguments", "ExecAndCloseContextMenu" },
                    { "Handle", shellContextMenuTag.menuHandle } });
            }
        }

        public void RightClickItemContextMenu_Opening(object sender, object e)
        {
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var showOpenMenu = (SelectedItems.Count == 1)
                && SelectedItem.PrimaryItemAttribute == StorageItemTypes.File
                && !string.IsNullOrEmpty(SelectedItem.FileExtension)
                && SelectedItem.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase);
            SetShellContextmenu(shiftPressed, showOpenMenu);

            if (!DataTransferManager.IsSupported())
            {
                UnloadMenuFlyoutItemByName("ShareItem");
            }

            // Find selected items that are not folders
            if (SelectedItems.Any(x => x.PrimaryItemAttribute != StorageItemTypes.Folder))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");

                if (SelectedItems.Count == 1)
                {
                    if (!string.IsNullOrEmpty(SelectedItem.FileExtension))
                    {
                        if (SelectedItem.IsShortcutItem)
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
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
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if (SelectedItem.FileExtension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            (this.FindName("RunAsAdmin") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("RunAsAnotherUser") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if (SelectedItem.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            (this.FindName("RunAsAnotherUser") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if (SelectedItem.FileExtension.Equals(".appx", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase))
                        {
                            (this.FindName("OpenItemWithAppPicker") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("OpenItemWithAppPicker") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
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
                    (this.FindName("SidebarPinItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                }
                else
                {
                    (this.FindName("SidebarPinItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }

                if (SelectedItems.Count <= 5 && SelectedItems.Count > 0)
                {
                    (this.FindName("OpenInNewTab") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("OpenInNewWindowItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    //this.FindName("SidebarPinItem");
                    //this.FindName("OpenInNewTab");
                    //this.FindName("OpenInNewWindowItem");
                }
                else if (SelectedItems.Count > 5)
                {
                    UnloadMenuFlyoutItemByName("OpenInNewTab");
                    UnloadMenuFlyoutItemByName("OpenInNewWindowItem");
                }
            }

            //check the file extension of the selected item
            App.CurrentInstance.ContentPage.SelectedItemsPropertiesViewModel.CheckFileExtension();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedViewModel == null && AssociatedInteractions == null)
            {
                AssociatedViewModel = App.CurrentInstance.FilesystemViewModel;
                AssociatedInteractions = App.CurrentInstance.InteractionOperations;
                if (App.CurrentInstance == null)
                {
                    App.CurrentInstance = VerticalTabView.GetCurrentSelectedTabInstance<ModernShellPage>();
                }
            }
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            char letterPressed = Convert.ToChar(args.KeyCode);
            App.CurrentInstance.InteractionOperations.PushJumpChar(letterPressed);
        }

        protected async void List_DragOver(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            ClearSelection();
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.Handled = true;
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();
                e.DragUIOverride.IsCaptionVisible = true;

                var folderName = Path.GetFileName(App.CurrentInstance.FilesystemViewModel.WorkingDirectory);
                // As long as one file doesn't already belong to this folder
                if (draggedItems.AreItemsAlreadyInFolder(App.CurrentInstance.FilesystemViewModel.WorkingDirectory))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else if (draggedItems.AreItemsInSameDrive(App.CurrentInstance.FilesystemViewModel.WorkingDirectory))
                {
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("MoveToFolderCaptionText"), folderName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("CopyToFolderCaptionText"), folderName);
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
                await AssociatedInteractions.PasteItems(e.DataView, App.CurrentInstance.FilesystemViewModel.WorkingDirectory, e.AcceptedOperation);
                e.Handled = true;
            }

            deferral.Complete();
        }

        protected async void Item_DragStarting(object sender, DragStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();

            foreach (ListedItem item in App.CurrentInstance.ContentPage.SelectedItems)
            {
                if (item is ShortcutItem)
                {
                    // Can't drag shortcut items
                    continue;
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    selectedStorageItems.Add(await ItemViewModel.GetFileFromPathAsync(item.ItemPath));
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    selectedStorageItems.Add(await ItemViewModel.GetFolderFromPathAsync(item.ItemPath));
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

        protected async void Item_DragOver(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            ListedItem item = GetItemFromElement(sender);
            SetSelectedItemOnUi(item);

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.Handled = true;
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();

                if (draggedItems.AreItemsAlreadyInFolder(item.ItemPath) || draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                // Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
                else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
                {
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("MoveToFolderCaptionText"), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("CopyToFolderCaptionText"), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            deferral.Complete();
        }

        protected async void Item_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            e.Handled = true;
            ListedItem rowItem = GetItemFromElement(sender);
            await App.CurrentInstance.InteractionOperations.PasteItems(e.DataView, (rowItem as ShortcutItem)?.TargetPath ?? rowItem.ItemPath, e.AcceptedOperation);
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
                element.Drop -= Item_Drop;
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    element.AllowDrop = true;
                    element.DragOver += Item_DragOver;
                    element.Drop += Item_Drop;
                }
            }
        }

        // VirtualKey doesn't support / accept plus and minus by default.
        public readonly VirtualKey plusKey = (VirtualKey)187;

        public readonly VirtualKey minusKey = (VirtualKey)189;

        public void GridViewSizeIncrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AppSettings.GridViewSize = AppSettings.GridViewSize + 25; // Make Larger
            if (args != null)
                args.Handled = true;
        }

        public void GridViewSizeDecrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AppSettings.GridViewSize = AppSettings.GridViewSize - 25; // Make Smaller
            if (args != null)
                args.Handled = true;
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