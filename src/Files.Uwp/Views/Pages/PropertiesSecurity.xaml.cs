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
using Files.Uwp.Helpers;
using Windows.Graphics;
using Microsoft.UI.Windowing;

namespace Files.Uwp.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public RelayCommand OpenAdvancedPropertiesCommand { get; set; }

        public SecurityProperties SecurityProperties { get; set; }

        private AppWindow? propsView;

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

        }

        private async void OpenAdvancedProperties()
        {
            if (SecurityProperties == null)
            {
                return;
            }

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                if (WindowDecorationsHelper.IsWindowDecorationsAllowed)
                {
                    if (propsView == null)
                    {
                        Frame frame = new Frame();
                        frame.RequestedTheme = ThemeHelper.RootTheme;
                        frame.Navigate(typeof(PropertiesSecurityAdvanced), new PropertiesPageNavigationArguments()
                        {
                            Item = SecurityProperties.Item
                        }, new SuppressNavigationTransitionInfo());

                        Window w = new Window();
                        w.Content = frame;
                        var appWindow = App.GetAppWindow(w);
                        (frame.Content as PropertiesSecurityAdvanced).appWindow = appWindow;

                        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                        appWindow.Title = string.Format("SecurityAdvancedPermissionsTitle".GetLocalized(), SecurityProperties.Item.ItemName);
                        appWindow.Resize(new SizeInt32(850, 550));
                        appWindow.Closing += AppWindow_Closing;
                        appWindow.Show();

                        propsView = appWindow;
                    }
                    else
                    {
                        propsView.Show(true);
                    }
                }
                else
                {
                    //WINUI3
                }
            }
            else
            {
                // Unsupported
            }
        }

        private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            sender.Closing -= AppWindow_Closing;
            propsView = null;

            if (SecurityProperties != null)
            {
                await DispatcherQueue.EnqueueAsync(() => SecurityProperties.GetFilePermissions()); // Reload permissions
            }
        }
    }
}