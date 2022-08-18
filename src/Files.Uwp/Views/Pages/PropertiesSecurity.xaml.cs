using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using static Files.Uwp.Views.PropertiesSecurityAdvanced;

namespace Files.Uwp.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public RelayCommand OpenAdvancedPropertiesCommand { get; set; }

        public SecurityProperties SecurityProperties { get; set; }

        private 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView propsView;

        public PropertiesSecurity()
        {
            this.InitializeComponent();

            OpenAdvancedPropertiesCommand = new RelayCommand(() => OpenAdvancedProperties());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var np = e.Parameter as Views.Properties.PropertyNavParam;

            if (np.navParameter is ListedItem listedItem)
            {
                SecurityProperties = new SecurityProperties(listedItem);
            }
            else if (np.navParameter is DriveItem driveitem)
            {
                SecurityProperties = new SecurityProperties(driveitem);
            }

            base.OnNavigatedTo(e);
        }

        public async override Task<bool> SaveChangesAsync(ListedItem item)
        {
            if (SecurityProperties != null)
            {
                return await SecurityProperties.SetFilePermissions();
            }
            return true;
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (SecurityProperties != null)
            {
                SecurityProperties.GetFilePermissions();
            }
        }

        public override void Dispose()
        {
            if (propsView != null)
            {
                propsView.Consolidated -= PropsView_Consolidated;
            }
        }

        private async void OpenAdvancedProperties()
        {
            if (SecurityProperties == null)
            {
                return;
            }

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                if (propsView == null)
                {
                    var newWindow = CoreApplication.CreateNewView();

                    await newWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        Frame frame = new Frame();
                        frame.Navigate(typeof(PropertiesSecurityAdvanced), new PropertiesPageNavigationArguments()
                        {
                            Item = SecurityProperties.Item
                        }, new SuppressNavigationTransitionInfo());
                        App.Window.Content = frame;
                        App.Window.Activate();

                        propsView = 
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView.GetForCurrentView();
                        newWindow.TitleBar.ExtendViewIntoTitleBar = true;
                        propsView.Title = string.Format("SecurityAdvancedPermissionsTitle".GetLocalized(), SecurityProperties.Item.ItemName);
                        propsView.PersistedStateId = "PropertiesSecurity";
                        propsView.SetPreferredMinSize(new Size(400, 500));
                        propsView.Consolidated += PropsView_Consolidated;

                        bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(propsView.Id);
                        if (viewShown && propsView != null)
                        {
                            // Set window size again here as sometimes it's not resized in the page Loaded event
                            propsView.TryResizeView(new Size(850, 550));
                        }
                    });
                }
                await ApplicationViewSwitcher.SwitchAsync(propsView.Id);
            }
            else
            {
                // Unsupported
            }
        }

        private async void PropsView_Consolidated(
                /*
                   TODO UA315_A Use Microsoft.UI.Windowing.AppWindow for window Management instead of ApplicationView/CoreWindow or Microsoft.UI.Windowing.AppWindow APIs
                   Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing
                */
                ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            propsView.Consolidated -= PropsView_Consolidated;
            propsView = null;

            if (SecurityProperties != null)
            {
                await /*
                TODO UA306_A2: UWP CoreDispatcher : Windows.UI.Core.CoreDispatcher is no longer supported. Use DispatcherQueue instead. Read: https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/threading
            */Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SecurityProperties.GetFilePermissions()); // Reload permissions
            }
        }
    }
}