using Files.Controls;
using Files.UserControls;
using Files.View_Models;
using Files.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private TabItem _SelectedTabItem;
        public TabItem SelectedTabItem 
        {
            get
            {
                return _SelectedTabItem;
            }
            set
            {
                _SelectedTabItem = value;
                NotifyPropertyChanged("SelectedTabItem");
            }
        }

        public SettingsViewModel AppSettings => App.AppSettings;
        public static ObservableCollection<TabItem> AppInstances = new ObservableCollection<TabItem>();

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }

            App.AppSettings = new SettingsViewModel();
            App.InteractionViewModel = new InteractionViewModel();

            // Turn on Navigation Cache
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            Helpers.ThemeHelper.Initialize();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            var navArgs = eventArgs.Parameter?.ToString();

            if (string.IsNullOrEmpty(navArgs) && App.AppSettings.OpenASpecificPageOnStartup)
            {
                try
                {
                    VerticalTabView.AddNewTab(typeof(ModernShellPage), App.AppSettings.OpenASpecificPageOnStartupPath);
                }
                catch (Exception)
                {
                    VerticalTabView.AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
                }
            }
            else if (string.IsNullOrEmpty(navArgs))
            {
                VerticalTabView.AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
            }
            else
            {
                VerticalTabView.AddNewTab(typeof(ModernShellPage), navArgs);
            }

            // Initial setting of SelectedTabItem
            Frame rootFrame = Window.Current.Content as Frame;
            var mainView = rootFrame.Content as MainPage;
            mainView.SelectedTabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
