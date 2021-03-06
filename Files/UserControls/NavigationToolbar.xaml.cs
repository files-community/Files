using Files.DataModels;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using static Files.UserControls.INavigationToolbar;

namespace Files.UserControls
{
    public sealed partial class NavigationToolbar : UserControl, INavigationToolbar, INotifyPropertyChanged
    {
        public delegate void ToolbarPathItemInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public delegate void ToolbarFlyoutOpenedEventHandler(object sender, ToolbarFlyoutOpenedEventArgs e);

        public delegate void ToolbarPathItemLoadedEventHandler(object sender, ToolbarPathItemLoadedEventArgs e);

        public delegate void AddressBarTextEnteredEventHandler(object sender, AddressBarTextEnteredEventArgs e);

        public delegate void PathBoxItemDroppedEventHandler(object sender, PathBoxItemDroppedEventArgs e);

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> SearchQuerySubmitted;

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> SearchTextChanged;

        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SearchSuggestionChosen;

        public event ToolbarPathItemInvokedEventHandler ToolbarPathItemInvoked;

        public event ToolbarFlyoutOpenedEventHandler ToolbarFlyoutOpened;

        public event ToolbarPathItemLoadedEventHandler ToolbarPathItemLoaded;

        public event PropertyChangedEventHandler PropertyChanged;

        public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;

        public event EventHandler EditModeEnabled;

        public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

        public event AddressBarTextEnteredEventHandler AddressBarTextEntered;

        public event PathBoxItemDroppedEventHandler PathBoxItemDropped;

        public event EventHandler BackRequested;

        public event EventHandler ForwardRequested;

        public event EventHandler UpRequested;

        public event EventHandler RefreshRequested;

        public static readonly DependencyProperty IsPageTypeNotHomeProperty = DependencyProperty.Register(
          "IsPageTypeNotHome",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool IsPageTypeNotHome
        {
            get
            {
                return (bool)GetValue(IsPageTypeNotHomeProperty);
            }
            set
            {
                SetValue(IsPageTypeNotHomeProperty, value);
            }
        }

        public static readonly DependencyProperty IsCreateButtonEnabledInPageProperty = DependencyProperty.Register(
          "IsCreateButtonEnabledInPage",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool IsCreateButtonEnabledInPage
        {
            get
            {
                return (bool)GetValue(IsCreateButtonEnabledInPageProperty);
            }
            set
            {
                SetValue(IsCreateButtonEnabledInPageProperty, value);
            }
        }

        public static readonly DependencyProperty CanCreateFileInPageProperty = DependencyProperty.Register(
          "CanCreateFileInPage",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool CanCreateFileInPage
        {
            get
            {
                return (bool)GetValue(CanCreateFileInPageProperty);
            }
            set
            {
                SetValue(CanCreateFileInPageProperty, value);
            }
        }

        public static readonly DependencyProperty CanCopyPathInPageProperty = DependencyProperty.Register(
          "CanCopyPathInPage",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool CanCopyPathInPage
        {
            get
            {
                return (bool)GetValue(CanCopyPathInPageProperty);
            }
            set
            {
                SetValue(CanCopyPathInPageProperty, value);
            }
        }

        public static readonly DependencyProperty CanPasteInPageProperty = DependencyProperty.Register(
          "CanPasteInPage",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool CanPasteInPage
        {
            get
            {
                return (bool)GetValue(CanPasteInPageProperty);
            }
            set
            {
                SetValue(CanPasteInPageProperty, value);
            }
        }

        public static readonly DependencyProperty CanOpenTerminalInPageProperty = DependencyProperty.Register(
          "CanOpenTerminalInPage",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool CanOpenTerminalInPage
        {
            get
            {
                return (bool)GetValue(CanOpenTerminalInPageProperty);
            }
            set
            {
                SetValue(CanOpenTerminalInPageProperty, value);
            }
        }

        public static readonly DependencyProperty NewFileInvokedCommandProperty = DependencyProperty.Register(
          "NewFileInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand NewFileInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(NewFileInvokedCommandProperty);
            }
            set
            {
                SetValue(NewFileInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty NewFolderInvokedCommandProperty = DependencyProperty.Register(
          "NewFolderInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand NewFolderInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(NewFolderInvokedCommandProperty);
            }
            set
            {
                SetValue(NewFolderInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty CopyPathInvokedCommandProperty = DependencyProperty.Register(
          "CopyPathInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand CopyPathInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(CopyPathInvokedCommandProperty);
            }
            set
            {
                SetValue(CopyPathInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty NewPaneInvokedCommandProperty = DependencyProperty.Register(
          "NewPaneInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand NewPaneInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(NewPaneInvokedCommandProperty);
            }
            set
            {
                SetValue(NewPaneInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty NewTabInvokedCommandProperty = DependencyProperty.Register(
          "NewTabInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand NewTabInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(NewTabInvokedCommandProperty);
            }
            set
            {
                SetValue(NewTabInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty NewWindowInvokedCommandProperty = DependencyProperty.Register(
          "NewWindowInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand NewWindowInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(NewWindowInvokedCommandProperty);
            }
            set
            {
                SetValue(NewWindowInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty PasteInvokedCommandProperty = DependencyProperty.Register(
          "PasteInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand PasteInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(PasteInvokedCommandProperty);
            }
            set
            {
                SetValue(PasteInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty OpenInTerminalInvokedCommandProperty = DependencyProperty.Register(
          "OpenInTerminalInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand OpenInTerminalInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(OpenInTerminalInvokedCommandProperty);
            }
            set
            {
                SetValue(OpenInTerminalInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty PreviewPaneEnabledProperty = DependencyProperty.Register(
            "PreviewPaneEnabled",
            typeof(bool),
            typeof(NavigationToolbar),
            new PropertyMetadata(null)
        );

        public bool PreviewPaneEnabled
        {
            get => (bool)GetValue(PreviewPaneEnabledProperty);
            set => SetValue(PreviewPaneEnabledProperty, value);
        }

        public SettingsViewModel AppSettings => App.AppSettings;

        private List<ShellNewEntry> cachedNewContextMenuEntries { get; set; }

        public NavigationToolbar()
        {
            this.InitializeComponent();
            this.Loading += NavigationToolbar_Loading;
        }

        private async void NavigationToolbar_Loading(FrameworkElement sender, object args)
        {
            cachedNewContextMenuEntries = await RegistryHelper.GetNewContextMenuEntries();
        }

        private bool manualEntryBoxLoaded = false;

        public bool ManualEntryBoxLoaded
        {
            get
            {
                return manualEntryBoxLoaded;
            }
            set
            {
                if (value != manualEntryBoxLoaded)
                {
                    manualEntryBoxLoaded = value;
                    NotifyPropertyChanged(nameof(ManualEntryBoxLoaded));
                }
            }
        }

        private bool clickablePathLoaded = true;

        public bool ClickablePathLoaded
        {
            get
            {
                return clickablePathLoaded;
            }
            set
            {
                if (value != clickablePathLoaded)
                {
                    clickablePathLoaded = value;
                    NotifyPropertyChanged(nameof(ClickablePathLoaded));
                }
            }
        }

        private bool showMultiPaneControls;

        public bool ShowMultiPaneControls
        {
            get
            {
                return showMultiPaneControls;
            }
            set
            {
                if (value != showMultiPaneControls)
                {
                    showMultiPaneControls = value;
                    NotifyPropertyChanged(nameof(ShowMultiPaneControls));
                }
            }
        }

        private bool isMultiPaneActive;

        public bool IsMultiPaneActive
        {
            get
            {
                return isMultiPaneActive;
            }
            set
            {
                if (value != isMultiPaneActive)
                {
                    isMultiPaneActive = value;
                    NotifyPropertyChanged(nameof(IsMultiPaneActive));
                    NotifyPropertyChanged(nameof(IsPageSecondaryPane));
                }
            }
        }

        public bool IsPageSecondaryPane => !IsMultiPaneActive || !IsPageMainPane;

        private bool isPageMainPane;

        public bool IsPageMainPane
        {
            get
            {
                return isPageMainPane;
            }
            set
            {
                if (value != isPageMainPane)
                {
                    isPageMainPane = value;
                    NotifyPropertyChanged(nameof(IsPageMainPane));
                    NotifyPropertyChanged(nameof(IsPageSecondaryPane));
                }
            }
        }

        private bool areKeyboardAcceleratorsEnabled;

        public bool AreKeyboardAcceleratorsEnabled
        {
            get
            {
                return areKeyboardAcceleratorsEnabled;
            }
            set
            {
                if (value != areKeyboardAcceleratorsEnabled)
                {
                    areKeyboardAcceleratorsEnabled = value;
                    NotifyPropertyChanged(nameof(AreKeyboardAcceleratorsEnabled));
                }
            }
        }

        public string PathText { get; set; }

        private bool isSearchRegionVisible;

        public bool IsSearchRegionVisible
        {
            get
            {
                return isSearchRegionVisible;
            }
            set
            {
                if (value != isSearchRegionVisible)
                {
                    isSearchRegionVisible = value;
                    NotifyPropertyChanged(nameof(IsSearchRegionVisible));
                }
            }
        }

        bool INavigationToolbar.IsEditModeEnabled
        {
            get
            {
                return ManualEntryBoxLoaded;
            }
            set
            {
                if (value)
                {
                    EditModeEnabled?.Invoke(this, new EventArgs());
                    VisiblePath.Focus(FocusState.Programmatic);
                    Interaction.FindChild<TextBox>(VisiblePath)?.SelectAll();
                }
                else
                {
                    ManualEntryBoxLoaded = false;
                    ClickablePathLoaded = true;
                }
            }
        }

        public ObservableCollection<ListedItem> NavigationBarSuggestions = new ObservableCollection<ListedItem>();

        private void VisiblePath_Loaded(object sender, RoutedEventArgs e)
        {
            // AutoSuggestBox won't receive focus unless it's fully loaded
            VisiblePath.Focus(FocusState.Programmatic);
            Interaction.FindChild<TextBox>(VisiblePath)?.SelectAll();
        }

        public bool CanRefresh
        {
            get
            {
                return Refresh.IsEnabled;
            }
            set
            {
                Refresh.IsEnabled = value;
            }
        }

        public bool CanNavigateToParent
        {
            get
            {
                return Up.IsEnabled;
            }
            set
            {
                Up.IsEnabled = value;
            }
        }

        public bool CanGoBack
        {
            get
            {
                return Back.IsEnabled;
            }
            set
            {
                Back.IsEnabled = value;
            }
        }

        public bool CanGoForward
        {
            get
            {
                return Forward.IsEnabled;
            }
            set
            {
                Forward.IsEnabled = value;
            }
        }

        string INavigationToolbar.PathControlDisplayText
        {
            get
            {
                return PathText;
            }
            set
            {
                PathText = value;
                NotifyPropertyChanged(nameof(PathText));
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<PathBoxItem> PathComponents { get; } = new ObservableCollection<PathBoxItem>();
        public UserControl MultitaskingControl => VerticalTabs;

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            (this as INavigationToolbar).IsEditModeEnabled = true;
        }

        private void VisiblePath_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                (this as INavigationToolbar).IsEditModeEnabled = false;
            }
        }

        private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() is FlyoutBase ||
                FocusManager.GetFocusedElement() is AppBarButton ||
                FocusManager.GetFocusedElement() is Popup)
            {
                return;
            }

            var element = FocusManager.GetFocusedElement();
            var elementAsControl = element as Control;

            if (elementAsControl.FocusState != FocusState.Programmatic && elementAsControl.FocusState != FocusState.Keyboard)
            {
                (this as INavigationToolbar).IsEditModeEnabled = false;
            }
            else
            {
                if ((this as INavigationToolbar).IsEditModeEnabled)
                {
                    this.VisiblePath.Focus(FocusState.Programmatic);
                }
            }
        }

        private async void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                cancelFlyoutOpen = false;
                await Task.Delay(1000);
                if (!cancelFlyoutOpen)
                {
                    if (sender != null)
                    {
                        (sender as Button).Flyout.ShowAt(sender as Button);
                    }
                    cancelFlyoutOpen = false;
                }
                else
                {
                    cancelFlyoutOpen = false;
                }
            }
        }

        private bool cancelFlyoutOpen = false;

        private void VerticalTabStripInvokeButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                if (!(sender as Button).Flyout.IsOpen)
                {
                    cancelFlyoutOpen = true;
                }
            }
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private void Flyout_Opened(object sender, object e)
        {
            if (VerticalTabStripInvokeButton != null)
            {
                VisualStateManager.GoToState(VerticalTabStripInvokeButton, "PointerOver", false);
            }
        }

        private void Flyout_Closed(object sender, object e)
        {
            if (VerticalTabStripInvokeButton != null)
            {
                VisualStateManager.GoToState(VerticalTabStripInvokeButton, "Normal", false);
            }
        }

        private void VerticalTabStripInvokeButton_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            (sender as Button).Flyout.ShowAt(sender as Button);
        }

        private bool cancelFlyoutAutoClose = false;

        private async void VerticalTabs_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                cancelFlyoutAutoClose = false;
                VerticalTabs.PointerEntered += VerticalTabs_PointerEntered;
                await Task.Delay(1000);
                if (VerticalTabs != null)
                {
                    VerticalTabs.PointerEntered -= VerticalTabs_PointerEntered;
                }
                if (!cancelFlyoutAutoClose)
                {
                    VerticalTabViewFlyout?.Hide();
                }
                cancelFlyoutAutoClose = false;
            }
        }

        private void VerticalTabs_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                e.Handled = true;
                cancelFlyoutAutoClose = true;
            }
        }

        private string dragOverPath = null;
        private DispatcherTimer dragOverTimer = new DispatcherTimer();

        private void PathBoxItem_DragLeave(object sender, DragEventArgs e)
        {
            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == "NewTab".GetLocalized())
            {
                return;
            }

            if (pathBoxItem.Path == dragOverPath)
            {
                // Reset dragged over pathbox item
                dragOverPath = null;
            }
        }

        private async void PathBoxItem_DragOver(object sender, DragEventArgs e)
        {
            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == "NewTab".GetLocalized())
            {
                return;
            }

            if (dragOverPath != pathBoxItem.Path)
            {
                dragOverPath = pathBoxItem.Path;
                dragOverTimer.Stop();
                if (dragOverPath != (this as INavigationToolbar).PathComponents.LastOrDefault()?.Path)
                {
                    dragOverTimer.Debounce(() =>
                    {
                        if (dragOverPath != null)
                        {
                            dragOverTimer.Stop();
                            ItemDraggedOverPathItem?.Invoke(this, new PathNavigationEventArgs()
                            {
                                ItemPath = dragOverPath
                            });
                            dragOverPath = null;
                        }
                    }, TimeSpan.FromMilliseconds(1000), false);
                }
            }

            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                return;
            }

            e.Handled = true;
            var deferral = e.GetDeferral();

            IReadOnlyList<IStorageItem> storageItems;
            try
            {
                storageItems = await e.DataView.GetStorageItemsAsync();
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

            if (!storageItems.Any(storageItem =>
            storageItem.Path.Replace(pathBoxItem.Path, string.Empty).
            Trim(Path.DirectorySeparatorChar).
            Contains(Path.DirectorySeparatorChar)))
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), pathBoxItem.Title);
                e.AcceptedOperation = DataPackageOperation.Move;
            }

            deferral.Complete();
        }

        private void PathBoxItem_Drop(object sender, DragEventArgs e)
        {
            dragOverPath = null; // Reset dragged over pathbox item

            if (!((sender as Grid).DataContext is PathBoxItem pathBoxItem) ||
                pathBoxItem.Path == "Home" || pathBoxItem.Path == "NewTab".GetLocalized())
            {
                return;
            }

            var deferral = e.GetDeferral();
            PathBoxItemDropped?.Invoke(this, new PathBoxItemDroppedEventArgs()
            {
                AcceptedOperation = e.AcceptedOperation,
                Package = e.DataView,
                Path = pathBoxItem.Path
            });
            deferral.Complete();
        }

        private void VisiblePath_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = sender });
            }
        }

        private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            PathBoxQuerySubmitted?.Invoke(this, new ToolbarQuerySubmittedEventArgs() { QueryText = args.QueryText });

            (this as INavigationToolbar).IsEditModeEnabled = false;
        }

        private void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var itemTappedPath = ((sender as TextBlock).DataContext as PathBoxItem).Path;
            ToolbarPathItemInvoked?.Invoke(this, new PathNavigationEventArgs()
            {
                ItemPath = itemTappedPath
            });
        }

        private void PathItemSeparator_Loaded(object sender, RoutedEventArgs e)
        {
            var pathSeparatorIcon = sender as FontIcon;
            pathSeparatorIcon.Tapped += (s, e) => pathSeparatorIcon.ContextFlyout.ShowAt(pathSeparatorIcon);
            pathSeparatorIcon.ContextFlyout.Opened += (s, e) => { pathSeparatorIcon.Glyph = "\uE9A5"; };
            pathSeparatorIcon.ContextFlyout.Closed += (s, e) => { pathSeparatorIcon.Glyph = "\uE9A8"; };
        }

        private void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var pathSeparatorIcon = sender as FontIcon;
            if (pathSeparatorIcon.DataContext == null)
            {
                return;
            }
            ToolbarPathItemLoaded?.Invoke(pathSeparatorIcon, new ToolbarPathItemLoadedEventArgs()
            {
                Item = pathSeparatorIcon.DataContext as PathBoxItem,
                OpenedFlyout = pathSeparatorIcon.ContextFlyout as MenuFlyout
            });
        }

        private void PathboxItemFlyout_Opened(object sender, object e)
        {
            ToolbarFlyoutOpened?.Invoke(this, new ToolbarFlyoutOpenedEventArgs() { OpenedFlyout = sender as MenuFlyout });
        }

        private void VerticalTabStripInvokeButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(MainPage.MultitaskingControl is VerticalTabViewControl))
            {
                MainPage.MultitaskingControl = VerticalTabs;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            ForwardRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            UpRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            SearchQuerySubmitted?.Invoke(sender, args);
        }

        private void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            SearchTextChanged?.Invoke(sender, args);
        }

        private void SearchRegion_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            SearchSuggestionChosen?.Invoke(sender, args);
            IsSearchRegionVisible = false;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            IsSearchRegionVisible = true;

            // Given that binding and layouting might take a few cycles, when calling UpdateLayout
            // we can guarantee that the focus call will be able to find an open ASB
            SearchRegion.UpdateLayout();

            SearchRegion.Focus(FocusState.Programmatic);
        }

        private void SearchRegion_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() is FlyoutBase ||
                FocusManager.GetFocusedElement() is AppBarButton ||
                FocusManager.GetFocusedElement() is Popup)
            {
                return;
            }

            SearchRegion.Text = "";
            IsSearchRegionVisible = false;
        }

        public void ClearSearchBoxQueryText(bool collapseSearchRegion = false)
        {
            SearchRegion.Text = "";
            if (IsSearchRegionVisible && collapseSearchRegion)
            {
                IsSearchRegionVisible = false;
            }
        }

        private void NavMoreButtonFlyout_Opening(object sender, object e)
        {
            var newItemMenu = (MenuFlyoutSubItem)(sender as MenuFlyout).Items.SingleOrDefault(x => x.Name == "NewEmptySpace");
            if (newItemMenu == null || cachedNewContextMenuEntries == null)
            {
                return;
            }
            if (!newItemMenu.Items.Any(x => (x.Tag as string) == "CreateNewFile"))
            {
                var separatorIndex = newItemMenu.Items.IndexOf(newItemMenu.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));
                foreach (var newEntry in Enumerable.Reverse(cachedNewContextMenuEntries))
                {
                    MenuFlyoutItem menuLayoutItem;
                    if (newEntry.Icon != null)
                    {
                        BitmapImage image = null;
                        image = new BitmapImage();
#pragma warning disable CS4014
                        image.SetSourceAsync(newEntry.Icon);
#pragma warning restore CS4014
                        menuLayoutItem = new MenuFlyoutItemWithImage()
                        {
                            Text = newEntry.Name,
                            BitmapIcon = image,
                            Tag = "CreateNewFile"
                        };
                    }
                    else
                    {
                        menuLayoutItem = new MenuFlyoutItem()
                        {
                            Text = newEntry.Name,
                            Icon = new FontIcon()
                            {
                                FontFamily = App.Current.Resources["FluentUIGlyphs"] as Windows.UI.Xaml.Media.FontFamily,
                                Glyph = "\xea00"
                            },
                            Tag = "CreateNewFile"
                        };
                    }
                    menuLayoutItem.Command = NewFileInvokedCommand;
                    menuLayoutItem.CommandParameter = newEntry;
                    newItemMenu.Items.Insert(separatorIndex + 1, menuLayoutItem);
                }
            }
        }

        private void PreviewPane_Click(object sender, RoutedEventArgs e)
        {
            PreviewPaneEnabled = !PreviewPaneEnabled;
        }
    }
}