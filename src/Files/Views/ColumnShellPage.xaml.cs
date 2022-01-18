using Files.Common;
using Files.Dialogs;
using Files.Enums;
using Files.EventArguments;
using Files.Filesystem;
using Files.Filesystem.FilesystemHistory;
using Files.Filesystem.Search;
using Files.Helpers;
using Files.Services;
using Files.UserControls;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views.LayoutModes;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using SortDirection = Files.Enums.SortDirection;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColumnShellPage : Page, IShellPage, INotifyPropertyChanged
    {
        private readonly StorageHistoryHelpers storageHistoryHelpers;
        public IBaseLayout SlimContentPage => ContentPage;
        public IFilesystemHelpers FilesystemHelpers { get; private set; }
        private CancellationTokenSource cancellationTokenSource;

        public bool CanNavigateBackward => false;
        public bool CanNavigateForward => false;

        public FolderSettingsViewModel FolderSettings => InstanceViewModel?.FolderSettings;

        public MainViewModel MainViewModel => App.MainViewModel;
        private bool isCurrentInstance { get; set; } = false;

        public bool IsColumnView { get; } = true;

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public bool IsCurrentInstance
        {
            get
            {
                return isCurrentInstance;
            }
            set
            {
                if (isCurrentInstance != value)
                {
                    isCurrentInstance = value;
                    if (isCurrentInstance)
                    {
                        ContentPage?.ItemManipulationModel.FocusFileList();
                    }
                    else
                    {
                        //NavigationToolbar.IsEditModeEnabled = false;
                    }
                    NotifyPropertyChanged(nameof(IsCurrentInstance));
                }
            }
        }

        public ItemViewModel FilesystemViewModel { get; private set; } = null;
        public CurrentInstanceViewModel InstanceViewModel { get; }
        private BaseLayout contentPage = null;

        public BaseLayout ContentPage
        {
            get
            {
                return contentPage;
            }
            set
            {
                if (value != contentPage)
                {
                    contentPage = value;
                    NotifyPropertyChanged(nameof(ContentPage));
                    NotifyPropertyChanged(nameof(SlimContentPage));
                }
            }
        }

        private bool isPageMainPane;

        public bool IsPageMainPane
        {
            get => isPageMainPane;
            set
            {
                if (value != isPageMainPane)
                {
                    isPageMainPane = value;
                    NotifyPropertyChanged(nameof(IsPageMainPane));
                }
            }
        }

        public SolidColorBrush CurrentInstanceBorderBrush
        {
            get { return (SolidColorBrush)GetValue(CurrentInstanceBorderBrushProperty); }
            set { SetValue(CurrentInstanceBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty CurrentInstanceBorderBrushProperty =
            DependencyProperty.Register("CurrentInstanceBorderBrush", typeof(SolidColorBrush), typeof(ColumnShellPage), new PropertyMetadata(null));

        public Type CurrentPageType => ItemDisplayFrame.SourcePageType;

        public NavToolbarViewModel NavToolbarViewModel { get; } = new NavToolbarViewModel();

        public ColumnShellPage()
        {
            InitializeComponent();

            InstanceViewModel = new CurrentInstanceViewModel(FolderLayoutModes.ColumnView);
            InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
            cancellationTokenSource = new CancellationTokenSource();
            FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
            storageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

            DisplayFilesystemConsentDialog();

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }

            //NavigationToolbar.PathControlDisplayText = "Home".GetLocalized();
            //NavigationToolbar.CanGoBack = false;
            //NavigationToolbar.CanGoForward = false;
            //NavigationToolbar.SearchBox.QueryChanged += ColumnShellPage_QueryChanged;
            //NavigationToolbar.SearchBox.QuerySubmitted += ColumnShellPage_QuerySubmitted;
            //NavigationToolbar.SearchBox.SuggestionChosen += ColumnShellPage_SuggestionChosen;

            NavToolbarViewModel.ToolbarPathItemInvoked += ColumnShellPage_NavigationRequested;
            NavToolbarViewModel.ToolbarFlyoutOpened += ColumnShellPage_ToolbarFlyoutOpened;
            NavToolbarViewModel.ToolbarPathItemLoaded += ColumnShellPage_ToolbarPathItemLoaded;
            NavToolbarViewModel.AddressBarTextEntered += ColumnShellPage_AddressBarTextEntered;
            NavToolbarViewModel.PathBoxItemDropped += ColumnShellPage_PathBoxItemDropped;
            NavToolbarViewModel.BackRequested += ColumnShellPage_BackNavRequested;
            NavToolbarViewModel.UpRequested += ColumnShellPage_UpNavRequested;
            NavToolbarViewModel.RefreshRequested += ColumnShellPage_RefreshRequested;
            NavToolbarViewModel.ForwardRequested += ColumnShellPage_ForwardNavRequested;
            NavToolbarViewModel.EditModeEnabled += NavigationToolbar_EditModeEnabled;
            NavToolbarViewModel.ItemDraggedOverPathItem += ColumnShellPage_NavigationRequested;
            NavToolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
            NavToolbarViewModel.SearchBox.TextChanged += ColumnShellPage_TextChanged;
            NavToolbarViewModel.SearchBox.QuerySubmitted += ColumnShellPage_QuerySubmitted;

            NavToolbarViewModel.InstanceViewModel = InstanceViewModel;
            //NavToolbarViewModel.RefreshWidgetsRequested += refreshwid;

            InitToolbarCommands();

            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;

            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested += ColumnShellPage_BackRequested;

            App.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;
        }

        private async void ColumnShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
        {
            await NavToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, e.Item, this);
        }

        private async void ColumnShellPage_ToolbarFlyoutOpened(object sender, ToolbarFlyoutOpenedEventArgs e)
        {
            await NavToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, (e.OpenedFlyout.Target as FontIcon).DataContext as PathBoxItem, this);
        }

        private void InitToolbarCommands()
        {
            NavToolbarViewModel.SelectAllContentPageItemsCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.SelectAllItems());
            NavToolbarViewModel.InvertContentPageSelctionCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.InvertSelection());
            NavToolbarViewModel.ClearContentPageSelectionCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.ClearSelection());
            NavToolbarViewModel.PasteItemsFromClipboardCommand = new RelayCommand(async () => await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this));
            NavToolbarViewModel.OpenNewWindowCommand = new RelayCommand(NavigationHelpers.LaunchNewWindow);
            NavToolbarViewModel.OpenNewPaneCommand = new RelayCommand(() => PaneHolder?.OpenPathInNewPane("Home".GetLocalized()));
            NavToolbarViewModel.ClosePaneCommand = new RelayCommand(() => PaneHolder?.CloseActivePane());
            NavToolbarViewModel.OpenDirectoryInDefaultTerminalCommand = new RelayCommand(async () => await NavigationHelpers.OpenDirectoryInTerminal(this.FilesystemViewModel.WorkingDirectory));
            NavToolbarViewModel.CreateNewFileCommand = new RelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemType.File, x, this));
            NavToolbarViewModel.CreateNewFolderCommand = new RelayCommand(() => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemType.Folder, null, this));
            NavToolbarViewModel.CopyCommand = new RelayCommand(async () => await UIFilesystemHelpers.CopyItem(this));
            NavToolbarViewModel.Rename = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RenameItemCommand.Execute(null));
            NavToolbarViewModel.Share = new RelayCommand(() => SlimContentPage?.CommandsViewModel.ShareItemCommand.Execute(null));
            NavToolbarViewModel.DeleteCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DeleteItemCommand.Execute(null));
            NavToolbarViewModel.CutCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.CutItemCommand.Execute(null));
            NavToolbarViewModel.EmptyRecycleBinCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.EmptyRecycleBinCommand.Execute(null));
            NavToolbarViewModel.PropertiesCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.ShowPropertiesCommand.Execute(null));
        }

        private void FolderSettings_LayoutPreferencesUpdateRequired(object sender, LayoutPreferenceEventArgs e)
        {
        }

        /*
         * Ensure that the path bar gets updated for user interaction
         * whenever the path changes. We will get the individual directories from
         * the updated, most-current path and add them to the UI.
         */

        public void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null)
        {
            if (string.IsNullOrWhiteSpace(singleItemOverride))
            {
                var components = StorageFileExtensions.GetDirectoryPathComponents(newWorkingDir);
                var lastCommonItemIndex = NavToolbarViewModel.PathComponents
                    .Select((value, index) => new { value, index })
                    .LastOrDefault(x => x.index < components.Count && x.value.Path == components[x.index].Path)?.index ?? 0;
                while (NavToolbarViewModel.PathComponents.Count > lastCommonItemIndex)
                {
                    NavToolbarViewModel.PathComponents.RemoveAt(lastCommonItemIndex);
                }
                foreach (var component in components.Skip(lastCommonItemIndex))
                {
                    NavToolbarViewModel.PathComponents.Add(component);
                }
            }
            else
            {
                NavToolbarViewModel.PathComponents.Clear(); // Clear the path UI
                NavToolbarViewModel.IsSingleItemOverride = true;
                NavToolbarViewModel.PathComponents.Add(new Views.PathBoxItem() { Path = null, Title = singleItemOverride });
            }
        }

        private async void ColumnShellPage_TextChanged(ISearchBox sender, SearchBoxTextChangedEventArgs e)
        {
            if (e.Reason == SearchBoxTextChangeReason.UserInput)
            {
                if (!string.IsNullOrWhiteSpace(sender.Query))
                {
                    var search = new FolderSearch
                    {
                        Query = sender.Query,
                        Folder = FilesystemViewModel.WorkingDirectory,
                        MaxItemCount = 10,
                        SearchUnindexedItems = UserSettingsService.PreferencesSettingsService.SearchUnindexedItems
                    };
                    sender.SetSuggestions(await search.SearchAsync());
                }
                else
                {
                    sender.ClearSuggestions();
                }
            }
        }

        private async void ColumnShellPage_QuerySubmitted(ISearchBox sender, SearchBoxQuerySubmittedEventArgs e)
        {
            if (e.ChosenSuggestion is ListedItem item)
            {
                await NavigationHelpers.OpenPath(item.ItemPath, this);
            }
            else if (e.ChosenSuggestion is null && !string.IsNullOrWhiteSpace(sender.Query))
            {
                SubmitSearch(sender.Query, UserSettingsService.PreferencesSettingsService.SearchUnindexedItems);
            }
        }

        private void ColumnShellPage_RefreshRequested(object sender, EventArgs e)
        {
            Refresh_Click();
        }

        private void ColumnShellPage_UpNavRequested(object sender, EventArgs e)
        {
            Up_Click();
        }

        private void ColumnShellPage_ForwardNavRequested(object sender, EventArgs e)
        {
            Forward_Click();
        }

        private void ColumnShellPage_BackNavRequested(object sender, EventArgs e)
        {
            Back_Click();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            ColumnParams = eventArgs.Parameter as ColumnParam;
        }

        private void AppSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
        {
            FilesystemViewModel?.UpdateSortDirectionStatus();
        }

        private void AppSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
        {
            FilesystemViewModel?.UpdateSortOptionStatus();
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (IsCurrentInstance)
            {
                if (args.CurrentPoint.Properties.IsXButton1Pressed)
                {
                    Back_Click();
                }
                else if (args.CurrentPoint.Properties.IsXButton2Pressed)
                {
                    Forward_Click();
                }
            }
        }

        private async void ColumnShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
        {
            await FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.Path, false, true);
            e.SignalEvent?.Set();
        }

        private void ColumnShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
        {
            NavToolbarViewModel.SetAddressBarSuggestions(e.AddressBarTextField, this);
        }

        private void ColumnShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
        {
            this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(e);
        }

        private async void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
        {
            await NavToolbarViewModel.CheckPathInput(e.QueryText, NavToolbarViewModel.PathComponents[NavToolbarViewModel.PathComponents.Count - 1].Path, this);
        }

        private void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
        {
            NavToolbarViewModel.ManualEntryBoxLoaded = true;
            NavToolbarViewModel.ClickablePathLoaded = false;
            NavToolbarViewModel.PathText = string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory)
                ? CommonPaths.HomePath
                : FilesystemViewModel.WorkingDirectory;
        }

        private void ColumnShellPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (IsCurrentInstance)
            {
                var browser = this.FindAscendant<ColumnViewBrowser>();
                if (browser.ParentShellPageInstance.CanNavigateBackward)
                {
                    e.Handled = true;
                    Back_Click();
                }
                else
                {
                    e.Handled = false;
                }
            }
        }

        private void DrivesManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowUserConsentOnInit")
            {
                DisplayFilesystemConsentDialog();
            }
        }

        private async Task<BaseLayout> GetContentOrNullAsync()
        {
            BaseLayout FrameContent = null;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                FrameContent = (ItemDisplayFrame.Content as BaseLayout);
            });
            return FrameContent;
        }

        private async void DisplayFilesystemConsentDialog()
        {
            if (App.DrivesManager?.ShowUserConsentOnInit ?? false)
            {
                App.DrivesManager.ShowUserConsentOnInit = false;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    DynamicDialog dialog = DynamicDialogFactory.GetFor_ConsentDialog();
                    await dialog.ShowAsync(ContentDialogPlacement.Popup);
                });
            }
        }

        private ColumnParam columnParams;

        public ColumnParam ColumnParams
        {
            get => columnParams;
            set
            {
                if (value != columnParams)
                {
                    columnParams = value;
                    if (IsLoaded)
                    {
                        OnNavigationParamsChanged();
                    }
                }
            }
        }

        private void OnNavigationParamsChanged()
        {
            ItemDisplayFrame.Navigate(typeof(ColumnViewBase),
                new NavigationArguments()
                {
                    IsSearchResultPage = columnParams.IsSearchResultPage,
                    SearchQuery = columnParams.SearchQuery,
                    NavPathParam = columnParams.NavPathParam,
                    SearchUnindexedItems = columnParams.SearchUnindexedItems,
                    SearchPathParam = columnParams.SearchPathParam,
                    AssociatedTabInstance = this
                });
        }

        public static readonly DependencyProperty NavParamsProperty =
            DependencyProperty.Register("NavParams", typeof(NavigationParams), typeof(ColumnShellPage), new PropertyMetadata(null));

        private TabItemArguments tabItemArguments;

        public TabItemArguments TabItemArguments
        {
            get => tabItemArguments;
            set
            {
                if (tabItemArguments != value)
                {
                    tabItemArguments = value;
                    ContentChanged?.Invoke(this, value);
                }
            }
        }

        private IPaneHolder paneHolder;

        public IPaneHolder PaneHolder
        {
            get => paneHolder;
            set
            {
                if (value != paneHolder)
                {
                    paneHolder = value;
                    NotifyPropertyChanged(nameof(PaneHolder));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<TabItemArguments> ContentChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FilesystemViewModel = new ItemViewModel(InstanceViewModel?.FolderSettings);
            FilesystemViewModel.DisableAdaptiveLayout = true;
            FilesystemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
            FilesystemViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
            FilesystemViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
            FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
            FilesystemViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;
            OnNavigationParamsChanged();
            this.Loaded -= Page_Loaded;
        }

        private void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
        {
            InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
        }

        private void FilesystemViewModel_OnSelectionRequestedEvent(object sender, List<ListedItem> e)
        {
            ContentPage.ItemManipulationModel.SetSelectedItems(e);
        }

        private void FilesystemViewModel_DirectoryInfoUpdated(object sender, EventArgs e)
        {
            if (ContentPage != null)
            {
                if (FilesystemViewModel.FilesAndFolders.Count == 1)
                {
                    ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemCount/Text".GetLocalized()}";
                }
                else
                {
                    ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemsCount/Text".GetLocalized()}";
                }
            }
        }

        private void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
        {
            string value = e.Path;
            if (!string.IsNullOrWhiteSpace(value))
            {
                UpdatePathUIToWorkingDirectory(value);
            }
        }

        private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ContentPage = await GetContentOrNullAsync();
            NavToolbarViewModel.SearchBox.Query = string.Empty;
            NavToolbarViewModel.IsSearchBoxVisible = false;
            if (ItemDisplayFrame.CurrentSourcePageType == typeof(ColumnViewBase))
            {
                // Reset DataGrid Rows that may be in "cut" command mode
                ContentPage.ResetItemOpacity();
            }
            var parameters = e.Parameter as NavigationArguments;
            TabItemArguments = new TabItemArguments()
            {
                InitialPageType = typeof(ColumnShellPage),
                NavigationArg = parameters.IsSearchResultPage ? parameters.SearchPathParam : parameters.NavPathParam
            };
        }

        private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
            var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            var tabInstance = CurrentPageType == (typeof(DetailsLayoutBrowser))
                || CurrentPageType == typeof(GridViewBrowser)
                || CurrentPageType == typeof(ColumnViewBrowser)
                || CurrentPageType == typeof(ColumnViewBase);

            switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
            {
                case (true, false, false, true, VirtualKey.Z): // ctrl + z, undo
                    if (!InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await storageHistoryHelpers.TryUndo();
                    }
                    break;

                case (true, false, false, true, VirtualKey.Y): // ctrl + y, redo
                    if (!InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await storageHistoryHelpers.TryRedo();
                    }
                    break;

                case (true, true, false, true, VirtualKey.C):
                    {
                        SlimContentPage?.CommandsViewModel.CopyPathOfSelectedItemCommand.Execute(null);
                        break;
                    }

                case (false, false, false, true, VirtualKey.F3): //f3
                case (true, false, false, true, VirtualKey.F): // ctrl + f
                    NavToolbarViewModel.SwitchSearchBoxVisibility();
                    break;

                case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
                    if (InstanceViewModel.CanCreateFileInPage)
                    {
                        var addItemDialog = new AddItemDialog();
                        await addItemDialog.ShowAsync();
                        if (addItemDialog.ResultType.ItemType != AddItemType.Cancel)
                        {
                            UIFilesystemHelpers.CreateFileFromDialogResultType(
                                addItemDialog.ResultType.ItemType,
                                addItemDialog.ResultType.ItemInfo,
                                this);
                        }
                    }
                    break;

                case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
                    if (ContentPage.IsItemSelected && !NavToolbarViewModel.IsEditModeEnabled && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        var items = await Task.Run(() => SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                            item.ItemPath,
                            item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)));
                        await FilesystemHelpers.DeleteItemsAsync(items, true, true, true);
                    }

                    break;

                case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
                    if (!NavToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        await UIFilesystemHelpers.CopyItem(this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
                    if (!NavToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
                    if (!NavToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        UIFilesystemHelpers.CutItem(this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
                    if (!NavToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        this.SlimContentPage.ItemManipulationModel.SelectAllItems();
                    }

                    break;

                case (true, false, false, true, VirtualKey.D): // ctrl + d, delete item
                case (false, false, false, true, VirtualKey.Delete): // delete, delete item
                    if (ContentPage.IsItemSelected && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        var items = await Task.Run(() => SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                            item.ItemPath,
                            item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)));
                        await FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
                    }

                    break;

                case (false, false, false, true, VirtualKey.Space): // space, quick look
                    if (!NavToolbarViewModel.IsEditModeEnabled && !NavToolbarViewModel.IsSearchBoxVisible)
                    {
                        await QuickLookHelpers.ToggleQuickLook(this);
                    }
                    break;

                case (true, false, false, true, VirtualKey.P):
                    UserSettingsService.PreviewPaneSettingsService.PreviewPaneEnabled = !UserSettingsService.PreviewPaneSettingsService.PreviewPaneEnabled;
                    break;

                case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
                    if (NavToolbarViewModel.CanRefresh)
                    {
                        Refresh_Click();
                    }
                    break;

                case (false, false, true, true, VirtualKey.D): // alt + d, select address bar (english)
                case (true, false, false, true, VirtualKey.L): // ctrl + l, select address bar
                    NavToolbarViewModel.IsEditModeEnabled = true;
                    break;
                case (true, false, false, true, VirtualKey.H): // ctrl + h, show/hide hidden items
                    UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible = !UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible;
                    break;

                case (false, false, false, _, VirtualKey.F1): // F1, open Files wiki
                    await Launcher.LaunchUriAsync(new Uri(@"https://files.community/docs"));
                    break;
            }

            switch (args.KeyboardAccelerator.Key)
            {
                case VirtualKey.F2: //F2, rename
                    if (CurrentPageType == typeof(DetailsLayoutBrowser)
                        || CurrentPageType == typeof(GridViewBrowser)
                        || CurrentPageType == typeof(ColumnViewBrowser)
                        || CurrentPageType == typeof(ColumnViewBase))
                    {
                        if (ContentPage.IsItemSelected)
                        {
                            ContentPage.ItemManipulationModel.StartRenameItem();
                        }
                    }
                    break;
            }
        }

        public async void Refresh_Click()
        {
            NavToolbarViewModel.CanRefresh = false;

            if (InstanceViewModel.IsPageTypeSearchResults)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var searchInstance = new FolderSearch
                    {
                        Query = InstanceViewModel.CurrentSearchQuery,
                        Folder = FilesystemViewModel.WorkingDirectory,
                        ThumbnailSize = InstanceViewModel.FolderSettings.GetIconSize(),
                        SearchUnindexedItems = InstanceViewModel.SearchedUnindexedItems
                    };
                    await FilesystemViewModel.SearchAsync(searchInstance);
                });
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var ContentOwnedViewModelInstance = FilesystemViewModel;
                    ContentOwnedViewModelInstance?.RefreshItems(null);
                });
            }
        }

        public void Back_Click()
        {
            NavToolbarViewModel.CanGoBack = false;
            if (ItemDisplayFrame.CanGoBack)
            {
                var previousPageContent = ItemDisplayFrame.BackStack[ItemDisplayFrame.BackStack.Count - 1];
                var previousPageNavPath = previousPageContent.Parameter as NavigationArguments;
                previousPageNavPath.IsLayoutSwitch = false;
                if (previousPageContent.SourcePageType != typeof(WidgetsPage))
                {
                    // Update layout type
                    InstanceViewModel.FolderSettings.GetLayoutType(previousPageNavPath.IsSearchResultPage ? previousPageNavPath.SearchPathParam : previousPageNavPath.NavPathParam);
                }
                SelectSidebarItemFromPath(previousPageContent.SourcePageType);

                if (previousPageContent.SourcePageType == typeof(WidgetsPage))
                {
                    ItemDisplayFrame.GoBack(new EntranceNavigationTransitionInfo());
                }
                else
                {
                    ItemDisplayFrame.GoBack();
                }
            }
            else
            {
                this.FindAscendant<ColumnViewBrowser>().NavigateBack();
            }
        }

        public void Forward_Click()
        {
            NavToolbarViewModel.CanGoForward = false;
            if (ItemDisplayFrame.CanGoForward)
            {
                var incomingPageContent = ItemDisplayFrame.ForwardStack[ItemDisplayFrame.ForwardStack.Count - 1];
                var incomingPageNavPath = incomingPageContent.Parameter as NavigationArguments;
                incomingPageNavPath.IsLayoutSwitch = false;
                if (incomingPageContent.SourcePageType != typeof(WidgetsPage))
                {
                    // Update layout type
                    InstanceViewModel.FolderSettings.GetLayoutType(incomingPageNavPath.IsSearchResultPage ? incomingPageNavPath.SearchPathParam : incomingPageNavPath.NavPathParam);
                }
                SelectSidebarItemFromPath(incomingPageContent.SourcePageType);
                ItemDisplayFrame.GoForward();
            }
            else
            {
                this.FindAscendant<ColumnViewBrowser>().NavigateForward();
            }
        }

        public void Up_Click()
        {
            this.FindAscendant<ColumnViewBrowser>().NavigateUp();
        }

        private void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
        {
            if (incomingSourcePageType == typeof(WidgetsPage) && incomingSourcePageType != null)
            {
                NavToolbarViewModel.PathControlDisplayText = "Home".GetLocalized();
            }
        }

        public void Dispose()
        {
            Window.Current.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested -= ColumnShellPage_BackRequested;
            App.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;

            NavToolbarViewModel.ToolbarPathItemInvoked -= ColumnShellPage_NavigationRequested;
            NavToolbarViewModel.ToolbarFlyoutOpened -= ColumnShellPage_ToolbarFlyoutOpened;
            NavToolbarViewModel.ToolbarPathItemLoaded -= ColumnShellPage_ToolbarPathItemLoaded;
            NavToolbarViewModel.AddressBarTextEntered -= ColumnShellPage_AddressBarTextEntered;
            NavToolbarViewModel.PathBoxItemDropped -= ColumnShellPage_PathBoxItemDropped;
            NavToolbarViewModel.BackRequested -= ColumnShellPage_BackNavRequested;
            NavToolbarViewModel.UpRequested -= ColumnShellPage_UpNavRequested;
            NavToolbarViewModel.RefreshRequested -= ColumnShellPage_RefreshRequested;
            NavToolbarViewModel.ForwardRequested -= ColumnShellPage_ForwardNavRequested;
            NavToolbarViewModel.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
            NavToolbarViewModel.ItemDraggedOverPathItem -= ColumnShellPage_NavigationRequested;
            NavToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;

            NavToolbarViewModel.SearchBox.TextChanged -= ColumnShellPage_TextChanged;
            NavToolbarViewModel.SearchBox.QuerySubmitted -= ColumnShellPage_QuerySubmitted;

            InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;

            if (FilesystemViewModel != null)    // Prevent weird case of this being null when many tabs are opened/closed quickly
            {
                FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
                FilesystemViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
                FilesystemViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
                FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
                FilesystemViewModel.OnSelectionRequestedEvent -= FilesystemViewModel_OnSelectionRequestedEvent;
                FilesystemViewModel.Dispose();
            }

            if (ItemDisplayFrame.Content is IDisposable disposableContent)
            {
                disposableContent?.Dispose();
            }
        }

        private void FilesystemViewModel_ItemLoadStatusChanged(object sender, ItemLoadStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting:
                    NavToolbarViewModel.CanRefresh = false;
                    SetLoadingIndicatorForTabs(true);
                    break;

                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
                    var browser = this.FindAscendant<ColumnViewBrowser>();
                    NavToolbarViewModel.CanGoBack = ItemDisplayFrame.CanGoBack || browser.ParentShellPageInstance.CanNavigateBackward;
                    NavToolbarViewModel.CanGoForward = ItemDisplayFrame.CanGoForward || browser.ParentShellPageInstance.CanNavigateForward;
                    SetLoadingIndicatorForTabs(true);
                    break;

                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
                    SetLoadingIndicatorForTabs(false);
                    NavToolbarViewModel.CanRefresh = true;
                    // Select previous directory
                    if (!string.IsNullOrWhiteSpace(e.PreviousDirectory))
                    {
                        if (e.PreviousDirectory.Contains(e.Path, StringComparison.Ordinal) && !e.PreviousDirectory.Contains("Shell:RecycleBinFolder", StringComparison.Ordinal))
                        {
                            // Remove the WorkingDir from previous dir
                            e.PreviousDirectory = e.PreviousDirectory.Replace(e.Path, string.Empty, StringComparison.Ordinal);

                            // Get previous dir name
                            if (e.PreviousDirectory.StartsWith('\\'))
                            {
                                e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
                            }
                            if (e.PreviousDirectory.Contains('\\'))
                            {
                                e.PreviousDirectory = e.PreviousDirectory.Split('\\')[0];
                            }

                            // Get the first folder and combine it with WorkingDir
                            string folderToSelect = string.Format("{0}\\{1}", e.Path, e.PreviousDirectory);

                            // Make sure we don't get double \\ in the e.Path
                            folderToSelect = folderToSelect.Replace("\\\\", "\\", StringComparison.Ordinal);

                            if (folderToSelect.EndsWith('\\'))
                            {
                                folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);
                            }

                            ListedItem itemToSelect = FilesystemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

                            if (itemToSelect != null && ContentPage != null)
                            {
                                ContentPage.ItemManipulationModel.SetSelectedItem(itemToSelect);
                                ContentPage.ItemManipulationModel.ScrollIntoView(itemToSelect);
                            }
                        }
                    }
                    break;
            }
        }

        private void SetLoadingIndicatorForTabs(bool isLoading)
        {
            var multitaskingControls = ((Window.Current.Content as Frame).Content as MainPage).ViewModel.MultitaskingControls;

            foreach (var x in multitaskingControls)
            {
                x.SetLoadingIndicatorStatus(x.Items.FirstOrDefault(x => x.Control.TabItemContent == PaneHolder), isLoading);
            }
        }

        public Task TabItemDragOver(object sender, DragEventArgs e) => SlimContentPage?.CommandsViewModel.CommandsModel.DragOver(e);

        public Task TabItemDrop(object sender, DragEventArgs e) => SlimContentPage?.CommandsViewModel.CommandsModel.Drop(e);

        public void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs)
        {
            NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
        }

        public void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
        {
            this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(navigationPath, sourcePageType, navArgs);
        }

        public void NavigateToPath(string navigationPath, NavigationArguments navArgs = null)
        {
            NavigateToPath(navigationPath, FolderSettings.GetLayoutType(navigationPath), navArgs);
        }

        public void NavigateHome()
        {
            throw new NotImplementedException("Can't show Home page in Column View");
        }

        public void RemoveLastPageFromBackStack()
        {
            ItemDisplayFrame.BackStack.Remove(ItemDisplayFrame.BackStack.Last());
        }

        public void SubmitSearch(string query, bool searchUnindexedItems)
        {
            FilesystemViewModel.CancelSearch();
            InstanceViewModel.CurrentSearchQuery = query;
            InstanceViewModel.SearchedUnindexedItems = searchUnindexedItems;
            ItemDisplayFrame.Navigate(typeof(ColumnViewBase), new NavigationArguments()
            {
                AssociatedTabInstance = this,
                IsSearchResultPage = true,
                SearchPathParam = FilesystemViewModel.WorkingDirectory,
                SearchQuery = query,
                SearchUnindexedItems = searchUnindexedItems,
            });
            //this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(null, typeof(ColumnViewBase), navArgs);
        }
    }
}