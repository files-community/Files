using Files.DataModels.NavigationControlItems;
using Files.Shared.Enums;
using Files.EventArguments;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.Backend.Services.Settings;
using Files.UserControls;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Files.Shared.EventArguments;
using Windows.UI.Xaml.Hosting;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.WindowManagement;
using Microsoft.Toolkit.Uwp.UI;
using Windows.System;
using SearchBox = Files.UserControls.SearchBox;
using Files.Uwp.Helpers;

namespace Files.Views
{
    /// <summary>
    /// The root page of Files
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public MainViewModel MainViewModel => App.MainViewModel;

        public MainPageViewModel ViewModel
        {
            get => (MainPageViewModel)DataContext;
            set => DataContext = value;
        }

        public SidebarViewModel SidebarAdaptiveViewModel = new SidebarViewModel();

        public OngoingTasksViewModel OngoingTasksViewModel => App.OngoingTasksViewModel;

        public ICommand ToggleFullScreenAcceleratorCommand { get; }

        private ICommand ToggleCompactOverlayCommand { get; }
        private ICommand SetCompactOverlayCommand { get; }
        private ICommand ToggleSidebarCollapsedStateCommand => new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(x => ToggleSidebarCollapsedState(x));

        private NavigationEventArgs _navigationEventArgs;
        public bool IsVerticalTabFlyoutEnabled => UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled;

        public MainPage()
        {
            InitializeComponent();

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }
            AllowDrop = true;

            ToggleFullScreenAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ToggleFullScreenAccelerator);
            ToggleCompactOverlayCommand = new RelayCommand(ToggleCompactOverlay);
            SetCompactOverlayCommand = new RelayCommand<bool>(SetCompactOverlay);

            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
        }

        private void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.SettingName)
            {
                case nameof(IPaneSettingsService.Content):
                    LoadPaneChanged();
                    break;
                case nameof(IMultitaskingSettingsService.IsVerticalTabFlyoutEnabled):
                    OnPropertyChanged(nameof(IsVerticalTabFlyoutEnabled));
                    break;
            }
        }

        public void FocusSearchBox()
        {
            // Given that binding and layouting might take a few cycles, when calling UpdateLayout
            // we can guarantee that the focus call will be able to find an open ASB
            var searchbox = NavToolbar.FindDescendant("SearchRegion") as SearchBox;
            searchbox?.UpdateLayout();
            searchbox?.Focus(FocusState.Programmatic);
        }

        public AutoSuggestBox FocusVisiblePath()
        {
            var visiblePath = NavToolbar.FindDescendant<AutoSuggestBox>(x => x.Name == "VisiblePath");
            visiblePath?.Focus(FocusState.Programmatic);
            visiblePath?.FindDescendant<TextBox>()?.SelectAll();

            return visiblePath;
        }

        private void VerticalTabStrip_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private void VerticalTabStripInvokeButton_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private void VerticalTabStripInvokeButton_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.MultitaskingControls.Add(VerticalTabs);
            VerticalTabs.CurrentInstanceChanged -= MultitaskingControl_CurrentInstanceChanged;
            VerticalTabs.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            if (WindowManagementHelpers.GetWindowFromUIContext(this.XamlRoot.UIContext) is Window window)
            {
                window.SetTitleBar(sender as Grid);
            }
            else if (WindowManagementHelpers.GetWindowFromUIContext(this.XamlRoot.UIContext) is AppWindow appWindow)
            {
                appWindow.Frame.DragRegionVisuals.Clear();
                appWindow.Frame.DragRegionVisuals.Add(sender as Grid);
            }
        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.MultitaskingControl == null)
            {
                ViewModel.MultitaskingControl = horizontalMultitaskingControl;
                ViewModel.MultitaskingControls.Add(horizontalMultitaskingControl);
                horizontalMultitaskingControl.CurrentInstanceChanged -= MultitaskingControl_CurrentInstanceChanged;
                horizontalMultitaskingControl.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;

                // Complete navigation only once primary tab control is loaded
                ViewModel.OnNavigatedTo(_navigationEventArgs);
            }

            if (UserSettingsService.MultitaskingSettingsService.IsVerticalTabFlyoutEnabled)
            {
                FindName(nameof(VerticalTabStripInvokeButton));
            }
        }

        public async void TabItemContent_ContentChanged(object sender, TabItemArguments e)
        {
            if (SidebarAdaptiveViewModel.PaneHolder != null)
            {
                var paneArgs = e.NavigationArg as PaneNavigationArguments;
                SidebarAdaptiveViewModel.UpdateSidebarSelectedItemFromArgs(SidebarAdaptiveViewModel.PaneHolder.IsLeftPaneActive ?
                    paneArgs.LeftPaneNavPathParam : paneArgs.RightPaneNavPathParam);
                UpdateStatusBarProperties();
                LoadPaneChanged();
                UpdateNavToolbarProperties();
                string windowTitle = await ViewModel.UpdateInstancePropertiesAsync(paneArgs);

                if (e.NavigationArg == ViewModel.MultitaskingControl.SelectedTabItem?.TabItemArguments.NavigationArg)
                {
                    if (WindowManagementHelpers.GetWindowFromUIContext(this.XamlRoot.UIContext) is Window window)
                    {
                        ApplicationView.GetForCurrentView().Title = windowTitle;
                    }
                    else if (WindowManagementHelpers.GetWindowFromUIContext(this.XamlRoot.UIContext) is AppWindow appWindow)
                    {
                        appWindow.Title = windowTitle;
                    }
                }
            }
        }

        public async void MultitaskingControl_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
        {
            if (SidebarAdaptiveViewModel.PaneHolder != null)
            {
                SidebarAdaptiveViewModel.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;
            }
            var navArgs = e.CurrentInstance.TabItemArguments?.NavigationArg;
            SidebarAdaptiveViewModel.PaneHolder = e.CurrentInstance as IPaneHolder;
            SidebarAdaptiveViewModel.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
            SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged((navArgs as PaneNavigationArguments).LeftPaneNavPathParam);
            UpdateStatusBarProperties();
            UpdateNavToolbarProperties();
            LoadPaneChanged();
            await ViewModel.UpdateInstancePropertiesAsync(navArgs);
            e.CurrentInstance.ContentChanged -= TabItemContent_ContentChanged;
            e.CurrentInstance.ContentChanged += TabItemContent_ContentChanged;
        }

        private void PaneHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged(SidebarAdaptiveViewModel.PaneHolder.ActivePane?.TabItemArguments?.NavigationArg?.ToString());
            UpdateStatusBarProperties();
            UpdateNavToolbarProperties();
            LoadPaneChanged();
        }

        private void UpdateStatusBarProperties()
        {
            if (StatusBarControl != null)
            {
                StatusBarControl.DirectoryPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.DirectoryPropertiesViewModel;
                StatusBarControl.SelectedItemsPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.SelectedItemsPropertiesViewModel;
            }
        }

        private void UpdateNavToolbarProperties()
        {
            if (NavToolbar != null)
            {
                NavToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.NavToolbarViewModel;
            }

            if (InnerNavigationToolbar != null)
            {
                InnerNavigationToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.NavToolbarViewModel;
                InnerNavigationToolbar.ShowMultiPaneControls = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneEnabled ?? false;
                InnerNavigationToolbar.IsMultiPaneActive = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _navigationEventArgs = e;
            SidebarControl.SidebarItemInvoked += SidebarControl_SidebarItemInvoked;
            SidebarControl.SidebarItemPropertiesInvoked += SidebarControl_SidebarItemPropertiesInvoked;
            SidebarControl.SidebarItemDropped += SidebarControl_SidebarItemDropped;
            SidebarControl.SidebarItemNewPaneInvoked += SidebarControl_SidebarItemNewPaneInvoked;
        }

        private async void SidebarControl_SidebarItemDropped(object sender, SidebarItemDroppedEventArgs e)
        {
            await SidebarAdaptiveViewModel.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.ItemPath, false, true);
            e.SignalEvent?.Set();
        }

        private async void SidebarControl_SidebarItemPropertiesInvoked(object sender, SidebarItemPropertiesInvokedEventArgs e)
        {
            if (e.InvokedItemDataContext is DriveItem)
            {
                await FilePropertiesHelpers.OpenPropertiesWindowAsync(e.InvokedItemDataContext, SidebarAdaptiveViewModel.PaneHolder.ActivePane);
            }
            else if (e.InvokedItemDataContext is LibraryLocationItem library)
            {
                await FilePropertiesHelpers.OpenPropertiesWindowAsync(new LibraryItem(library), SidebarAdaptiveViewModel.PaneHolder.ActivePane);
            }
            else if (e.InvokedItemDataContext is LocationItem locationItem)
            {
                ListedItem listedItem = new ListedItem(null)
                {
                    ItemPath = locationItem.Path,
                    ItemNameRaw = locationItem.Text,
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemType = "FileFolderListItem".GetLocalized(),
                };
                await FilePropertiesHelpers.OpenPropertiesWindowAsync(listedItem, SidebarAdaptiveViewModel.PaneHolder.ActivePane);
            }
        }

        private void SidebarControl_SidebarItemNewPaneInvoked(object sender, SidebarItemNewPaneInvokedEventArgs e)
        {
            if (e.InvokedItemDataContext is INavigationControlItem navItem)
            {
                SidebarAdaptiveViewModel.PaneHolder.OpenPathInNewPane(navItem.Path);
            }
        }

        private void SidebarControl_SidebarItemInvoked(object sender, SidebarItemInvokedEventArgs e)
        {
            var invokedItemContainer = e.InvokedItemContainer;

            string navigationPath; // path to navigate
            Type sourcePageType = null; // type of page to navigate

            switch ((invokedItemContainer.DataContext as INavigationControlItem).ItemType)
            {
                case NavigationControlItemType.Location:
                    {
                        var ItemPath = (invokedItemContainer.DataContext as INavigationControlItem).Path; // Get the path of the invoked item

                        if (string.IsNullOrEmpty(ItemPath)) // Section item
                        {
                            navigationPath = invokedItemContainer.Tag?.ToString();
                        }
                        else if (ItemPath.Equals("Home".GetLocalized(), StringComparison.OrdinalIgnoreCase)) // Home item
                        {
                            if (ItemPath.Equals(SidebarAdaptiveViewModel.SidebarSelectedItem?.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                return; // return if already selected
                            }

                            navigationPath = "Home".GetLocalized();
                            sourcePageType = typeof(WidgetsPage);
                        }
                        else // Any other item
                        {
                            navigationPath = invokedItemContainer.Tag?.ToString();
                        }

                        break;
                    }

                case NavigationControlItemType.FileTag:
                    var tagPath = (invokedItemContainer.DataContext as INavigationControlItem).Path; // Get the path of the invoked item
                    if (SidebarAdaptiveViewModel.PaneHolder?.ActivePane is IShellPage shp)
                    {
                        shp.NavigateToPath(tagPath, new NavigationArguments()
                        {
                            IsSearchResultPage = true,
                            SearchPathParam = "Home".GetLocalized(),
                            SearchQuery = tagPath,
                            AssociatedTabInstance = shp
                        });
                    }
                    return;

                default:
                    {
                        navigationPath = invokedItemContainer.Tag?.ToString();
                        break;
                    }
            }

            if (SidebarAdaptiveViewModel.PaneHolder?.ActivePane is IShellPage shellPage)
            {
                shellPage.NavigateToPath(navigationPath, sourcePageType);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (SidebarControl != null)
            {
                SidebarControl.SidebarItemInvoked -= SidebarControl_SidebarItemInvoked;
                SidebarControl.SidebarItemPropertiesInvoked -= SidebarControl_SidebarItemPropertiesInvoked;
                SidebarControl.SidebarItemDropped -= SidebarControl_SidebarItemDropped;
                SidebarControl.SidebarItemNewPaneInvoked -= SidebarControl_SidebarItemNewPaneInvoked;
            }
        }

        private void ToggleFullScreenAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            object currentWindow = WindowManagementHelpers.GetWindowFromUIContext(this.UIContext);
            
            if (currentWindow is AppWindow appWindow)
            {
                var presentationKind = appWindow.Presenter.GetConfiguration().Kind;
                if (presentationKind == AppWindowPresentationKind.FullScreen)
                {
                    appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.Default);
                }
                else
                {
                    appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.FullScreen);
                }
            }
            else
            {
                ApplicationView view = ApplicationView.GetForCurrentView();
                if (view.IsFullScreenMode)
                {
                    view.ExitFullScreenMode();
                }
                else
                {
                    view.TryEnterFullScreenMode();
                }
            }

            e.Handled = true;
        }

        private void ToggleSidebarCollapsedState(KeyboardAcceleratorInvokedEventArgs e)
        {
            SidebarAdaptiveViewModel.IsSidebarOpen = !SidebarAdaptiveViewModel.IsSidebarOpen;

            e.Handled = true;
        }

        private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
        {
            SidebarAdaptiveViewModel.UpdateTabControlMargin(); // Set the correct tab margin on startup
        }

        //private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) => LoadPaneChanged();

        /// <summary>
        /// Call this function to update the positioning of the preview pane.
        /// This is a workaround as the VisualStateManager causes problems.
        /// </summary>
        private void UpdatePositioning()
        {
            if (Pane is null || !IsPaneEnabled)
            {
                PaneRow.MinHeight = 0;
                PaneRow.MaxHeight = double.MaxValue;
                PaneRow.Height = new GridLength(0);
                PaneColumn.MinWidth = 0;
                PaneColumn.MaxWidth = double.MaxValue;
                PaneColumn.Width = new GridLength(0);
            }
            else
            {
                Pane.UpdatePosition(RootGrid.ActualWidth, RootGrid.ActualHeight);
                switch (Pane.Position)
                {
                    case PanePositions.None:
                        PaneRow.MinHeight = 0;
                        PaneRow.Height = new GridLength(0);
                        PaneColumn.MinWidth = 0;
                        PaneColumn.Width = new GridLength(0);
                        break;
                    case PanePositions.Right:
                        Pane.SetValue(Grid.RowProperty, 1);
                        Pane.SetValue(Grid.ColumnProperty, 2);
                        PaneSplitter.SetValue(Grid.RowProperty, 1);
                        PaneSplitter.SetValue(Grid.ColumnProperty, 1);
                        PaneSplitter.Width = 2;
                        PaneSplitter.Height = RootGrid.ActualHeight;
                        PaneColumn.MinWidth = Pane.MinWidth;
                        PaneColumn.MaxWidth = Pane.MaxWidth;
                        PaneColumn.Width = new GridLength(UserSettingsService.PaneSettingsService.VerticalSizePx, GridUnitType.Pixel);
                        PaneRow.MinHeight = 0;
                        PaneRow.MaxHeight = double.MaxValue;
                        PaneRow.Height = new GridLength(0);
                        break;
                    case PanePositions.Bottom:
                        Pane.SetValue(Grid.RowProperty, 3);
                        Pane.SetValue(Grid.ColumnProperty, 0);
                        PaneSplitter.SetValue(Grid.RowProperty, 2);
                        PaneSplitter.SetValue(Grid.ColumnProperty, 0);
                        PaneSplitter.Height = 2;
                        PaneSplitter.Width = RootGrid.ActualWidth;
                        PaneColumn.MinWidth = 0;
                        PaneColumn.MaxWidth = double.MaxValue;
                        PaneColumn.Width = new GridLength(0);
                        PaneRow.MinHeight = Pane.MinHeight;
                        PaneRow.MaxHeight = Pane.MaxHeight;
                        PaneRow.Height = new GridLength(UserSettingsService.PaneSettingsService.HorizontalSizePx, GridUnitType.Pixel);
                        break;
                }
            }
        }

        private void PaneSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (Pane is IPane p)
            {
                switch (p.Position)
                {
                    case PanePositions.Right:
                        UserSettingsService.PaneSettingsService.VerticalSizePx = Pane.ActualWidth;
                        break;
                    case PanePositions.Bottom:
                        UserSettingsService.PaneSettingsService.HorizontalSizePx = Pane.ActualHeight;
                        break;
                }
            }
        }

        public bool IsPaneEnabled => UserSettingsService.PaneSettingsService.Content switch
        {
            PaneContents.Preview => IsPreviewPaneEnabled,
            _ => false,
        };

        public bool IsPreviewPaneEnabled
        {
            get
            {
                bool isHomePage = !(SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false);
                bool isMultiPane = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;
                Size size = this.XamlRoot.Size;
                bool isBigEnough = size.Width > 450 && size.Height > 400;

                return (!isHomePage || isMultiPane) && isBigEnough;
            }
        }

        DispatcherQueueTimer timer = DispatcherQueue.GetForCurrentThread().CreateTimer();

        private void LoadPaneChanged()
        {
            DispatcherQueueTimerExtensions.Debounce(timer, () =>
            {
                OnPropertyChanged(nameof(IsPaneEnabled));
                OnPropertyChanged(nameof(IsPreviewPaneEnabled));
            }, TimeSpan.FromSeconds(1));

            UpdatePositioning();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void SetCompactOverlay(bool shouldEnterCompact)
        {
            object currentWindow = WindowManagementHelpers.GetWindowFromUIContext(this.UIContext);
            if (currentWindow is AppWindow appWindow)
            {
                if (!shouldEnterCompact)
                {
                    IsCompactOverlay = !appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.Default);
                }
                else
                {
                    IsCompactOverlay = appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);
                }
            }
            else
            {
                var view = ApplicationView.GetForCurrentView();
                if (!shouldEnterCompact)
                {
                    IsCompactOverlay = !await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                }
                else
                {
                    IsCompactOverlay = await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                    view.TryResizeView(new Size(400, 350));
                }
            }
        }

        private async void ToggleCompactOverlay()
        {
            object currentWindow = WindowManagementHelpers.GetWindowFromUIContext(this.UIContext);
            if (currentWindow is AppWindow appWindow)
            {
                var presentationKind = appWindow.Presenter.GetConfiguration().Kind;
                if (presentationKind == AppWindowPresentationKind.CompactOverlay)
                {
                    IsCompactOverlay = !appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.Default);
                }
                else
                {
                    IsCompactOverlay = appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);
                }
            }
            else
            {
                var view = ApplicationView.GetForCurrentView();
                if (view.ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    IsCompactOverlay = !await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                }
                else
                {
                    IsCompactOverlay = await view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                    view.TryResizeView(new Size(400, 350));
                }
            }
        }

        private bool isCompactOverlay;

        public bool IsCompactOverlay
        {
            get => isCompactOverlay;
            set
            {
                if (value != isCompactOverlay)
                {
                    isCompactOverlay = value;
                    OnPropertyChanged(nameof(IsCompactOverlay));
                }
            }
        }

        private void RootGrid_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // prevents the arrow key events from navigating the list instead of switching compact overlay
            if (EnterCompactOverlayKeyboardAccelerator.CheckIsPressed() || ExitCompactOverlayKeyboardAccelerator.CheckIsPressed())
            {
                Focus(FocusState.Keyboard);
            }
        }

        private void NavToolbar_Loaded(object sender, RoutedEventArgs e) => UpdateNavToolbarProperties();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Initialize the static theme helper
            //to handle theme changes without restarting the app
            ThemeHelper.Initialize();
            double width = 0;

            if (WindowManagementHelpers.GetWindowFromUIContext(this.XamlRoot.UIContext) is Window window)
            {
                Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
                CoreApplication.MainView.TitleBar.ExtendViewIntoTitleBar = true;
                width = Window.Current.CoreWindow.Bounds.Width;
                CoreApplication.MainView.TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            }
            else if (WindowManagementHelpers.GetWindowFromUIContext(this.XamlRoot.UIContext) is AppWindow appWindow)
            {
                var micaIsSupported = ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush");
                if (micaIsSupported)
                {
                    var micaBrush = new Brushes.MicaBrush(false);
                    micaBrush.SetAppWindow(appWindow);
                    Frame.Background = micaBrush;
                }
                else
                {
                    Microsoft.UI.Xaml.Controls.BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
                }

                width = WindowManagementHelpers.GetWindowContentFromAppWindow(appWindow).XamlRoot.Size.Width;
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                RightPaddingColumn.Width = (CoreApplication.MainView?.TitleBar is CoreApplicationViewTitleBar titlebar) ? new GridLength(titlebar.SystemOverlayRightInset) : new GridLength(300);
            }

            // Defers the status bar loading until after the page has loaded to improve startup perf
            FindName(nameof(StatusBarControl));
            FindName(nameof(InnerNavigationToolbar));
            FindName(nameof(horizontalMultitaskingControl));
            FindName(nameof(NavToolbar));

            // the adaptive triggers do not evaluate on app startup, manually checking and calling GoToState here fixes https://github.com/files-community/Files/issues/5801
            if (width > 0)
            {
                if (width < CollapseSearchBoxAdaptiveTrigger.MinWindowWidth)
                {
                    _ = VisualStateManager.GoToState(this, nameof(CollapseSearchBoxState), true);
                }

                if (width < MinimalSidebarAdaptiveTrigger.MinWindowWidth)
                {
                    _ = VisualStateManager.GoToState(this, nameof(MinimalSidebarState), true);
                }

                if (width < CollapseHorizontalTabViewTrigger.MinWindowWidth)
                {
                    _ = VisualStateManager.GoToState(this, nameof(HorizontalTabViewCollapsed), true);
                }
            }
            RootGrid.SizeChanged -= RootGrid_SizeChanged;
            RootGrid.SizeChanged += RootGrid_SizeChanged;
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LoadPaneChanged();
        }
    }
}
