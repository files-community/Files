using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Enums;
using Files.Filesystem;
using Windows.System;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;

namespace Files
{
    public sealed partial class GenericFileBrowser : BaseLayout
    {
        private bool _isQuickLookEnabled { get; set; } = false;
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
                    App.OccupiedInstance.instanceViewModel.DirectorySortOption = SortOption.Name;
                else if (value == dateColumn)
                    App.OccupiedInstance.instanceViewModel.DirectorySortOption = SortOption.DateModified;
                else if (value == typeColumn)
                    App.OccupiedInstance.instanceViewModel.DirectorySortOption = SortOption.FileType;
                else if (value == sizeColumn)
                    App.OccupiedInstance.instanceViewModel.DirectorySortOption = SortOption.Size;
                else
                    App.OccupiedInstance.instanceViewModel.DirectorySortOption = SortOption.Name;

                if (value != _sortedColumn)
                {
                    // Remove arrow on previous sorted column
                    if (_sortedColumn != null)
                        _sortedColumn.SortDirection = null;
                }
                value.SortDirection = App.OccupiedInstance.instanceViewModel.DirectorySortDirection == SortDirection.Ascending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
                _sortedColumn = value;
            }
        }

        public GenericFileBrowser()
        {
            this.InitializeComponent();

            switch (App.OccupiedInstance.instanceViewModel.DirectorySortOption)
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

            App.OccupiedInstance.instanceViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // QuickLook Integration
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isQuickLookIntegrationEnabled = localSettings.Values["quicklook_enabled"];

            if (isQuickLookIntegrationEnabled != null && isQuickLookIntegrationEnabled.Equals(true))
            {
                _isQuickLookEnabled = true;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DirectorySortOption")
            {
                switch (App.OccupiedInstance.instanceViewModel.DirectorySortOption)
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
        }

        private void AllView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

        }

        private async void AllView_DropAsync(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                App.OccupiedInstance.instanceInteraction.itemsPasted = 0;
                App.OccupiedInstance.instanceInteraction.ItemsToPaste = await e.DataView.GetStorageItemsAsync();
                foreach (IStorageItem item in await e.DataView.GetStorageItemsAsync())
                {
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        App.OccupiedInstance.instanceInteraction.CloneDirectoryAsync((item as StorageFolder).Path, App.OccupiedInstance.instanceViewModel.Universal.path, (item as StorageFolder).DisplayName, false);
                    }
                    else
                    {
                        App.OccupiedInstance.UpdateProgressFlyout(InteractionOperationType.PasteItems, ++App.OccupiedInstance.instanceInteraction.itemsPasted, App.OccupiedInstance.instanceInteraction.ItemsToPaste.Count);
                        await (item as StorageFile).CopyAsync(await StorageFolder.GetFolderFromPathAsync(App.OccupiedInstance.instanceViewModel.Universal.path));
                    }
                }
            }
        }

        private void AllView_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            var textBox = e.EditingElement as TextBox;
            var selectedItem = AllView.SelectedItem as ListedItem;
            int extensionLength = selectedItem.DotFileExtension?.Length ?? 0;

            previousFileName = selectedItem.FileName;
            textBox.Focus(FocusState.Programmatic); // Without this, cannot edit text box when renaming via right-click
            textBox.Select(0, selectedItem.FileName.Length - extensionLength);
            isRenamingItem = true;
        }

        private async void AllView_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            var selectedItem = AllView.SelectedItem as ListedItem;
            string currentName = previousFileName;
            string newName = (e.EditingElement as TextBox).Text;

            bool successful = await App.OccupiedInstance.instanceInteraction.RenameFileItem(selectedItem, currentName, newName);
            if (!successful)
            {
                selectedItem.FileName = currentName;
                ((sender as DataGrid).Columns[1].GetCellContent(e.Row) as TextBlock).Text = currentName;
            }
        }

        private void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            isRenamingItem = false;
        }

        private void GenericItemView_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            AllView.SelectedItem = null;
            App.OccupiedInstance.HomeItems.isEnabled = false;
            App.OccupiedInstance.ShareItems.isEnabled = false;
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
            if (e.AddedItems.Count > 0)
            {
                App.OccupiedInstance.HomeItems.isEnabled = true;
                App.OccupiedInstance.ShareItems.isEnabled = true;
            }
            else if (AllView.SelectedItems.Count == 0)
            {
                App.OccupiedInstance.HomeItems.isEnabled = false;
                App.OccupiedInstance.ShareItems.isEnabled = false;
            }
        }

        private void AllView_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.DragUI.SetContentFromDataPackage();
        }
        
        private void AllView_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column == SortedColumn)
                App.OccupiedInstance.instanceViewModel.IsSortedAscending = !App.OccupiedInstance.instanceViewModel.IsSortedAscending;
            else if (e.Column != iconColumn)
                SortedColumn = e.Column;
        }

        private void AllView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (isRenamingItem)
                {
                    AllView.CommitEdit();
                }
                else
                {
                    App.OccupiedInstance.instanceInteraction.List_ItemClick(null, null);
                }
                e.Handled = true;
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            base.Page_CharacterReceived(sender, args);
            AllView.Focus(FocusState.Keyboard);
        }
    }
}
