using Files.EventArguments;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.ContextFlyouts;
using Files.Interacts;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using static Files.Helpers.PathNormalization;

namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public abstract class BaseLayout : Page, IBaseLayout, INotifyPropertyChanged
    {
        private readonly DispatcherTimer jumpTimer;

        protected NamedPipeAsAppServiceConnection Connection => ParentShellPageInstance?.ServiceConnection;

        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

        public SettingsViewModel AppSettings => App.AppSettings;

        public FolderSettingsViewModel FolderSettings => ParentShellPageInstance.InstanceViewModel.FolderSettings;

        public CurrentInstanceViewModel InstanceViewModel => ParentShellPageInstance.InstanceViewModel;

        public InteractionViewModel InteractionViewModel => App.InteractionViewModel;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

        public Microsoft.UI.Xaml.Controls.CommandBarFlyout ItemContextMenuFlyout { get; set; } = new Microsoft.UI.Xaml.Controls.CommandBarFlyout();
        public MenuFlyout BaseContextMenuFlyout { get; set; } = new MenuFlyout();

        public BaseLayoutCommandsViewModel CommandsViewModel { get; protected set; }

        public IShellPage ParentShellPageInstance { get; private set; } = null;

        public bool IsRenamingItem { get; set; } = false;

        private CollectionViewSource collectionViewSource = new CollectionViewSource()
        {
            IsSourceGrouped = true,
        };

        public CollectionViewSource CollectionViewSource
        {
            get => collectionViewSource;
            set
            {
                if (collectionViewSource != value)
                {
                    collectionViewSource = value;
                    NotifyPropertyChanged(nameof(CollectionViewSource));
                }
            }
        }

        private NavigationArguments navigationArguments;

        private bool isItemSelected = false;

        public bool IsItemSelected
        {
            get
            {
                return isItemSelected;
            }
            internal set
            {
                if (value != isItemSelected)
                {
                    isItemSelected = value;
                    NotifyPropertyChanged(nameof(IsItemSelected));
                }
            }
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
                    IEnumerable<ListedItem> candidateItems = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.Where(f => f.ItemName.Length >= value.Length && f.ItemName.Substring(0, value.Length).ToLower() == value);

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
            get
            {
                return selectedItems;
            }
            internal set
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
                    NotifyPropertyChanged(nameof(SelectedItems));
                    ItemManipulationModel.SetDragModeForItems();
                }
            }
        }

        public ListedItem SelectedItem { get; private set; }

        private DispatcherQueueTimer dragOverTimer;

        public BaseLayout()
        {
            ItemManipulationModel = new ItemManipulationModel();

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

            dragOverTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
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

        private void FolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
        {
            if (ParentShellPageInstance.SlimContentPage != null)
            {
                var layoutType = FolderSettings.GetLayoutType(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory);

                if (layoutType != ParentShellPageInstance.CurrentPageType)
                {
                    FolderSettings.IsLayoutModeChanging = true;
                    ParentShellPageInstance.NavigateWithArguments(layoutType, new NavigationArguments()
                    {
                        NavPathParam = navigationArguments.NavPathParam,
                        IsSearchResultPage = navigationArguments.IsSearchResultPage,
                        SearchPathParam = navigationArguments.SearchPathParam,
                        SearchResults = navigationArguments.SearchResults,
                        IsLayoutSwitch = true,
                        AssociatedTabInstance = ParentShellPageInstance
                    });

                    // Remove old layout from back stack
                    ParentShellPageInstance.RemoveLastPageFromBackStack();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            // Add item jumping handler
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
            navigationArguments = (NavigationArguments)eventArgs.Parameter;
            ParentShellPageInstance = navigationArguments.AssociatedTabInstance;
            InitializeCommandsViewModel();

            IsItemSelected = false;
            FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
            FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
            ParentShellPageInstance.FilesystemViewModel.IsFolderEmptyTextDisplayed = false;
            FolderSettings.SetLayoutInformation();

            if (!navigationArguments.IsSearchResultPage)
            {
                ParentShellPageInstance.NavigationToolbar.CanRefresh = true;
                string previousDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
                await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

                // pathRoot will be empty on recycle bin path
                var workingDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
                string pathRoot = GetPathRoot(workingDir);
                if (string.IsNullOrEmpty(pathRoot) || workingDir.StartsWith(AppSettings.RecycleBinPath)) // Can't go up from recycle bin
                {
                    ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = false;
                }
                else
                {
                    ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = true;
                }

                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(App.AppSettings.RecycleBinPath);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\");
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
                ParentShellPageInstance.NavigationToolbar.PathControlDisplayText = navigationArguments.NavPathParam;
                if (!navigationArguments.IsLayoutSwitch)
                {
                    ParentShellPageInstance.FilesystemViewModel.RefreshItems(previousDir);
                }
                else
                {
                    ParentShellPageInstance.NavigationToolbar.CanGoForward = false;
                }
            }
            else
            {
                ParentShellPageInstance.NavigationToolbar.CanRefresh = false;
                ParentShellPageInstance.NavigationToolbar.CanGoForward = false;
                ParentShellPageInstance.NavigationToolbar.CanGoBack = true;  // Impose no artificial restrictions on back navigation. Even in a search results page.
                ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;
                if (!navigationArguments.IsLayoutSwitch)
                {
                    await ParentShellPageInstance.FilesystemViewModel.AddSearchResultsToCollection(navigationArguments.SearchResults, navigationArguments.SearchPathParam);
                    var displayName = App.LibraryManager.TryGetLibrary(navigationArguments.SearchPathParam, out var lib) ? lib.Text : navigationArguments.SearchPathParam;
                    ParentShellPageInstance.UpdatePathUIToWorkingDirectory(null, $"{"SearchPagePathBoxOverrideText".GetLocalized()} {displayName}");
                }
            }

            ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
            ParentShellPageInstance.LoadPreviewPaneChanged();
            ParentShellPageInstance.FilesystemViewModel.UpdateGroupOptions();
            UpdateCollectionViewSource();
            FolderSettings.IsLayoutModeChanging = false;

            ItemManipulationModel.FocusFileList(); // Set focus on layout specific file list control

            try
            {
                if (navigationArguments.SelectItems != null && navigationArguments.SelectItems.Count() > 0)
                {
                    List<ListedItem> liItemsToSelect = new List<ListedItem>();
                    foreach (string item in navigationArguments.SelectItems)
                    {
                        liItemsToSelect.Add(ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.Where((li) => li.ItemName == item).First());
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

        private CancellationTokenSource groupingCancellationToken;
        private async void FolderSettings_GroupOptionPreferenceUpdated(object sender, EventArgs e)
        {
            // Two or more of these running at the same time will cause a crash, so cancel the previous one before beginning
            groupingCancellationToken?.Cancel();
            groupingCancellationToken = new CancellationTokenSource();
            var token = groupingCancellationToken.Token;
            await ParentShellPageInstance.FilesystemViewModel.GroupOptionsUpdated(token);
            UpdateCollectionViewSource();
            await ParentShellPageInstance.FilesystemViewModel.ReloadItemGroupHeaderImagesAsync();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
            FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
            FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
            ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
            BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

            var parameter = e.Parameter as NavigationArguments;
            if (!parameter.IsLayoutSwitch)
            {
                ParentShellPageInstance.FilesystemViewModel.CancelLoadAndClearFiles();
            }
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

        public void BaseContextFlyout_Opening(object sender, object e)
        {
            try
            {
                var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                var items = ContextFlyoutItemHelper.GetBaseContextCommands(connection: Connection, currentInstanceViewModel: InstanceViewModel, itemViewModel: ParentShellPageInstance.FilesystemViewModel, commandsViewModel: CommandsViewModel, shiftPressed: shiftPressed, false);
                BaseContextMenuFlyout.Items.Clear();
                ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(items).ForEach(i => BaseContextMenuFlyout.Items.Add(i));
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private void LoadMenuItemsAsync()
        {
            SelectedItemsPropertiesViewModel.CheckFileExtension();
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var items = ContextFlyoutItemHelper.GetItemContextCommands(connection: Connection, currentInstanceViewModel: InstanceViewModel, workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, selectedItems: SelectedItems, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel, shiftPressed: shiftPressed, showOpenMenu: false);
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
                    ParentShellPageInstance.SlimContentPage.SelectedItems.Add(item);
                }
            }

            foreach (ListedItem item in ParentShellPageInstance.SlimContentPage.SelectedItems)
            {
                if (item is ShortcutItem)
                {
                    // Can't drag shortcut items
                    continue;
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    await ParentShellPageInstance.FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    await ParentShellPageInstance.FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
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
                    await ParentShellPageInstance.FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    await ParentShellPageInstance.FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
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
                    App.Logger.Warn(ex, ex.Message);
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

        public abstract void Dispose();

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
            if (ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.IsGrouped)
            {
                CollectionViewSource = new Windows.UI.Xaml.Data.CollectionViewSource()
                {
                    IsSourceGrouped = true,
                    Source = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.GroupedCollection
                };
            }
            else
            {
                CollectionViewSource = new Windows.UI.Xaml.Data.CollectionViewSource()
                {
                    IsSourceGrouped = false,
                    Source = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders
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
    }
}