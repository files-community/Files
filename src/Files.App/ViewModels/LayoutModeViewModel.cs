using Files.Shared.Enums;
using Files.Shared.Extensions;
using Files.Backend.Services.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Extensions;
using CommunityToolkit.Mvvm.Input;
using Files.App.Helpers;
using Files.App.Filesystem;
using Files.App.Interacts;
using Files.App.EventArguments;
using Files.App.UserControls;
using Windows.Foundation.Collections;

namespace Files.App.ViewModels
{
    public class LayoutModeViewModel : ObservableObject
    {
        private readonly FolderSettingsViewModel folderSettingsViewModel;
        private readonly SelectedItemsPropertiesViewModel selectedItemsViewModel;
        private readonly DirectoryPropertiesViewModel directoryViewModel;
        private readonly ItemViewModel itemViewModel;

        private bool isMiddleClickToScrollEnabled = true;
        private IEnumerable<ListedItem> selectedItems = new List<ListedItem>();

        public ItemViewModel FilesystemViewModel => itemViewModel;
        public ToolbarViewModel ToolbarViewModel => toolbarViewModel;

        public BaseLayoutCommandsViewModel? CommandsViewModel { get; protected set; }
        public bool IsRenamingItem { get; set; } = false;
        public ListedItem? RenamingItem { get; set; } = null;
        public string? OldItemName { get; set; } = null;

        public bool IsMiddleClickToScrollEnabled
        {
            get => isMiddleClickToScrollEnabled;
            set => SetProperty(ref isMiddleClickToScrollEnabled, value);
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
                    value = jumpString;
                if (value != string.Empty)
                {
                    ListedItem? jumpedToItem = null;
                    ListedItem? previouslySelectedItem = null;

                    if (IsItemSelected)
                        previouslySelectedItem = SelectedItem;

                    // Select first matching item after currently selected item
                    if (previouslySelectedItem != null)
                    {
                        // Use FilesAndFolders because only displayed entries should be jumped to
                        IEnumerable<ListedItem> candidateItems = itemViewModel.FilesAndFolders
                            .SkipWhile(x => x != previouslySelectedItem)
                            .Skip(value.Length == 1 ? 1 : 0) // User is trying to cycle through items starting with the same letter
                            .Where(f => f.ItemName.Length >= value.Length && string.Equals(f.ItemName.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase));
                        jumpedToItem = candidateItems.FirstOrDefault();
                    }

                    if (jumpedToItem == null)
                    {
                        // Use FilesAndFolders because only displayed entries should be jumped to
                        IEnumerable<ListedItem> candidateItems = itemViewModel.FilesAndFolders
                            .Where(f => f.ItemName.Length >= value.Length && string.Equals(f.ItemName.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase));
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

        public IEnumerable<ListedItem> SelectedItems
        {
            get => selectedItems;
            set => SetProperty(ref selectedItems, value);
        }

        public LayoutModeViewModel(IUserSettingsService userSettingsService,
                                   IFileTagsSettingsService fileTagsSettingsService,
                                   FolderSettingsViewModel folderSettingsViewModel,
                                   SelectedItemsPropertiesViewModel selectedItemsViewModel, 
                                   DirectoryPropertiesViewModel directoryViewModel,
                                   ItemViewModel itemViewModel)
        {
            this.folderSettingsViewModel = folderSettingsViewModel;
            this.selectedItemsViewModel = selectedItemsViewModel;
            this.directoryViewModel = directoryViewModel;
            this.itemViewModel = itemViewModel;

            itemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
            itemViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
            itemViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
            itemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
            itemViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;
            HookBaseEvents();
            HookEvents();

            jumpTimer = .CreateTimer();
            jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
            jumpTimer.Tick += JumpTimer_Tick;

            

            toolbarViewModel.SearchBox.TextChanged += ModernShellPage_TextChanged;
            toolbarViewModel.SearchBox.QuerySubmitted += ModernShellPage_QuerySubmitted;
            toolbarViewModel.InstanceViewModel = InstanceViewModel;
            InitToolbarCommands();

            toolbarViewModel.PathControlDisplayText = "Home".GetLocalizedResource();
            toolbarViewModel.ToolbarPathItemInvoked += ModernShellPage_NavigationRequested;
            toolbarViewModel.ToolbarFlyoutOpened += ModernShellPage_ToolbarFlyoutOpened;
            toolbarViewModel.ToolbarPathItemLoaded += ModernShellPage_ToolbarPathItemLoaded;
            toolbarViewModel.AddressBarTextEntered += ModernShellPage_AddressBarTextEntered;
            toolbarViewModel.PathBoxItemDropped += ModernShellPage_PathBoxItemDropped;

            toolbarViewModel.BackRequested += ModernShellPage_BackNavRequested;
            toolbarViewModel.UpRequested += ModernShellPage_UpNavRequested;
            toolbarViewModel.RefreshRequested += ModernShellPage_RefreshRequested;
            toolbarViewModel.ForwardRequested += ModernShellPage_ForwardNavRequested;
            toolbarViewModel.EditModeEnabled += NavigationToolbar_EditModeEnabled;
            toolbarViewModel.ItemDraggedOverPathItem += ModernShellPage_NavigationRequested;
            toolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
            toolbarViewModel.RefreshWidgetsRequested += ModernShellPage_RefreshWidgetsRequested;

            folderSettingsViewModel.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
            folderSettingsViewModel.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;
            folderSettingsViewModel.SortDirectoriesAlongsideFilesPreferenceUpdated += AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;
        }

        private void InitToolbarCommands()
        {
            toolbarViewModel.SelectAllContentPageItemsCommand = new RelayCommand(() => ItemManipulationModel.SelectAllItems());
            toolbarViewModel.InvertContentPageSelctionCommand = new RelayCommand(() => ItemManipulationModel.InvertSelection());
            toolbarViewModel.ClearContentPageSelectionCommand = new RelayCommand(() => ItemManipulationModel.ClearSelection());
            toolbarViewModel.PasteItemsFromClipboardCommand = new RelayCommand(async () => await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this));
            toolbarViewModel.OpenNewWindowCommand = new RelayCommand(NavigationHelpers.LaunchNewWindow);
            toolbarViewModel.OpenNewPaneCommand = new RelayCommand(() => PaneHolder?.OpenPathInNewPane("Home".GetLocalizedResource()));
            toolbarViewModel.ClosePaneCommand = new RelayCommand(() => PaneHolder?.CloseActivePane());
            toolbarViewModel.OpenDirectoryInDefaultTerminalCommand = new RelayCommand(async () => await NavigationHelpers.OpenDirectoryInTerminal(this.FilesystemViewModel.WorkingDirectory));
            toolbarViewModel.CreateNewFileCommand = new RelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.File, x, this));
            toolbarViewModel.CreateNewFolderCommand = new RelayCommand(() => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null, this));
            toolbarViewModel.CopyCommand = new RelayCommand(async () => await UIFilesystemHelpers.CopyItem(this));
            toolbarViewModel.Rename = new RelayCommand(() => CommandsViewModel.RenameItemCommand.Execute(null));
            toolbarViewModel.Share = new RelayCommand(() => CommandsViewModel.ShareItemCommand.Execute(null));
            toolbarViewModel.DeleteCommand = new RelayCommand(() => CommandsViewModel.DeleteItemCommand.Execute(null));
            toolbarViewModel.CutCommand = new RelayCommand(() => CommandsViewModel.CutItemCommand.Execute(null));
            toolbarViewModel.EmptyRecycleBinCommand = new RelayCommand(() => CommandsViewModel.EmptyRecycleBinCommand.Execute(null));
            toolbarViewModel.RestoreRecycleBinCommand = new RelayCommand(() => CommandsViewModel.RestoreRecycleBinCommand.Execute(null));
            toolbarViewModel.RestoreSelectionRecycleBinCommand = new RelayCommand(() => CommandsViewModel.RestoreSelectionRecycleBinCommand.Execute(null));
            toolbarViewModel.RunWithPowerShellCommand = new RelayCommand(async () => await Win32Helpers.InvokeWin32ComponentAsync("powershell", this, PathNormalization.NormalizePath(SelectedItem.ItemPath)));
            toolbarViewModel.PropertiesCommand = new RelayCommand(() => CommandsViewModel.ShowPropertiesCommand.Execute(null));
            toolbarViewModel.SetAsBackgroundCommand = new RelayCommand(() => CommandsViewModel.SetAsDesktopBackgroundItemCommand.Execute(null));
            toolbarViewModel.SetAsLockscreenBackgroundCommand = new RelayCommand(() => CommandsViewModel.SetAsLockscreenBackgroundItemCommand.Execute(null));
            toolbarViewModel.SetAsSlideshowCommand = new RelayCommand(() => CommandsViewModel.SetAsSlideshowItemCommand.Execute(null));
            toolbarViewModel.ExtractCommand = new RelayCommand(() => CommandsViewModel.DecompressArchiveCommand.Execute(null));
            toolbarViewModel.ExtractHereCommand = new RelayCommand(() => CommandsViewModel.DecompressArchiveHereCommand.Execute(null));
            toolbarViewModel.ExtractToCommand = new RelayCommand(() => CommandsViewModel.DecompressArchiveToChildFolderCommand.Execute(null));
            toolbarViewModel.InstallInfCommand = new RelayCommand(() => CommandsViewModel.InstallInfDriver.Execute(null));
            toolbarViewModel.RotateImageLeftCommand = new RelayCommand(() => CommandsViewModel.RotateImageLeftCommand.Execute(null), () => CommandsViewModel.RotateImageLeftCommand.CanExecute(null) == true);
            toolbarViewModel.RotateImageRightCommand = new RelayCommand(() => CommandsViewModel.RotateImageRightCommand.Execute(null), () => CommandsViewModel.RotateImageRightCommand.CanExecute(null) == true);
            toolbarViewModel.InstallFontCommand = new RelayCommand(() => CommandsViewModel.InstallFontCommand.Execute(null));
            toolbarViewModel.UpdateCommand = new AsyncRelayCommand(async () => await UpdateSettingsService.DownloadUpdates());
        }

        public virtual void ResetItemOpacity()
        {
            foreach (var item in itemViewModel.FilesAndFolders)
            {
                if (item != null)
                    item.Opacity = item.IsHiddenItem ? Constants.UI.DimItemOpacity : 1.0d;
            }
        }

        protected virtual void BaseFolderSettings_LayoutModeChangeRequested(LayoutModeEventArgs e)
        {
            var layoutType = folderSettingsViewModel.GetLayoutType(itemViewModel.WorkingDirectory);

            if (layoutType != CurrentPageType)
            {
                ParentShellPageInstance.NavigateWithArguments(layoutType, new NavigationArguments()
                {
                    NavPathParam = navigationArguments!.NavPathParam,
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
            itemViewModel.UpdateEmptyTextType();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            // Add item jumping handler
            this.CharacterReceived += Page_CharacterReceived;
            ParentShellPageInstance = navigationArguments.AssociatedTabInstance;
            InitializeCommandsViewModel();

            folderSettingsViewModel.LayoutModeChangeRequested += FolderSettingsViewModel_LayoutModeChangeRequested; ;
            folderSettingsViewModel.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
            itemViewModel.EmptyTextType = EmptyTextType.None;
            toolbarViewModel.UpdateSortAndGroupOptions();

            if (!navigationArguments.IsSearchResultPage)
            {
                ParentShellPageInstance.ToolbarViewModel.CanRefresh = true;
                string previousDir = itemViewModel.WorkingDirectory;
                await itemViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

                // pathRoot will be empty on recycle bin path
                var workingDir = itemViewModel.WorkingDirectory ?? string.Empty;
                string pathRoot = GetPathRoot(workingDir);
                if (string.IsNullOrEmpty(pathRoot) || workingDir.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal)) // Can't go up from recycle bin
                    ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = false;
                else
                    ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = true;

                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryHelper.IsLibraryPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
                ParentShellPageInstance.ToolbarViewModel.PathControlDisplayText = navigationArguments.NavPathParam;
                if (!navigationArguments.IsLayoutSwitch || previousDir != workingDir)
                    itemViewModel.RefreshItems(previousDir, SetSelectedItemsOnNavigation);
                else
                    ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;
            }
            else
            {
                ParentShellPageInstance.ToolbarViewModel.CanRefresh = true;
                await itemViewModel.SetWorkingDirectoryAsync(navigationArguments.SearchPathParam);

                ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;
                ParentShellPageInstance.ToolbarViewModel.CanGoBack = true;  // Impose no artificial restrictions on back navigation. Even in a search results page.
                ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = false;

                var workingDir = itemViewModel.WorkingDirectory ?? string.Empty;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryHelper.IsLibraryPath(workingDir);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;

                if (!navigationArguments.IsLayoutSwitch)
                {
                    var displayName = App.LibraryManager.TryGetLibrary(navigationArguments.SearchPathParam, out var lib) ? lib.Text : navigationArguments.SearchPathParam;
                    ParentShellPageInstance.UpdatePathUIToWorkingDirectory(null, string.Format("SearchPagePathBoxOverrideText".GetLocalizedResource(), navigationArguments.SearchQuery, displayName));
                    var searchInstance = new Filesystem.Search.FolderSearch
                    {
                        Query = navigationArguments.SearchQuery,
                        Folder = navigationArguments.SearchPathParam,
                        ThumbnailSize = InstanceViewModel!.FolderSettings.GetIconSize(),
                        SearchUnindexedItems = navigationArguments.SearchUnindexedItems
                    };
                    _ = itemViewModel.SearchAsync(searchInstance);
                }
            }

            ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
            itemViewModel.UpdateGroupOptions();
            UpdateCollectionViewSource();
            FolderSettings.IsLayoutModeChanging = false;

            SetSelectedItemsOnNavigation();

            ItemContextMenuFlyout.Opening += ItemContextFlyout_Opening;
            BaseContextMenuFlyout.Opening += BaseContextFlyout_Opening;
        }

        private async void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
        {
            await toolbarViewModel.CheckPathInput(e.QueryText, toolbarViewModel.PathComponents.LastOrDefault()?.Path, this);
        }

        private void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
        {
            toolbarViewModel.ManualEntryBoxLoaded = true;
            toolbarViewModel.ClickablePathLoaded = false;
            toolbarViewModel.PathText = string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory)
                ? CommonPaths.HomePath
                : FilesystemViewModel.WorkingDirectory;
        }

        private void FolderSettingsViewModel_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
        {
            if (FilesystemViewModel != null)
            {
                folderSettingsViewModel.SetLayoutPreferencesForPath(FilesystemViewModel.WorkingDirectory, e.LayoutPreference);
                if (e.IsAdaptiveLayoutUpdateRequired)
                {
                    AdaptiveLayoutHelpers.PredictLayoutMode(currentInstanceViewModel.FolderSettings, FilesystemViewModel.WorkingDirectory, FilesystemViewModel.FilesAndFolders);
                }
            }
        }

        public void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null)
        {
            if (string.IsNullOrWhiteSpace(singleItemOverride))
            {
                var components = StorageFileExtensions.GetDirectoryPathComponents(newWorkingDir);
                var lastCommonItemIndex = toolbarViewModel.PathComponents
                    .Select((value, index) => new { value, index })
                    .LastOrDefault(x => x.index < components.Count && x.value.Path == components[x.index].Path)?.index ?? 0;
                while (toolbarViewModel.PathComponents.Count > lastCommonItemIndex)
                {
                    toolbarViewModel.PathComponents.RemoveAt(lastCommonItemIndex);
                }
                foreach (var component in components.Skip(lastCommonItemIndex))
                {
                    toolbarViewModel.PathComponents.Add(component);
                }
            }
            else
            {
                toolbarViewModel.PathComponents.Clear(); // Clear the path UI
                toolbarViewModel.PathComponents.Add(new Views.PathBoxItem() { Path = null, Title = singleItemOverride });
            }
        }

        public void SetSelectedItemsOnNavigation()
        {
            try
            {
                if (navigationArguments != null && navigationArguments.SelectItems != null && navigationArguments.SelectItems.Any())
                {
                    List<ListedItem> liItemsToSelect = new List<ListedItem>();
                    foreach (string item in navigationArguments.SelectItems)
                        liItemsToSelect.Add(itemViewModel.FilesAndFolders.Where((li) => li.ItemNameRaw == item).First());

                    ItemManipulationModel.SetSelectedItems(liItemsToSelect);
                    ItemManipulationModel.FocusSelectedItems();
                }
                else if (navigationArguments != null && navigationArguments.FocusOnNavigation)
                {
                    ItemManipulationModel.FocusFileList(); // Set focus on layout specific file list control
                }
            }
            catch (Exception)
            {
            }
        }

        private CancellationTokenSource? groupingCancellationToken;

        private async void FolderSettings_GroupOptionPreferenceUpdated(object? sender, GroupOption e)
        {
            // Two or more of these running at the same time will cause a crash, so cancel the previous one before beginning
            groupingCancellationToken?.Cancel();
            groupingCancellationToken = new CancellationTokenSource();
            var token = groupingCancellationToken.Token;
            await itemViewModel.GroupOptionsUpdated(token);
            UpdateCollectionViewSource();
            await itemViewModel.ReloadItemGroupHeaderImagesAsync();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            // Remove item jumping handler
            this.CharacterReceived -= Page_CharacterReceived;
            folderSettingsViewModel.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
            FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
            ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
            BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

            var parameter = e.Parameter as NavigationArguments;
            if (parameter is not null && !parameter.IsLayoutSwitch)
                itemViewModel.CancelLoadAndClearFiles();
        }

        public async void ItemContextFlyout_Opening(object? sender, object e)
        {
            try
            {
                if (!IsItemSelected) // Workaround for item sometimes not getting selected
                {
                    if (((sender as CommandBarFlyout)?.Target as ListViewItem)?.Content is ListedItem li)
                        ItemManipulationModel.SetSelectedItem(li);
                }
                if (IsItemSelected)
                    await LoadMenuItemsAsync();
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        private CancellationTokenSource? shellContextMenuItemCancellationToken;

        public async void BaseContextFlyout_Opening(object? sender, object e)
        {
            try
            {
                if (BaseContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
                    itc.MaxHeight = Constants.UI.ContextMenuMaxHeight; // Reset menu max height
                shellContextMenuItemCancellationToken?.Cancel();
                shellContextMenuItemCancellationToken = new CancellationTokenSource();
                var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                var items = ContextFlyoutItemHelper.GetBaseContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, itemViewModel: itemViewModel, commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, false);
                BaseContextMenuFlyout.PrimaryCommands.Clear();
                BaseContextMenuFlyout.SecondaryCommands.Clear();
                var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
                primaryElements.OfType<AppBarButton>().ForEach(i =>
                {
                    i.Click += new RoutedEventHandler((s, e) => BaseContextMenuFlyout.Hide());  // Workaround for WinUI (#5508)
                });
                primaryElements.ForEach(i => BaseContextMenuFlyout.PrimaryCommands.Add(i));
                secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
                secondaryElements.ForEach(i => BaseContextMenuFlyout.SecondaryCommands.Add(i));

                if (!InstanceViewModel!.IsPageTypeSearchResults && !InstanceViewModel.IsPageTypeZipFolder)
                {
                    var shellMenuItems = await ContextFlyoutItemHelper.GetBaseContextShellCommandsAsync(currentInstanceViewModel: InstanceViewModel, workingDir: itemViewModel.WorkingDirectory, shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
                    if (shellMenuItems.Any())
                        AddShellItemsToMenu(shellMenuItems, BaseContextMenuFlyout, shiftPressed);
                }
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        public void UpdateSelectionSize()
        {
            var items = selectedItems?.Any() ?? false ? selectedItems : itemViewModel.FilesAndFolders;
            if (items is null)
                return;
            bool isSizeKnown = !items.Any(item => string.IsNullOrEmpty(item.FileSize));
            if (isSizeKnown)
            {
                long size = items.Sum(item => item.FileSizeBytes);
                SelectedItemsPropertiesViewModel.ItemSizeBytes = size;
                SelectedItemsPropertiesViewModel.ItemSize = size.ToSizeString();
            }
            else
            {
                SelectedItemsPropertiesViewModel.ItemSizeBytes = 0;
                SelectedItemsPropertiesViewModel.ItemSize = string.Empty;
            }
            SelectedItemsPropertiesViewModel.ItemSizeVisibility = isSizeKnown;
        }

        private async Task LoadMenuItemsAsync()
        {
            if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
                itc.MaxHeight = Constants.UI.ContextMenuMaxHeight; // Reset menu max height
            shellContextMenuItemCancellationToken?.Cancel();
            shellContextMenuItemCancellationToken = new CancellationTokenSource();
            SelectedItemsPropertiesViewModel.CheckAllFileExtensions(SelectedItems!.Select(selectedItem => selectedItem?.FileExtension).ToList()!);
            var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, workingDir: itemViewModel.WorkingDirectory, selectedItems: SelectedItems!, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, showOpenMenu: false);
            ItemContextMenuFlyout.PrimaryCommands.Clear();
            ItemContextMenuFlyout.SecondaryCommands.Clear();
            var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
            primaryElements.OfType<AppBarButton>().ForEach(i =>
            {
                i.Click += new RoutedEventHandler((s, e) => ItemContextMenuFlyout.Hide()); // Workaround for WinUI (#5508)
            });
            primaryElements.ForEach(i => ItemContextMenuFlyout.PrimaryCommands.Add(i));
            secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
            secondaryElements.ForEach(i => ItemContextMenuFlyout.SecondaryCommands.Add(i));

            if (InstanceViewModel!.CanTagFilesInPage)
                AddNewFileTagsToMenu(ItemContextMenuFlyout);

            if (!InstanceViewModel.IsPageTypeZipFolder)
            {
                var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(currentInstanceViewModel: InstanceViewModel, workingDir: itemViewModel.WorkingDirectory, selectedItems: SelectedItems!, shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
                if (shellMenuItems.Any())
                    AddShellItemsToMenu(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
            }
        }

        private void AddNewFileTagsToMenu(CommandBarFlyout contextMenu)
        {
            var fileTagsContextMenu = new FileTagsContextMenu(SelectedItems!);
            var overflowSeparator = contextMenu.SecondaryCommands.FirstOrDefault(x => x is FrameworkElement fe && fe.Tag as string == "OverflowSeparator") as AppBarSeparator;
            var index = contextMenu.SecondaryCommands.IndexOf(overflowSeparator);
            index = index >= 0 ? index : contextMenu.SecondaryCommands.Count;
            contextMenu.SecondaryCommands.Insert(index, new AppBarSeparator());
            contextMenu.SecondaryCommands.Insert(index + 1, new AppBarButton()
            {
                Label = "SettingsEditFileTagsExpander/Title".GetLocalizedResource(),
                Icon = new FontIcon() { Glyph = "\uE1CB" },
                Flyout = fileTagsContextMenu
            });
        }

        private void AddShellItemsToMenu(List<ContextMenuFlyoutItemViewModel> shellMenuItems, Microsoft.UI.Xaml.Controls.CommandBarFlyout contextMenuFlyout, bool shiftPressed)
        {
            var openWithSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(ShellContextmenuHelper.GetOpenWithItems(shellMenuItems));
            var mainShellMenuItems = shellMenuItems.RemoveFrom(!UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 4);
            var overflowShellMenuItems = shellMenuItems.Except(mainShellMenuItems).ToList();

            var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(overflowShellMenuItems);
            var mainItems = ItemModelListToContextFlyoutHelper.GetAppBarButtonsFromModelIgnorePrimary(mainShellMenuItems);

            var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(App.Window);
            var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");
            var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
            if (itemsControl is not null && secondaryMenu is not null)
            {
                contextMenuFlyout.SetValue(ContextMenuExtensions.ItemsControlProperty, itemsControl);

                var ttv = secondaryMenu.TransformToVisual(App.Window.Content);
                var cMenuPos = ttv.TransformPoint(new Point(0, 0));
                var requiredHeight = contextMenuFlyout.SecondaryCommands.Concat(mainItems).Where(x => x is not AppBarSeparator).Count() * Constants.UI.ContextMenuSecondaryItemsHeight;
                var availableHeight = App.Window.Bounds.Height - cMenuPos.Y - Constants.UI.ContextMenuPrimaryItemsHeight;
                if (requiredHeight > availableHeight)
                    itemsControl.MaxHeight = Math.Min(Constants.UI.ContextMenuMaxHeight, Math.Max(itemsControl.ActualHeight, Math.Min(availableHeight, requiredHeight))); // Set menu max height to current height (avoids menu repositioning)

                mainItems.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin); // Set items max width to current menu width (#5555)
            }

            var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && appBarButton.Tag as string == "ItemOverflow") as AppBarButton;
            if (overflowItem is not null)
            {
                var overflowItemFlyout = overflowItem.Flyout as MenuFlyout;
                if (overflowItemFlyout is not null)
                {
                    if (overflowItemFlyout.Items.Count > 0)
                        overflowItemFlyout.Items.Insert(0, new MenuFlyoutSeparator());

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
                        (contextMenuFlyout.SecondaryCommands.First(x => x is FrameworkElement fe && fe.Tag as string == "OverflowSeparator") as AppBarSeparator)!.Visibility = Visibility.Visible;
                        overflowItem.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                mainItems.ForEach(x => contextMenuFlyout.SecondaryCommands.Add(x));
            }

            // add items to openwith dropdown
            var openWithOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && abb.Tag as string == "OpenWithOverflow") as AppBarButton;
            var openWith = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && abb.Tag as string == "OpenWith") as AppBarButton;
            if (openWithSubItems is not null && openWithOverflow is not null && openWith is not null)
            {
                var flyout = (MenuFlyout)openWithOverflow.Flyout;

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
                        label.TextTrimming = TextTrimming.CharacterEllipsis;
                    if ((item as AppBarButton)?.Flyout as MenuFlyout is MenuFlyout flyout) // Close main menu when clicking on subitems (#5508)
                    {
                        Action<IList<MenuFlyoutItemBase>> clickAction = null!;
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

        protected virtual void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
        {
            if (ParentShellPageInstance!.IsCurrentInstance)
            {
                char letter = args.Character;
                JumpString += letter.ToString().ToLowerInvariant();
            }
        }

        protected void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            RefreshContainer(args.ItemContainer, args.InRecycleQueue);
            RefreshItem(args.ItemContainer, args.Item, args.InRecycleQueue, args);
        }

        private void RefreshContainer(SelectorItem container, bool inRecycleQueue)
        {
            if (inRecycleQueue)
            {
                UninitializeDrag(container);
            }
        }

        private void RefreshItem(SelectorItem container, object item, bool inRecycleQueue, ContainerContentChangingEventArgs args)
        {
            if (item is not ListedItem listedItem)
                return;

            if (inRecycleQueue)
            {
                itemViewModel.CancelExtendedPropertiesLoadingForItem(listedItem);
            }
            else
            {
                InitializeDrag(container, listedItem);

                if (!listedItem.ItemPropertiesInitialized)
                {
                    uint callbackPhase = 3;
                    args.RegisterUpdateCallback(callbackPhase, async (s, c) =>
                    {
                        await itemViewModel.LoadExtendedItemProperties(listedItem, IconSize);
                    });
                }
            }
        }

        private readonly RecycleBinHelpers recycleBinHelpers = new();

        protected void InitializeDrag(UIElement containter, ListedItem item)
        {
            if (item is null)
                return;

            UninitializeDrag(containter);
            if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !recycleBinHelpers.IsPathUnderRecycleBin(item.ItemPath) || item.IsExecutable)
            {
                containter.AllowDrop = true;
                containter.DragOver += Item_DragOver;
                containter.DragLeave += Item_DragLeave;
                containter.Drop += Item_Drop;
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
            // todo: unhook these
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

        private void UpdateCollectionViewSource()
        {
            if (ParentShellPageInstance is null)
                return;
            if (itemViewModel.FilesAndFolders.IsGrouped)
            {
                CollectionViewSource = new CollectionViewSource()
                {
                    IsSourceGrouped = true,
                    Source = itemViewModel.FilesAndFolders.GroupedCollection
                };
            }
            else
            {
                CollectionViewSource = new CollectionViewSource()
                {
                    IsSourceGrouped = false,
                    Source = itemViewModel.FilesAndFolders
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
            if (element is not null)
                VisualStateManager.GoToState(element, "PointerOver", true);
        }

        protected void StackPanel_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
            if (element is not null)
                VisualStateManager.GoToState(element, "Normal", true);
        }

        protected void RootPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
            if (element is not null)
                VisualStateManager.GoToState(element, "Pressed", true);
        }

        private void ItemManipulationModel_RefreshItemsOpacityInvoked(object? sender, EventArgs e)
        {
            foreach (ListedItem listedItem in itemViewModel.FilesAndFolders)
            {
                if (listedItem.IsHiddenItem)
                    listedItem.Opacity = Constants.UI.DimItemOpacity;
                else
                    listedItem.Opacity = 1;
            }
        }

    }
}
