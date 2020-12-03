using ByteSizeLib;
using Files.Enums;
using Files.Filesystem;
using Files.Filesystem.Cloud;
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
using System.Threading;
using Files.Common;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
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
using Windows.ApplicationModel;

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
            FirstBlade.Items.VectorChanged += Items_VectorChanged;
            //App.Current.Suspending += Current_Suspending;

            //var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
            //selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
            App.AppSettings.LayoutModeChangeRequested += AppSettings_LayoutModeChangeRequested;

            SetItemTemplate(); // Set ItemTemplate
        }

        private void Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
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
            ListViewToWorkWith = FirstBlade;
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
            FirstBlade.ItemTemplate = ListTemplate;
            ListViewToWorkWith = FirstBlade; // Choose Template

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
            FileList.Visibility = Visibility.Visible;
        }

        public override void SetSelectedItemOnUi(ListedItem item)
        {
            ClearSelection();
            ListViewToWorkWith.SelectedItems.Add(item);
        }

        public override void SetSelectedItemsOnUi(List<ListedItem> items)
        {
            ClearSelection();

            foreach (ListedItem item in items)
            {
                ListViewToWorkWith.SelectedItems.Add(item);
            }
        }

        public override void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            foreach (ListedItem selectedItem in selectedItems)
            {
                ListViewToWorkWith.SelectedItems.Add(selectedItem);
            }
        }

        public override void SelectAllItems()
        {
            ClearSelection();
            ListViewToWorkWith.SelectAll();
        }

        public override void InvertSelection()
        {
            List<ListedItem> allItems = ListViewToWorkWith.Items.Cast<ListedItem>().ToList();
            List<ListedItem> newSelectedItems = allItems.Except(SelectedItems).ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public override void ClearSelection()
        {
            while (ListViewToWorkWith.SelectedItems.Count > 0)
            {
                try
                {
                    ListViewToWorkWith.SelectedItems.RemoveAt(0);
                }
                catch
                {
                    break;
                }
            }
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
                ListViewToWorkWith = FirstBlade;
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

            var syncStatus = await CheckCloudDriveSyncStatusAsync(folder);
            var item = new ListedItem(folder.FolderRelativeId, dateReturnFormat)
            {
                PrimaryItemAttribute = StorageItemTypes.Folder,
                ItemName = folder.Name,
                SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus),
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

            //BitmapImage icon = new BitmapImage();
            bool itemThumbnailImgVis = false;
            bool itemEmptyImgVis = true;

            if (file.Name.EndsWith(".lnk") || file.Name.EndsWith(".url"))
            {
                // This shouldn't happen, StorageFile api does not support shortcuts
                Debug.WriteLine("Something strange: StorageFile api returned a shortcut");
            }
            else
            {
                var syncStatus = await CheckCloudDriveSyncStatusAsync(file);
                fileitem = new ListedItem(file.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus),
                    IsHiddenItem = false,
                    Opacity = 1,
                    LoadUnknownTypeGlyph = itemEmptyImgVis,
                    FileImage = null,
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

            ListViewToWorkWith = sender as ListView;
            var clickedlistviewitem1 = (sender as ListView).ContainerFromItem(e.ClickedItem as ListedItem) as ListViewItem;
            try { clickedlistviewitem1.Style = DefaultListView; } catch { }
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            // Skip code if the control or shift key is pressed
            if (ctrlPressed || shiftPressed)
            {
                return;
            }
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
            if (AppSettings.OpenItemsWithOneclick)
            {
                var ClickedItem = e.ClickedItem as ListedItem;
                if (ClickedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !ClickedItem.ItemPath.Contains("$Recycle.Bin"))
                {
                    if (Cancellation != null)
                    {
                        try
                        {
                            Cancellation.Cancel();
                            Cancellation.Dispose();
                        }
                        catch
                        {

                        }
                    }
                    Cancellation = new CancellationTokenSource();
                    Token = Cancellation.Token;
                    await Task.Factory.StartNew(() => GetFiles(ClickedItem.ItemPath, Token));
                }
                else if (ClickedItem.PrimaryItemAttribute == StorageItemTypes.File && !ClickedItem.ItemPath.Contains("$Recycle.Bin"))
                {
                    var clickedlistviewitem = lvi.ContainerFromItem(ClickedItem) as ListViewItem;
                    clickedlistviewitem.Style = DefaultListView;
                    await Task.Delay(200); // The delay gives time for the item to be selected
                    ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);

                    ListViewToWorkWith = lvi;
                }
            }
        }

        private async Task GetFiles(string itemPath, CancellationToken token)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                App.InteractionViewModel.IsContentLoadingIndicatorVisible = true;
            });

            if (token.IsCancellationRequested)
            {
                App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                return;
            }
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";


            var collection = new List<ListedItem>();
            var filelist = new List<ListedItem>();
            var folderlist = new List<ListedItem>();
            try
            {
                if (token.IsCancellationRequested)
                {
                    App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                    return;
                }
                var folder = await StorageFolder.GetFolderFromPathAsync(itemPath);
                var items = await folder.GetItemsAsync();
                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
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
                    ParentShellPageInstance.InteractionOperations.Prepare(itemPath);

                    if (token.IsCancellationRequested)
                    {
                        App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                        return;
                    }
                    

                    if (token.IsCancellationRequested)
                    {
                        App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                        return;
                    }
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        var lv = new ListView
                        {
                            ItemsSource = collection2,
                            ItemTemplate = ListTemplate,
                            IsItemClickEnabled = true,
                            ItemContainerStyle = DefaultListView
                        };
                        lv.IsDoubleTapEnabled = true;
                        lv.SelectionMode = ListViewSelectionMode.Extended;
                        lv.DoubleTapped += FirstBlade_DoubleTapped;
                        lv.IsRightTapEnabled = true;
                        lv.SelectionChanged += FirstBlade_SelectionChanged;
                        lv.RightTapped += FirstBlade_RightTapped;
                        lv.ItemClick += FirstBlade_ItemClick;
                        ColumnBladeView.Items.Add(new BladeItem
                        {
                            Content = lv,
                            Style = BladeView
                        });

                        lv.Loaded += async (s, e) =>
                        {
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                            {
                                if (token.IsCancellationRequested)
                                {
                                    App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                                    return;
                                }
                                if (lv.Items.Cast<ListedItem>().Any(x => x.PrimaryItemAttribute == StorageItemTypes.File))
                                {
                                    foreach (ListedItem listedItem in lv.Items.Cast<ListedItem>().Where(x => x.PrimaryItemAttribute == StorageItemTypes.File))
                                    {
                                        var Icon = await LoadIcon(listedItem.ItemPath, 24);
                                        if (Icon != null) // Only set folder icon if it's a custom icon
                                        {
                                            listedItem.FileImage = Icon;
                                            listedItem.LoadUnknownTypeGlyph = false;
                                            listedItem.LoadFolderGlyph = false;
                                            listedItem.LoadFileIcon = true;
                                        }
                                    }
                                }
                            });
                            ListViewToWorkWith = lv;
                        };
                    });
                }
            }
            catch
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                    return;
                });
            }
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
            });
        }

        private async Task<BitmapImage> LoadIcon(string itemPath, int v)
        {
            BitmapImage icon = new BitmapImage();
            var file = await StorageFile.GetFileFromPathAsync(itemPath);
            var itemThumbnailImg = await file.GetThumbnailAsync(ThumbnailMode.ListView, (uint)v, ThumbnailOptions.UseCurrentScale);
            if (itemThumbnailImg != null)
            {
                icon.DecodePixelWidth = 80;
                icon.DecodePixelHeight = 80;
                await icon.SetSourceAsync(itemThumbnailImg);
            }
            else
            {
                icon = null;
            }
            return icon;
        }

        private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatusAsync(StorageFolder folder)
        {
            int? syncStatus = null;
            IDictionary<string, object> extraProperties = await folder.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus", "System.FileOfflineAvailabilityStatus" });
            syncStatus = (int?)(uint?)extraProperties["System.FileOfflineAvailabilityStatus"];
            // If no FileOfflineAvailabilityStatus, check FilePlaceholderStatus
            syncStatus = syncStatus ?? (int?)(uint?)extraProperties["System.FilePlaceholderStatus"];
            if (syncStatus == null || !Enum.IsDefined(typeof(CloudDriveSyncStatus), syncStatus))
            {
                return CloudDriveSyncStatus.Unknown;
            }
            return (CloudDriveSyncStatus)syncStatus;
        }
        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Connection?.Dispose();
            Connection = null;
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            Connection?.Dispose();
            Connection = null;
        }
        private async Task<CloudDriveSyncStatus> CheckCloudDriveSyncStatusAsync(StorageFile file)
        {
            int? syncStatus = null;
                IDictionary<string, object> extraProperties = await (file.Properties.RetrievePropertiesAsync(new string[] { "System.FilePlaceholderStatus" }));
                syncStatus = (int?)(uint?)extraProperties["System.FilePlaceholderStatus"];
            
            if (syncStatus == null || !Enum.IsDefined(typeof(CloudDriveSyncStatus), syncStatus))
            {
                return CloudDriveSyncStatus.Unknown;
            }
            return (CloudDriveSyncStatus)syncStatus;
        }
        private async void FirstBlade_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListViewToWorkWith = sender as ListView;
            if (((FrameworkElement)e.OriginalSource).DataContext is ListedItem)
            {
                ListView lv = new ListView();
                var clickedlistviewitem1 = (sender as ListView).ContainerFromItem(((FrameworkElement)e.OriginalSource).DataContext as ListedItem) as ListViewItem;
                try { clickedlistviewitem1.Style = DefaultListView; } catch { }
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

                if (!AppSettings.OpenItemsWithOneclick)
                {
                    var item = ((FrameworkElement)e.OriginalSource).DataContext as ListedItem;
                    if (item.PrimaryItemAttribute == StorageItemTypes.File && !item.ItemPath.Contains("$Recycle.Bin"))
                    {
                        await Task.Delay(200); // The delay gives time for the item to be selected
                        ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                        var clickedlistviewitem = lvi.ContainerFromItem(item) as ListViewItem;
                        clickedlistviewitem.Style = DefaultListView;
                        //lvi.ItemContainerStyle = DefaultListView;
                    }
                    else if (item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !item.ItemPath.Contains("$Recycle.Bin"))
                    {
                        if (Cancellation != null)
                        {
                            try
                            {
                                Cancellation.Cancel();
                                Cancellation.Dispose();
                            }
                            catch
                            {

                            }
                        }
                        Cancellation = new CancellationTokenSource();
                        Token = Cancellation.Token;
                        App.InteractionViewModel.IsContentLoadingIndicatorVisible = true;
                        await Task.Factory.StartNew(() => GetFiles(item.ItemPath, Token));

                        ListViewToWorkWith = lvi;
                        App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                    }
                }
            }
        }

        public AppServiceConnection Connection;
        private CancellationTokenSource Cancellation;
        private CancellationToken Token;
        private ListView ListViewToWorkWith;

        public ItemViewModel FilesystemViewModel { get; }

        
        private void FirstBlade_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var currentLv = sender as ListView;
            ListViewToWorkWith = currentLv;

             var lvs = RootGrid.FindDescendants<ListView>().ToList();
            if (lvs.IndexOf(currentLv) > 0)
            {
                int previous = lvs.IndexOf(currentLv) - 1;
                try
                {
                    var item = lvs[previous].ContainerFromItem(lvs[previous].SelectedItem) as ListViewItem;
                    if (item != null) { item.Style = NotCurentListView; }
                }
                catch
                {

                }
            }
            
            if (e.AddedItems.Count > 0)
            {
                foreach (ListedItem selecteditem in e.AddedItems)
                {
                    var clickedlistviewitem = currentLv.ContainerFromItem(selecteditem) as ListViewItem;
                    try { clickedlistviewitem.Style = DefaultListView; } catch { }
                }
                SelectedItems = e.AddedItems.Cast<ListedItem>().ToList();
            }
        }

        private void FirstBlade_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

            ListViewToWorkWith = sender as ListView;
            if (((FrameworkElement)e.OriginalSource).DataContext is ListedItem)
            {
                var item = ((FrameworkElement)e.OriginalSource).DataContext as ListedItem;
                if (!(sender as ListView).SelectedItems.Contains(item) || (sender as ListView).SelectedItems.Count == 0)
                {
                    while ((sender as ListView).SelectedItems.Count > 0)
                    {
                        try
                        {
                            (sender as ListView).SelectedItems.RemoveAt(0);
                        }
                        catch
                        {
                            break;
                        }
                    }
                    (sender as ListView).SelectedItem = item;
                }
                SelectedItems = (sender as ListView).SelectedItems.Cast<ListedItem>().ToList();
                var clickedlistviewitem = (sender as ListView).ContainerFromItem(item) as ListViewItem;
                clickedlistviewitem.Style = DefaultListView;
            }
            else
            {

            }
        }

    }
}