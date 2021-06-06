using Files.DataModels;
using Files.Filesystem;
using Files.Helpers;
using Files.Helpers.XamlHelpers;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
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
    public sealed partial class NavigationToolbar : UserControl
    {
        // TODO: Remove this MainPage reference when we work on new Vertical Tabs control in MainPage
        private MainPage mainPage => ((Window.Current.Content as Frame).Content as MainPage);

        public NavToolbarViewModel ViewModel
        {
            get => (NavToolbarViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(NavToolbarViewModel), typeof(NavigationToolbar), new PropertyMetadata(null));

        public ISearchBox SearchBox => SearchRegion;


        #region Selection Options

        public static readonly DependencyProperty MultiselectEnabledProperty = DependencyProperty.Register(
          "MultiselectEnabled",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool MultiselectEnabled
        {
            get
            {
                return (bool)GetValue(MultiselectEnabledProperty);
            }
            set
            {
                SetValue(MultiselectEnabledProperty, value);
            }
        }

        public static readonly DependencyProperty SelectAllInvokedCommandProperty = DependencyProperty.Register(
          "SelectAllInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand SelectAllInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(SelectAllInvokedCommandProperty);
            }
            set
            {
                SetValue(SelectAllInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty InvertSelectionInvokedCommandProperty = DependencyProperty.Register(
          "InvertSelectionInvokedCommand",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand InvertSelectionInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(InvertSelectionInvokedCommandProperty);
            }
            set
            {
                SetValue(InvertSelectionInvokedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty ClearSelectionInvokedCommandProperty = DependencyProperty.Register(
         "ClearSelectionInvokedCommand",
         typeof(ICommand),
         typeof(NavigationToolbar),
         new PropertyMetadata(null)
       );

        public ICommand ClearSelectionInvokedCommand
        {
            get
            {
                return (ICommand)GetValue(ClearSelectionInvokedCommandProperty);
            }
            set
            {
                SetValue(ClearSelectionInvokedCommandProperty, value);
            }
        }

        #endregion Selection Options

        public static readonly DependencyProperty ArrangementOptionsFlyoutContentProperty = DependencyProperty.Register(
         nameof(ArrangementOptionsFlyoutContent),
         typeof(UIElement),
         typeof(NavigationToolbar),
         new PropertyMetadata(null)
        );

        public UIElement ArrangementOptionsFlyoutContent
        {
            get => (UIElement)GetValue(ArrangementOptionsFlyoutContentProperty);
            set => SetValue(ArrangementOptionsFlyoutContentProperty, value);
        }

        #region Layout Options

        public static readonly DependencyProperty LayoutModeInformationProperty = DependencyProperty.Register(
          "LayoutModeInformation",
          typeof(FolderLayoutInformation),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public FolderLayoutInformation LayoutModeInformation
        {
            get
            {
                return (FolderLayoutInformation)GetValue(LayoutModeInformationProperty);
            }
            set
            {
                SetValue(LayoutModeInformationProperty, value);
            }
        }

        public static readonly DependencyProperty ToggleLayoutModeDetailsViewProperty = DependencyProperty.Register(
          "ToggleLayoutModeDetailsView",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand ToggleLayoutModeDetailsView
        {
            get
            {
                return (ICommand)GetValue(ToggleLayoutModeDetailsViewProperty);
            }
            set
            {
                SetValue(ToggleLayoutModeDetailsViewProperty, value);
            }
        }

        public static readonly DependencyProperty ToggleLayoutModeTilesProperty = DependencyProperty.Register(
          "ToggleLayoutModeTiles",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand ToggleLayoutModeTiles
        {
            get
            {
                return (ICommand)GetValue(ToggleLayoutModeTilesProperty);
            }
            set
            {
                SetValue(ToggleLayoutModeTilesProperty, value);
            }
        }

        public static readonly DependencyProperty ToggleLayoutModeGridViewSmallProperty = DependencyProperty.Register(
          "ToggleLayoutModeGridViewSmall",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand ToggleLayoutModeGridViewSmall
        {
            get
            {
                return (ICommand)GetValue(ToggleLayoutModeGridViewSmallProperty);
            }
            set
            {
                SetValue(ToggleLayoutModeGridViewSmallProperty, value);
            }
        }

        public static readonly DependencyProperty ToggleLayoutModeGridViewMediumProperty = DependencyProperty.Register(
          "ToggleLayoutModeGridViewMedium",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand ToggleLayoutModeGridViewMedium
        {
            get
            {
                return (ICommand)GetValue(ToggleLayoutModeGridViewMediumProperty);
            }
            set
            {
                SetValue(ToggleLayoutModeGridViewMediumProperty, value);
            }
        }

        public static readonly DependencyProperty ToggleLayoutModeGridViewLargeProperty = DependencyProperty.Register(
          "ToggleLayoutModeGridViewLarge",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand ToggleLayoutModeGridViewLarge
        {
            get
            {
                return (ICommand)GetValue(ToggleLayoutModeGridViewLargeProperty);
            }
            set
            {
                SetValue(ToggleLayoutModeGridViewLargeProperty, value);
            }
        }

        public static readonly DependencyProperty ToggleLayoutModeColumnViewProperty = DependencyProperty.Register(
          "ToggleLayoutModeColumnView",
          typeof(ICommand),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public ICommand ToggleLayoutModeColumnView
        {
            get
            {
                return (ICommand)GetValue(ToggleLayoutModeColumnViewProperty);
            }
            set
            {
                SetValue(ToggleLayoutModeColumnViewProperty, value);
            }
        }

        #endregion Layout Options

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

        //public static readonly DependencyProperty CanCopyPathInPageProperty = DependencyProperty.Register(
        //  "CanCopyPathInPage",
        //  typeof(bool),
        //  typeof(NavigationToolbar),
        //  new PropertyMetadata(null)
        //);

        //public bool CanCopyPathInPage
        //{
        //    get
        //    {
        //        return (bool)GetValue(CanCopyPathInPageProperty);
        //    }
        //    set
        //    {
        //        SetValue(CanCopyPathInPageProperty, value);
        //    }
        //}

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
            InitializeComponent();
            Loading += NavigationToolbar_Loading;
        }

        private async void NavigationToolbar_Loading(FrameworkElement sender, object args)
        {
            cachedNewContextMenuEntries = await RegistryHelper.GetNewContextMenuEntries();
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

        private void VisiblePath_Loaded(object sender, RoutedEventArgs e)
        {
            // AutoSuggestBox won't receive focus unless it's fully loaded
            VisiblePath.Focus(FocusState.Programmatic);
            DependencyObjectHelpers.FindChild<TextBox>(VisiblePath)?.SelectAll();
        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public UserControl MultitaskingControl => VerticalTabs;

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsEditModeEnabled = true;
        }

        private void VisiblePath_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                ViewModel.IsEditModeEnabled = false;
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
            if (elementAsControl == null)
            {
                return;
            }
            else if (elementAsControl.FocusState != FocusState.Programmatic && elementAsControl.FocusState != FocusState.Keyboard)
            {
                ViewModel.IsEditModeEnabled = false;
            }
            else
            {
                if (ViewModel.IsEditModeEnabled)
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

        private void PathItemSeparator_Loaded(object sender, RoutedEventArgs e)
        {
            var pathSeparatorIcon = sender as FontIcon;
            pathSeparatorIcon.Tapped += (s, e) => pathSeparatorIcon.ContextFlyout.ShowAt(pathSeparatorIcon);
            pathSeparatorIcon.ContextFlyout.Opened += (s, e) => { pathSeparatorIcon.Glyph = "\uE70D"; };
            pathSeparatorIcon.ContextFlyout.Closed += (s, e) => { pathSeparatorIcon.Glyph = "\uE76C"; };
        }

        private void VerticalTabStripInvokeButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(mainPage.ViewModel.MultitaskingControl is VerticalTabViewControl))
            {
                // Set multitasking control if changed and subscribe it to event for sidebar items updating
                if (mainPage.ViewModel.MultitaskingControl != null)
                {
                    mainPage.ViewModel.MultitaskingControl.CurrentInstanceChanged -= mainPage.MultitaskingControl_CurrentInstanceChanged;
                }
                mainPage.ViewModel.MultitaskingControl = VerticalTabs;
                mainPage.ViewModel.MultitaskingControl.CurrentInstanceChanged += mainPage.MultitaskingControl_CurrentInstanceChanged;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchSearchBoxVisibility();

        private void SearchBox_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ViewModel.CloseSearchBox();

        private void SearchRegion_LostFocus(object sender, RoutedEventArgs e)
        {
            var focusedElement = FocusManager.GetFocusedElement();
            if (focusedElement == SearchButton || focusedElement is FlyoutBase || focusedElement is AppBarButton)
            {
                return;
            }

            ViewModel.CloseSearchBox();
        }

        private void NavMoreButtonFlyout_Opening(object sender, object e)
        {
            if (cachedNewContextMenuEntries == null)
            {
                return;
            }
            if (!NewEmptySpace.Items.Any(x => (x.Tag as string) == "CreateNewFile"))
            {
                var separatorIndex = NewEmptySpace.Items.IndexOf(NewEmptySpace.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));
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
                                Glyph = "\xE7C3"
                            },
                            Tag = "CreateNewFile"
                        };
                    }
                    menuLayoutItem.Command = NewFileInvokedCommand;
                    menuLayoutItem.CommandParameter = newEntry;
                    NewEmptySpace.Items.Insert(separatorIndex + 1, menuLayoutItem);
                }
            }
        }

        private void PreviewPane_Click(object sender, RoutedEventArgs e) => PreviewPaneEnabled = !PreviewPaneEnabled;

        private void SearchRegion_SuggestionChosen(ISearchBox sender, SearchBoxSuggestionChosenEventArgs args) => ViewModel.IsSearchBoxVisible = false;

        private void SearchRegion_Escaped(object sender, ISearchBox searchBox) => ViewModel.IsSearchBoxVisible = false;

        private void PathboxItemFlyout_Opened(object sender, object e) => ViewModel.PathboxItemFlyout_Opened(sender, e);

        private void PathItemSeparator_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) => ViewModel.PathItemSeparator_DataContextChanged(sender, args);

        private void PathBoxItem_DragLeave(object sender, DragEventArgs e) => ViewModel.PathBoxItem_DragLeave(sender, e);

        private void PathBoxItem_DragOver(object sender, DragEventArgs e) => ViewModel.PathBoxItem_DragOver(sender, e);

        private void PathBoxItem_Drop(object sender, DragEventArgs e) => ViewModel.PathBoxItem_Drop(sender, e);
        private void PathBoxItem_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.PathBoxItem_Tapped(sender, e);
        private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ViewModel.VisiblePath_QuerySubmitted(sender, args);
    }
}