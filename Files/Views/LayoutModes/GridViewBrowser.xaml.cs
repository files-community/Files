using Files.Filesystem;
using Files.UserControls.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Interaction = Files.Interacts.Interaction;

namespace Files
{
    public sealed partial class GridViewBrowser : BaseLayout
    {
        public string oldItemName;

        public GridViewBrowser()
        {
            InitializeComponent();
            base.BaseLayoutContextFlyout = BaseLayoutContextFlyout;
            base.BaseLayoutItemContextFlyout = BaseLayoutItemContextFlyout;

            var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
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
            FileList.ItemTemplate = (App.AppSettings.LayoutMode == 1) ? TilesBrowserTemplate : GridViewBrowserTemplate; // Choose Template

            // Set GridViewSize event handlers
            if (App.AppSettings.LayoutMode == 1)
            {
                App.AppSettings.GridViewSizeChangeRequested -= AppSettings_GridViewSizeChangeRequested;
            }
            else if (App.AppSettings.LayoutMode == 2)
            {
                currentIconSize = GetIconSize(); // Get icon size for jumps from other layouts directly to a grid size
                App.AppSettings.GridViewSizeChangeRequested += AppSettings_GridViewSizeChangeRequested;
            }
        }

        protected override void AddSelectedItem(ListedItem item)
        {
            FileList.SelectedItems.Add(item);
        }

        protected override IEnumerable GetAllItems()
        {
            return FileList.Items;
        }

        public override void SelectAllItems()
        {
            FileList.SelectAll();
        }

        public override void ClearSelection()
        {
            FileList.SelectedItems.Clear();
        }

        public override void SetDragModeForItems()
        {
            if (!InstanceViewModel.IsPageTypeSearchResults)
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
        }

        public override void ScrollIntoView(ListedItem item)
        {
            FileList.ScrollIntoView(item);
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
                Focus(FocusState.Programmatic);
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
                    if (args.KeyCode == 13 
                        || focusedElement is Button 
                        || focusedElement is TextBox 
                        || focusedElement is PasswordBox 
                        || Interaction.FindParent<ContentDialog>(focusedElement) != null)
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
                    ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(item, currentIconSize);
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

        private uint currentIconSize = GetIconSize();

        private static uint GetIconSize()
        {
            if (App.AppSettings.LayoutMode == 1 || App.AppSettings.GridViewSize < Constants.Browser.GridViewBrowser.GridViewSizeSmall + 75)
            {
                return 80; // Small thumbnail
            }
            else if (App.AppSettings.GridViewSize < Constants.Browser.GridViewBrowser.GridViewSizeMedium + 25)
            {
                return 120; // Medium thumbnail
            }
            else if (App.AppSettings.GridViewSize < Constants.Browser.GridViewBrowser.GridViewSizeMedium - 50)
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
            var requestedIconSize = GetIconSize(); // Get new icon size

            // Prevents reloading icons when the icon size hasn't changed
            if (requestedIconSize != currentIconSize)
            {
                currentIconSize = requestedIconSize; // Update icon size before refreshing
                ParentShellPageInstance.Refresh_Click(); // Refresh icons
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
    }
}