using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.UserControls;
using Microsoft.Toolkit.Uwp.UI;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Files.Backend.ViewModels.ItemListing;
using Files.Backend.ViewModels.Layouts;
using Files.Shared.Extensions;

#nullable enable

namespace Files.Layouts
{
    internal abstract class BaseListedLayout<TViewModel, TLayoutType> : BaseLayout<TViewModel>, IBaseListedLayout
        where TViewModel : BaseListedLayoutViewModel
        where TLayoutType : class
    {
        protected abstract ListViewBase FileListInternal { get; }

        protected uint CurrentIconSize { get; set; }

        private CollectionViewSource? _FileListCollectionViewSource;
        public CollectionViewSource? FileListCollectionViewSource
        {
            get => _FileListCollectionViewSource;
            set
            {
                if (_FileListCollectionViewSource?.View != null)
                {
                    _FileListCollectionViewSource.View.VectorChanged -= View_VectorChanged;
                }
                
                if (SetProperty(ref _FileListCollectionViewSource, value) && _FileListCollectionViewSource?.View != null)
                {
                    _FileListCollectionViewSource.View.VectorChanged += View_VectorChanged;
                }
            }
        }

        protected BaseListedLayout()
        {
            _FileListCollectionViewSource = new()
            {
                IsSourceGrouped = true
            };
        }

        #region UI Event Handlers

        protected virtual void View_VectorChanged(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            ParentShellPageInstance.NavToolbarViewModel.HasItem = FileListCollectionViewSource?.View?.Any() ?? false;
        }

        protected virtual async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ViewModel.SelectionChanged(FileListInternal.SelectedItems.Cast<ListedItemViewModel>().Where((item) => item != null));
        }

        protected virtual void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                InitializeDrag(args.ItemContainer);
                args.ItemContainer.PointerPressed -= FileListGridItem_PointerPressed;
                args.ItemContainer.PointerPressed += FileListGridItem_PointerPressed;

                if (args.Item is ListedItem item && !item.ItemPropertiesInitialized)
                {
                    args.RegisterUpdateCallback(3, async (s, c) =>
                    {
                        await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(item, CurrentIconSize);
                    });
                }
            }
            else
            {
                UninitializeDrag(args.ItemContainer);
                args.ItemContainer.PointerPressed -= FileListGridItem_PointerPressed;
                if (args.Item is ListedItem item)
                {
                    ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoadingForItem(item);
                }
            }
        }

        protected virtual void FileListGridItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is SelectorItem selectorItem)
            {
                if (e.KeyModifiers == VirtualKeyModifiers.Control)
                {
                    if (selectorItem.IsSelected)
                    {
                        selectorItem.IsSelected = false;

                        // Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
                        e.Handled = true;
                    }
                }
                else if (e.GetCurrentPoint(selectorItem).Properties.IsLeftButtonPressed)
                {
                    selectorItem.IsSelected = true;
                }
            }
        }

        protected virtual async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            if ((e.OriginalSource as FrameworkElement)?.DataContext is not ListedItemViewModel listedItem)
            {
                return;
            }

            if (!ViewModel.ItemTapped(listedItem, ctrlPressed, shiftPressed))
            {
                var clickedItem = e.OriginalSource as FrameworkElement;
                if (clickedItem is TextBlock textBlock && textBlock.Name == "ItemName")
                {
                    CheckRenameDoubleClick(clickedItem.DataContext);
                }
                else if (IsRenamingItem)
                {
                    if (FileListInternal.ContainerFromItem(RenamingItem) is SelectorItem selectorItem)
                    {
                        if (selectorItem.FindDescendant("ItemNameTextBox") is TextBox textBox)
                        {
                            await CommitRename(textBox);
                        }
                        else if (selectorItem.FindDescendant("EditPopup") is Popup popup && popup.Child is TextBox textBox2)
                        {
                            await CommitRename(textBox2);
                        }
                    }
                }
            }
        }

        protected virtual void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Skip opening selected items if the double tap doesn't capture an item
            if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItemViewModel listedItem)
            {
                ViewModel.ItemDoubleTapped(listedItem);
            }
        }

        protected virtual async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
            var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) != null;
            var isFooterFocused = focusedElement is HyperlinkButton;

            if (e.Key == VirtualKey.Down)
            {
                if (!IsRenamingItem && isHeaderFocused && !ParentShellPageInstance.NavToolbarViewModel.IsEditModeEnabled)
                {
                    var selectIndex = FileListInternal.SelectedIndex < 0 ? 0 : FileListInternal.SelectedIndex;

                    if (FileListInternal.ContainerFromIndex(selectIndex) is ListViewItem item)
                    {
                        // Focus selected list item or first item
                        item.Focus(FocusState.Programmatic);

                        if (!IsItemSelected && e.KeyStatus.IsMenuKeyDown)
                        {
                            FileListInternal.SelectedIndex = 0;
                        }

                        e.Handled = true;
                    }
                }
            }
            else if (await ViewModel.PreviewKeyDown(e.Key, e.KeyStatus.IsMenuKeyDown, ctrlPressed, shiftPressed, isHeaderFocused, isFooterFocused))
            {
                e.Handled = true;
            }
        }

        protected virtual void FileListTextBoxItemName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (FilesystemHelpers.ContainsRestrictedCharacters(textBox.Text))
                {
                    ViewModel.FileNameTeachingTipOpened = true;
                }
                else
                {
                    ViewModel.FileNameTeachingTipOpened = false;
                }
            }
        }

        protected virtual async void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // This check allows the user to use the text box context menu without ending the rename
            if (FocusManager.GetFocusedElement() is not AppBarButton or Popup && e.OriginalSource is TextBox textBox)
            {
                await CommitRename(textBox);
            }
        }

        protected virtual async void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (e.Key == VirtualKey.Escape)
                {
                    textBox.LostFocus -= RenameTextBox_LostFocus;
                    textBox.Text = OldItemName;
                    EndRename(textBox);
                    e.Handled = true;
                }
                else if (e.Key == VirtualKey.Enter)
                {
                    textBox.LostFocus -= RenameTextBox_LostFocus;
                    await CommitRename(textBox);
                    e.Handled = true;
                }
            }
        }

        protected virtual void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var parentContainer = DependencyObjectHelpers.FindParent<SelectorItem>(e.OriginalSource as DependencyObject);
            if (!parentContainer.IsSelected && FileListInternal.ItemFromContainer(parentContainer) is ListedItemViewModel listedItem)
            {
                SetSelection(listedItem);
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance != null)
            {
                if (ParentShellPageInstance.CurrentPageType == typeof(TLayoutType) && !IsRenamingItem)
                {
                    // Don't block the various uses of enter key (key 13)
                    var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
                    var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) != null;

                    if (args.KeyCode == 13
                        || (focusedElement is Button && !isHeaderFocused) // Allow jumpstring when header is focused
                        || focusedElement is TextBox
                        || focusedElement is PasswordBox
                        || DependencyObjectHelpers.FindParent<ContentDialog>(focusedElement) != null)
                    {
                        return;
                    }

                    base.Page_CharacterReceived(sender, args);
                }
            }
        }

        #endregion UI Event Handlers

        #region Helpers

        protected virtual void StartRenameItem()
        {
            if ((RenamingItem = SelectedItem) == null)
            {
                return;
            }

            var extensionLength = RenamingItem.FileExtension?.Length ?? 0;

            if (FileListInternal.ContainerFromItem(RenamingItem) is not SelectorItem selectorItem)
            {
                return;
            }
            if (selectorItem.FindDescendant("ItemNameTextBox") is not TextBox textBox)
            {
                return;
            }
            if (selectorItem.FindDescendant("ItemName") is not TextBlock textBlock)
            {
                return;
            }

            textBox.Text = textBlock.Text;
            OldItemName = textBlock.Text;

            textBlock.Visibility = Visibility.Collapsed;
            textBox.Visibility = Visibility.Visible;

            Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;

            var selectedTextLength = SelectedItem.ItemName.Length;
            if (!SelectedItem.IsShortcutItem && UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }

            textBox.Select(0, selectedTextLength);
            IsRenamingItem = true;
        }

        protected virtual void EndRename(TextBox textBox)
        {
            if (textBox.FindParent<Grid>() is FrameworkElement parent)
            {
                Grid.SetColumnSpan(parent, 1);
            }

            var selectorItem = (SelectorItem?)FileListInternal.ContainerFromItem(RenamingItem);
            if (selectorItem != null)
            {
                if (selectorItem.FindDescendant("ItemName") is TextBlock textBlock)
                {
                    textBlock.Visibility = Visibility.Visible;
                }

                textBox.Visibility = Visibility.Collapsed;
            }

            textBox.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown -= RenameTextBox_KeyDown;

            ViewModel.FileNameTeachingTipOpened = false;
            IsRenamingItem = false; // TODO(i): Move it or change logic so it's in VM.EndRename()

            // Re-focus selected list item
            selectorItem?.Focus(FocusState.Programmatic);
        }

        protected virtual async Task CommitRename(TextBox textBox)
        {
            EndRename(textBox);

            var newItemName = textBox.Text.Trim().TrimEnd('.');
            await UIFilesystemHelpers.RenameFileItemAsync(RenamingItem, newItemName, ParentShellPageInstance);
        }

        protected virtual ListedItem? GetItemFromElement(object element)
        {
            if (element is SelectorItem selectorItem)
            {
                return selectorItem.DataContext as ListedItem ?? selectorItem.Content as ListedItem ?? FileListInternal.ItemFromContainer(selectorItem) as ListedItem;
            }

            return null;
        }

        protected virtual async Task ReloadItemIcons()
        {
            ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();

            foreach (var item in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
            {
                item.ItemPropertiesInitialized = false;

                if (FileListInternal.ContainerFromItem(item) != null)
                {
                    await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(item, CurrentIconSize);
                }
            }
        }

        public virtual void FocusFileList()
        {
            FileListInternal.Focus(FocusState.Programmatic);
        }

        public virtual void SelectAllItems()
        {
            FileListInternal.SelectAll();
            ViewModel.SetSelection(FileListInternal.SelectedItems.Cast<ListedItemViewModel>());
        }

        public virtual void ClearSelection()
        {
            FileListInternal.SelectedItems.Clear();
            ViewModel.SelectedItems.Clear();
        }

        public virtual void InvertSelection()
        {
            var allItemsCount = GetAllItems().Count();

            if (ViewModel.SelectedItems.Count < allItemsCount / 2)
            {
                var oldSelectedItems = ViewModel.SelectedItems.ToList();
                SelectAllItems();

                foreach (var item in oldSelectedItems)
                {
                    RemoveSelection(item);
                }
            }
            else
            {
                var newSelectedItems = GetAllItems().Except(SelectedItems);

                foreach (var item in newSelectedItems)
                {
                    AddSelection(item);
                }
            }
        }

        public void SetSelection(ListedItemViewModel listedItem)
        {
            ClearSelection();
            AddSelection(listedItem);
        }

        public virtual void AddSelection(ListedItemViewModel listedItem)
        {
            FileListInternal.SelectedItems.Add(listedItem);
            ViewModel.SelectedItems.Add(listedItem);
        }

        public virtual void RemoveSelection(ListedItemViewModel listedItem)
        {
            FileListInternal.SelectedItems.Remove(listedItem);
            ViewModel.SelectedItems.Remove(listedItem);
        }

        public virtual void FocusSelectedItems()
        {
            if (!ViewModel.SelectedItems.IsEmpty())
            {
                var lastItem = ViewModel.SelectedItems.Last();

                FileListInternal.ScrollIntoView(lastItem);
                (FileListInternal.ContainerFromItem(lastItem) as SelectorItem)?.Focus(FocusState.Keyboard);
            }
        }

        public void ReloadItem(ListedItemViewModel listedItem)
        {
            ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
            ParentShellPageInstance.SlimContentPage.SelectedItem.ItemPropertiesInitialized = false;
            await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(ParentShellPageInstance.SlimContentPage.SelectedItem, currentIconSize);
        }

        #endregion
    }
}
