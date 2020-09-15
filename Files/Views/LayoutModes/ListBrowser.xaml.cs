using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.Views.LayoutModes
{
    public sealed partial class ListBrowser : BaseLayout
    {
        public ListBrowser()
        {
            InitializeComponent();
            base.BaseLayoutItemContextFlyout = this.BaseLayoutItemContextFlyout;
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
        }

        private void ListViewBrowser_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Page).Properties.IsLeftButtonPressed)
            {
                ClearSelection();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            App.CurrentInstance.FilesystemViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            App.CurrentInstance.FilesystemViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
        }

        private /* async */ void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            /* if (e.PropertyName == "DirectorySortOption")
            {
                switch (AppSettings.DirectorySortOption)
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
                }
            }
            else if (e.PropertyName == "DirectorySortDirection")
            {
                // Swap arrows
                SortedColumn = _sortedColumn;
            }
            else if (e.PropertyName == "IsLoadingItems")
            {
                if (!AssociatedViewModel.IsLoadingItems && AssociatedViewModel.FilesAndFolders.Count > 0)
                {
                    var allRows = new List<DataGridRow>();

                    Interacts.Interaction.FindChildren<DataGridRow>(allRows, AllView);
                    foreach (DataGridRow row in allRows.Take(25))
                    {
                        if (!(row.DataContext as ListedItem).ItemPropertiesInitialized)
                        {
                            await Window.Current.CoreWindow.Dispatcher.RunIdleAsync((e) =>
                            {
                                App.CurrentInstance.FilesystemViewModel.LoadExtendedItemProperties(row.DataContext as ListedItem);
                                (row.DataContext as ListedItem).ItemPropertiesInitialized = true;
                            });
                        }
                    }
                }
            }
            */

        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItems = FileList.SelectedItems.Cast<ListedItem>().ToList();
        }

        public override int GetSelectedIndex() => FileList.SelectedIndex;

        public override void SetItemOpacity(ListedItem item)
        {
            item.IsDimmed = true;
        }

        public override void ClearSelection()
        {
            FileList.SelectedItems.Clear();
        }

        public override void ScrollIntoView(ListedItem item)
        {
            FileList.ScrollIntoView(item);
        }

        public override void FocusSelectedItems()
        {
            FileList.ScrollIntoView(FileList.Items.Last());
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            FrameworkElement gridItem = element as FrameworkElement;
            return gridItem.DataContext as ListedItem;
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
                listedItem.IsDimmed = false;
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

        public override void InvertSelection()
        {
            List<ListedItem> allItems = FileList.Items.Cast<ListedItem>().ToList();
            List<ListedItem> newSelectedItems = allItems.Except(SelectedItems).ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public override void SelectAllItems() => FileList.SelectAll();

        public override void SetDragModeForItems()
        {
            foreach (ListedItem listedItem in FileList.Items)
            {
                ListViewItem gridViewItem = FileList.ContainerFromItem(listedItem) as ListViewItem;

                if (gridViewItem != null)
                {
                    List<Grid> grids = new List<Grid>();
                    Interaction.FindChildren(grids, gridViewItem);
                    var rootItem = grids.Find(x => x.Tag?.ToString() == "ItemRoot");
                    rootItem.CanDrag = SelectedItems.Contains(listedItem);
                }
            }
        }

        public override async void StartRenameItem()
        {
            RenameDialog dialog = new RenameDialog();
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string oldItemName = SelectedItem.ItemName;
                string newItemName = dialog.storedRenameInput.Trim().TrimEnd('.');

                bool successful = await App.CurrentInstance.InteractionOperations.RenameFileItem(SelectedItem, SelectedItem.ItemName, newItemName);
                if (!successful)
                {
                    SelectedItem.ItemName = oldItemName;
                }
            }
        }

        public override void SetShellContextmenu(bool shiftPressed, bool showOpenMenu)
        {
            base.SetShellContextmenu(shiftPressed, showOpenMenu);
        }

        private void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (!isRenamingItem)
                {
                    App.CurrentInstance.InteractionOperations.List_ItemClick(null, null);
                    e.Handled = true;
                }
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                AssociatedInteractions.ShowPropertiesButton_Click(null, null);
            }
            else if (e.Key == VirtualKey.Space)
            {
                if (!isRenamingItem && !App.CurrentInstance.NavigationToolbar.IsEditModeEnabled)
                {
                    if ((App.CurrentInstance.ContentPage).IsQuickLookEnabled)
                    {
                        App.CurrentInstance.InteractionOperations.ToggleQuickLook();
                    }

                    e.Handled = true;
                }
            }
        }

        private void FileListItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) => InitializeDrag(sender as UIElement);

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

        private void FileListItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Page).Properties.IsLeftButtonPressed)
            {
                ClearSelection();
            }
        }
    }
}