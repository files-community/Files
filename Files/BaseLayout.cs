using Files.Enums;
using Files.EventArguments;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Helpers.ContextFlyouts;
using Files.Interacts;
using Files.Services;
using Files.UserControls;
using Files.ViewModels;
using Files.ViewModels.Previews;
using Files.Views;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

        protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        protected IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>();

        protected Task<NamedPipeAsAppServiceConnection> Connection => AppServiceConnectionHelper.Instance;

        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

        public SettingsViewModel AppSettings => App.AppSettings;

        public FolderSettingsViewModel FolderSettings => ParentShellPageInstance.InstanceViewModel.FolderSettings;

        public CurrentInstanceViewModel InstanceViewModel => ParentShellPageInstance.InstanceViewModel;

        public MainViewModel MainViewModel => App.MainViewModel;
        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

        public Microsoft.UI.Xaml.Controls.CommandBarFlyout ItemContextMenuFlyout { get; set; } = new Microsoft.UI.Xaml.Controls.CommandBarFlyout()
        {
            AlwaysExpanded = true,
        };
        public Microsoft.UI.Xaml.Controls.CommandBarFlyout BaseContextMenuFlyout { get; set; } = new Microsoft.UI.Xaml.Controls.CommandBarFlyout();

        public BaseLayoutCommandsViewModel CommandsViewModel { get; protected set; }

        public IShellPage ParentShellPageInstance { get; private set; } = null;

        public PreviewPaneViewModel PreviewPaneViewModel { get; } = new PreviewPaneViewModel();

        public bool IsRenamingItem { get; set; } = false;
        public ListedItem RenamingItem { get; set; } = null;

        public string OldItemName { get; set; } = null;

        public TextBlock RenamingTextBlock { get; set; } = null;

        private bool isMiddleClickToScrollEnabled = true;

        public bool IsMiddleClickToScrollEnabled
        {
            get => isMiddleClickToScrollEnabled;
            set
            {
                if (isMiddleClickToScrollEnabled != value)
                {
                    isMiddleClickToScrollEnabled = value;
                    NotifyPropertyChanged(nameof(IsMiddleClickToScrollEnabled));
                }
            }
        }

        protected NavigationToolbar NavToolbar => (Window.Current.Content as Frame).FindDescendant<NavigationToolbar>();

        private CollectionViewSource collectionViewSource = new CollectionViewSource()
        {
            IsSourceGrouped = true,
        };

        public CollectionViewSource CollectionViewSource
        {
            get => collectionViewSource;
            set
            {
                if (collectionViewSource == value)
                {
                    return;
                }
                if (collectionViewSource?.View is not null)
                {
                    collectionViewSource.View.VectorChanged -= View_VectorChanged;
                }
                collectionViewSource = value;
                NotifyPropertyChanged(nameof(CollectionViewSource));
                if (collectionViewSource?.View is not null)
                {
                    collectionViewSource.View.VectorChanged += View_VectorChanged;
                }
            }
        }

        protected NavigationArguments navigationArguments;

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
                        ItemManipulationModel.FocusSelectedItems();
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
                //if (!(value?.All(x => selectedItems?.Contains(x) ?? false) ?? value == selectedItems)) // check if the new list is different then the old one
                if (value != selectedItems) // check if the new list is different then the old one
                {
                    if (value?.FirstOrDefault() != selectedItems?.FirstOrDefault())
                    {
                        // update preview pane properties
                        if (value?.Count == 1)
                        {
                            PreviewPaneViewModel.IsItemSelected = true;
                            PreviewPaneViewModel.SelectedItem = value.First();
                        }
                        else
                        {
                            PreviewPaneViewModel.IsItemSelected = value?.Count > 0;
                            PreviewPaneViewModel.SelectedItem = null;
                        }

                        // check if the preview pane is open before updating the model
                        if (((Window.Current.Content as Frame)?.Content as MainPage)?.LoadPreviewPane ?? false)
                        {
                            PreviewPaneViewModel.UpdateSelectedItemPreview();
                        }
                    }

                    selectedItems = value;
                    if (selectedItems.Count == 0 || selectedItems[0] == null)
                    {
                        IsItemSelected = false;
                        SelectedItem = null;
                        SelectedItemsPropertiesViewModel.IsItemSelected = false;
                        ResetRenameDoubleClick();
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
                            DispatcherQueue.GetForCurrentThread().EnqueueAsync(async () =>
                            {
                                await Task.Delay(50); // Tapped event must be executed first
                                preRenamingItem = SelectedItem;
                            });
                        }
                        else
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{SelectedItems.Count} {"ItemsSelected/Text".GetLocalized()}";
                            ResetRenameDoubleClick();

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
                    //ItemManipulationModel.SetDragModeForItems();
                }

                ParentShellPageInstance.NavToolbarViewModel.SelectedItems = value;
            }
        }

        public ListedItem SelectedItem { get; private set; }

        private DispatcherQueueTimer dragOverTimer, tapDebounceTimer;

        public BaseLayout()
        {
            ItemManipulationModel = new ItemManipulationModel();

            HookBaseEvents();
            HookEvents();

            jumpTimer = new DispatcherTimer();
            jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
            jumpTimer.Tick += JumpTimer_Tick;

            SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel();
            DirectoryPropertiesViewModel = new DirectoryPropertiesViewModel();

            dragOverTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            tapDebounceTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
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
            var items = GetAllItems();
            if (items == null)
            {
                return;
            }

            foreach (var item in items.ToList()) // ToList() is necessary
            {
                if (item != null)
                {
                    item.Opacity = item.IsHiddenItem ? Constants.UI.DimItemOpacity : 1.0d;
                }
            }
        }

        protected abstract ListedItem GetItemFromElement(object element);

        protected virtual void BaseFolderSettings_LayoutModeChangeRequested(object sender, LayoutModeEventArgs e)
        {
            if (ParentShellPageInstance.SlimContentPage != null)
            {
                var layoutType = FolderSettings.GetLayoutType(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, false);

                if (layoutType != ParentShellPageInstance.CurrentPageType)
                {
                    FolderSettings.IsLayoutModeChanging = true;
                    ParentShellPageInstance.NavigateWithArguments(layoutType, new NavigationArguments()
                    {
                        NavPathParam = navigationArguments.NavPathParam,
                        IsSearchResultPage = navigationArguments.IsSearchResultPage,
                        SearchPathParam = navigationArguments.SearchPathParam,
                        SearchQuery = navigationArguments.SearchQuery,
                        SearchUnindexedItems = navigationArguments.SearchUnindexedItems,
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
            FolderSettings.LayoutModeChangeRequested += BaseFolderSettings_LayoutModeChangeRequested;
            FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
            ParentShellPageInstance.FilesystemViewModel.EmptyTextType = EmptyTextType.None;
            FolderSettings.SetLayoutInformation();
            ParentShellPageInstance.NavToolbarViewModel.UpdateSortAndGroupOptions();

            if (!navigationArguments.IsSearchResultPage)
            {
                ParentShellPageInstance.NavToolbarViewModel.CanRefresh = true;
                string previousDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
                await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

                // pathRoot will be empty on recycle bin path
                var workingDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory ?? string.Empty;
                string pathRoot = GetPathRoot(workingDir);
                if (string.IsNullOrEmpty(pathRoot) || workingDir.StartsWith(CommonPaths.RecycleBinPath)) // Can't go up from recycle bin
                {
                    ParentShellPageInstance.NavToolbarViewModel.CanNavigateToParent = false;
                }
                else
                {
                    ParentShellPageInstance.NavToolbarViewModel.CanNavigateToParent = true;
                }

                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(CommonPaths.RecycleBinPath);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\");
                ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryHelper.IsLibraryPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
                ParentShellPageInstance.NavToolbarViewModel.PathControlDisplayText = navigationArguments.NavPathParam;
                if (!navigationArguments.IsLayoutSwitch || previousDir != workingDir)
                {
                    ParentShellPageInstance.FilesystemViewModel.RefreshItems(previousDir, SetSelectedItemsOnNavigation);
                }
                else
                {
                    ParentShellPageInstance.NavToolbarViewModel.CanGoForward = false;
                }
            }
            else
            {
                ParentShellPageInstance.NavToolbarViewModel.CanRefresh = true;
                ParentShellPageInstance.NavToolbarViewModel.CanGoForward = false;
                ParentShellPageInstance.NavToolbarViewModel.CanGoBack = true;  // Impose no artificial restrictions on back navigation. Even in a search results page.
                ParentShellPageInstance.NavToolbarViewModel.CanNavigateToParent = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;

                await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(navigationArguments.SearchPathParam);

                if (!navigationArguments.IsLayoutSwitch)
                {
                    var displayName = App.LibraryManager.TryGetLibrary(navigationArguments.SearchPathParam, out var lib) ? lib.Text : navigationArguments.SearchPathParam;
                    ParentShellPageInstance.UpdatePathUIToWorkingDirectory(null, string.Format("SearchPagePathBoxOverrideText".GetLocalized(), navigationArguments.SearchQuery, displayName));
                    var searchInstance = new Filesystem.Search.FolderSearch
                    {
                        Query = navigationArguments.SearchQuery,
                        Folder = navigationArguments.SearchPathParam,
                        ThumbnailSize = InstanceViewModel.FolderSettings.GetIconSize(),
                        SearchUnindexedItems = navigationArguments.SearchUnindexedItems
                    };
                    _ = ParentShellPageInstance.FilesystemViewModel.SearchAsync(searchInstance);
                }
            }

            ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
            ParentShellPageInstance.FilesystemViewModel.UpdateGroupOptions();
            UpdateCollectionViewSource();
            FolderSettings.IsLayoutModeChanging = false;

            ItemManipulationModel.FocusFileList(); // Set focus on layout specific file list control

            SetSelectedItemsOnNavigation();

            ItemContextMenuFlyout.Opening += ItemContextFlyout_Opening;
            BaseContextMenuFlyout.Opening += BaseContextFlyout_Opening;
        }

        public void SetSelectedItemsOnNavigation()
        {
            try
            {
                if (navigationArguments != null && navigationArguments.SelectItems != null && navigationArguments.SelectItems.Any())
                {
                    List<ListedItem> liItemsToSelect = new List<ListedItem>();
                    foreach (string item in navigationArguments.SelectItems)
                    {
                        liItemsToSelect.Add(ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.Where((li) => li.ItemName == item).First());
                    }

                    ItemManipulationModel.SetSelectedItems(liItemsToSelect);
                    ItemManipulationModel.FocusSelectedItems();
                }
            }
            catch (Exception)
            {
            }
        }

        private CancellationTokenSource groupingCancellationToken;

        private async void FolderSettings_GroupOptionPreferenceUpdated(object sender, GroupOption e)
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
            FolderSettings.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
            FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
            ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
            BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

            var parameter = e.Parameter as NavigationArguments;
            if (!parameter.IsLayoutSwitch)
            {
                ParentShellPageInstance.FilesystemViewModel.CancelLoadAndClearFiles();
            }
        }

        public async void ItemContextFlyout_Opening(object sender, object e)
        {
            try
            {
                if (!IsItemSelected) // Workaround for item sometimes not getting selected
                {
                    if (((sender as Microsoft.UI.Xaml.Controls.CommandBarFlyout)?.Target as ListViewItem)?.Content is ListedItem li)
                    {
                        ItemManipulationModel.SetSelectedItem(li);
                    }
                }
                if (IsItemSelected)
                {
                    await LoadMenuItemsAsync();
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private CancellationTokenSource shellContextMenuItemCancellationToken;

        public async void BaseContextFlyout_Opening(object sender, object e)
        {
            try
            {
                if (BaseContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
                {
                    itc.MaxHeight = Constants.UI.ContextMenuMaxHeight; // Reset menu max height
                }
                shellContextMenuItemCancellationToken?.Cancel();
                shellContextMenuItemCancellationToken = new CancellationTokenSource();
                var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                var items = ContextFlyoutItemHelper.GetBaseContextCommandsWithoutShellItems(connection: await Connection, currentInstanceViewModel: InstanceViewModel, itemViewModel: ParentShellPageInstance.FilesystemViewModel, commandsViewModel: CommandsViewModel, shiftPressed: shiftPressed, false);
                BaseContextMenuFlyout.PrimaryCommands.Clear();
                BaseContextMenuFlyout.SecondaryCommands.Clear();
                var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
                primaryElements.Where(i => i is AppBarButton).ForEach(i =>
                {
                    (i as AppBarButton).Click += new RoutedEventHandler((s, e) => BaseContextMenuFlyout.Hide());  // Workaround for WinUI (#5508)
                });
                primaryElements.ForEach(i => BaseContextMenuFlyout.PrimaryCommands.Add(i));
                secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
                secondaryElements.ForEach(i => BaseContextMenuFlyout.SecondaryCommands.Add(i));

                if (!InstanceViewModel.IsPageTypeSearchResults)
                {
                    var shellMenuItems = await ContextFlyoutItemHelper.GetBaseContextShellCommandsAsync(connection: await Connection, currentInstanceViewModel: InstanceViewModel, workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, shiftPressed: shiftPressed, showOpenMenu: false);
                    if (shellContextMenuItemCancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!InstanceViewModel.IsPageTypeZipFolder)
                    {
                        AddShellItemsToMenu(shellMenuItems, BaseContextMenuFlyout, shiftPressed);
                    }
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private async Task LoadMenuItemsAsync()
        {
            if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
            {
                itc.MaxHeight = Constants.UI.ContextMenuMaxHeight; // Reset menu max height
            }
            shellContextMenuItemCancellationToken?.Cancel();
            shellContextMenuItemCancellationToken = new CancellationTokenSource();
            SelectedItemsPropertiesViewModel.CheckFileExtension(SelectedItem?.FileExtension);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel, workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, selectedItems: SelectedItems, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel, shiftPressed: shiftPressed, showOpenMenu: false);
            ItemContextMenuFlyout.PrimaryCommands.Clear();
            ItemContextMenuFlyout.SecondaryCommands.Clear();
            var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
            primaryElements.Where(i => i is AppBarButton).ForEach(i =>
            {
                (i as AppBarButton).Click += new RoutedEventHandler((s, e) => ItemContextMenuFlyout.Hide()); // Workaround for WinUI (#5508)
            });
            primaryElements.ForEach(i => ItemContextMenuFlyout.PrimaryCommands.Add(i));
            secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
            secondaryElements.ForEach(i => ItemContextMenuFlyout.SecondaryCommands.Add(i));

            if (UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled && !InstanceViewModel.IsPageTypeSearchResults && !InstanceViewModel.IsPageTypeRecycleBin && !InstanceViewModel.IsPageTypeFtp && !InstanceViewModel.IsPageTypeZipFolder)
            {
                AddFileTagsItemToMenu(ItemContextMenuFlyout);
            }

            var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(connection: await Connection, currentInstanceViewModel: InstanceViewModel, workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, selectedItems: SelectedItems, shiftPressed: shiftPressed, showOpenMenu: false);
            if (shellContextMenuItemCancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!InstanceViewModel.IsPageTypeZipFolder)
            {
                AddShellItemsToMenu(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
            }
        }

        private void AddFileTagsItemToMenu(Microsoft.UI.Xaml.Controls.CommandBarFlyout contextMenu)
        {
            var fileTagMenuFlyout = new MenuFlyoutItemFileTag()
            {
                ItemsSource = FileTagsSettingsService.FileTagList,
                SelectedItems = SelectedItems
            };
            var overflowSeparator = contextMenu.SecondaryCommands.FirstOrDefault(x => x is FrameworkElement fe && fe.Tag as string == "OverflowSeparator") as AppBarSeparator;
            var index = contextMenu.SecondaryCommands.IndexOf(overflowSeparator);
            index = index >= 0 ? index : contextMenu.SecondaryCommands.Count;
            contextMenu.SecondaryCommands.Insert(index, new AppBarSeparator());
            contextMenu.SecondaryCommands.Insert(index + 1, new AppBarElementContainer()
            {
                Content = fileTagMenuFlyout
            });
        }

        private void AddShellItemsToMenu(List<ContextMenuFlyoutItemViewModel> shellMenuItems, Microsoft.UI.Xaml.Controls.CommandBarFlyout contextMenuFlyout, bool shiftPressed)
        {
            var openWithSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(ShellContextmenuHelper.GetOpenWithItems(shellMenuItems));
            var mainShellMenuItems = shellMenuItems.RemoveFrom(!UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 4);
            var overflowShellMenuItems = shellMenuItems.Except(mainShellMenuItems).ToList();

            var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(overflowShellMenuItems);
            var mainItems = ItemModelListToContextFlyoutHelper.GetAppBarButtonsFromModelIgnorePrimary(mainShellMenuItems);

            var openedPopups = Windows.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(Window.Current);
            var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");
            var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
            if (itemsControl is not null)
            {
                contextMenuFlyout.SetValue(ContextMenuExtensions.ItemsControlProperty, itemsControl);

                var ttv = secondaryMenu.TransformToVisual(Window.Current.Content);
                var cMenuPos = ttv.TransformPoint(new Point(0, 0));
                var requiredHeight = contextMenuFlyout.SecondaryCommands.Concat(mainItems).Where(x => x is not AppBarSeparator).Count() * Constants.UI.ContextMenuSecondaryItemsHeight;
                var availableHeight = Window.Current.Bounds.Height - cMenuPos.Y - Constants.UI.ContextMenuPrimaryItemsHeight;
                if (requiredHeight > availableHeight)
                {
                    itemsControl.MaxHeight = Math.Min(Constants.UI.ContextMenuMaxHeight, Math.Max(itemsControl.ActualHeight, Math.Min(availableHeight, requiredHeight))); // Set menu max height to current height (avoids menu repositioning)
                }

                mainItems.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin); // Set items max width to current menu width (#5555)
            }

            var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
            if (overflowItem is not null)
            {
                var overflowItemFlyout = overflowItem.Flyout as MenuFlyout;
                if (overflowItemFlyout.Items.Count > 0)
                {
                    overflowItemFlyout.Items.Insert(0, new MenuFlyoutSeparator());
                }

                var index = contextMenuFlyout.SecondaryCommands.Count - 2;
                foreach (var i in mainItems)
                {
                    index++;
                    contextMenuFlyout.SecondaryCommands.Insert(index, i);
                }

                index = 0;
                foreach (var i in overflowItems)
                {
                    overflowItemFlyout.Items.Insert(index, i);
                    index++;
                }

                if (overflowItemFlyout.Items.Count > 0)
                {
                    (contextMenuFlyout.SecondaryCommands.First(x => x is FrameworkElement fe && fe.Tag as string == "OverflowSeparator") as AppBarSeparator).Visibility = Visibility.Visible;
                    overflowItem.Visibility = Visibility.Visible;
                }
            }
            else
            {
                mainItems.ForEach(x => contextMenuFlyout.SecondaryCommands.Add(x));
            }

            // add items to openwith dropdown
            var openWithOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWithOverflow") as AppBarButton;
            if (openWithSubItems is not null && openWithOverflow is not null)
            {
                var openWith = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWith") as AppBarButton;
                var flyout = openWithOverflow.Flyout as MenuFlyout;
                flyout.Items.Clear();

                foreach (var item in openWithSubItems)
                {
                    flyout.Items.Add(item);
                }

                openWithOverflow.Flyout = flyout;
                openWith.Visibility = Visibility.Collapsed;
                openWithOverflow.Visibility = Visibility.Visible;
            }

            if (itemsControl is not null)
            {
                itemsControl.Items.OfType<FrameworkElement>().ForEach(item =>
                {
                    if (item.FindDescendant("OverflowTextLabel") is TextBlock label) // Enable CharacterEllipsis text trimming for menu items
                    {
                        label.TextTrimming = TextTrimming.CharacterEllipsis;
                    }
                    if ((item as AppBarButton)?.Flyout as MenuFlyout is MenuFlyout flyout) // Close main menu when clicking on subitems (#5508)
                    {
                        Action<IList<MenuFlyoutItemBase>> clickAction = null;
                        clickAction = (items) =>
                        {
                            items.OfType<MenuFlyoutItem>().ForEach(i =>
                            {
                                i.Click += new RoutedEventHandler((s, e) => contextMenuFlyout.Hide());
                            });
                            items.OfType<MenuFlyoutSubItem>().ForEach(i =>
                            {
                                clickAction(i.Items);
                            });
                        };
                        clickAction(flyout.Items);
                    }
                });
            }
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance.IsCurrentInstance)
            {
                char letter = Convert.ToChar(args.KeyCode);
                JumpString += letter.ToString().ToLowerInvariant();
            }
        }

        protected async void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();
            var result = (FilesystemResult)false;

            e.Items.OfType<ListedItem>().ForEach(item => SelectedItems.Add(item));

            foreach (var item in e.Items.OfType<ListedItem>())
            {
                if (item is FtpItem ftpItem)
                {
                    if (item.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        selectedStorageItems.Add(await new FtpStorageFile(ftpItem).ToStorageFileAsync());
                    }
                    else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        selectedStorageItems.Add(new FtpStorageFolder(ftpItem));
                    }
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.File || item is ZipItem)
                {
                    result = await ParentShellPageInstance.FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                    if (!result)
                    {
                        break;
                    }
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    result = await ParentShellPageInstance.FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                    if (!result)
                    {
                        break;
                    }
                }
            }

            if (result.ErrorCode == FileSystemStatusCode.Unauthorized)
            {
                var itemList = e.Items.OfType<ListedItem>().Select(x => StorageItemHelpers.FromPathAndType(
                    x.ItemPath, x.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
                e.Data.Properties["FileDrop"] = itemList.ToList();
                return;
            }

            var onlyStandard = selectedStorageItems.All(x => x is StorageFile || x is StorageFolder || x is SystemStorageFile || x is SystemStorageFolder);
            if (onlyStandard)
            {
                selectedStorageItems = await selectedStorageItems.ToStandardStorageItemsAsync();
            }
            if (selectedStorageItems.Count == 1)
            {
                if (selectedStorageItems[0] is IStorageFile file)
                {
                    var itemExtension = System.IO.Path.GetExtension(file.Name);
                    if (ImagePreviewViewModel.Extensions.Any((ext) => ext.Equals(itemExtension, StringComparison.OrdinalIgnoreCase)))
                    {
                        var streamRef = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(file);
                        e.Data.SetBitmap(streamRef);
                    }
                }
                e.Data.SetStorageItems(selectedStorageItems, false);
            }
            else if (selectedStorageItems.Count > 1)
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
            if (item is null)
            {
                return;
            }

            DragOperationDeferral deferral = null;
            try
            {
                deferral = e.GetDeferral();

                ItemManipulationModel.SetSelectedItem(item);

                if (dragOverItem != item)
                {
                    dragOverItem = item;
                    dragOverTimer.Stop();
                    dragOverTimer.Debounce(() =>
                    {
                        if (dragOverItem != null && !dragOverItem.IsExecutable)
                        {
                            dragOverItem = null;
                            dragOverTimer.Stop();
                            NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                        }
                    }, TimeSpan.FromMilliseconds(1000), false);
                }

                if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
                {
                    e.Handled = true;

                    var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
                    var draggedItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

                    if (draggedItems.IsEmpty())
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                    else
                    {
                        if (draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
                        {
                            e.AcceptedOperation = DataPackageOperation.None;
                        }
                        else if (handledByFtp)
                        {
                            e.DragUIOverride.IsCaptionVisible = true;
                            e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), item.ItemName);
                            e.AcceptedOperation = DataPackageOperation.Copy;
                        }
                        else
                        {
                            e.DragUIOverride.IsCaptionVisible = true;
                            if (item.IsExecutable)
                            {
                                e.DragUIOverride.Caption = $"{"OpenItemsWithCaptionText".GetLocalized()} {item.ItemName}";
                                e.AcceptedOperation = DataPackageOperation.Link;
                            } // Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
                            else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
                            {
                                e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalized(), item.ItemName);
                                e.AcceptedOperation = DataPackageOperation.Link;
                            }
                            else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
                            {
                                e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), item.ItemName);
                                e.AcceptedOperation = DataPackageOperation.Copy;
                            }
                            else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
                            {
                                e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), item.ItemName);
                                e.AcceptedOperation = DataPackageOperation.Move;
                            }
                            else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
                                || ZipStorageFolder.IsZipPath(item.ItemPath))
                            {
                                e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), item.ItemName);
                                e.AcceptedOperation = DataPackageOperation.Copy;
                            }
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
                    }
                }
            }
            finally
            {
                deferral?.Complete();
            }
        }

        protected async void Item_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            e.Handled = true;
            dragOverItem = null; // Reset dragged over item

            ListedItem item = GetItemFromElement(sender);
            if (item != null)
            {
                await ParentShellPageInstance.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (item as ShortcutItem)?.TargetPath ?? item.ItemPath, false, true, item.IsExecutable);
            }
            deferral.Complete();
        }

        protected void InitializeDrag(UIElement element)
        {
            ListedItem item = GetItemFromElement(element);
            if (item != null)
            {
                element.AllowDrop = false;
                element.DragOver -= Item_DragOver;
                element.DragLeave -= Item_DragLeave;
                element.Drop -= Item_Drop;
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder || item.IsExecutable)
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
            element.DragOver -= Item_DragOver;
            element.DragLeave -= Item_DragLeave;
            element.Drop -= Item_Drop;
        }

        // VirtualKey doesn't support / accept plus and minus by default.
        public readonly VirtualKey PlusKey = (VirtualKey)187;

        public readonly VirtualKey MinusKey = (VirtualKey)189;

        public virtual void Dispose()
        {
            PreviewPaneViewModel?.Dispose();
            UnhookBaseEvents();
        }

        protected void ItemsLayout_DragOver(object sender, DragEventArgs e)
        {
            CommandsViewModel?.DragOverCommand?.Execute(e);
        }

        protected void ItemsLayout_Drop(object sender, DragEventArgs e)
        {
            CommandsViewModel?.DropCommand?.Execute(e);
        }

        public void UpdateCollectionViewSource()
        {
            if (ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.IsGrouped)
            {
                CollectionViewSource = new CollectionViewSource()
                {
                    IsSourceGrouped = true,
                    Source = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.GroupedCollection
                };
            }
            else
            {
                CollectionViewSource = new CollectionViewSource()
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
                e.DestinationItem.Item = destination?.FirstOrDefault();
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

        private void View_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            ParentShellPageInstance.NavToolbarViewModel.HasItem = CollectionViewSource.View.Any();
        }

        virtual public void StartRenameItem() { }

        private ListedItem preRenamingItem = null;

        public void CheckRenameDoubleClick(object clickedItem)
        {
            if (clickedItem is ListedItem item)
            {
                if (item == preRenamingItem)
                {
                    tapDebounceTimer.Debounce(() =>
                    {
                        if (item == preRenamingItem)
                        {
                            StartRenameItem();
                            tapDebounceTimer.Stop();
                        }
                    }, TimeSpan.FromMilliseconds(500));
                }
                else
                {
                    tapDebounceTimer.Stop();
                    preRenamingItem = item;
                }
            }
            else
            {
                ResetRenameDoubleClick();
            }
        }

        public void ResetRenameDoubleClick()
        {
            preRenamingItem = null;
            tapDebounceTimer.Stop();
        }
    }

    public class ContextMenuExtensions : DependencyObject
    {
        public static ItemsControl GetItemsControl(DependencyObject obj)
        {
            return (ItemsControl)obj.GetValue(ItemsControlProperty);
        }

        public static void SetItemsControl(DependencyObject obj, ItemsControl value)
        {
            obj.SetValue(ItemsControlProperty, value);
        }

        public static readonly DependencyProperty ItemsControlProperty =
            DependencyProperty.RegisterAttached("ItemsControl", typeof(ItemsControl), typeof(ContextMenuExtensions), new PropertyMetadata(null));
    }
}