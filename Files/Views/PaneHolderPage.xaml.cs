﻿using Files.Filesystem;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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
        public bool IsLeftPaneActive => ActivePane == PaneLeft;
        public bool IsRightPaneActive => ActivePane == PaneRight;

        public event EventHandler<TabItemArguments> ContentChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel AppSettings => App.AppSettings;
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
            get => AppSettings.IsDualPaneEnabled && !(Window.Current.Bounds.Width <= 750);
        }

        private string navParamsLeft;

        public string NavParamsLeft
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

        private string navParamsRight;

        public string NavParamsRight
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
                    NotifyPropertyChanged(nameof(FilesystemHelpers));
                }
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
            this.IsRightPaneVisible = IsMultiPaneEnabled && AppSettings.AlwaysOpenDualPaneInNewTab;
            App.AppSettings.PropertyChanged += AppSettings_PropertyChanged;

            // TODO: fallback / error when failed to get NavigationViewCompactPaneLength value?
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
                NavParamsLeft = navPath;
                NavParamsRight = "NewTab".GetLocalized();
            }
            if (eventArgs.Parameter is PaneNavigationArguments paneArgs)
            {
                NavParamsLeft = paneArgs.LeftPaneNavPathParam;
                NavParamsRight = paneArgs.RightPaneNavPathParam;
                IsRightPaneVisible = IsMultiPaneEnabled && paneArgs.RightPaneNavPathParam != null;
            }

            TabItemArguments = new TabItemArguments()
            {
                InitialPageType = typeof(PaneHolderPage),
                NavigationArg = new PaneNavigationArguments()
                {
                    LeftPaneNavPathParam = NavParamsLeft,
                    RightPaneNavPathParam = IsRightPaneVisible ? NavParamsRight : null
                }
            };
        }

        private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(App.AppSettings.IsDualPaneEnabled):
                    NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
                    break;
            }
        }

        private void PaneLeft_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ActivePane = PaneLeft;
            e.Handled = false;
        }

        private void PaneRight_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ActivePane = PaneRight;
            e.Handled = false;
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
                    LeftPaneNavPathParam = PaneLeft.TabItemArguments?.NavigationArg as string,
                    RightPaneNavPathParam = IsRightPaneVisible ? PaneRight?.TabItemArguments?.NavigationArg as string : null
                }
            };
        }

        public DataPackageOperation TabItemDragOver(object sender, DragEventArgs e)
        {
            return ActivePane?.TabItemDragOver(sender, e) ?? DataPackageOperation.None;
        }

        public async Task<DataPackageOperation> TabItemDrop(object sender, DragEventArgs e)
        {
            if (ActivePane != null)
            {
                return await ActivePane.TabItemDrop(sender, e);
            }
            return DataPackageOperation.None;
        }

        public void OpenPathInNewPane(string path)
        {
            IsRightPaneVisible = true;
            NavParamsRight = path;
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
                    if (AppSettings.IsDualPaneEnabled)
                    {
                        ActivePane = PaneLeft;
                    }
                    break;

                case (true, true, false, VirtualKey.Right): // ctrl + shift + "->" select right pane
                    if (AppSettings.IsDualPaneEnabled)
                    {
                        if (string.IsNullOrEmpty(NavParamsRight))
                        {
                            NavParamsRight = "NewTab".GetLocalized();
                        }
                        IsRightPaneVisible = true;
                        ActivePane = PaneRight;
                    }
                    break;

                case (true, true, false, VirtualKey.W): // ctrl + shift + "W" close right pane
                    IsRightPaneVisible = false;
                    break;

                case (false, true, true, VirtualKey.Add): // alt + shift + "+" open pane
                    if (AppSettings.IsDualPaneEnabled)
                    {
                        if (string.IsNullOrEmpty(NavParamsRight))
                        {
                            NavParamsRight = "NewTab".GetLocalized();
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

        public void Dispose()
        {
            App.AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
            Window.Current.SizeChanged -= Current_SizeChanged;
            PaneLeft?.Dispose();
            PaneRight?.Dispose();
        }

        public void CloseActivePane()
        {
            // Can only close right pane atm
            IsRightPaneVisible = false;
        }
    }

    public class PaneNavigationArguments
    {
        public string LeftPaneNavPathParam { get; set; } = null;
        public string RightPaneNavPathParam { get; set; } = null;
    }
}