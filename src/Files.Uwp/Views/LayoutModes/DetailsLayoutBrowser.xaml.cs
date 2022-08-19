using Files.Shared.Enums;
using Files.Uwp.EventArguments;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Uwp.Helpers.XamlHelpers;
using Files.Uwp.Interacts;
using Files.Uwp.UserControls;
using Files.Uwp.UserControls.Selection;
using Files.Uwp.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using SortDirection = Files.Shared.Enums.SortDirection;

namespace Files.Uwp.Views.LayoutModes
{
    public sealed partial class DetailsLayoutBrowser : BaseLayout
    {
        private uint currentIconSize;

        protected override uint IconSize => currentIconSize;

        protected override ItemsControl ItemsControl => FileList;

        private ColumnsViewModel columnsViewModel = new ColumnsViewModel();

        public ColumnsViewModel ColumnsViewModel
        {
            get => columnsViewModel;
            set
            {
                if (value != columnsViewModel)
                {
                    columnsViewModel = value;
                    NotifyPropertyChanged(nameof(ColumnsViewModel));
                }
            }
        }

        private double maxWidthForRenameTextbox;

        public double MaxWidthForRenameTextbox
        {
            get => maxWidthForRenameTextbox;
            set
            {
                if (value != maxWidthForRenameTextbox)
                {
                    maxWidthForRenameTextbox = value;
                    NotifyPropertyChanged(nameof(MaxWidthForRenameTextbox));
                }
            }
        }

        private RelayCommand<string> UpdateSortOptionsCommand { get; set; }

        public ScrollViewer ContentScroller { get; private set; }

        public DetailsLayoutBrowser() : base()
        {
            InitializeComponent();
            this.DataContext = this;

            var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
        }

        protected override void HookEvents()
        {
            UnhookEvents();
            ItemManipulationModel.FocusFileListInvoked += ItemManipulationModel_FocusFileListInvoked;
            ItemManipulationModel.SelectAllItemsInvoked += ItemManipulationModel_SelectAllItemsInvoked;
            ItemManipulationModel.ClearSelectionInvoked += ItemManipulationModel_ClearSelectionInvoked;
            ItemManipulationModel.InvertSelectionInvoked += ItemManipulationModel_InvertSelectionInvoked;
            ItemManipulationModel.AddSelectedItemInvoked += ItemManipulationModel_AddSelectedItemInvoked;
            ItemManipulationModel.RemoveSelectedItemInvoked += ItemManipulationModel_RemoveSelectedItemInvoked;
            ItemManipulationModel.FocusSelectedItemsInvoked += ItemManipulationModel_FocusSelectedItemsInvoked;
            ItemManipulationModel.StartRenameItemInvoked += ItemManipulationModel_StartRenameItemInvoked;
            ItemManipulationModel.ScrollIntoViewInvoked += ItemManipulationModel_ScrollIntoViewInvoked;
        }

        private void ItemManipulationModel_ScrollIntoViewInvoked(object sender, ListedItem e)
        {
            FileList.ScrollIntoView(e);
            ContentScroller?.ChangeView(null, FileList.Items.IndexOf(e) * Convert.ToInt32(Application.Current.Resources["ListItemHeight"]), null, true); // Scroll to index * item height
        }

        private void ItemManipulationModel_StartRenameItemInvoked(object sender, EventArgs e)
        {
            StartRenameItem();
        }

        private void ItemManipulationModel_FocusSelectedItemsInvoked(object sender, EventArgs e)
        {
            if (SelectedItems.Any())
            {
                FileList.ScrollIntoView(SelectedItems.Last());
                (FileList.ContainerFromItem(SelectedItems.Last()) as ListViewItem)?.Focus(FocusState.Keyboard);
            }
        }

        private void ItemManipulationModel_AddSelectedItemInvoked(object sender, ListedItem e)
        {
            if (FileList?.Items.Contains(e) ?? false)
            {
                FileList.SelectedItems.Add(e);
            }
        }

        private void ItemManipulationModel_RemoveSelectedItemInvoked(object sender, ListedItem e)
        {
            if (FileList?.Items.Contains(e) ?? false)
            {
                FileList.SelectedItems.Remove(e);
            }
        }

        private void ItemManipulationModel_InvertSelectionInvoked(object sender, EventArgs e)
        {
            if (SelectedItems.Count < GetAllItems().Count() / 2)
            {
                var oldSelectedItems = SelectedItems.ToList();
                ItemManipulationModel.SelectAllItems();
                ItemManipulationModel.RemoveSelectedItems(oldSelectedItems);
            }
            else
            {
                List<ListedItem> newSelectedItems = GetAllItems()
                    .Cast<ListedItem>()
                    .Except(SelectedItems)
                    .ToList();

                ItemManipulationModel.SetSelectedItems(newSelectedItems);
            }
        }

        private void ItemManipulationModel_ClearSelectionInvoked(object sender, EventArgs e)
        {
            FileList.SelectedItems.Clear();
        }

        private void ItemManipulationModel_SelectAllItemsInvoked(object sender, EventArgs e)
        {
            FileList.SelectAll();
        }

        private void ItemManipulationModel_FocusFileListInvoked(object sender, EventArgs e)
        {
            FileList.Focus(FocusState.Programmatic);
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
                ItemManipulationModel.RemoveSelectedItemInvoked -= ItemManipulationModel_RemoveSelectedItemInvoked;
                ItemManipulationModel.FocusSelectedItemsInvoked -= ItemManipulationModel_FocusSelectedItemsInvoked;
                ItemManipulationModel.StartRenameItemInvoked -= ItemManipulationModel_StartRenameItemInvoked;
                ItemManipulationModel.ScrollIntoViewInvoked -= ItemManipulationModel_ScrollIntoViewInvoked;
            }
        }

        protected override void InitializeCommandsViewModel()
        {
            CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (eventArgs.Parameter is NavigationArguments navArgs)
            {
                navArgs.FocusOnNavigation = true;
            }
            base.OnNavigatedTo(eventArgs);

            if (ParentShellPageInstance.InstanceViewModel?.FolderSettings.ColumnsViewModel != null)
            {
                ColumnsViewModel = ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel;
            }

            currentIconSize = FolderSettings.GetIconSize();
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
            FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
            FolderSettings.GridViewSizeChangeRequested += FolderSettings_GridViewSizeChangeRequested;
            FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
            FolderSettings.SortDirectionPreferenceUpdated += FolderSettings_SortDirectionPreferenceUpdated;
            FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
            FolderSettings.SortOptionPreferenceUpdated += FolderSettings_SortOptionPreferenceUpdated;
            ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
            ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;

            var parameters = (NavigationArguments)eventArgs.Parameter;
            if (parameters.IsLayoutSwitch)
            {
                ReloadItemIcons();
            }

            UpdateSortOptionsCommand = new RelayCommand<string>(x =>
            {
                if (!Enum.TryParse<SortOption>(x, out var val))
                {
                    return;
                }
                if (FolderSettings.DirectorySortOption == val)
                {
                    FolderSettings.DirectorySortDirection = (SortDirection)(((int)FolderSettings.DirectorySortDirection + 1) % 2);
                }
                else
                {
                    FolderSettings.DirectorySortOption = val;
                    FolderSettings.DirectorySortDirection = SortDirection.Ascending;
                }
            });

            FilesystemViewModel_PageTypeUpdated(null, new PageTypeUpdatedEventArgs()
            {
                IsTypeCloudDrive = InstanceViewModel.IsPageTypeCloudDrive,
                IsTypeRecycleBin = InstanceViewModel.IsPageTypeRecycleBin
            });

            RootGrid_SizeChanged(null, null);
        }

        private void FolderSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
        {
            UpdateSortIndicator();
        }

        private void FolderSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
        {
            UpdateSortIndicator();
        }

        private void UpdateSortIndicator()
        {
            NameHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Name ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            TagHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileTag ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            OriginalPathHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.OriginalFolder ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            DateDeletedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateDeleted ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            DateModifiedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateModified ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            DateCreatedHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateCreated ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            FileTypeHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileType ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            ItemSizeHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Size ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
            SyncStatusHeader.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.SyncStatus ? FolderSettings.DirectorySortDirection : (SortDirection?)null;
        }

        private void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
        {
            // This code updates which columns are hidden and which ones are shwn
            if (!e.IsTypeRecycleBin)
            {
                ColumnsViewModel.DateDeletedColumn.Hide();
                ColumnsViewModel.OriginalPathColumn.Hide();
            }
            else
            {
                ColumnsViewModel.OriginalPathColumn.Show();
                ColumnsViewModel.DateDeletedColumn.Show();
            }

            if (!e.IsTypeCloudDrive)
            {
                ColumnsViewModel.StatusColumn.Hide();
            }
            else
            {
                ColumnsViewModel.StatusColumn.Show();
            }

            if (!UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled)
            {
                ColumnsViewModel.TagColumn.Hide();
            }
            else
            {
                ColumnsViewModel.TagColumn.Show();
            }

            UpdateSortIndicator();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.GridViewSizeChangeRequested -= FolderSettings_GridViewSizeChangeRequested;
            FolderSettings.SortDirectionPreferenceUpdated -= FolderSettings_SortDirectionPreferenceUpdated;
            FolderSettings.SortOptionPreferenceUpdated -= FolderSettings_SortOptionPreferenceUpdated;
            ParentShellPageInstance.FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
        }

        private void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            FileList.Focus(FocusState.Programmatic);
        }

        private void FolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
        {
        }

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var parentContainer = DependencyObjectHelpers.FindParent<ListViewItem>(e.OriginalSource as DependencyObject);
            if (!parentContainer.IsSelected)
            {
                ItemManipulationModel.SetSelectedItem(FileList.ItemFromContainer(parentContainer) as ListedItem);
            }
        }

        private async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItems = FileList.SelectedItems.Cast<ListedItem>().Where(x => x != null).ToList();
            if (SelectedItems.Count == 1)
            {
                await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance, true);
            }
        }

        override public void StartRenameItem()
        {
            RenamingItem = SelectedItem;
            if (RenamingItem == null)
            {
                return;
            }
            int extensionLength = RenamingItem.FileExtension?.Length ?? 0;
            ListViewItem listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
            TextBox textBox = null;
            if (listViewItem == null)
            {
                return;
            }
            TextBlock textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
            textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
            textBox.Text = textBlock.Text;
            OldItemName = textBlock.Text;
            textBlock.Visibility = Visibility.Collapsed;
            textBox.Visibility = Visibility.Visible;
            Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (!SelectedItem.IsShortcutItem && UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            textBox.Select(0, selectedTextLength);
            IsRenamingItem = true;
        }

        private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
        {
            if (IsRenamingItem)
            {
                ValidateItemNameInputText(textBox, args, (showError) =>
                {
                    FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
                    FileNameTeachingTip.IsOpen = showError;
                });
            }
        }

        private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                TextBox textBox = sender as TextBox;
                textBox.LostFocus -= RenameTextBox_LostFocus;
                textBox.Text = OldItemName;
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
            // This check allows the user to use the text box context menu without ending the rename
            if (!(FocusManager.GetFocusedElement() is AppBarButton or Popup))
            {
                TextBox textBox = e.OriginalSource as TextBox;
                CommitRename(textBox);
            }
        }

        private async void CommitRename(TextBox textBox)
        {
            EndRename(textBox);
            string newItemName = textBox.Text.Trim().TrimEnd('.');
            await UIFilesystemHelpers.RenameFileItemAsync(RenamingItem, newItemName, ParentShellPageInstance);
        }

        private void EndRename(TextBox textBox)
        {
            if (textBox != null && textBox.FindParent<Grid>() is FrameworkElement parent)
            {
                Grid.SetColumnSpan(parent, 1);
            }

            ListViewItem listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;

            if (textBox == null || listViewItem == null)
            {
                // Navigating away, do nothing
            }
            else
            {
                TextBlock textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
                textBox.Visibility = Visibility.Collapsed;
                textBlock.Visibility = Visibility.Visible;
            }

            textBox.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown -= RenameTextBox_KeyDown;
            FileNameTeachingTip.IsOpen = false;
            IsRenamingItem = false;

            // Re-focus selected list item
            listViewItem?.Focus(FocusState.Programmatic);
        }

        private async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
            var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) != null;
            var isFooterFocused = focusedElement is HyperlinkButton;

            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (!IsRenamingItem && !isHeaderFocused && !isFooterFocused)
                {
                    NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                    e.Handled = true;
                }
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Space)
            {
                if (!IsRenamingItem && !isHeaderFocused && !isFooterFocused && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
                {
                    e.Handled = true;
                    await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance);
                }
            }
            else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
            {
                // Unfocus the GridView so keyboard shortcut can be handled
                NavToolbar?.Focus(FocusState.Pointer);
            }
            else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
            {
                // Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
                NavToolbar?.Focus(FocusState.Pointer);
            }
            else if (e.Key == VirtualKey.Down)
            {
                if (!IsRenamingItem && isHeaderFocused && !ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
                {
                    var selectIndex = FileList.SelectedIndex < 0 ? 0 : FileList.SelectedIndex;
                    if (FileList.ContainerFromIndex(selectIndex) is ListViewItem item)
                    {
                        // Focus selected list item or first item
                        item.Focus(FocusState.Programmatic);
                        if (!IsItemSelected)
                        {
                            FileList.SelectedIndex = 0;
                        }
                        e.Handled = true;
                    }
                }
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance != null)
            {
                if (ParentShellPageInstance.CurrentPageType == typeof(DetailsLayoutBrowser) && !IsRenamingItem)
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

        protected override bool CanGetItemFromElement(object element)
            => element is ListViewItem;

        private void FolderSettings_GridViewSizeChangeRequested(object sender, EventArgs e)
        {
            var requestedIconSize = FolderSettings.GetIconSize(); // Get new icon size

            // Prevents reloading icons when the icon size hasn't changed
            if (requestedIconSize != currentIconSize)
            {
                currentIconSize = requestedIconSize; // Update icon size before refreshing
                ReloadItemIcons();
            }
        }

        private async void ReloadItemIcons()
        {
            ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
            foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
            {
                listedItem.ItemPropertiesInitialized = false;
                if (FileList.ContainerFromItem(listedItem) != null)
                {
                    await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, currentIconSize);
                }
            }
        }

        private void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;
            if (item == null)
            {
                return;
            }
            // Skip code if the control or shift key is pressed or if the user is using multiselect
            if (ctrlPressed || shiftPressed || MainViewModel.MultiselectEnabled)
            {
                return;
            }

            // Check if the setting to open items with a single click is turned on
            if (item != null
                && ((UserSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick && item.PrimaryItemAttribute == StorageItemTypes.Folder) || (UserSettingsService.PreferencesSettingsService.OpenFilesWithOneClick && item.PrimaryItemAttribute == StorageItemTypes.File)))
            {
                ResetRenameDoubleClick();
                NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
            }
            else
            {
                var clickedItem = e.OriginalSource as FrameworkElement;
                if (clickedItem is TextBlock && ((TextBlock)clickedItem).Name == "ItemName")
                {
                    CheckRenameDoubleClick(clickedItem?.DataContext);
                }
                else if (IsRenamingItem)
                {
                    ListViewItem listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
                    if (listViewItem != null)
                    {
                        var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
                        CommitRename(textBox);
                    }
                }
            }
        }

        private void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Skip opening selected items if the double tap doesn't capture an item
            if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item
                 && ((!UserSettingsService.PreferencesSettingsService.OpenFilesWithOneClick && item.PrimaryItemAttribute == StorageItemTypes.File)
                 || (!UserSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick && item.PrimaryItemAttribute == StorageItemTypes.Folder)))
            {
                NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
            }
            else
            {
                ParentShellPageInstance.Up_Click();
            }
            ResetRenameDoubleClick();
        }

        #region IDisposable

        public override void Dispose()
        {
            base.Dispose();
            UnhookEvents();
            CommandsViewModel?.Dispose();
        }

        #endregion IDisposable

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
            // because you can't use bindings in the setters
            DependencyObject item = VisualTreeHelper.GetParent(sender as Grid);
            while (!(item is ListViewItem))
                item = VisualTreeHelper.GetParent(item);
            var itemContainer = item as ListViewItem;
            itemContainer.ContextFlyout = ItemContextMenuFlyout;
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // This prevents the drag selection rectangle from appearing when resizing the columns
            e.Handled = true;
        }

        private void GridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            UpdateColumnLayout();
        }

        private void GridSplitter_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
            {
                UpdateColumnLayout();
                ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
            }
        }

        private void UpdateColumnLayout()
        {
            ColumnsViewModel.IconColumn.UserLength = new GridLength(Column1.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.NameColumn.UserLength = new GridLength(Column2.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.TagColumn.UserLength = new GridLength(Column3.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.OriginalPathColumn.UserLength = new GridLength(Column4.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.DateDeletedColumn.UserLength = new GridLength(Column5.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.DateModifiedColumn.UserLength = new GridLength(Column6.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.DateCreatedColumn.UserLength = new GridLength(Column7.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.ItemTypeColumn.UserLength = new GridLength(Column8.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.SizeColumn.UserLength = new GridLength(Column9.ActualWidth, GridUnitType.Pixel);
            ColumnsViewModel.StatusColumn.UserLength = new GridLength(Column10.ActualWidth, GridUnitType.Pixel);
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ColumnsViewModel.SetDesiredSize(Math.Max(0, RootGrid.ActualWidth - 80));
            MaxWidthForRenameTextbox = Math.Max(0, RootGrid.ActualWidth - 80);
        }

        private void GridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
        }

        private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
        }

        private void GridSplitter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var columnToResize = (Grid.GetColumn(sender as Microsoft.Toolkit.Uwp.UI.Controls.GridSplitter) - 1) / 2;
            ResizeColumnToFit(columnToResize);
            e.Handled = true;
        }

        private void SizeAllColumnsToFit_Click(object sender, RoutedEventArgs e)
        {
            if (!FileList.Items.Any())
            {
                return;
            }

            // for scalability, just count the # of public `ColumnViewModel` properties in ColumnsViewModel
            int totalColumnCount = ColumnsViewModel.GetType().GetProperties().Count(prop => prop.PropertyType == typeof(ColumnViewModel));
            for (int columnIndex = 1; columnIndex <= totalColumnCount; columnIndex++)
            {
                ResizeColumnToFit(columnIndex);
            }
        }

        private void ResizeColumnToFit(int columnToResize)
        {
            if (!FileList.Items.Any())
            {
                return;
            }

            var maxItemLength = columnToResize switch
            {
                1 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemName?.Length ?? 0).Max(), // file name column
                2 => FileList.Items.Cast<ListedItem>().Select(x => x.FileTagsUI?.FirstOrDefault()?.TagName?.Length ?? 0).Max(), // file tag column
                3 => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemOriginalPath?.Length ?? 0).Max(), // original path column
                4 => FileList.Items.Cast<ListedItem>().Select(x => (x as RecycleBinItem)?.ItemDateDeleted?.Length ?? 0).Max(), // date deleted column
                5 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateModified?.Length ?? 0).Max(), // date modified column
                6 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemDateCreated?.Length ?? 0).Max(), // date created column
                7 => FileList.Items.Cast<ListedItem>().Select(x => x.ItemType?.Length ?? 0).Max(), // item type column
                8 => FileList.Items.Cast<ListedItem>().Select(x => x.FileSize?.Length ?? 0).Max(), // item size column
                _ => 20 // cloud status column
            };

            // if called programmatically, the column could be hidden
            // in this case, resizing doesn't need to be done at all
            if (maxItemLength == 0)
            {
                return;
            }

            var columnSizeToFit = new[] { 9 }.Contains(columnToResize) ? maxItemLength : MeasureTextColumnEstimate(columnToResize, 5, maxItemLength);
            if (columnSizeToFit > 0)
            {
                var column = columnToResize switch
                {
                    1 => ColumnsViewModel.NameColumn,
                    2 => ColumnsViewModel.TagColumn,
                    3 => ColumnsViewModel.OriginalPathColumn,
                    4 => ColumnsViewModel.DateDeletedColumn,
                    5 => ColumnsViewModel.DateModifiedColumn,
                    6 => ColumnsViewModel.DateCreatedColumn,
                    7 => ColumnsViewModel.ItemTypeColumn,
                    8 => ColumnsViewModel.SizeColumn,
                    _ => ColumnsViewModel.StatusColumn
                };

                if (columnToResize == 1) // file name column
                {
                    columnSizeToFit += UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled ? 20 : 0;
                }

                var minFitLength = Math.Max(columnSizeToFit, column.NormalMinLength);
                var maxFitLength = Math.Min(minFitLength + 36, column.NormalMaxLength); // 36 to account for SortIcon & padding

                column.UserLength = new GridLength(maxFitLength, GridUnitType.Pixel);
            }

            ParentShellPageInstance.InstanceViewModel.FolderSettings.ColumnsViewModel = ColumnsViewModel;
        }

        private double MeasureTextColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
        {
            var tbs = DependencyObjectHelpers.FindChildren<TextBlock>(FileList.ItemsPanelRoot).Where(tb =>
            {
                // isolated <TextBlock Grid.Column=...>
                if (tb.ReadLocalValue(Grid.ColumnProperty) != DependencyProperty.UnsetValue)
                {
                    return Grid.GetColumn(tb) == columnIndex;
                }
                // <TextBlock> nested in <Grid Grid.Column=...>
                else if (tb.Parent is Grid parentGrid)
                {
                    return Grid.GetColumn(parentGrid) == columnIndex;
                }

                return false;
            });

            // heuristic: usually, text with more letters are wider than shorter text with wider letters
            // with this, we can calculate avg width using longest text(s) to avoid overshooting the width
            var widthPerLetter = tbs.OrderByDescending(x => x.Text.Length).Where(tb => !string.IsNullOrEmpty(tb.Text)).Take(measureItemsCount).Select(tb =>
            {
                var sampleTb = new TextBlock { Text = tb.Text, FontSize = tb.FontSize, FontFamily = tb.FontFamily };
                sampleTb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                return sampleTb.DesiredSize.Width / Math.Max(1, tb.Text.Length);
            });

            if (!widthPerLetter.Any())
            {
                return 0;
            }

            // take weighted avg between mean and max since width is an estimate
            var weightedAvg = (widthPerLetter.Average() + widthPerLetter.Max()) / 2;
            return weightedAvg * maxItemLength;
        }

        private void FileList_Loaded(object sender, RoutedEventArgs e)
        {
            ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
        }

        private void RefreshContainer_RefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
        {
            ParentShellPageInstance.FilesystemViewModel.RefreshItems(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, SetSelectedItemsOnNavigation);
        }
    }
}