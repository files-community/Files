using Files.Filesystem;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using static Files.Views.PropertiesSecurityAdvanced;

namespace Files.Views
{
    public sealed partial class PropertiesSecurity : PropertiesTab
    {
        public RelayCommand OpenAdvancedPropertiesCommand { get; set; }

        private AppWindow appWindow;

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
                if (appWindow == null)
                {
                    appWindow = await AppWindow.TryCreateAsync();
                    Frame frame = new Frame();
                    appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                    frame.Navigate(typeof(PropertiesSecurityAdvanced), new PropertiesPageNavigationArguments()
                    {
                        Item = fileSysProps.Item,
                        AppInstanceArgument = AppInstance,
                        AppWindowArgument = appWindow
                    }, new SuppressNavigationTransitionInfo());

                    WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(400, 550));

                    appWindow.RequestSize(new Size(400, 550));
                    appWindow.Title = "PropertiesTitle".GetLocalized();

                    ElementCompositionPreview.SetAppWindowContent(appWindow, frame);

                    appWindow.Closed += delegate
                    {
                        frame.Content = null;
                        appWindow = null;
                    };
                }
                await appWindow.TryShowAsync();
            }
            else
            {
                // Unsupported
            }
        }
    }
}
