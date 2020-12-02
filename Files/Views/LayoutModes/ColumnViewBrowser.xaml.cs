using ByteSizeLib;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.Selection;
using Files.Views.Pages;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Files.Helpers.NativeFindStorageItemHelper;
using Interaction = Files.Interacts.Interaction;

namespace Files
{
    public sealed partial class ColumnViewBrowser : BaseLayout
    {
        public string oldItemName;

        public ColumnViewBrowser()
        {
            this.InitializeComponent();
            base.BaseLayoutContextFlyout = this.BaseLayoutContextFlyout;
            base.BaseLayoutItemContextFlyout = this.BaseLayoutItemContextFlyout;

            //var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
            //selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
            App.AppSettings.LayoutModeChangeRequested += AppSettings_LayoutModeChangeRequested;

            SetItemTemplate(); // Set ItemTemplate
        }

        private async void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            await Task.Delay(200);
            FileList.Focus(FocusState.Programmatic);
        }
        private void AppSettings_LayoutModeChangeRequested(object sender, EventArgs e)
        {
            SetItemTemplate(); // Set ItemTemplate
        }

        private void SetItemTemplate()
        {
            if (this.IsLoaded)
            {
                while (ColumnBladeView.Items.Count > 1)
                {
                    try
                    {
                        ColumnBladeView.Items.RemoveAt(1);
                        ColumnBladeView.ActiveBlades.RemoveAt(1);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            FileList.ItemTemplate = (App.AppSettings.LayoutMode == 1) ? TilesBrowserTemplate : GridViewBrowserTemplate; // Choose Template
            FirstBlade.ItemTemplate = ListTemplate; // Choose Template

            // Set GridViewSize event handlers
            if (App.AppSettings.LayoutMode == 1)
            {
                App.AppSettings.GridViewSizeChangeRequested -= AppSettings_GridViewSizeChangeRequested;
            }
            else if (App.AppSettings.LayoutMode == 2)
            {
                _iconSize = UpdateThumbnailSize(); // Get icon size for jumps from other layouts directly to a grid size
                App.AppSettings.GridViewSizeChangeRequested += AppSettings_GridViewSizeChangeRequested;
            }
        }

        public override void SetSelectedItemOnUi(ListedItem item)
        {
            ClearSelection();
            FileList.SelectedItems.Add(item);
        }

        public override void SetSelectedItemsOnUi(List<ListedItem> items)
        {
            ClearSelection();

            foreach (ListedItem item in items)
            {
                FileList.SelectedItems.Add(item);
            }
        }

        public override void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            foreach (ListedItem selectedItem in selectedItems)
            {
                FileList.SelectedItems.Add(selectedItem);
            }
        }

        public override void SelectAllItems()
        {
            ClearSelection();
            FileList.SelectAll();
        }

        public override void InvertSelection()
        {
            List<ListedItem> allItems = FileList.Items.Cast<ListedItem>().ToList();
            List<ListedItem> newSelectedItems = allItems.Except(SelectedItems).ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public override void ClearSelection()
        {
            FileList.SelectedItems.Clear();
        }

        public override void SetDragModeForItems()
        {
            foreach (ListedItem listedItem in FileList.Items)
            {
                GridViewItem gridViewItem = FileList.ContainerFromItem(listedItem) as GridViewItem;

                if (gridViewItem != null)
                {
                    List<Grid> grids = new List<Grid>();
                    Interaction.FindChildren(grids, gridViewItem);
                    var rootItem = grids.Find(x => x.Tag?.ToString() == "ItemRoot");
                    rootItem.CanDrag = SelectedItems.Contains(listedItem);
                }
            }
        }

        public override void ScrollIntoView(ListedItem item)
        {
            FileList.ScrollIntoView(item);
        }

        public override int GetSelectedIndex()
        {
            return FileList.SelectedIndex;
        }

        public override void FocusSelectedItems()
        {
            FileList.ScrollIntoView(FileList.Items.Last());
        }

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var parentContainer = Interaction.FindParent<GridViewItem>(e.OriginalSource as DependencyObject);
            if (FileList.SelectedItems.Contains(FileList.ItemFromContainer(parentContainer)))
            {
                return;
            }
            // The following code is only reachable when a user RightTapped an unselected row
            SetSelectedItemOnUi(FileList.ItemFromContainer(parentContainer) as ListedItem);
        }

        private void GridViewBrowserViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Page).Properties.IsLeftButtonPressed)
            {
                ClearSelection();
            }
            else if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
            {
                ParentShellPageInstance.InteractionOperations.ItemPointerPressed(sender, e);
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItems = FileList.SelectedItems.Cast<ListedItem>().ToList();
        }

        private ListedItem renamingItem;

        public override void StartRenameItem()
        {
            renamingItem = SelectedItem;
            int extensionLength = renamingItem.FileExtension?.Length ?? 0;
            GridViewItem gridViewItem = FileList.ContainerFromItem(renamingItem) as GridViewItem;
            TextBox textBox = null;

            // Handle layout differences between tiles browser and photo album
            if (App.AppSettings.LayoutMode == 2)
            {
                Popup popup = (gridViewItem.ContentTemplateRoot as Grid).FindName("EditPopup") as Popup;
                TextBlock textBlock = (gridViewItem.ContentTemplateRoot as Grid).FindName("ItemName") as TextBlock;
                textBox = popup.Child as TextBox;
                textBox.Text = textBlock.Text;
                popup.IsOpen = true;
                oldItemName = textBlock.Text;
            }
            else
            {
                TextBlock textBlock = (gridViewItem.ContentTemplateRoot as Grid).FindName("ItemName") as TextBlock;
                textBox = (gridViewItem.ContentTemplateRoot as Grid).FindName("TileViewTextBoxItemName") as TextBox;
                textBox.Text = textBlock.Text;
                oldItemName = textBlock.Text;
                textBlock.Visibility = Visibility.Collapsed;
                textBox.Visibility = Visibility.Visible;
            }

            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (App.AppSettings.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            textBox.Select(0, selectedTextLength);
            IsRenamingItem = true;
        }

        private void GridViewTextBoxItemName_TextChanged(object sender, TextChangedEventArgs e)
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

        public override void ResetItemOpacity()
        {
            IEnumerable items = (IEnumerable)FileList.ItemsSource;
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

        public override void SetItemOpacity(ListedItem item)
        {
            item.Opacity = 0.4;
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

            bool successful = await ParentShellPageInstance.InteractionOperations.RenameFileItemAsync(renamingItem, oldItemName, newItemName);
            if (!successful)
            {
                renamingItem.ItemName = oldItemName;
            }
        }

        private void EndRename(TextBox textBox)
        {
            if (App.AppSettings.LayoutMode == 2)
            {
                Popup popup = textBox.Parent as Popup;
                TextBlock textBlock = (popup.Parent as Grid).Children[1] as TextBlock;
                popup.IsOpen = false;
            }
            else
            {
                StackPanel parentPanel = textBox.Parent as StackPanel;
                TextBlock textBlock = parentPanel.Children[0] as TextBlock;
                textBox.Visibility = Visibility.Collapsed;
                textBlock.Visibility = Visibility.Visible;
            }

            textBox.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown -= RenameTextBox_KeyDown;
            FileNameTeachingTip.IsOpen = false;
            IsRenamingItem = false;
        }

        private void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (!IsRenamingItem)
                {
                    ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                    e.Handled = true;
                }
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                ParentShellPageInstance.InteractionOperations.ShowPropertiesButton_Click(null, null);
            }
            else if (e.Key == VirtualKey.Space)
            {
                if (!IsRenamingItem && !ParentShellPageInstance.NavigationToolbar.IsEditModeEnabled)
                {
                    if (IsQuickLookEnabled)
                    {
                        ParentShellPageInstance.InteractionOperations.ToggleQuickLook();
                    }
                    e.Handled = true;
                }
            }
            else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
            {
                // Unfocus the GridView so keyboard shortcut can be handled
                this.Focus(FocusState.Programmatic);
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance != null)
            {
                if (ParentShellPageInstance.CurrentPageType == typeof(GridViewBrowser) && !IsRenamingItem)
                {
                    // Don't block the various uses of enter key (key 13)
                    var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
                    if (args.KeyCode == 13 || focusedElement is Button || focusedElement is TextBox || focusedElement is PasswordBox ||
                        Interacts.Interaction.FindParent<ContentDialog>(focusedElement) != null)
                    {
                        return;
                    }

                    base.Page_CharacterReceived(sender, args);
                    FileList.Focus(FocusState.Keyboard);
                }
            }
        }

        private async void Grid_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            if (sender.DataContext is ListedItem item && (!item.ItemPropertiesInitialized) && (args.BringIntoViewDistanceX < sender.ActualHeight))
            {
                await Window.Current.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(item, _iconSize);
                    item.ItemPropertiesInitialized = true;
                });

                sender.CanDrag = FileList.SelectedItems.Contains(item); // Update CanDrag
            }
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            FrameworkElement gridItem = element as FrameworkElement;
            return gridItem.DataContext as ListedItem;
        }

        private void FileListGridItem_DataContextChanged(object sender, DataContextChangedEventArgs e)
        {
            InitializeDrag(sender as UIElement);
        }

        private void FileListGridItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers == VirtualKeyModifiers.Control)
            {
                var listedItem = (sender as Grid).DataContext as ListedItem;
                if (FileList.SelectedItems.Contains(listedItem))
                {
                    FileList.SelectedItems.Remove(listedItem);
                    // Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
                    e.Handled = true;
                }
            }
            else if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed)
            {
                var listedItem = (sender as Grid).DataContext as ListedItem;

                if (!FileList.SelectedItems.Contains(listedItem))
                {
                    SetSelectedItemOnUi(listedItem);
                }
            }
        }

        private uint _iconSize = UpdateThumbnailSize();

        private static uint UpdateThumbnailSize()
        {
            if (App.AppSettings.LayoutMode == 1 || App.AppSettings.GridViewSize < 200)
            {
                return 80; // Small thumbnail
            }
            else if (App.AppSettings.GridViewSize < 275)
            {
                return 120; // Medium thumbnail
            }
            else if (App.AppSettings.GridViewSize < 325)
            {
                return 160; // Large thumbnail
            }
            else
            {
                return 240; // Extra large thumbnail
            }
        }

        private void AppSettings_GridViewSizeChangeRequested(object sender, EventArgs e)
        {
            var iconSize = UpdateThumbnailSize(); // Get new icon size

            // Prevents reloading icons when the icon size hasn't changed
            if (iconSize != _iconSize)
            {
                _iconSize = iconSize; // Update icon size before refreshing
                ParentShellPageInstance.Refresh_Click(); // Refresh icons
            }
            else
                _iconSize = iconSize; // Update icon size
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            if (this.IsLoaded)
            {
                while (ColumnBladeView.Items.Count > 1)
                {
                    try
                    {
                        ColumnBladeView.Items.RemoveAt(1);
                        ColumnBladeView.ActiveBlades.RemoveAt(1);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }
        private async void FileList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            // Skip code if the control or shift key is pressed
            if (ctrlPressed || shiftPressed)
            {
                return;
            }

            // Check if the setting to open items with a single click is turned on
            if (AppSettings.OpenItemsWithOneclick)
            {
                await Task.Delay(200); // The delay gives time for the item to be selected
                ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
            }
        }
        public async Task<ListedItem> AddFolderAsync(StorageFolder folder, string dateReturnFormat)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            var item = new ListedItem(folder.FolderRelativeId, dateReturnFormat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemName = folder.Name,
                ItemDateModifiedReal = basicProperties.DateModified,
                ItemType = folder.DisplayType,
                IsHiddenItem = false,
                Opacity = 1,
                LoadFolderGlyph = true,
                FileImage = null,
                LoadFileIcon = false,
                ItemPath = folder.Path,
                LoadUnknownTypeGlyph = false,
                FileSize = null,
                FileSizeBytes = 0
                //FolderTooltipText = tooltipString,
            };
            return item;
        }

        public async Task<ListedItem> AddFileAsync(StorageFile file, string dateReturnFormat, bool suppressThumbnailLoading = false)
        {
            var shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;
            ListedItem fileitem = null;
            var basicProperties = await file.GetBasicPropertiesAsync();

            // Display name does not include extension
            var itemName = string.IsNullOrEmpty(file.DisplayName) || shouldDisplayFileExtensions ?
                file.Name : file.DisplayName;
            var itemDate = basicProperties.DateModified;
            var itemPath = file.Path;
            var itemSize = ByteSize.FromBytes(basicProperties.Size).ToBinaryString().ConvertSizeAbbreviation();
            var itemSizeBytes = basicProperties.Size;
            var itemType = file.DisplayType;
            var itemFolderImgVis = false;
            var itemFileExtension = file.FileType;

            BitmapImage icon = new BitmapImage();
            bool itemThumbnailImgVis;
            bool itemEmptyImgVis;

            try
            {
                var itemThumbnailImg = suppressThumbnailLoading ? null :
                    await file.GetThumbnailAsync(ThumbnailMode.ListView, 80, ThumbnailOptions.UseCurrentScale);
                if (itemThumbnailImg != null)
                {
                    itemEmptyImgVis = false;
                    itemThumbnailImgVis = true;
                    icon.DecodePixelWidth = 80;
                    icon.DecodePixelHeight = 80;
                    await icon.SetSourceAsync(itemThumbnailImg);
                }
                else
                {
                    itemEmptyImgVis = true;
                    itemThumbnailImgVis = false;
                }
            }
            catch
            {
                itemEmptyImgVis = true;
                itemThumbnailImgVis = false;
            }

            if (file.Name.EndsWith(".lnk") || file.Name.EndsWith(".url"))
            {
                // This shouldn't happen, StorageFile api does not support shortcuts
                Debug.WriteLine("Something strange: StorageFile api returned a shortcut");
            }
            else
            {
                fileitem = new ListedItem(file.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    IsHiddenItem = false,
                    Opacity = 1,
                    LoadUnknownTypeGlyph = itemEmptyImgVis,
                    FileImage = icon,
                    LoadFileIcon = itemThumbnailImgVis,
                    LoadFolderGlyph = itemFolderImgVis,
                    ItemName = itemName,
                    ItemDateModifiedReal = itemDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = (long)itemSizeBytes
                };
            }
            return fileitem;
        }
        private async Task EnumFromStorageFolderAsync(StorageFolder _rootFolder)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
            var shouldDisplayFileExtensions = App.AppSettings.ShowFileExtensions;

            uint count = 0;
            while (true)
            {
                IStorageItem item = null;
                try
                {
                    var results = await _rootFolder.GetItemsAsync(count, 1);
                    item = results?.FirstOrDefault();
                    if (item == null)
                    {
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    ++count;
                    continue;
                }
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    await AddFolderAsync(item as StorageFolder, returnformat);
                    ++count;
                }
                else
                {
                    var file = item as StorageFile;
                    await AddFileAsync(file, returnformat, true);
                    ++count;
                }
                //if (_addFilesCTS.IsCancellationRequested)
                //{
                //    break;
                //}
                //if (count % 300 == 0)
                //{
                //    OrderFiles();
                //}
            }
        }
        private async void FirstBlade_ItemClick(object sender, ItemClickEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            // Skip code if the control or shift key is pressed
            if (ctrlPressed || shiftPressed)
            {
                return;
            }
            if (AppSettings.OpenItemsWithOneclick)
            {
                SelectedItems.Clear();
                var lvi = ((FrameworkElement)e.OriginalSource) as ListView;
                var Blade = lvi.FindAscendant<BladeItem>();
                var index = ColumnBladeView.Items.IndexOf(Blade);
                while (ColumnBladeView.Items.Count > index + 1)
                {
                    try
                    {
                        ColumnBladeView.Items.RemoveAt(index + 1);
                        ColumnBladeView.ActiveBlades.RemoveAt(index + 1);
                    }
                    catch
                    {
                        break;
                    }
                }
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                
                var ClickedItem = e.ClickedItem as ListedItem;
                if (ClickedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !ClickedItem.ItemPath.Contains("$Recycle.Bin"))
                {
                    
                    var collection = new List<ListedItem>();
                    var filelist = new List<ListedItem>();
                    var folderlist = new List<ListedItem>();
                    var folder = await StorageFolder.GetFolderFromPathAsync(ClickedItem.ItemPath);
                    var items = await folder.GetItemsAsync();
                    foreach (var item in items)
                    {
                        if (item.IsOfType(StorageItemTypes.File))
                        {
                            var it = await AddFileAsync(item as StorageFile, returnformat, true);
                            if (it != null)
                            { 
                                filelist.Add(it); 
                            }
                        }
                        else if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            var it = await AddFolderAsync(item as StorageFolder, returnformat);
                            folderlist.Add(it);
                        }
                    }
                    collection.AddRange(folderlist.OrderBy(x => x.ItemName));
                    collection.AddRange(filelist.OrderBy(x => x.ItemName));
                    var observable = new ObservableCollection<ListedItem>(collection);
                    var collection2 = new ReadOnlyObservableCollection<ListedItem>(observable);
                    ParentShellPageInstance.InteractionOperations.Prepare(ClickedItem.ItemPath);
                    var lv = new ListView
                    {
                        Padding = new Thickness(5),
                        ItemsSource = collection2,
                        ItemTemplate = ListTemplate,
                        IsItemClickEnabled = true,
                        ItemContainerStyle = DefaultListView
                    };
                    lv.IsDoubleTapEnabled = true;
                    lv.DoubleTapped += FirstBlade_DoubleTapped;
                    lv.ItemClick += FirstBlade_ItemClick;
                    ColumnBladeView.Items.Add(new BladeItem
                    {
                        Content = lv,
                        Style = BladeView
                    });
                    lvi.ItemContainerStyle = NotCurentListView;
                }
                else if (ClickedItem.PrimaryItemAttribute == StorageItemTypes.File && !ClickedItem.ItemPath.Contains("$Recycle.Bin"))
                {
                    lvi.ItemContainerStyle = DefaultListView;
                    var list = new List<ListedItem>();
                    list.Add(ClickedItem);
                    SelectedItems = list.ToList();
                    await Task.Delay(200); // The delay gives time for the item to be selected
                    ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                }
            }
        }

        private async void FirstBlade_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is ListedItem)
            {
                if (!AppSettings.OpenItemsWithOneclick)
                {
                    var lvi = sender as ListView;
                    var Blade = lvi.FindAscendant<BladeItem>();
                    var index = ColumnBladeView.Items.IndexOf(Blade);
                    while (ColumnBladeView.Items.Count > index + 1)
                    {
                        try
                        {
                            ColumnBladeView.Items.RemoveAt(index + 1);
                            ColumnBladeView.ActiveBlades.RemoveAt(index + 1);
                        }
                        catch
                        {
                            break;
                        }
                    }
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                    var item = ((FrameworkElement)e.OriginalSource).DataContext as ListedItem;
                    if (item.PrimaryItemAttribute == StorageItemTypes.File && !item.ItemPath.Contains("$Recycle.Bin"))
                    {
                        var list = new List<ListedItem>();
                        list.Add(item);
                        SelectedItems = list.ToList();
                        await Task.Delay(200); // The delay gives time for the item to be selected
                        ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                        lvi.ItemContainerStyle = DefaultListView;
                    }
                    else if (item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !item.ItemPath.Contains("$Recycle.Bin"))
                    {
                       
                        var collection = new List<ListedItem>();
                        var filelist = new List<ListedItem>();
                        var folderlist = new List<ListedItem>();
                        var folder = await StorageFolder.GetFolderFromPathAsync(item.ItemPath);
                        var items = await folder.GetItemsAsync();
                        foreach (var item2 in items)
                        {
                            if (item2.IsOfType(StorageItemTypes.File))
                            {
                                var it = await AddFileAsync(item2 as StorageFile, returnformat, true);
                                if (it != null)
                                {
                                    filelist.Add(it);
                                }
                            }
                            else if (item2.IsOfType(StorageItemTypes.Folder))
                            {
                                var it = await AddFolderAsync(item2 as StorageFolder, returnformat);
                                folderlist.Add(it);
                            }
                        }
                        collection.AddRange(folderlist.OrderBy(x => x.ItemName));
                        collection.AddRange(filelist.OrderBy(x => x.ItemName));
                        var observable = new ObservableCollection<ListedItem>(collection);
                        var collection2 = new ReadOnlyObservableCollection<ListedItem>(observable);
                        ParentShellPageInstance.InteractionOperations.Prepare(item.ItemPath);
                        var lv = new ListView
                        {
                            Padding = new Thickness(5),
                            ItemsSource = collection2,
                            ItemTemplate = ListTemplate,
                            IsItemClickEnabled = true,
                            ItemContainerStyle = DefaultListView
                        };
                        lv.IsDoubleTapEnabled = true;
                        lv.DoubleTapped += FirstBlade_DoubleTapped;
                        lv.ItemClick += FirstBlade_ItemClick;
                        ColumnBladeView.Items.Add(new BladeItem
                        {
                            Content = lv,
                            Style = BladeView
                        });
                        lvi.ItemContainerStyle = NotCurentListView;
                    }
                }
            }
        }

        private void FirstBlade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItems = e.AddedItems.Cast<ListedItem>().ToList();
        }
    }
}