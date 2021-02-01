using Files.Common;
using Files.Dialogs;
using Files.Filesystem;
using Files.Filesystem.FilesystemHistory;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.UserControls.Selection;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColumnViewShellPage : Page
    {
        private ItemViewModel FileSystemViewModel;
        private SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel;
        private ReadOnlyObservableCollection<ListedItem> ListOfFiles;
        private Interaction Interaction;
        private IShellPage CurrentInstance;
        private int RootBladeNumber;
        private string CurrentFolderPath;
        private ListedItem renamingItem;
        private string oldItemName;
        private bool IsRenamingItem;

        public static event EventHandler NotifyRoot;
        public static event EventHandler GetCurrentColumnAndClearRest;
        public static event EventHandler SetSelectedItems;
        public ColumnViewShellPage()
        {
            InitializeComponent();

            var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, ListView_SelectionChanged);
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
            FileList.Focus(FocusState.Programmatic);
        }

        private async void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            await Task.Delay(200);
            FileList.Focus(FocusState.Programmatic);
        }

        private void RightClickContextMenu_Opening(object sender, object e)
        {

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
        private void LoadMenuFlyoutItem(IList<MenuFlyoutItemBase> MenuItemsList, IEnumerable<Win32ContextMenuItem> menuFlyoutItems, string menuHandle, bool showIcons = true, int itemsBeforeOverflow = int.MaxValue)
        {
            var items_count = 0; // Separators do not count for reaching the overflow threshold
            var menu_items = menuFlyoutItems.TakeWhile(x => x.Type == MenuItemType.MFT_SEPARATOR || ++items_count <= itemsBeforeOverflow).ToList();
            var overflow_items = menuFlyoutItems.Except(menu_items).ToList();

            if (overflow_items.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any())
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
        private async void MenuLayoutItem_Click(object sender, RoutedEventArgs e)
        {
            var currentMenuLayoutItem = (MenuFlyoutItem)sender;
            if (currentMenuLayoutItem != null)
            {
                var (menuItem, menuHandle) = ParseContextMenuTag(currentMenuLayoutItem.Tag);
                if (FileSystemViewModel.Connection != null)
                {
                    await FileSystemViewModel.Connection.SendMessageAsync(new ValueSet()
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
            if (tag is ValueTuple<Win32ContextMenuItem, string>)
            {
                (Win32ContextMenuItem menuItem, string menuHandle) = (ValueTuple<Win32ContextMenuItem, string>)tag;
                return (menuItem, menuHandle);
            }

            return (null, null);
        }
        public void SetShellContextmenu(MenuFlyout menuFlyout, bool shiftPressed, bool showOpenMenu)
        {
            ClearShellContextMenus(menuFlyout);
            var currentBaseLayoutItemCount = menuFlyout.Items.Count;
            var maxItems = App.AppSettings.ShowAllContextMenuItems ? int.MaxValue : shiftPressed ? 6 : 4;
            var Connection = FileSystemViewModel.Connection;
            if (Connection != null)
            {
                var response = Connection.SendMessageAsync(new ValueSet()
                {
                        { "Arguments", "LoadContextMenu" },
                        { "FilePath", SelectedItemsPropertiesViewModel.IsItemSelected ?
                            string.Join('|', FileList.SelectedItems.Cast<ListedItem>().ToList().Select(x => x.ItemPath)) :
                            FileSystemViewModel.CurrentFolder.ItemPath},
                        { "ExtendedMenu", shiftPressed },
                        { "ShowOpenMenu", showOpenMenu }
                }).AsTask().Result;
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
        private void RightClickItemContextMenu_Opening(object sender, object e)
        {
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var showOpenMenu = (FileList.SelectedItems.Cast<ListedItem>().ToList().Count == 1)
                && (FileList.SelectedItem as ListedItem).PrimaryItemAttribute == StorageItemTypes.File
                && !string.IsNullOrEmpty((FileList.SelectedItem as ListedItem).FileExtension)
                && (FileList.SelectedItem as ListedItem).FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase);
            SetShellContextmenu(BaseLayoutItemContextFlyout, shiftPressed, showOpenMenu);

            if (!App.AppSettings.ShowCopyLocationOption)
            {
                UnloadMenuFlyoutItemByName("CopyLocationItem");
            }

            if (!DataTransferManager.IsSupported())
            {
                UnloadMenuFlyoutItemByName("ShareItem");
            }

            // Find selected items that are not folders
            if (FileList.SelectedItems.Cast<ListedItem>().ToList().Any(x => x.PrimaryItemAttribute != StorageItemTypes.Folder))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");

                if (FileList.SelectedItems.Cast<ListedItem>().ToList().Count == 1)
                {
                    if (!string.IsNullOrEmpty((FileList.SelectedItem as ListedItem).FileExtension))
                    {
                        if ((FileList.SelectedItem as ListedItem).IsShortcutItem)
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            UnloadMenuFlyoutItemByName("CreateShortcut");
                        }
                        else if ((FileList.SelectedItem as ListedItem).FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if ((FileList.SelectedItem as ListedItem).FileExtension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                            || (FileList.SelectedItem as ListedItem).FileExtension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            (this.FindName("RunAsAdmin") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("RunAsAnotherUser") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if ((FileList.SelectedItem as ListedItem).FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            (this.FindName("RunAsAnotherUser") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if ((FileList.SelectedItem as ListedItem).FileExtension.Equals(".appx", StringComparison.OrdinalIgnoreCase)
                            || (FileList.SelectedItem as ListedItem).FileExtension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                            || (FileList.SelectedItem as ListedItem).FileExtension.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase)
                            || (FileList.SelectedItem as ListedItem).FileExtension.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase))
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
                else if (FileList.SelectedItems.Cast<ListedItem>().ToList().Count > 1)
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

                if (FileList.SelectedItems.Cast<ListedItem>().ToList().Any(x => x.IsShortcutItem))
                {
                    UnloadMenuFlyoutItemByName("SidebarPinItem");
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }
                else if (FileList.SelectedItems.Cast<ListedItem>().ToList().Count == 1)
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

                if (FileList.SelectedItems.Cast<ListedItem>().ToList().Count <= 5 && FileList.SelectedItems.Cast<ListedItem>().ToList().Count > 0)
                {
                    (this.FindName("OpenInNewTab") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("OpenInNewWindowItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    //this.FindName("SidebarPinItem");
                    //this.FindName("OpenInNewTab");
                    //this.FindName("OpenInNewWindowItem");
                }
                else if (FileList.SelectedItems.Cast<ListedItem>().ToList().Count > 5)
                {
                    UnloadMenuFlyoutItemByName("OpenInNewTab");
                    UnloadMenuFlyoutItemByName("OpenInNewWindowItem");
                }
            }

            //check the file extension of the selected item
            ColumnViewBrowser.SelectedItemsPropertiesViewModel2.CheckFileExtension();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var navparams = e.Parameter as ColumnViewNavParams; 
            FileSystemViewModel = navparams.ViewModel;
            SelectedItemsPropertiesViewModel = ColumnViewBrowser.SelectedItemsPropertiesViewModel2;
            ListOfFiles = navparams.ItemsSource;
            Interaction = navparams.Interaction;
            CurrentInstance = navparams.CurrentInstance;
            RootBladeNumber = navparams.BladeNumber;
            CurrentFolderPath = navparams.Path;
            await CurrentInstance.FilesystemViewModel.SetWorkingDirectoryAsync(navparams.Path);
        }

        private async void FileList_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            foreach (var item in ListOfFiles.ToList())
            {
                await Window.Current.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    FileSystemViewModel.LoadExtendedItemProperties(item, 24);
                    item.ItemPropertiesInitialized = true;
                });
            }
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            // Skip code if the control or shift key is pressed
            if (ctrlPressed || shiftPressed)
            {
                return;
            }
            if (App.AppSettings.OpenItemsWithOneclick)
            {
                var item = e.ClickedItem as ListedItem;
                if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    try
                    {
                        //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                        await Task.Delay(200);
                        Interaction.OpenItem_Click(null,null);
                    }
                    catch
                    {
                        
                    }
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                    await Task.Delay(200);
                    NotifyRoot?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Folder = item }, EventArgs.Empty);
                }
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FileList.EffectiveViewportChanged += FileList_EffectiveViewportChanged;
        }
        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            var menuItem = this.FindName(nameToUnload) as DependencyObject;
            if (menuItem != null) // Prevent crash if the MenuFlyoutItem is missing
            {
                (menuItem as MenuFlyoutItemBase).Visibility = Visibility.Collapsed;
            }
        }
        private void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var data = ((FrameworkElement)e.OriginalSource).DataContext;
            if (data is ListedItem)
            {
                if (!FileList.SelectedItems.Cast<ListedItem>().ToList().Contains(data))
                {
                    FileList.SelectedItem = data;
                    SelectedItemsPropertiesViewModel.CheckFileExtension();
                }
            }
            else
            {
                ClearSelection();
                if (((FrameworkElement)e.OriginalSource).DataContext == CurrentFolderPath)
                {
                    GetCurrentColumnAndClearRest?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Path = ((FrameworkElement)e.OriginalSource).DataContext.ToString() }, EventArgs.Empty);
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSelectedItems?.Invoke(FileList.SelectedItems.Cast<ListedItem>().ToList(), EventArgs.Empty);
        }

        private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!App.AppSettings.OpenItemsWithOneclick)
            {
                var data = ((FrameworkElement)e.OriginalSource).DataContext;
                if (data is ListedItem)
                {
                    var item = data as ListedItem;
                    if (item.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        try
                        {
                            //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                            await Task.Delay(200);
                            Interaction.OpenItem_Click(null, null);
                        }
                        catch
                        {

                        }
                    }
                    else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        //SetSelectedItems?.Invoke(new List<ListedItem> { item }, EventArgs.Empty);
                        await Task.Delay(200);
                        NotifyRoot?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Folder = item }, EventArgs.Empty);
                    }
                }
            }
        }
        public void SetSelectedItemOnUi(ListedItem item)
        {
            ClearSelection();
            FileList.SelectedItems.Add(item);
        }

        public void SetSelectedItemsOnUi(List<ListedItem> items)
        {
            ClearSelection();

            foreach (ListedItem item in items)
            {
                FileList.SelectedItems.Add(item);
            }
        }

        public void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            foreach (ListedItem selectedItem in selectedItems)
            {
                FileList.SelectedItems.Add(selectedItem);
            }
        }

        public void SelectAllItems()
        {
            ClearSelection();
            FileList.SelectAll();
        }

        public void InvertSelection()
        {
            List<ListedItem> allItems = FileList.Items.Cast<ListedItem>().ToList();
            List<ListedItem> newSelectedItems = allItems.Except(FileList.SelectedItems.Cast<ListedItem>().ToList()).ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public void ClearSelection()
        {
            FileList.SelectedItems.Clear();
        }

        private void FileList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext == CurrentFolderPath)
            {
                GetCurrentColumnAndClearRest?.Invoke(new FolderInfo { RootBladeNumber = RootBladeNumber, Path = ((FrameworkElement)e.OriginalSource).DataContext.ToString() }, EventArgs.Empty);
            }
        }

        private void Page_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Page).Properties.IsLeftButtonPressed)
            {
                ClearSelection();
            }
            else if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
            {
                CurrentInstance.InteractionOperations.ItemPointerPressed(sender, e);
            }
        }
        private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                TextBox textBox = sender as TextBox;
                textBox.LostFocus -= RenameTextBox_LostFocus;
                textBox.Text = oldItemName;
                EndRename(textBox);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter)
            {
                TextBox textBox = sender as TextBox;
                textBox.LostFocus -= RenameTextBox_LostFocus;
                CommitRename(textBox);
                e.Handled = true;
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = e.OriginalSource as TextBox;
            CommitRename(textBox);
        }

        private async void CommitRename(TextBox textBox)
        {
            EndRename(textBox);
            string newItemName = textBox.Text.Trim().TrimEnd('.');

            bool successful = await CurrentInstance.InteractionOperations.RenameFileItemAsync(renamingItem, oldItemName, newItemName);
            if (!successful)
            {
                renamingItem.ItemName = oldItemName;
            }
        }
        private void ColumnViewTextBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
            {
                FileNameTeachingTip.IsOpen = true;
            }
            else
            {
                FileNameTeachingTip.IsOpen = false;
            }
        }
        private void EndRename(TextBox textBox)
        {
            Grid parentPanel = textBox.Parent as Grid;
            TextBlock textBlock = parentPanel.FindName("ItemName") as TextBlock;
            textBox.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Visible;

            textBox.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown -= RenameTextBox_KeyDown;
            FileNameTeachingTip.IsOpen = false;
            IsRenamingItem = false;
        }
        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            renamingItem = FileList.SelectedItem as ListedItem;
            int extensionLength = renamingItem.FileExtension?.Length ?? 0;
            ListViewItem listViewItem = FileList.ContainerFromItem(renamingItem) as ListViewItem;
            TextBox textBox = null;
            TextBlock textBlock = (listViewItem.ContentTemplateRoot as Grid).FindName("ItemName") as TextBlock;
            textBox = (listViewItem.ContentTemplateRoot as Grid).FindName("ColumnViewTextBoxName") as TextBox;
            textBox.Text = textBlock.Text;
            oldItemName = textBlock.Text;
            textBlock.Visibility = Visibility.Collapsed;
            textBox.Visibility = Visibility.Visible;
            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;

            int selectedTextLength = (FileList.SelectedItem as ListedItem).ItemName.Length;
            if (App.AppSettings.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            textBox.Select(0, selectedTextLength);
            IsRenamingItem = true;
        }
    }
}
