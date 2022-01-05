using Files.Filesystem;
using Files.Services;
using Files.UserControls.MultitaskingControl;
using Files.Views.LayoutModes;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files.Views
{
    public sealed partial class PaneHolderPage : Page, IPaneHolder, ITabItemContent
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public bool IsLeftPaneActive => ActivePane == PaneLeft;
        public bool IsRightPaneActive => ActivePane == PaneRight;

        public event EventHandler<TabItemArguments> ContentChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public IFilesystemHelpers FilesystemHelpers => ActivePane?.FilesystemHelpers;

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

        private bool _windowIsCompact = Window.Current.Bounds.Width <= 750;

        private bool windowIsCompact
        {
            get
            {
                return _windowIsCompact;
            }
            set
            {
                if (value != _windowIsCompact)
                {
                    _windowIsCompact = value;
                    if (value)
                    {
                        wasRightPaneVisible = isRightPaneVisible;
                        IsRightPaneVisible = false;
                    }
                    else if (wasRightPaneVisible)
                    {
                        IsRightPaneVisible = true;
                        wasRightPaneVisible = false;
                    }
                    NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
                }
            }
        }

        private bool wasRightPaneVisible;

        public bool IsMultiPaneActive => IsRightPaneVisible;

        public bool IsMultiPaneEnabled
        {
            get => UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled && !(Window.Current.Bounds.Width <= 750);
        }

        private NavigationParams navParamsLeft;

        public NavigationParams NavParamsLeft
        {
            get => navParamsLeft;
            set
            {
                if (navParamsLeft != value)
                {
                    navParamsLeft = value;
                    NotifyPropertyChanged(nameof(NavParamsLeft));
                }
            }
        }

        private NavigationParams navParamsRight;

        public NavigationParams NavParamsRight
        {
            get => navParamsRight;
            set
            {
                if (navParamsRight != value)
                {
                    navParamsRight = value;
                    NotifyPropertyChanged(nameof(NavParamsRight));
                }
            }
        }

        private IShellPage activePane;

        public IShellPage ActivePane
        {
            get => activePane;
            set
            {
                if (activePane != value)
                {
                    activePane = value;
                    PaneLeft.IsCurrentInstance = false;
                    if (PaneRight != null)
                    {
                        PaneRight.IsCurrentInstance = false;
                    }
                    if (ActivePane != null)
                    {
                        ActivePane.IsCurrentInstance = isCurrentInstance;
                    }
                    NotifyPropertyChanged(nameof(ActivePane));
                    NotifyPropertyChanged(nameof(IsLeftPaneActive));
                    NotifyPropertyChanged(nameof(IsRightPaneActive));
                    NotifyPropertyChanged(nameof(ActivePaneOrColumn));
                    NotifyPropertyChanged(nameof(FilesystemHelpers));
                }
            }
        }

        public IShellPage ActivePaneOrColumn
        {
            get
            {
                if (ActivePane.IsColumnView)
                {
                    return (ActivePane.SlimContentPage as ColumnViewBrowser).ActiveColumnShellPage;
                }

                return ActivePane;
            }
        }

        private bool isRightPaneVisible;

        public bool IsRightPaneVisible
        {
            get => isRightPaneVisible;
            set
            {
                if (value != isRightPaneVisible)
                {
                    isRightPaneVisible = value;
                    if (!isRightPaneVisible)
                    {
                        ActivePane = PaneLeft;
                    }
                    Pane_ContentChanged(null, null);
                    NotifyPropertyChanged(nameof(IsRightPaneVisible));
                    NotifyPropertyChanged(nameof(IsMultiPaneActive));
                }
            }
        }

        private bool isCurrentInstance;

        public bool IsCurrentInstance
        {
            get => isCurrentInstance;
            set
            {
                isCurrentInstance = value;
                PaneLeft.IsCurrentInstance = false;
                if (PaneRight != null)
                {
                    PaneRight.IsCurrentInstance = false;
                }
                if (ActivePane != null)
                {
                    ActivePane.IsCurrentInstance = value;
                }
            }
        }

        public PaneHolderPage()
        {
            this.InitializeComponent();
            Window.Current.SizeChanged += Current_SizeChanged;
            this.ActivePane = PaneLeft;
            this.IsRightPaneVisible = IsMultiPaneEnabled && UserSettingsService.MultitaskingSettingsService.AlwaysOpenDualPaneInNewTab;
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

            // TODO: fallback / error when failed to get NavigationViewCompactPaneLength value?
        }

        private void UserSettingsService_OnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled):
                    NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
                    break;
            }
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            windowIsCompact = Window.Current.Bounds.Width <= 750;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);

            if (eventArgs.Parameter is string navPath)
            {
                NavParamsLeft = new NavigationParams { NavPath = navPath };
                NavParamsRight = new NavigationParams { NavPath = "Home".GetLocalized() };
            }
            if (eventArgs.Parameter is PaneNavigationArguments paneArgs)
            {
                NavParamsLeft = new NavigationParams
                {
                    NavPath = paneArgs.LeftPaneNavPathParam,
                    SelectItem = paneArgs.LeftPaneSelectItemParam
                };
                NavParamsRight = new NavigationParams
                {
                    NavPath = paneArgs.RightPaneNavPathParam,
                    SelectItem = paneArgs.RightPaneSelectItemParam
                };
                IsRightPaneVisible = IsMultiPaneEnabled && paneArgs.RightPaneNavPathParam != null;
            }

            TabItemArguments = new TabItemArguments()
            {
                InitialPageType = typeof(PaneHolderPage),
                NavigationArg = new PaneNavigationArguments()
                {
                    LeftPaneNavPathParam = NavParamsLeft?.NavPath,
                    LeftPaneSelectItemParam = NavParamsLeft?.SelectItem,
                    RightPaneNavPathParam = IsRightPaneVisible ? NavParamsRight?.NavPath : null,
                    RightPaneSelectItemParam = IsRightPaneVisible ? NavParamsRight?.SelectItem : null,
                }
            };
        }

        private void PaneResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (PaneRight != null && PaneRight.ActualWidth <= 300)
            {
                IsRightPaneVisible = false;
            }
        }

        private void Pane_ContentChanged(object sender, TabItemArguments e)
        {
            TabItemArguments = new TabItemArguments()
            {
                InitialPageType = typeof(PaneHolderPage),
                NavigationArg = new PaneNavigationArguments()
                {
                    LeftPaneNavPathParam = e?.NavigationArg as string ?? PaneLeft.TabItemArguments?.NavigationArg as string,
                    RightPaneNavPathParam = IsRightPaneVisible ? PaneRight?.TabItemArguments?.NavigationArg as string : null
                }
            };
        }

        public Task TabItemDragOver(object sender, DragEventArgs e) => ActivePane?.TabItemDragOver(sender, e) ?? Task.CompletedTask;

        public Task TabItemDrop(object sender, DragEventArgs e) => ActivePane?.TabItemDrop(sender, e) ?? Task.CompletedTask;

        public void OpenPathInNewPane(string path)
        {
            IsRightPaneVisible = true;
            NavParamsRight = new NavigationParams { NavPath = path };
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            var menu = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

            switch (c: ctrl, s: shift, m: menu, k: args.KeyboardAccelerator.Key)
            {
                case (true, true, false, VirtualKey.Left): // ctrl + shift + "<-" select left pane
                    if (UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                    {
                        ActivePane = PaneLeft;
                    }
                    break;

                case (true, true, false, VirtualKey.Right): // ctrl + shift + "->" select right pane
                    if (UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                    {
                        if (string.IsNullOrEmpty(NavParamsRight?.NavPath))
                        {
                            NavParamsRight = new NavigationParams { NavPath = "Home".GetLocalized() };
                        }
                        IsRightPaneVisible = true;
                        ActivePane = PaneRight;
                    }
                    break;

                case (true, true, false, VirtualKey.W): // ctrl + shift + "W" close right pane
                    IsRightPaneVisible = false;
                    break;

                case (false, true, true, VirtualKey.Add): // alt + shift + "+" open pane
                    if (UserSettingsService.MultitaskingSettingsService.IsDualPaneEnabled)
                    {
                        if (string.IsNullOrEmpty(NavParamsRight?.NavPath))
                        {
                            NavParamsRight = new NavigationParams { NavPath = "Home".GetLocalized() };
                        }
                        IsRightPaneVisible = true;
                    }
                    break;
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void CloseActivePane()
        {
            // Can only close right pane atm
            IsRightPaneVisible = false;
        }

        private void PaneLeft_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as UIElement).GotFocus += Pane_GotFocus;
        }

        private void PaneRight_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as UIElement).GotFocus += Pane_GotFocus;
        }

        private void Pane_GotFocus(object sender, RoutedEventArgs e)
        {
            ActivePane = sender == PaneLeft ? PaneLeft : PaneRight;
        }

        public void Dispose()
        {
            UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
            Window.Current.SizeChanged -= Current_SizeChanged;
            PaneLeft?.Dispose();
            PaneRight?.Dispose();
        }
    }

    public class PaneNavigationArguments
    {
        public string LeftPaneNavPathParam { get; set; } = null;
        public string LeftPaneSelectItemParam { get; set; } = null;
        public string RightPaneNavPathParam { get; set; } = null;
        public string RightPaneSelectItemParam { get; set; } = null;
    }
}