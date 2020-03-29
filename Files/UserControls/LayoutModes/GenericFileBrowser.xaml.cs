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
using Windows.UI.Input;
using Files.Controls;
using Microsoft.Xaml.Interactivity;
using System.Linq;
using Microsoft.Toolkit.Uwp.UI.Behaviors;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Files.Views.Pages;

namespace Files
{
    public sealed partial class GenericFileBrowser : BaseLayout
    {
        public bool IsSelectionRectangleDisplayed { get; set; } = false;
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
            this.InitializeComponent();

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

            App.CurrentInstance.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            else if (e.PropertyName == "isLoadingItems")
            {
                if (!AssociatedViewModel.isLoadingItems && AssociatedViewModel.FilesAndFolders.Count > 0)
                {
                    var allRows = new List<DataGridRow>();

                    Interacts.Interaction.FindChildren<DataGridRow>(allRows, AllView);
                    foreach(DataGridRow row in allRows.Take(20))
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

        private void AllView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

        }

        private async void AllView_DropAsync(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                App.CurrentInstance.InteractionOperations.itemsPasted = 0;
                App.CurrentInstance.InteractionOperations.ItemsToPaste = await e.DataView.GetStorageItemsAsync();
                foreach (IStorageItem item in await e.DataView.GetStorageItemsAsync())
                {
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        await App.CurrentInstance.InteractionOperations.CloneDirectoryAsync((item as StorageFolder).Path, App.CurrentInstance.ViewModel.Universal.WorkingDirectory, (item as StorageFolder).DisplayName, false);
                    }
                    else
                    {
                        (App.CurrentInstance as ModernShellPage).UpdateProgressFlyout(InteractionOperationType.PasteItems, ++App.CurrentInstance.InteractionOperations.itemsPasted, App.CurrentInstance.InteractionOperations.ItemsToPaste.Count);
                        await (item as StorageFile).CopyAsync(await StorageFolder.GetFolderFromPathAsync(App.CurrentInstance.ViewModel.Universal.WorkingDirectory));
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

            bool successful = await App.CurrentInstance.InteractionOperations.RenameFileItem(selectedItem, currentName, newName);
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
            //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
            //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
            //if (e.AddedItems.Count > 0)
            //{
            //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = true;
            //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = true;
            //}
            //else if (AllView.SelectedItems.Count == 0)
            //{
            //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
            //    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;
            //}
        }

        private void AllView_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.DragUI.SetContentFromDataPackage();
        }
        
        private async void AllView_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column == SortedColumn)
                App.CurrentInstance.ViewModel.IsSortedAscending = !App.CurrentInstance.ViewModel.IsSortedAscending;
            else if (e.Column != iconColumn)
                SortedColumn = e.Column;

            if (!AssociatedViewModel.isLoadingItems && AssociatedViewModel.FilesAndFolders.Count > 0)
            {
                var allRows = new List<DataGridRow>();

                Interacts.Interaction.FindChildren<DataGridRow>(allRows, AllView);
                foreach (DataGridRow row in allRows.Take(20))
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

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (App.CurrentInstance != null)
            {
                if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                {
                    base.Page_CharacterReceived(sender, args);
                    AllView.Focus(FocusState.Keyboard);
                }
            }
        }

        PointerPoint startingPoint;
        private void BaseLayout_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            startingPoint = e.GetCurrentPoint(this);
            IsSelectionRectangleDisplayed = true;
            SelectionRectangle.Margin = new Thickness(e.GetCurrentPoint(this).Position.X, e.GetCurrentPoint(this).Position.Y, 0, 0);
            
        }

        private void BaseLayout_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if(e.GetCurrentPoint(this).Position.X < startingPoint.Position.X)
            {
                SelectionRectangle.Width -= e.GetCurrentPoint(this).Position.X;
            }
            else
            {
                SelectionRectangle.Width += (e.GetCurrentPoint(this).Position.X - startingPoint.Position.X);
            }
        }

        private async void Icon_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            var parentRow = Interacts.Interaction.FindParent<DataGridRow>(sender);
            if((!(parentRow.DataContext as ListedItem).ItemPropertiesInitialized) && (args.BringIntoViewDistanceX < sender.ActualHeight))
            {
                await Window.Current.CoreWindow.Dispatcher.RunIdleAsync((e) =>
                { 
                    App.CurrentInstance.ViewModel.LoadExtendedItemProperties(parentRow.DataContext as ListedItem);
                    (parentRow.DataContext as ListedItem).ItemPropertiesInitialized = true;
                    //sender.EffectiveViewportChanged -= Icon_EffectiveViewportChanged;
                });
            }
        }
    }
}
