using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.Extensions;
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
    public sealed partial class PaneHolderPage : Page, IPaneHolder, ITabItemContent, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public PaneHolderPage()
        {
            this.InitializeComponent();
            
            AppSettings.PropertyChanged += AppSettings_PropertyChanged;
            Window.Current.SizeChanged += Current_SizeChanged;
            Current_SizeChanged(null, null);

            this.ActivePane = PaneLeft;
            this.IsRightPaneVisible = IsMultiPaneEnabled && AppSettings.AlwaysOpenDualPaneInNewTab;
        }

        private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.IsDualPaneEnabled):
                    NotifyPropertyChanged(nameof(IsMultiPaneEnabled));
                    break;
            }
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            IsWindowCompactSize = Window.Current.Bounds.Width <= 800;
        }

        private bool wasRightPaneVisible;

        private bool isWindowCompactSize;

        public bool IsWindowCompactSize
        {
            get => isWindowCompactSize;
            set
            {
                if (isWindowCompactSize != value)
                {
                    isWindowCompactSize = value;
                    if (isWindowCompactSize)
                    {
                        wasRightPaneVisible = isRightPaneVisible;
                        IsRightPaneVisible = false;
                    }
                    else if (wasRightPaneVisible)
                    {
                        IsRightPaneVisible = true;
                        wasRightPaneVisible = false;
                    }
                    NotifyPropertyChanged("IsWindowCompactSize");
                    NotifyPropertyChanged("IsMultiPaneEnabled");
                }
            }
        }

        public bool IsMultiPaneEnabled
        {
            get => AppSettings.IsDualPaneEnabled && !IsWindowCompactSize;
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
                    NotifyPropertyChanged("NavParamsLeft");
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
                    NotifyPropertyChanged("NavParamsRight");
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
                    NotifyPropertyChanged("ActivePane");
                    NotifyPropertyChanged("IsLeftPaneActive");
                    NotifyPropertyChanged("IsRightPaneActive");
                }
            }
        }

        public bool IsLeftPaneActive => ActivePane == PaneLeft;

        public bool IsRightPaneActive => ActivePane == PaneRight;

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
                    NotifyPropertyChanged("IsRightPaneVisible");
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

        public event EventHandler<TabItemArguments> ContentChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        }

        public void Clipboard_ContentChanged(object sender, object e)
        {
        }

        public void Refresh_Click()
        {
            ActivePane?.Refresh_Click();
        }

        public void Dispose()
        {
            PaneLeft?.Dispose();
            PaneRight?.Dispose();
            Window.Current.SizeChanged -= Current_SizeChanged;
            AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
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

            switch (c: ctrl, k: args.KeyboardAccelerator.Key)
            {
                case (true, VirtualKey.Left):
                    if (AppSettings.IsDualPaneEnabled)
                    {
                        ActivePane = PaneLeft;
                    }
                    break;

                case (true, VirtualKey.Right):
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
            }
        }
    }

    public class PaneNavigationArguments
    {
        public string LeftPaneNavPathParam { get; set; } = null;
        public string RightPaneNavPathParam { get; set; } = null;
    }
}
