using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.ViewModels.Properties;
using Files.Uwp.Extensions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using static Files.Uwp.Views.PropertiesSecurityAdvanced;
using Files.Uwp.Helpers;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Microsoft.UI;

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
                        var frame = new Frame();
                        frame.RequestedTheme = ThemeHelper.RootTheme;
                        frame.Navigate(typeof(PropertiesSecurityAdvanced), new PropertiesPageNavigationArguments()
                        {
                            Item = SecurityProperties.Item
                        }, new SuppressNavigationTransitionInfo());

                        // Initialize window
                        var propertiesWindow = new WinUIEx.WindowEx();
                        var appWindow = propertiesWindow.AppWindow;

                        // Set content
                        propertiesWindow.Content = frame;
                        if (frame.Content is PropertiesSecurityAdvanced properties)
                            properties.appWindow = appWindow;

                        // Set min size
                        propertiesWindow.MinWidth = 850;
                        propertiesWindow.MinHeight = 550;

                        // Set backdrop
                        propertiesWindow.Backdrop = new WinUIEx.MicaSystemBackdrop() { DarkTintOpacity = 0.8 };

                        if (AppWindowTitleBar.IsCustomizationSupported())
                        {
                            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                            // Set window buttons background to transparent
                            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                        }
                        else
                        {
                            propertiesWindow.ExtendsContentIntoTitleBar = true;
                        }

                        appWindow.Title = string.Format("SecurityAdvancedPermissionsTitle".GetLocalizedResource(), SecurityProperties.Item.ItemName);
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