using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Views.Pages;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class GenericFileBrowser : BaseLayout
    {
        public string previousFileName;
        private DataGridColumn _sortedColumn;

        public DataGridColumn SortedColumn
        {
            get
            {
                return _sortedColumn;
            }
            set
            {
                if (value == nameColumn)
                    App.CurrentInstance.ViewModel.DirectorySortOption = SortOption.Name;
                else if (value == dateColumn)
                    App.CurrentInstance.ViewModel.DirectorySortOption = SortOption.DateModified;
                else if (value == typeColumn)
                    App.CurrentInstance.ViewModel.DirectorySortOption = SortOption.FileType;
                else if (value == sizeColumn)
                    App.CurrentInstance.ViewModel.DirectorySortOption = SortOption.Size;
                else
                    App.CurrentInstance.ViewModel.DirectorySortOption = SortOption.Name;

                if (value != _sortedColumn)
                {
                    // Remove arrow on previous sorted column
                    if (_sortedColumn != null)
                        _sortedColumn.SortDirection = null;
                }
                value.SortDirection = App.CurrentInstance.ViewModel.DirectorySortDirection == SortDirection.Ascending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
                _sortedColumn = value;
            }
        }

        public GenericFileBrowser()
        {
            InitializeComponent();

            switch (App.CurrentInstance.ViewModel.DirectorySortOption)
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

            App.AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
        }
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            App.CurrentInstance.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            App.CurrentInstance.ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
        }

        public override void SetSelectedItemOnUi(ListedItem item)
        {
            ClearSelection();
            AllView.SelectedItems.Add(item);
        }

        public override void SetSelectedItemsOnUi(List<ListedItem> items)
        {
            ClearSelection();

            foreach (ListedItem item in items)
            {
                AllView.SelectedItems.Add(item);
            }
        }

        public override void SelectAllItems()
        {
            SetSelectedItemsOnUi(AssociatedViewModel.FilesAndFolders.ToList());
        }

        public override void InvertSelection()
        {
            List<ListedItem> allItems = AssociatedViewModel.FilesAndFolders.ToList();
            List<ListedItem> newSelectedItems = allItems.Except(SelectedItems).ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }
        
        public override void ClearSelection()
        {
            AllView.SelectedItems.Clear();
        }

        public override void SetDragModeForItems()
        {
            if (IsItemSelected)
            {
                var rows = new List<DataGridRow>();
                Interacts.Interaction.FindChildren<DataGridRow>(rows, AllView);
                foreach (DataGridRow row in rows)
                {
                    row.CanDrag = SelectedItems.Contains(row.DataContext);
                }
            }
        }

        public override void ScrollIntoView(ListedItem item)
        {
            AllView.ScrollIntoView(item, null);
        }

        public override int GetSelectedIndex()
        {
            return AllView.SelectedIndex;
        }

        public override void FocusSelectedItems()
        {
            AllView.ScrollIntoView(AllView.ItemsSource.Cast<ListedItem>().Last(), null);
        }

        public override void StartRenameItem()
        {
            AllView.CurrentColumn = AllView.Columns[1];
            AllView.BeginEdit();
        }

        public override void ResetItemOpacity()
        {
            IEnumerable items = AllView.ItemsSource;
            if (items == null)
            {
                return;
            }

            foreach (ListedItem listedItem in items)
            {
                FrameworkElement element = AllView.Columns[0].GetCellContent(listedItem);
                if (element != null)
                {
                    element.Opacity = 1;
                }
            }
        }

        public override void SetItemOpacity(ListedItem item)
        {
            AllView.Columns[0].GetCellContent(item).Opacity = 0.4;
        }

        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DirectorySortOption")
            {
                switch (App.CurrentInstance.ViewModel.DirectorySortOption)
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
                                App.CurrentInstance.ViewModel.LoadExtendedItemProperties(row.DataContext as ListedItem);
                                (row.DataContext as ListedItem).ItemPropertiesInitialized = true;
                            });
                        }
                    }
                }
            }
        }

        private void AllView_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (App.CurrentInstance.ViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                // Do not rename files and folders inside the recycle bin
                AllView.CancelEdit(); // cancel the edit operation
                return;
            }

            // Check if the double tap to rename files setting is off
            if (App.AppSettings.DoubleTapToRenameFiles == false)
            {
                AllView.CancelEdit(); // cancel the edit operation
                App.CurrentInstance.InteractionOperations.OpenItem_Click(null, null); // open the file instead
                return;
            }

            var textBox = e.EditingElement as TextBox;
            var selectedItem = SelectedItem;
            int extensionLength = selectedItem.FileExtension?.Length ?? 0;

            previousFileName = selectedItem.ItemName;
            textBox.Focus(FocusState.Programmatic); // Without this, cannot edit text box when renaming via right-click
            textBox.Select(0, selectedItem.ItemName.Length - extensionLength);
            isRenamingItem = true;
        }

        private async void AllView_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            var selectedItem = e.Row.DataContext as ListedItem;
            string currentName = previousFileName;
            string newName = (e.EditingElement as TextBox).Text;

            bool successful = await App.CurrentInstance.InteractionOperations.RenameFileItem(selectedItem, currentName, newName);
            if (!successful)
            {
                selectedItem.ItemName = currentName;
                ((sender as DataGrid).Columns[1].GetCellContent(e.Row) as TextBlock).Text = currentName;
            }
        }

        private void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            isRenamingItem = false;
        }

        private void GenericItemView_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ClearSelection();
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
            SelectedItems = AllView.SelectedItems.Cast<ListedItem>().ToList();
        }

        private async void AllView_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column == SortedColumn)
                App.CurrentInstance.ViewModel.IsSortedAscending = !App.CurrentInstance.ViewModel.IsSortedAscending;
            else if (e.Column != iconColumn)
                SortedColumn = e.Column;

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
                            App.CurrentInstance.ViewModel.LoadExtendedItemProperties(row.DataContext as ListedItem);
                            (row.DataContext as ListedItem).ItemPropertiesInitialized = true;
                        });
                    }
                }
            }
        }

        private void AllView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (isRenamingItem)
                {
                    AllView.CommitEdit();
                }
                else
                {
                    App.CurrentInstance.InteractionOperations.List_ItemClick(null, null);
                }
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                AssociatedInteractions.ShowPropertiesButton_Click(null, null);
            }
        }

        public void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var rowPressed = Interacts.Interaction.FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (rowPressed != null)
            {
                var objectPressed = ((ReadOnlyObservableCollection<ListedItem>)AllView.ItemsSource)[rowPressed.GetIndex()];

                // Check if RightTapped row is currently selected
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    if (App.CurrentInstance.ContentPage.SelectedItems.Contains(objectPressed))
                    {
                        return;
                    }
                }

                // The following code is only reachable when a user RightTapped an unselected row
                SetSelectedItemOnUi(objectPressed);
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (App.CurrentInstance != null)
            {
                if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                {
                    var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
                    if (focusedElement is TextBox)
                    {
                        return;
                    }

                    base.Page_CharacterReceived(sender, args);
                    AllView.Focus(FocusState.Keyboard);
                }
            }
        }

        private async void Icon_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            var parentRow = Interacts.Interaction.FindParent<DataGridRow>(sender);
            if ((!(parentRow.DataContext as ListedItem).ItemPropertiesInitialized) && (args.BringIntoViewDistanceX < sender.ActualHeight))
            {
                await Window.Current.CoreWindow.Dispatcher.RunIdleAsync((e) =>
                {
                    App.CurrentInstance.ViewModel.LoadExtendedItemProperties(parentRow.DataContext as ListedItem);
                    (parentRow.DataContext as ListedItem).ItemPropertiesInitialized = true;
                    //sender.EffectiveViewportChanged -= Icon_EffectiveViewportChanged;
                });
            }
        }

        private void AllView_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            InitializeDrag(e.Row);
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            DataGridRow row = element as DataGridRow;
            return row.DataContext as ListedItem;
        }
    }
}