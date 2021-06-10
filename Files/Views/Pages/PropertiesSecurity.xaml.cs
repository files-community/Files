using Files.Filesystem;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using static Files.Views.PropertiesSecurityAdvanced;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public RelayCommand OpenAdvancedPropertiesCommand { get; set; }

        private ApplicationView propsView;

        public PropertiesSecurity()
        {
            this.InitializeComponent();

            OpenAdvancedPropertiesCommand = new RelayCommand(() => OpenAdvancedProperties());
        }

        public async override Task<bool> SaveChangesAsync(ListedItem item)
        {
            if (BaseProperties is FileSystemProperties fileSysProps)
            {
                return await fileSysProps.SetFilePermissions();
            }
            return true;
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (BaseProperties is FileSystemProperties fileSysProps)
            {
                fileSysProps.GetFilePermissions();
            }
        }

        public override void Dispose()
        {
        }

        private async void OpenAdvancedProperties()
        {
            if (!(BaseProperties is FileSystemProperties fileSysProps))
            {
                return;
            }
            
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                if (propsView == null)
                {
                    var newWindow = CoreApplication.CreateNewView();

                    await newWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Frame frame = new Frame();
                        frame.Navigate(typeof(PropertiesSecurityAdvanced), new PropertiesPageNavigationArguments()
                        {
                            Item = fileSysProps.Item,
                            AppInstanceArgument = AppInstance

                        }, new SuppressNavigationTransitionInfo());
                        Window.Current.Content = frame;
                        Window.Current.Activate();

                        propsView = ApplicationView.GetForCurrentView();
                        newWindow.TitleBar.ExtendViewIntoTitleBar = true;
                        propsView.Title = string.Format("SecurityAdvancedPermissionsTitle".GetLocalized(), fileSysProps.Item.ItemName);
                        propsView.PersistedStateId = "PropertiesSecurity";
                        propsView.SetPreferredMinSize(new Size(400, 550));
                        propsView.Consolidated += async delegate
                        {
                            Window.Current.Close();
                            propsView = null;
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => fileSysProps.GetFilePermissions()); // Reload permissions
                        };
                    });

                    bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(propsView.Id);
                    if (viewShown && propsView != null)
                    {
                        // Set window size again here as sometimes it's not resized in the page Loaded event
                        propsView.TryResizeView(new Size(850, 550));
                    }
                }
                await ApplicationViewSwitcher.SwitchAsync(propsView.Id);
            }
            else
            {
                // Unsupported
            }
        }
    }
}
