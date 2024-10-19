// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Files.App.UserControls.Sidebar;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.Services.Store;
using WinRT.Interop;
using VirtualKey = Windows.System.VirtualKey;

namespace Files.App.Views
{
    public sealed partial class MainPage : Page
    {
        private IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
        public IUserSettingsService UserSettingsService { get; }
        private readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();
        public ICommandManager Commands { get; }
        public SidebarViewModel SidebarAdaptiveViewModel { get; }
        public MainPageViewModel ViewModel { get; }
        public StatusCenterViewModel OngoingTasksViewModel { get; }

        private bool keyReleased = true;
        private DispatcherQueueTimer _updateDateDisplayTimer;

        public MainPage()
        {
            InitializeComponent();

            UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
            Commands = Ioc.Default.GetRequiredService<ICommandManager>();
            SidebarAdaptiveViewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();
            SidebarAdaptiveViewModel.PaneFlyout = (MenuFlyout)Resources["SidebarContextMenu"];
            ViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
            OngoingTasksViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

            if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
                FlowDirection = FlowDirection.RightToLeft;

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

            _updateDateDisplayTimer = DispatcherQueue.CreateTimer();
            _updateDateDisplayTimer.Interval = TimeSpan.FromSeconds(1);
            _updateDateDisplayTimer.Tick += UpdateDateDisplayTimer_Tick;
        }

        private void LoadPaneChanged()
        {
            try
            {
                if (SidebarAdaptiveViewModel.PaneHolder != null)
                {
                    bool isHomePage = !(SidebarAdaptiveViewModel.PaneHolder.ActivePane.InstanceViewModel is not IShellPanesPage);
                    bool isMultiPane = SidebarAdaptiveViewModel.PaneHolder.IsMultiPaneActive;
                    bool isBigEnough = !App.AppModel.IsMainWindowClosed &&
                        (MainWindow.Instance.Bounds.Width > 450 && MainWindow.Instance.Bounds.Height > 450 ||
                         RootGrid.ActualWidth > 700 && MainWindow.Instance.Bounds.Height > 360);

                    ViewModel.ShouldPreviewPaneBeDisplayed = (!isHomePage || isMultiPane) && isBigEnough;
                    ViewModel.ShouldPreviewPaneBeActive = UserSettingsService.InfoPaneSettingsService.IsInfoPaneEnabled && ViewModel.ShouldPreviewPaneBeDisplayed;

                    UpdateStatusBarProperties();
                    UpdateNavToolbarProperties();
                    UpdatePositioning();
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogWarning(ex, "Error while loading pane changes: {Message}", ex.Message);
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.ShouldPreviewPaneBeActive):
                    if (ViewModel.ShouldPreviewPaneBeActive)
                    {
                        var infoPaneViewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
                        infoPaneViewModel.UpdateSelectedItemPreviewAsync();
                    }
                    break;

                case nameof(ViewModel.ShouldViewControlBeDisplayed):
                    ViewControl.Visibility = ViewModel.ShouldViewControlBeDisplayed ? Visibility.Visible : Visibility.Collapsed;
                    break;

                case nameof(ViewModel.MultitaskingControl):
                    UpdateMultitaskingControl(ViewModel.MultitaskingControl);
                    break;

                case nameof(ViewModel.SelectedTabIndex):
                    UpdateTabSelection(ViewModel.SelectedTabIndex);
                    break;
            }
        }

        private void UpdateMultitaskingControl(object multitaskingControl)
        {
            if (multitaskingControl is TabBar tabBar)
            {
                TabControl.ItemsSource = tabBar.Items;
            }
        }

        private void UpdateTabSelection(int selectedIndex)
        {
            TabControl.SelectedIndex = selectedIndex;
        }

        private async Task PromptForReviewAsync()
        {
            var promptForReviewDialog = new ContentDialog
            {
                Title = "ReviewFiles",
                Content = "ReviewFilesContent",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                promptForReviewDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

            var result = await promptForReviewDialog.TryShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var storeContext = StoreContext.GetDefault();
                    InitializeWithWindow.Initialize(storeContext, MainWindow.Instance.WindowHandle);
                    var storeRateAndReviewResult = await storeContext.RequestRateAndReviewAppAsync();

                    App.Logger.LogInformation($"STORE: review request status: {storeRateAndReviewResult.Status}");

                    UserSettingsService.ApplicationSettingsService.ClickedToReviewApp = true;
                }
                catch (Exception) { }
            }
        }

        private async Task AppRunningAsAdminPromptAsync()
        {
            var runningAsAdminPrompt = new ContentDialog
            {
                Title = "FilesRunningAsAdmin",
                Content = "FilesRunningAsAdminContent",
                PrimaryButtonText = "Ok",
                SecondaryButtonText = "DontShowAgain"
            };

            var result = await SetContentDialogRoot(runningAsAdminPrompt).TryShowAsync();

            if (result == ContentDialogResult.Secondary)
                UserSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt = false;
        }

        private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

            return contentDialog;
        }

        private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
        {
            if (e.SettingName == nameof(IInfoPaneSettingsService.IsInfoPaneEnabled))
            {
                LoadPaneChanged();
            }
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            TabControl.DragArea.SizeChanged += (_, _) => MainWindow.Instance.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);
            if (ViewModel.MultitaskingControl is not TabBar)
            {
                ViewModel.MultitaskingControl = TabControl;
                ViewModel.MultitaskingControls.Add(TabControl);
                ViewModel.MultitaskingControl.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
            }
        }

        private int SetTitleBarDragRegion(InputNonClientPointerSource source, SizeInt32 size, double scaleFactor, Func<UIElement, RectInt32?, RectInt32> getScaledRect)
        {
            var height = (int)TabControl.ActualHeight;
            source.SetRegionRects(NonClientRegionKind.Passthrough, [getScaledRect(this, new RectInt32(0, 0, (int)(TabControl.ActualWidth + TabControl.Margin.Left - TabControl.DragArea.ActualWidth), height))]);
            return height;
        }

        public async void TabItemContent_ContentChanged(object? sender, TabBarItemParameter e)
        {
            if (SidebarAdaptiveViewModel.PaneHolder is null)
                return;

            var paneArgs = e.NavigationParameter as PaneNavigationArguments;
            SidebarAdaptiveViewModel.UpdateSidebarSelectedItemFromArgs(SidebarAdaptiveViewModel.PaneHolder.IsLeftPaneActive ?
                paneArgs?.LeftPaneNavPathParam : paneArgs?.RightPaneNavPathParam);

            UpdateStatusBarProperties();
            LoadPaneChanged();
            UpdateNavToolbarProperties();
            await NavigationHelpers.UpdateInstancePropertiesAsync(paneArgs);

            AppLifecycleHelper.SaveSessionTabs();
            LoadPaneChanged();
        }

        public async void MultitaskingControl_CurrentInstanceChanged(object? sender, CurrentInstanceChangedEventArgs e)
        {
            if (SidebarAdaptiveViewModel.PaneHolder is not null)
                SidebarAdaptiveViewModel.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;

            var navArgs = e.CurrentInstance.TabBarItemParameter?.NavigationParameter;
            if (e.CurrentInstance is IShellPanesPage currentInstance)
            {
                SidebarAdaptiveViewModel.PaneHolder = currentInstance;
                SidebarAdaptiveViewModel.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
            }

            SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged((navArgs as PaneNavigationArguments)?.LeftPaneNavPathParam);

            if (SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.StatusBarViewModel is not null)
                SidebarAdaptiveViewModel.PaneHolder.ActivePaneOrColumn.SlimContentPage.StatusBarViewModel.ShowLocals = true;

            UpdateStatusBarProperties();
            UpdateNavToolbarProperties();
            LoadPaneChanged();

            e.CurrentInstance.ContentChanged -= TabItemContent_ContentChanged;
            e.CurrentInstance.ContentChanged += TabItemContent_ContentChanged;

            await NavigationHelpers.UpdateInstancePropertiesAsync(navArgs);
        }

        private void PaneHolder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged(SidebarAdaptiveViewModel.PaneHolder.ActivePane?.TabBarItemParameter?.NavigationParameter);
            UpdateStatusBarProperties();
            UpdateNavToolbarProperties();
        }

        private void UpdateDateDisplayTimer_Tick(object? sender, object e)
        {
            ViewModel.UpdateDateDisplay();
        }
    }
}
