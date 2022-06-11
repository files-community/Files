using Files.Shared;
using Files.Uwp.Dialogs;
using Files.Shared.Enums;
using Files.Uwp.EventArguments;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.FilesystemHistory;
using Files.Uwp.Filesystem.Search;
using Files.Uwp.Helpers;
using Files.Backend.Services.Settings;
using Files.Uwp.UserControls;
using Files.Uwp.UserControls.MultitaskingControl;
using Files.Uwp.ViewModels;
using Files.Uwp.Views.LayoutModes;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
using Files.Uwp.Interacts;
using SortDirection = Files.Shared.Enums.SortDirection;
using Files.Backend.Enums;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files.Uwp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColumnShellPage : Page, IShellPage, INotifyPropertyChanged
    {
        private readonly StorageHistoryHelpers storageHistoryHelpers;
        public IBaseLayout SlimContentPage => ContentPage;
        public IFilesystemHelpers FilesystemHelpers { get; private set; }
        private readonly CancellationTokenSource cancellationTokenSource;

        public bool CanNavigateBackward => false;
        public bool CanNavigateForward => false;

        public FolderSettingsViewModel FolderSettings => InstanceViewModel?.FolderSettings;

        public MainViewModel MainViewModel => App.MainViewModel;

        public bool IsColumnView { get; } = true;

        private IDialogService DialogService { get; } = Ioc.Default.GetRequiredService<IDialogService>();

        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private IUpdateService UpdateSettingsService { get; } = Ioc.Default.GetService<IUpdateService>();

        private bool isCurrentInstance = false;
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

        public ToolbarViewModel ToolbarViewModel { get; } = new ToolbarViewModel();

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

            ToolbarViewModel.ToolbarPathItemInvoked += ColumnShellPage_NavigationRequested;
            ToolbarViewModel.ToolbarFlyoutOpened += ColumnShellPage_ToolbarFlyoutOpened;
            ToolbarViewModel.ToolbarPathItemLoaded += ColumnShellPage_ToolbarPathItemLoaded;
            ToolbarViewModel.AddressBarTextEntered += ColumnShellPage_AddressBarTextEntered;
            ToolbarViewModel.PathBoxItemDropped += ColumnShellPage_PathBoxItemDropped;
            ToolbarViewModel.BackRequested += ColumnShellPage_BackNavRequested;
            ToolbarViewModel.UpRequested += ColumnShellPage_UpNavRequested;
            ToolbarViewModel.RefreshRequested += ColumnShellPage_RefreshRequested;
            ToolbarViewModel.ForwardRequested += ColumnShellPage_ForwardNavRequested;
            ToolbarViewModel.EditModeEnabled += NavigationToolbar_EditModeEnabled;
            ToolbarViewModel.ItemDraggedOverPathItem += ColumnShellPage_NavigationRequested;
            ToolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
            ToolbarViewModel.SearchBox.TextChanged += ColumnShellPage_TextChanged;
            ToolbarViewModel.SearchBox.QuerySubmitted += ColumnShellPage_QuerySubmitted;

            ToolbarViewModel.InstanceViewModel = InstanceViewModel;
            //NavToolbarViewModel.RefreshWidgetsRequested += refreshwid;

            InitToolbarCommands();

            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated += AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested += ColumnShellPage_BackRequested;

            App.DrivesManager.PropertyChanged += DrivesManager_PropertyChanged;

            PreviewKeyDown += ColumnShellPage_PreviewKeyDown;
        }

        /**
         * Some keys are overriden by control built-in defaults (e.g. 'Space').
         * They must be handled here since they're not propagated to KeyboardAccelerator.
         */
        private async void ColumnShellPage_PreviewKeyDown(object sender, KeyRoutedEventArgs args)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            var tabInstance = CurrentPageType == typeof(DetailsLayoutBrowser) ||
                              CurrentPageType == typeof(GridViewBrowser) ||
                              CurrentPageType == typeof(ColumnViewBrowser) ||
                              CurrentPageType == typeof(ColumnViewBase);

            switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.Key)
            {
                case (true, false, false, true, (VirtualKey) 192): // ctrl + ` (accent key), open terminal
                    // Check if there is a folder selected, if not use the current directory.
                    string path = FilesystemViewModel.WorkingDirectory;
                    if (SlimContentPage?.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        path = SlimContentPage.SelectedItem.ItemPath;
                    }
                    await NavigationHelpers.OpenDirectoryInTerminal(path);
                    args.Handled = true;
                    break;

                case (false, false, false, true, VirtualKey.Space): // space, quick look
                    // handled in `CurrentPageType`::FileList_PreviewKeyDown
                    break;

                case (true, false, false, true, VirtualKey.Space): // ctrl + space, toggle media playback
                    if (App.PreviewPaneViewModel.PreviewPaneContent is UserControls.FilePreviews.MediaPreview mediaPreviewContent)
                    {
                        mediaPreviewContent.ViewModel.TogglePlayback();
                        args.Handled = true;
                    }
                    break;
            }
        }

        private async void ColumnShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
        {
            await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, e.Item, this);
        }

        private async void ColumnShellPage_ToolbarFlyoutOpened(object sender, ToolbarFlyoutOpenedEventArgs e)
        {
            await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, (e.OpenedFlyout.Target as FontIcon).DataContext as PathBoxItem, this);
        }

        private void InitToolbarCommands()
        {
            ToolbarViewModel.SelectAllContentPageItemsCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.SelectAllItems());
            ToolbarViewModel.InvertContentPageSelctionCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.InvertSelection());
            ToolbarViewModel.ClearContentPageSelectionCommand = new RelayCommand(() => SlimContentPage?.ItemManipulationModel.ClearSelection());
            ToolbarViewModel.PasteItemsFromClipboardCommand = new RelayCommand(async () => await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this));
            ToolbarViewModel.OpenNewWindowCommand = new RelayCommand(NavigationHelpers.LaunchNewWindow);
            ToolbarViewModel.OpenNewPaneCommand = new RelayCommand(() => PaneHolder?.OpenPathInNewPane("Home".GetLocalized()));
            ToolbarViewModel.ClosePaneCommand = new RelayCommand(() => PaneHolder?.CloseActivePane());
            ToolbarViewModel.OpenDirectoryInDefaultTerminalCommand = new RelayCommand(async () => await NavigationHelpers.OpenDirectoryInTerminal(this.FilesystemViewModel.WorkingDirectory));
            ToolbarViewModel.CreateNewFileCommand = new RelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.File, x, this));
            ToolbarViewModel.CreateNewFolderCommand = new RelayCommand(() => UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null, this));
            ToolbarViewModel.CopyCommand = new RelayCommand(async () => await UIFilesystemHelpers.CopyItem(this));
            ToolbarViewModel.Rename = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RenameItemCommand.Execute(null));
            ToolbarViewModel.Share = new RelayCommand(() => SlimContentPage?.CommandsViewModel.ShareItemCommand.Execute(null));
            ToolbarViewModel.DeleteCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DeleteItemCommand.Execute(null));
            ToolbarViewModel.CutCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.CutItemCommand.Execute(null));
            ToolbarViewModel.EmptyRecycleBinCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.EmptyRecycleBinCommand.Execute(null));
            ToolbarViewModel.RunWithPowerShellCommand = new RelayCommand(async () => await Win32Helpers.InvokeWin32ComponentAsync("powershell", this, PathNormalization.NormalizePath(SlimContentPage?.SelectedItem.ItemPath)));
            ToolbarViewModel.PropertiesCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.ShowPropertiesCommand.Execute(null));
            ToolbarViewModel.SetAsBackgroundCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.SetAsDesktopBackgroundItemCommand.Execute(null));
            ToolbarViewModel.ExtractCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DecompressArchiveCommand.Execute(null));
            ToolbarViewModel.ExtractHereCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DecompressArchiveHereCommand.Execute(null));
            ToolbarViewModel.ExtractToCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.DecompressArchiveToChildFolderCommand.Execute(null));
            ToolbarViewModel.InstallInfCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.InstallInfDriver.Execute(null));
            ToolbarViewModel.RotateImageLeftCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RotateImageLeftCommand.Execute(null), () => SlimContentPage?.CommandsViewModel.RotateImageLeftCommand.CanExecute(null) == true);
            ToolbarViewModel.RotateImageRightCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.RotateImageRightCommand.Execute(null), () => SlimContentPage?.CommandsViewModel.RotateImageRightCommand.CanExecute(null) == true);
            ToolbarViewModel.InstallFontCommand = new RelayCommand(() => SlimContentPage?.CommandsViewModel.InstallFontCommand.Execute(null));
            ToolbarViewModel.UpdateCommand = new AsyncRelayCommand(async () => await UpdateSettingsService.DownloadUpdates());
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
                var lastCommonItemIndex = ToolbarViewModel.PathComponents
                    .Select((value, index) => new { value, index })
                    .LastOrDefault(x => x.index < components.Count && x.value.Path == components[x.index].Path)?.index ?? 0;
                while (ToolbarViewModel.PathComponents.Count > lastCommonItemIndex)
                {
                    ToolbarViewModel.PathComponents.RemoveAt(lastCommonItemIndex);
                }
                foreach (var component in components.Skip(lastCommonItemIndex))
                {
                    ToolbarViewModel.PathComponents.Add(component);
                }
            }
            else
            {
                ToolbarViewModel.PathComponents.Clear(); // Clear the path UI
                ToolbarViewModel.IsSingleItemOverride = true;
                ToolbarViewModel.PathComponents.Add(new Views.PathBoxItem() { Path = null, Title = singleItemOverride });
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

        private void AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated(object sender, bool e)
        {
            FilesystemViewModel?.UpdateSortDirectoriesAlongsideFiles();
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
            ToolbarViewModel.SetAddressBarSuggestions(e.AddressBarTextField, this);
        }

        private void ColumnShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
        {
            this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(e);
        }

        private async void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
        {
            await ToolbarViewModel.CheckPathInput(e.QueryText, ToolbarViewModel.PathComponents[ToolbarViewModel.PathComponents.Count - 1].Path, this);
        }

        private void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
        {
            ToolbarViewModel.ManualEntryBoxLoaded = true;
            ToolbarViewModel.ClickablePathLoaded = false;
            ToolbarViewModel.PathText = string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory)
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
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                FrameContent = ItemDisplayFrame.Content as BaseLayout;
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
            // set focus since selection might occur before the UI finishes updating
            ContentPage.ItemManipulationModel.FocusFileList();
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
                ContentPage.UpdateSelectionSize();
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
            ToolbarViewModel.SearchBox.Query = string.Empty;
            ToolbarViewModel.IsSearchBoxVisible = false;
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
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);
            var tabInstance = CurrentPageType == typeof(DetailsLayoutBrowser) ||
                              CurrentPageType == typeof(GridViewBrowser) ||
                              CurrentPageType == typeof(ColumnViewBrowser) ||
                              CurrentPageType == typeof(ColumnViewBase);

            switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
            {
                case (true, false, false, true, VirtualKey.E): // ctrl + e, extract
                    {
                        if (ToolbarViewModel.CanExtract)
                        {
                            ToolbarViewModel.ExtractCommand.Execute(null);
                        }
                        break;
                    }

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
                    ToolbarViewModel.SwitchSearchBoxVisibility();
                    break;

                case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
                    if (InstanceViewModel.CanCreateFileInPage)
                    {
                        var addItemDialogViewModel = new AddItemDialogViewModel();
                        await DialogService.ShowDialogAsync(addItemDialogViewModel);
                        if (addItemDialogViewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
                        {
                            UIFilesystemHelpers.CreateFileFromDialogResultType(
                                addItemDialogViewModel.ResultType.ItemType,
                                addItemDialogViewModel.ResultType.ItemInfo,
                                this);
                        }
                    }
                    break;

                case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
                    if (ContentPage.IsItemSelected && !ToolbarViewModel.IsEditModeEnabled && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                            item.ItemPath,
                            item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
                        await FilesystemHelpers.DeleteItemsAsync(items, true, true, true);
                    }

                    break;

                case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
                    if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        await UIFilesystemHelpers.CopyItem(this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
                    if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults && !ToolbarViewModel.SearchHasFocus)
                    {
                        await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
                    if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        UIFilesystemHelpers.CutItem(this);
                    }

                    break;

                case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
                    if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
                    {
                        this.SlimContentPage.ItemManipulationModel.SelectAllItems();
                    }

                    break;

                case (true, false, false, true, VirtualKey.D): // ctrl + d, delete item
                case (false, false, false, true, VirtualKey.Delete): // delete, delete item
                    if (ContentPage.IsItemSelected && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                            item.ItemPath,
                            item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
                        await FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
                    }

                    break;

                case (true, false, false, true, VirtualKey.P): // ctrl + p, toggle preview pane
                    App.PaneViewModel.IsPreviewSelected = !App.PaneViewModel.IsPreviewSelected;
                    break;

                case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
                    if (ToolbarViewModel.CanRefresh)
                    {
                        Refresh_Click();
                    }
                    break;

                case (false, false, true, true, VirtualKey.D): // alt + d, select address bar (english)
                case (true, false, false, true, VirtualKey.L): // ctrl + l, select address bar
                    ToolbarViewModel.IsEditModeEnabled = true;
                    break;

                case (true, true, false, true, VirtualKey.K): // ctrl + shift + k, duplicate tab
                    await NavigationHelpers.OpenPathInNewTab(this.FilesystemViewModel.WorkingDirectory);
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
            if (InstanceViewModel.IsPageTypeSearchResults)
            {
                ToolbarViewModel.CanRefresh = false;
                var searchInstance = new FolderSearch
                {
                    Query = InstanceViewModel.CurrentSearchQuery,
                    Folder = FilesystemViewModel.WorkingDirectory,
                    ThumbnailSize = InstanceViewModel.FolderSettings.GetIconSize(),
                    SearchUnindexedItems = InstanceViewModel.SearchedUnindexedItems
                };
                await FilesystemViewModel.SearchAsync(searchInstance);
            }
            else if (CurrentPageType != typeof(WidgetsPage))
            {
                ToolbarViewModel.CanRefresh = false;
                FilesystemViewModel?.RefreshItems(null);
            }
        }

        public void Back_Click()
        {
            ToolbarViewModel.CanGoBack = false;
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
            ToolbarViewModel.CanGoForward = false;
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
                ToolbarViewModel.PathControlDisplayText = "Home".GetLocalized();
            }
        }

        public void Dispose()
        {
            PreviewKeyDown -= ColumnShellPage_PreviewKeyDown;
            Window.Current.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested -= ColumnShellPage_BackRequested;
            App.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;

            ToolbarViewModel.ToolbarPathItemInvoked -= ColumnShellPage_NavigationRequested;
            ToolbarViewModel.ToolbarFlyoutOpened -= ColumnShellPage_ToolbarFlyoutOpened;
            ToolbarViewModel.ToolbarPathItemLoaded -= ColumnShellPage_ToolbarPathItemLoaded;
            ToolbarViewModel.AddressBarTextEntered -= ColumnShellPage_AddressBarTextEntered;
            ToolbarViewModel.PathBoxItemDropped -= ColumnShellPage_PathBoxItemDropped;
            ToolbarViewModel.BackRequested -= ColumnShellPage_BackNavRequested;
            ToolbarViewModel.UpRequested -= ColumnShellPage_UpNavRequested;
            ToolbarViewModel.RefreshRequested -= ColumnShellPage_RefreshRequested;
            ToolbarViewModel.ForwardRequested -= ColumnShellPage_ForwardNavRequested;
            ToolbarViewModel.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
            ToolbarViewModel.ItemDraggedOverPathItem -= ColumnShellPage_NavigationRequested;
            ToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;

            ToolbarViewModel.SearchBox.TextChanged -= ColumnShellPage_TextChanged;
            ToolbarViewModel.SearchBox.QuerySubmitted -= ColumnShellPage_QuerySubmitted;

            InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
            InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;
            InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated -= AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

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
                    ToolbarViewModel.CanRefresh = false;
                    SetLoadingIndicatorForTabs(true);
                    break;

                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
                    var browser = this.FindAscendant<ColumnViewBrowser>();
                    ToolbarViewModel.CanGoBack = ItemDisplayFrame.CanGoBack || browser.ParentShellPageInstance.CanNavigateBackward;
                    ToolbarViewModel.CanGoForward = ItemDisplayFrame.CanGoForward || browser.ParentShellPageInstance.CanNavigateForward;
                    SetLoadingIndicatorForTabs(true);
                    break;

                case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
                    SetLoadingIndicatorForTabs(false);
                    ToolbarViewModel.CanRefresh = true;
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