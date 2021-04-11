using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.Interacts;
using Files.UserControls.Selection;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files.Views.LayoutModes
{
    public sealed partial class GenericFileBrowser : BaseLayout
    {
        private string oldItemName;
        private DataGridColumn sortedColumn;

        private static readonly MethodInfo SelectAllMethod = typeof(DataGrid)
            .GetMethod("SelectAll", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

        public DataGridColumn SortedColumn
        {
            get
            {
                return sortedColumn;
            }
            set
            {
                if (value == nameColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.Name;
                }
                else if (value == dateColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.DateModified;
                }
                else if (value == typeColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.FileType;
                }
                else if (value == sizeColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.Size;
                }
                else if (value == originalPathColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.OriginalPath;
                }
                else if (value == dateDeletedColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.DateDeleted;
                }
                else if (value == dateCreatedColumn)
                {
                    FolderSettings.DirectorySortOption = SortOption.DateCreated;
                }
                else
                {
                    FolderSettings.DirectorySortOption = SortOption.Name;
                }

                if (value != sortedColumn)
                {
                    // Remove arrow on previous sorted column
                    if (sortedColumn != null)
                    {
                        sortedColumn.SortDirection = null;
                    }
                }
                value.SortDirection = FolderSettings.DirectorySortDirection == SortDirection.Ascending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
                sortedColumn = value;
            }
        }

        private DispatcherQueueTimer tapDebounceTimer;

        public GenericFileBrowser()
            : base()
        {
            InitializeComponent();

            var selectionRectangle = RectangleSelection.Create(AllView, SelectionRectangle, AllView_SelectionChanged);
            selectionRectangle.SelectionStarted += SelectionRectangle_SelectionStarted;
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
            AllView.PointerCaptureLost += AllView_ItemPress;

            tapDebounceTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        }

        protected override void HookEvents()
        {
            UnhookEvents();
            ItemManipulationModel.FocusFileListInvoked += ItemManipulationModel_FocusFileListInvoked;
            ItemManipulationModel.SelectAllItemsInvoked += ItemManipulationModel_SelectAllItemsInvoked;
            ItemManipulationModel.ClearSelectionInvoked += ItemManipulationModel_ClearSelectionInvoked;
            ItemManipulationModel.InvertSelectionInvoked += ItemManipulationModel_InvertSelectionInvoked;
            ItemManipulationModel.AddSelectedItemInvoked += ItemManipulationModel_AddSelectedItemInvoked;
            ItemManipulationModel.FocusSelectedItemsInvoked += ItemManipulationModel_FocusSelectedItemsInvoked;
            ItemManipulationModel.StartRenameItemInvoked += ItemManipulationModel_StartRenameItemInvoked;
            ItemManipulationModel.ScrollIntoViewInvoked += ItemManipulationModel_ScrollIntoViewInvoked;
            ItemManipulationModel.SetDragModeForItemsInvoked += ItemManipulationModel_SetDragModeForItemsInvoked;
            ItemManipulationModel.RefreshItemsOpacityInvoked += ItemManipulationModel_RefreshItemsOpacityInvoked;
        }

        private void ItemManipulationModel_RefreshItemsOpacityInvoked(object sender, EventArgs e)
        {
            foreach (ListedItem listedItem in (IEnumerable)AllView.ItemsSource)
            {
                if (listedItem.IsHiddenItem)
                {
                    listedItem.Opacity = Constants.UI.DimItemOpacity;
                }
                else
                {
                    listedItem.Opacity = 1;
                }
            }
        }

        private void ItemManipulationModel_SetDragModeForItemsInvoked(object sender, EventArgs e)
        {
            if (IsItemSelected && !InstanceViewModel.IsPageTypeSearchResults)
            {
                var rows = new List<DataGridRow>();
                DependencyObjectHelpers.FindChildren<DataGridRow>(rows, AllView);

                foreach (DataGridRow row in rows)
                {
                    row.CanDrag = SelectedItems.Contains(row.DataContext);
                }
            }
        }

        private void ItemManipulationModel_ScrollIntoViewInvoked(object sender, ListedItem e)
        {
            try
            {
                AllView.ScrollIntoView(e, null);
            }
            catch (Exception)
            {
                // Catch error where row index could not be found
            }
        }

        private void ItemManipulationModel_StartRenameItemInvoked(object sender, EventArgs e)
        {
            try
            {
                if (IsItemSelected)
                {
                    AllView.CurrentColumn = AllView.Columns[1];
                    AllView.BeginEdit();
                }
            }
            catch (InvalidOperationException)
            {
                // System.InvalidOperationException: There is no current row. Operation cannot be completed.
            }
        }

        private void ItemManipulationModel_FocusSelectedItemsInvoked(object sender, EventArgs e)
        {
            if (SelectedItems.Any())
            {
                AllView.ScrollIntoView(SelectedItems.Last(), null);
            }
        }

        private void ItemManipulationModel_AddSelectedItemInvoked(object sender, ListedItem e)
        {
            if (((IList<ListedItem>)AllView?.ItemsSource)?.Contains(e) ?? false)
            {
                AllView.SelectedItems.Add(e);
            }
        }

        private void ItemManipulationModel_InvertSelectionInvoked(object sender, EventArgs e)
        {
            List<ListedItem> newSelectedItems = GetAllItems()
                .Cast<ListedItem>()
                .Except(SelectedItems)
                .ToList();

            ItemManipulationModel.SetSelectedItems(newSelectedItems);
        }

        private void ItemManipulationModel_ClearSelectionInvoked(object sender, EventArgs e)
        {
            AllView.SelectedItems.Clear();
        }

        private void ItemManipulationModel_SelectAllItemsInvoked(object sender, EventArgs e)
        {
            SelectAllMethod.Invoke(AllView, null);
        }

        private void ItemManipulationModel_FocusFileListInvoked(object sender, EventArgs e)
        {
            AllView.Focus(FocusState.Programmatic);
        }

        protected override void UnhookEvents()
        {
            if (ItemManipulationModel != null)
            {
                ItemManipulationModel.FocusFileListInvoked -= ItemManipulationModel_FocusFileListInvoked;
                ItemManipulationModel.SelectAllItemsInvoked -= ItemManipulationModel_SelectAllItemsInvoked;
                ItemManipulationModel.ClearSelectionInvoked -= ItemManipulationModel_ClearSelectionInvoked;
                ItemManipulationModel.InvertSelectionInvoked -= ItemManipulationModel_InvertSelectionInvoked;
                ItemManipulationModel.AddSelectedItemInvoked -= ItemManipulationModel_AddSelectedItemInvoked;
                ItemManipulationModel.FocusSelectedItemsInvoked -= ItemManipulationModel_FocusSelectedItemsInvoked;
                ItemManipulationModel.StartRenameItemInvoked -= ItemManipulationModel_StartRenameItemInvoked;
                ItemManipulationModel.ScrollIntoViewInvoked -= ItemManipulationModel_ScrollIntoViewInvoked;
                ItemManipulationModel.SetDragModeForItemsInvoked -= ItemManipulationModel_SetDragModeForItemsInvoked;
                ItemManipulationModel.RefreshItemsOpacityInvoked -= ItemManipulationModel_RefreshItemsOpacityInvoked;
            }
        }

        protected override void InitializeCommandsViewModel()
        {
            CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
        }

        private void SelectionRectangle_SelectionStarted(object sender, EventArgs e)
        {
            // If drag selection is active do not trigger file open on pointer release
            AllView.PointerCaptureLost -= AllView_ItemPress;
        }

        private void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            // Restore file open on pointer release
            AllView.PointerCaptureLost += AllView_ItemPress;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            ParentShellPageInstance.FilesystemViewModel.PropertyChanged += ViewModel_PropertyChanged;
            AllView.LoadingRow += AllView_LoadingRow;
            AllView.UnloadingRow += AllView_UnloadingRow;
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
            if (AllView.ItemsSource == null)
            {
                AllView.ItemsSource = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders;
            }
            ViewModel_PropertyChanged(null, new PropertyChangedEventArgs("DirectorySortOption"));
            var parameters = (NavigationArguments)eventArgs.Parameter;
            if (parameters.IsLayoutSwitch)
            {
                ReloadItemIcons();
            }
        }

        private void AllView_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            base.UninitializeDrag(e.Row);
        }

        private async void ReloadItemIcons()
        {
            var rows = new List<DataGridRow>();
            DependencyObjectHelpers.FindChildren<DataGridRow>(rows, AllView);
            ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
            foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
            {
                listedItem.ItemPropertiesInitialized = false;
                if (rows.Any(x => x.DataContext == listedItem))
                {
                    listedItem.ItemPropertiesInitialized = true;
                    await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem);
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            ParentShellPageInstance.FilesystemViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            AllView.LoadingRow -= AllView_LoadingRow;
            AllView.UnloadingRow -= AllView_UnloadingRow;
            AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
            if (e.SourcePageType != typeof(GenericFileBrowser))
            {
                AllView.ItemsSource = null;
            }
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
        }

        protected override IEnumerable GetAllItems()
        {
            return AllView.ItemsSource;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DirectorySortOption")
            {
                switch (FolderSettings.DirectorySortOption)
                {
                    case SortOption.Name:
                        SortedColumn = nameColumn;
                        break;

                    case SortOption.DateModified:
                        SortedColumn = dateColumn;
                        break;

                    case SortOption.FileType:
                        SortedColumn = typeColumn;
                        break;

                    case SortOption.Size:
                        SortedColumn = sizeColumn;
                        break;

                    case SortOption.OriginalPath:
                        SortedColumn = originalPathColumn;
                        break;

                    case SortOption.DateDeleted:
                        SortedColumn = dateDeletedColumn;
                        break;

                    case SortOption.DateCreated:
                        SortedColumn = dateCreatedColumn;
                        break;
                }
            }
            else if (e.PropertyName == "DirectorySortDirection")
            {
                // Swap arrows
                SortedColumn = sortedColumn;
            }
        }

        private TextBox renamingTextBox;

        private void AllView_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (ParentShellPageInstance.FilesystemViewModel.WorkingDirectory.StartsWith(AppSettings.RecycleBinPath))
            {
                // Do not rename files and folders inside the recycle bin
                e.Cancel = true;
                return;
            }

            if (e.EditingEventArgs is TappedRoutedEventArgs)
            {
                // A tap should never trigger an immediate edit
                e.Cancel = true;

                if (AppSettings.OpenItemsWithOneclick || tapDebounceTimer.IsRunning)
                {
                    // If we handle a tap in one-click mode or handling a second tap within a timer duration,
                    // just stop the timer (to avoid extra edits).
                    // The relevant handlers (item pressed / double-click) will kick in and handle this tap
                    tapDebounceTimer.Stop();
                }
                else
                {
                    // We have an edit due to the first tap in the double-click mode
                    // Let's wait to see if there is another tap (double click).

                    tapDebounceTimer.Debounce(() =>
                    {
                        tapDebounceTimer.Stop();

                        AllView.BeginEdit();
                    }, TimeSpan.FromMilliseconds(700), false);
                }
            }
            else
            {
                // If we got here, then the edit is not triggered by tap.
                // We proceed with the edit, and stop the timer to avoid extra edits.
                tapDebounceTimer.Stop();
            }
        }

        private void AllView_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (SelectedItem == null)
            {
                return;
            }
            int extensionLength = SelectedItem.FileExtension?.Length ?? 0;
            oldItemName = SelectedItem.ItemName;

            renamingTextBox = e.EditingElement as TextBox;
            renamingTextBox.Focus(FocusState.Programmatic); // Without this,the user cannot edit the text box when renaming via right-click

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (!SelectedItem.IsShortcutItem && AppSettings.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            renamingTextBox.Select(0, selectedTextLength);
            renamingTextBox.TextChanged += TextBox_TextChanged;
            e.EditingElement.LosingFocus += EditingElement_LosingFocus;
            IsRenamingItem = true;
        }

        private void EditingElement_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.NewFocusedElement is Popup || args.NewFocusedElement is AppBarButton)
            {
                args.Cancel = true;
                args.TryCancel();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
            {
                FileNameTeachingTip.Visibility = Visibility.Visible;
                FileNameTeachingTip.IsOpen = true;
            }
            else
            {
                FileNameTeachingTip.IsOpen = false;
                FileNameTeachingTip.Visibility = Visibility.Collapsed;
            }
        }

        private async void AllView_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            e.EditingElement.LosingFocus -= EditingElement_LosingFocus;
            if (e.EditAction == DataGridEditAction.Cancel || renamingTextBox == null)
            {
                return;
            }

            renamingTextBox.Text = renamingTextBox.Text.Trim().TrimEnd('.');

            var selectedItem = e.Row.DataContext as ListedItem;
            string newItemName = renamingTextBox.Text;

            bool successful = await UIFilesystemHelpers.RenameFileItemAsync(selectedItem, oldItemName, newItemName, ParentShellPageInstance);
            if (!successful)
            {
                selectedItem.ItemName = oldItemName;
                renamingTextBox.Text = oldItemName;
            }
        }

        private void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            if (renamingTextBox != null)
            {
                renamingTextBox.TextChanged -= TextBox_TextChanged;
            }
            FileNameTeachingTip.IsOpen = false;
            IsRenamingItem = false;
        }

        private async void AllView_ItemPress(object sender, PointerRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            var cp = e.GetCurrentPoint((UIElement)sender);
            if (cp.Position.Y <= AllView.ColumnHeaderHeight // Return if click is on the header
                || cp.Properties.IsLeftButtonPressed // Return if dragging an item
                || cp.Properties.IsRightButtonPressed // Return if the user right clicks an item
                || ctrlPressed || shiftPressed) // Allow for Ctrl+Shift selection
            {
                return;
            }
            if (IsRenamingItem)
            {
                return;
            }

            // Check if the setting to open items with a single click is turned on
            if (AppSettings.OpenItemsWithOneclick)
            {
                tapDebounceTimer.Stop();
                await Task.Delay(200); // The delay gives time for the item to be selected
                NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
            }
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null)
            {
                // Do not commit rename if SelectionChanged is due to selction rectangle (#3660)
                AllView.CommitEdit();
            }
            tapDebounceTimer.Stop();
            SelectedItems = AllView.SelectedItems.Cast<ListedItem>().Where(x => x != null).ToList();
        }

        private void AllView_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column == SortedColumn)
            {
                ParentShellPageInstance.FilesystemViewModel.IsSortedAscending = !ParentShellPageInstance.FilesystemViewModel.IsSortedAscending;
                e.Column.SortDirection = ParentShellPageInstance.FilesystemViewModel.IsSortedAscending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
            }
            else if (e.Column != iconColumn)
            {
                SortedColumn = e.Column;
                e.Column.SortDirection = DataGridSortDirection.Ascending;
                ParentShellPageInstance.FilesystemViewModel.IsSortedAscending = true;
            }
        }

        private void AllView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (IsRenamingItem)
                {
                    AllView.CommitEdit();
                }
                else
                {
                    NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                }
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
                e.Handled = true;
            }
            else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
            {
                // Unfocus the ListView so keyboard shortcut can be handled
                (ParentShellPageInstance.NavigationToolbar as Control)?.Focus(FocusState.Pointer);
            }
            else if (ctrlPressed && shiftPressed && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.W))
            {
                // Unfocus the ListView so keyboard shortcut can be handled (ctrl + shift + W/"->"/"<-")
                (ParentShellPageInstance.NavigationToolbar as Control)?.Focus(FocusState.Pointer);
            }
            else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
            {
                // Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
                (ParentShellPageInstance.NavigationToolbar as Control)?.Focus(FocusState.Pointer);
            }
        }

        public void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (!IsRenamingItem)
            {
                HandleRightClick(sender, e);
            }
        }

        public void AllView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            HandleRightClick(sender, e);
        }

        private void HandleRightClick(object sender, RoutedEventArgs e)
        {
            var rowPressed = DependencyObjectHelpers.FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            var objectPressed = rowPressed == null ? null : ((IList<ListedItem>)AllView.ItemsSource).ElementAtOrDefault(rowPressed.GetIndex());

            if (rowPressed == null || objectPressed == null)
            {
                // Clear the selection if no item was clicked
                ItemManipulationModel.ClearSelection();
            }
            else if (!IsItemSelected || !SelectedItems.Contains(objectPressed))
            {
                // Select item if it is not already selected
                ItemManipulationModel.SetSelectedItem(objectPressed);
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance != null)
            {
                if (ParentShellPageInstance.CurrentPageType == typeof(GenericFileBrowser))
                {
                    // Don't block the various uses of enter key (key 13)
                    var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
                    if (args.KeyCode == 13 || focusedElement is Button || focusedElement is TextBox || focusedElement is PasswordBox ||
                        DependencyObjectHelpers.FindParent<ContentDialog>(focusedElement) != null)
                    {
                        return;
                    }

                    base.Page_CharacterReceived(sender, args);
                    AllView.Focus(FocusState.Keyboard);
                }
            }
        }

        private async void AllView_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.CanDrag = false;
            InitializeDrag(e.Row);

            if (e.Row.DataContext is ListedItem item && !item.ItemPropertiesInitialized)
            {
                item.ItemPropertiesInitialized = true;
                await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(item);
            }
            e.Row.ContextFlyout = ItemContextMenuFlyout;
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            DataGridRow row = element as DataGridRow;
            return row.DataContext as ListedItem;
        }

        private void RadioMenuSortColumn_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumnEventArgs args = null;

            switch ((sender as RadioMenuFlyoutItem).Tag)
            {
                case "nameColumn":
                    args = new DataGridColumnEventArgs(nameColumn);
                    break;

                case "dateColumn":
                    args = new DataGridColumnEventArgs(dateColumn);
                    break;

                case "typeColumn":
                    args = new DataGridColumnEventArgs(typeColumn);
                    break;

                case "sizeColumn":
                    args = new DataGridColumnEventArgs(sizeColumn);
                    break;

                case "originalPathColumn":
                    args = new DataGridColumnEventArgs(originalPathColumn);
                    break;

                case "dateDeletedColumn":
                    args = new DataGridColumnEventArgs(dateDeletedColumn);
                    break;

                case "dateCreatedColumn":
                    args = new DataGridColumnEventArgs(dateCreatedColumn);
                    break;
            }

            if (args != null)
            {
                AllView_Sorting(sender, args);
            }
        }

        private void RadioMenuSortDirection_Click(object sender, RoutedEventArgs e)
        {
            if (((sender as RadioMenuFlyoutItem).Tag as string) == "sortAscending")
            {
                SortedColumn.SortDirection = DataGridSortDirection.Ascending;
            }
            else if (((sender as RadioMenuFlyoutItem).Tag as string) == "sortDescending")
            {
                SortedColumn.SortDirection = DataGridSortDirection.Descending;
            }
        }

        private void AllView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            tapDebounceTimer.Stop();
            NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
        }

        #region IDisposable

        public override void Dispose()
        {
            UnhookEvents();
            CommandsViewModel?.Dispose();
        }

        #endregion IDisposable
    }
}