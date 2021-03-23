using Files.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.Views
{
    /// <summary>
    /// The root page of Files
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        private double dragReigonWidth;
        public double DragRegionWidth
        {
            get => dragReigonWidth;
            set
            {
                if (value != dragReigonWidth)
                {
                    dragReigonWidth = value;
                    NotifyPropertyChanged("DragRegionWidth");
                }
            }
        }
        public static IMultitaskingControl MultitaskingControl { get; set; }

        private TabItem selectedTabItem;

        public TabItem SelectedTabItem
        public MainPageViewModel ViewModel
        {
            get => (MainPageViewModel)DataContext;
            set => DataContext = value;
        }
        public AdaptiveSidebarViewModel SidebarAdaptiveViewModel = new AdaptiveSidebarViewModel();
        public MainPage()
        {
            this.InitializeComponent();

            this.ViewModel = new MainPageViewModel();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            CoreTitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }
            AllowDrop = true;
        }

        }

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            DragRegionWidth = sender.SystemOverlayRightInset;
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(MultitaskingControl is HorizontalMultitaskingControl))
            {
                MultitaskingControl = horizontalMultitaskingControl;
            }
        }
        {
            ViewModel.OnNavigatedTo(e);
        }
    }
}
