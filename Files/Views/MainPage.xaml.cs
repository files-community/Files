using Files.Controllers;
using Files.Controls;
using Files.Filesystem;
using Files.UserControls;
using Files.View_Models;
using Files.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
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
        public static ObservableCollection<INavigationControlItem> sideBarItems = new ObservableCollection<INavigationControlItem>();

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
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            var navArgs = eventArgs.Parameter?.ToString();
            if (eventArgs.NavigationMode != NavigationMode.Back)
            {
                App.AppSettings = new SettingsViewModel();
                App.InteractionViewModel = new InteractionViewModel();
                App.SidebarPinnedController = new SidebarPinnedController();

                Helpers.ThemeHelper.Initialize();
                Clipboard.ContentChanged += Clipboard_ContentChanged;
                Clipboard_ContentChanged(null, null);

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

        public static void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                if (App.CurrentInstance != null)
                {
                    DataPackageView packageView = Clipboard.GetContent();
                    if (packageView.Contains(StandardDataFormats.StorageItems)
                        && App.CurrentInstance.CurrentPageType != typeof(YourHome)
                        && !App.CurrentInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
                    {
                        App.InteractionViewModel.IsPasteEnabled = true;
                    }
                    else
                    {
                        App.InteractionViewModel.IsPasteEnabled = false;
                    }
                }
                else
                {
                    App.InteractionViewModel.IsPasteEnabled = false;
                }
            }
            catch (Exception)
            {
                App.InteractionViewModel.IsPasteEnabled = false;
            }
        }
    }
}
