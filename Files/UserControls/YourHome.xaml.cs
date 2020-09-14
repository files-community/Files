using Files.View_Models;
using Files.Views.Pages;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class YourHome : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public YourHome()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = false;
            App.CurrentInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
            App.CurrentInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
            App.CurrentInstance.InstanceViewModel.IsPageTypeCloudDrive = false;
            var parameters = eventArgs.Parameter.ToString();
            App.MultitaskingControl?.SetSelectedTabInfo(parameters, null);
            App.MultitaskingControl?.SelectionChanged();
            App.CurrentInstance.NavigationToolbar.CanRefresh = false;
            App.CurrentInstance.NavigationToolbar.CanGoBack = App.CurrentInstance.ContentFrame.CanGoBack;
            App.CurrentInstance.NavigationToolbar.CanGoForward = App.CurrentInstance.ContentFrame.CanGoForward;
            App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;

            // Set path of working directory empty
            await App.CurrentInstance.FilesystemViewModel.SetWorkingDirectory("Home");

            // Clear the path UI and replace with Favorites
            App.CurrentInstance.NavigationToolbar.PathComponents.Clear();
            string componentLabel = parameters;
            string tag = parameters;
            PathBoxItem item = new PathBoxItem()
            {
                Title = componentLabel,
                Path = tag,
            };
            App.CurrentInstance.NavigationToolbar.PathComponents.Add(item);
        }
    }
}