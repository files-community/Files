using Files.EventArguments;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.ContextFlyouts;
using Files.Interacts;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public class BaseLayoutViewModel : ObservableObject, IBaseLayout, IDisposable
    {
        private readonly DispatcherTimer jumpTimer;

        public CurrentInstanceViewModel InstanceViewModel { get; }


        public ItemViewModel FilesystemViewModel { get; }

        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

        public SettingsViewModel AppSettings => App.AppSettings;

        public InteractionViewModel InteractionViewModel => App.InteractionViewModel;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

        public Microsoft.UI.Xaml.Controls.CommandBarFlyout ItemContextMenuFlyout { get; set; } = new Microsoft.UI.Xaml.Controls.CommandBarFlyout();
        public MenuFlyout BaseContextMenuFlyout { get; set; } = new MenuFlyout();

        public BaseLayoutCommandsViewModel CommandsViewModel { get; protected set; }

        public bool IsRenamingItem { get; set; } = false;

        private CollectionViewSource collectionViewSource = new CollectionViewSource()
        {
            IsSourceGrouped = true,
        };

        public CollectionViewSource CollectionViewSource
        {
            get => collectionViewSource;
            set => SetProperty(ref collectionViewSource, value);
        }


        private bool isItemSelected = false;

        public bool IsItemSelected
        {
            get => isItemSelected;
            set => SetProperty(ref isItemSelected, value);
        }

        private string jumpString = string.Empty;

        public string JumpString
        {
            get => jumpString;
            set
            {
                // If current string is "a", and the next character typed is "a",
                // search for next file that starts with "a" (a.k.a. _jumpString = "a")
                if (jumpString.Length == 1 && value == jumpString + jumpString)
                {
                    value = jumpString;
                }
                if (value != string.Empty)
                {
                    ListedItem jumpedToItem = null;
                    ListedItem previouslySelectedItem = null;

                    // Use FilesAndFolders because only displayed entries should be jumped to
                    IEnumerable<ListedItem> candidateItems = FilesystemViewModel.FilesAndFolders.Where(f => f.ItemName.Length >= value.Length && f.ItemName.Substring(0, value.Length).ToLower() == value);

                    if (IsItemSelected)
                    {
                        previouslySelectedItem = SelectedItem;
                    }

                    // If the user is trying to cycle through items
                    // starting with the same letter
                    if (value.Length == 1 && previouslySelectedItem != null)
                    {
                        // Try to select item lexicographically bigger than the previous item
                        jumpedToItem = candidateItems.FirstOrDefault(f => f.ItemName.CompareTo(previouslySelectedItem.ItemName) > 0);
                    }
                    if (jumpedToItem == null)
                    {
                        jumpedToItem = candidateItems.FirstOrDefault();
                    }

                    if (jumpedToItem != null)
                    {
                        ItemManipulationModel.SetSelectedItem(jumpedToItem);
                        ItemManipulationModel.ScrollIntoView(jumpedToItem);
                    }

                    // Restart the timer
                    jumpTimer.Start();
                }
                jumpString = value;
            }
        }

        private List<ListedItem> selectedItems = new List<ListedItem>();

        public List<ListedItem> SelectedItems
        {
            get => selectedItems;
            set
            {
                if (value != selectedItems)
                {
                    selectedItems = value;
                    if (selectedItems.Count == 0 || selectedItems[0] == null)
                    {
                        IsItemSelected = false;
                        SelectedItem = null;
                        SelectedItemsPropertiesViewModel.IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                        SelectedItem = selectedItems.First();
                        SelectedItemsPropertiesViewModel.IsItemSelected = true;

                        if (SelectedItems.Count >= 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCount = SelectedItems.Count;
                        }

                        if (SelectedItems.Count == 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{SelectedItems.Count} {"ItemSelected/Text".GetLocalized()}";
                            SelectedItemsPropertiesViewModel.ItemSize = SelectedItem.FileSize;
                        }
                        else
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{SelectedItems.Count} {"ItemsSelected/Text".GetLocalized()}";

                            if (SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File))
                            {
                                long size = 0;
                                foreach (var item in SelectedItems)
                                {
                                    size += item.FileSizeBytes;
                                }
                                SelectedItemsPropertiesViewModel.ItemSize = ByteSizeLib.ByteSize.FromBytes(size).ToBinaryString().ConvertSizeAbbreviation();
                            }
                            else
                            {
                                SelectedItemsPropertiesViewModel.ItemSize = string.Empty;
                            }
                        }
                    }
                    SetProperty(ref selectedItems, value);
                    ItemManipulationModel.SetDragModeForItems();
                }
            }
        }

        public ListedItem SelectedItem { get; private set; }

        private DispatcherQueueTimer dragOverTimer;

        public BaseLayoutViewModel(ItemViewModel itemViewModel, CurrentInstanceViewModel currentInstanceViewModel, NavigationArguments navigationArguments)
        {
            ItemManipulationModel = new ItemManipulationModel();
            FilesystemViewModel = itemViewModel;
            InstanceViewModel = currentInstanceViewModel;

            HookBaseEvents();
            HookEvents();

            jumpTimer = new DispatcherTimer();
            jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
            jumpTimer.Tick += JumpTimer_Tick;

            SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel(this);
            DirectoryPropertiesViewModel = new DirectoryPropertiesViewModel();

            // QuickLook Integration
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isQuickLookIntegrationEnabled = localSettings.Values["quicklook_enabled"];

            if (isQuickLookIntegrationEnabled != null && isQuickLookIntegrationEnabled.Equals(true))
            {
                App.InteractionViewModel.IsQuickLookEnabled = true;
            }

            InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;

            FilesystemViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
            InstanceViewModel.FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
            FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;

            UpdateFilesystemViewModelConnection();




            dragOverTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();

            // Add item jumping handler
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;

            InitializeCommandsViewModel();

            IsItemSelected = false;
            
            FilesystemViewModel.IsFolderEmptyTextDisplayed = false;
            

            InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
            FilesystemViewModel.UpdateGroupOptions();
            UpdateCollectionViewSource();
            InstanceViewModel.FolderSettings.IsLayoutModeChanging = false;

            ItemManipulationModel.FocusFileList(); // Set focus on layout specific file list control

            try
            {
                if (navigationArguments.SelectItems != null && navigationArguments.SelectItems.Count() > 0)
                {
                    List<ListedItem> liItemsToSelect = new List<ListedItem>();
                    foreach (string item in navigationArguments.SelectItems)
                    {
                        liItemsToSelect.Add(FilesystemViewModel.FilesAndFolders.Where((li) => li.ItemName == item).First());
                    }

                    ItemManipulationModel.SetSelectedItems(liItemsToSelect);
                }
            }
            catch (Exception)
            {
            }

            ItemContextMenuFlyout.Opening += ItemContextFlyout_Opening;
            BaseContextMenuFlyout.Opening += BaseContextFlyout_Opening;


        }

        private void FolderSettings_LayoutPreferencesUpdateRequired(object sender, LayoutPreferenceEventArgs e)
        {
            if (FilesystemViewModel != null)
            {
                (sender as FolderSettingsViewModel).UpdateLayoutPreferencesForPath(FilesystemViewModel.WorkingDirectory, e.LayoutPreference);
                if (e.IsAdaptiveLayoutUpdateRequired)
                {
                    AdaptiveLayoutHelpers.PredictLayoutMode(InstanceViewModel.FolderSettings, FilesystemViewModel);
                }
            }
        }

        private void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
        {
            InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
        }

        private void FilesystemViewModel_DirectoryInfoUpdated(object sender, EventArgs e)
        {
            if (FilesystemViewModel.FilesAndFolders.Count == 1)
            {
                DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemCount/Text".GetLocalized()}";
            }
            else
            {
                DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemsCount/Text".GetLocalized()}";
            }
        }

        private void AppSettings_SortDirectionPreferenceUpdated(object sender, EventArgs e)
        {
            FilesystemViewModel?.UpdateSortDirectionStatus();
        }

        private void AppSettings_SortOptionPreferenceUpdated(object sender, EventArgs e)
        {
            FilesystemViewModel?.UpdateSortOptionStatus();
        }

        private async void UpdateFilesystemViewModelConnection()
        {
            var serviceConnection = await AppServiceConnectionHelper.Instance;
            FilesystemViewModel.OnAppServiceConnectionChanged(serviceConnection);
        }

        protected abstract void HookEvents();

        protected abstract void UnhookEvents();

        private void HookBaseEvents()
        {
            ItemManipulationModel.RefreshItemsOpacityInvoked += ItemManipulationModel_RefreshItemsOpacityInvoked;
        }

        private void UnhookBaseEvents()
        {
            ItemManipulationModel.RefreshItemsOpacityInvoked -= ItemManipulationModel_RefreshItemsOpacityInvoked;
        }

        public ItemManipulationModel ItemManipulationModel { get; private set; }


        private void JumpTimer_Tick(object sender, object e)
        {
            jumpString = string.Empty;
            jumpTimer.Stop();
        }

        protected abstract void InitializeCommandsViewModel();

        protected IEnumerable<ListedItem> GetAllItems()
        {
            if (CollectionViewSource.IsSourceGrouped)
            {
                // add all items from each group to the new list
                return (CollectionViewSource.Source as BulkConcurrentObservableCollection<GroupedCollection<ListedItem>>)?.SelectMany(g => g);
            }

            return CollectionViewSource.Source as IEnumerable<ListedItem>;
        }

        public virtual void ResetItemOpacity()
        {
            IEnumerable items = GetAllItems();
            if (items == null)
            {
                return;
            }

            foreach (ListedItem listedItem in items)
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

        protected abstract ListedItem GetItemFromElement(object element);

        private CancellationTokenSource groupingCancellationToken;
        private async void FolderSettings_GroupOptionPreferenceUpdated(object sender, EventArgs e)
        {
            // Two or more of these running at the same time will cause a crash, so cancel the previous one before beginning
            groupingCancellationToken?.Cancel();
            groupingCancellationToken = new CancellationTokenSource();
            var token = groupingCancellationToken.Token;
            await FilesystemViewModel.GroupOptionsUpdated(token);
            UpdateCollectionViewSource();
            await FilesystemViewModel.ReloadItemGroupHeaderImagesAsync();
        }

        public void ItemContextFlyout_Opening(object sender, object e)
        {
            try
            {
                LoadMenuItemsAsync();
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        public async void BaseContextFlyout_Opening(object sender, object e)
        {
            try
            {
                var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                var items = ContextFlyoutItemHelper.GetBaseContextCommands(connection: await AppServiceConnectionHelper.Instance, currentInstanceViewModel: InstanceViewModel, itemViewModel: FilesystemViewModel, commandsViewModel: CommandsViewModel, shiftPressed: shiftPressed, false);
                BaseContextMenuFlyout.Items.Clear();
                ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(items).ForEach(i => BaseContextMenuFlyout.Items.Add(i));
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private async void LoadMenuItemsAsync()
        {
            SelectedItemsPropertiesViewModel.CheckFileExtension();
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var items = ContextFlyoutItemHelper.GetItemContextCommands(connection: await AppServiceConnectionHelper.Instance, currentInstanceViewModel: InstanceViewModel, workingDir: FilesystemViewModel.WorkingDirectory, selectedItems: SelectedItems, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel, shiftPressed: shiftPressed, showOpenMenu: false);
            ItemContextMenuFlyout.PrimaryCommands.Clear();
            ItemContextMenuFlyout.SecondaryCommands.Clear();
            var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
            primaryElements.ForEach(i => ItemContextMenuFlyout.PrimaryCommands.Add(i));
            secondaryElements.ForEach(i => ItemContextMenuFlyout.SecondaryCommands.Add(i));
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance.IsCurrentInstance)
            {
                char letter = Convert.ToChar(args.KeyCode);
                JumpString += letter.ToString().ToLowerInvariant();
            }
        }

        private async void Item_DragStarting(object sender, DragStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();

            if (sender is DataGridRow dataGridRow)
            {
                if (dataGridRow.DataContext is ListedItem item)
                {
                    SelectedItems.Add(item);
                }
            }

            foreach (ListedItem item in SelectedItems)
            {
                if (item is ShortcutItem)
                {
                    // Can't drag shortcut items
                    continue;
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    await FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    await FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
            }

            if (selectedStorageItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Data.SetStorageItems(selectedStorageItems, false);
            e.DragUI.SetContentFromDataPackage();
        }

        protected async void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();

            foreach (var itemObj in e.Items)
            {
                var item = itemObj as ListedItem;
                if (item == null || item is ShortcutItem)
                {
                    // Can't drag shortcut items
                    continue;
                }

                SelectedItems.Add(item);
                if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    await FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    await FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
            }

            if (selectedStorageItems.Count > 0)
            {
                e.Data.SetStorageItems(selectedStorageItems, false);
            }
            else
            {
                e.Cancel = true;
            }
        }

        private ListedItem dragOverItem = null;

        private void Item_DragLeave(object sender, DragEventArgs e)
        {
            ListedItem item = GetItemFromElement(sender);
            if (item == dragOverItem)
            {
                // Reset dragged over item
                dragOverItem = null;
            }
        }

        protected async void Item_DragOver(object sender, DragEventArgs e)
        {
            ListedItem item = GetItemFromElement(sender);

            if (item is null && sender is GridViewItem gvi)
            {
                item = gvi.Content as ListedItem;
            }
            else if (item is null && sender is ListViewItem lvi)
            {
                item = lvi.Content as ListedItem;
            }

            if (item is null)
            {
                return;
            }

            var deferral = e.GetDeferral();

            ItemManipulationModel.SetSelectedItem(item);

            if (dragOverItem != item)
            {
                dragOverItem = item;
                dragOverTimer.Stop();
                dragOverTimer.Debounce(() =>
                {
                    if (dragOverItem != null && !InstanceViewModel.IsPageTypeSearchResults && !dragOverItem.IsExecutable)
                    {
                        dragOverItem = null;
                        dragOverTimer.Stop();
                        NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                    }
                }, TimeSpan.FromMilliseconds(1000), false);
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> draggedItems;
                try
                {
                    draggedItems = await e.DataView.GetStorageItemsAsync();
                }
                catch (Exception ex) when ((uint)ex.HResult == 0x80040064)
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }

                e.Handled = true;
                e.DragUIOverride.IsCaptionVisible = true;

                if (InstanceViewModel.IsPageTypeSearchResults || draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else if (item.IsExecutable)
                {
                    e.DragUIOverride.Caption = $"{"OpenItemsWithCaptionText".GetLocalized()} {item.ItemName}";
                    e.AcceptedOperation = DataPackageOperation.Link;
                } // Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
                else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            deferral.Complete();
        }

        protected async void Item_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            e.Handled = true;
            dragOverItem = null; // Reset dragged over item

            ListedItem rowItem = GetItemFromElement(sender);
            if (rowItem != null)
            {
                await ParentShellPageInstance.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (rowItem as ShortcutItem)?.TargetPath ?? rowItem.ItemPath, false, true, rowItem.IsExecutable);
            }
            deferral.Complete();
        }

        protected void InitializeDrag(UIElement element)
        {
            ListedItem item = GetItemFromElement(element);
            if (item != null)
            {
                element.AllowDrop = false;
                element.DragStarting -= Item_DragStarting;
                element.DragOver -= Item_DragOver;
                element.DragLeave -= Item_DragLeave;
                element.Drop -= Item_Drop;
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder || (item.IsShortcutItem && (Path.GetExtension((item as ShortcutItem).TargetPath)?.Contains("exe") ?? false)))
                {
                    element.AllowDrop = true;
                    element.DragOver += Item_DragOver;
                    element.DragLeave += Item_DragLeave;
                    element.Drop += Item_Drop;
                }
            }
        }

        protected void UninitializeDrag(UIElement element)
        {
            element.AllowDrop = false;
            element.DragStarting -= Item_DragStarting;
            element.DragOver -= Item_DragOver;
            element.DragLeave -= Item_DragLeave;
            element.Drop -= Item_Drop;
        }

        // VirtualKey doesn't support / accept plus and minus by default.
        public readonly VirtualKey PlusKey = (VirtualKey)187;

        public readonly VirtualKey MinusKey = (VirtualKey)189;

        protected void ItemsLayout_DragEnter(object sender, DragEventArgs e)
        {
            CommandsViewModel?.DragEnterCommand?.Execute(e);
        }

        protected void ItemsLayout_Drop(object sender, DragEventArgs e)
        {
            CommandsViewModel?.DropCommand?.Execute(e);
        }

        public void UpdateCollectionViewSource()
        {
            if (FilesystemViewModel.FilesAndFolders.IsGrouped)
            {
                CollectionViewSource = new Windows.UI.Xaml.Data.CollectionViewSource()
                {
                    IsSourceGrouped = true,
                    Source = FilesystemViewModel.FilesAndFolders.GroupedCollection
                };
            }
            else
            {
                CollectionViewSource = new Windows.UI.Xaml.Data.CollectionViewSource()
                {
                    IsSourceGrouped = false,
                    Source = FilesystemViewModel.FilesAndFolders
                };
            }
        }

        protected void SemanticZoom_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
        {
            if (!e.IsSourceZoomedInView)
            {
                // According to the docs this isn't necessary, but it would crash otherwise
                var destination = e.DestinationItem.Item as GroupedCollection<ListedItem>;
                e.DestinationItem.Item = destination.FirstOrDefault();
            }
        }

        protected void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
            if (!(element is null))
            {
                VisualStateManager.GoToState(element, "PointerOver", true);
            }
        }

        protected void StackPanel_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
            if (!(element is null))
            {
                VisualStateManager.GoToState(element, "Normal", true);
            }
        }

        protected void RootPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
            if (!(element is null))
            {
                VisualStateManager.GoToState(element, "Pressed", true);
            }
        }

        private void ItemManipulationModel_RefreshItemsOpacityInvoked(object sender, EventArgs e)
        {
            foreach (ListedItem listedItem in GetAllItems())
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

        public void Dispose()
        {
            InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
            InstanceViewModel.FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;

            FilesystemViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
            FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;

            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;

            ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
            BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

            FilesystemViewModel.CancelLoadAndClearFiles();
            FilesystemViewModel.Dispose();
        }
    }
}