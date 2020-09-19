using Files.Filesystem;
using Files.View_Models;
using Files.View_Models.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class PropertiesDetails : Page
    {
        public BaseProperties BaseProperties { get; set; }

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        public TimeSpan FallbackTime= new TimeSpan(0, 0, 0);

        public PropertiesDetails()
        {
            this.InitializeComponent();
        }

        private void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (BaseProperties != null)
            {
                BaseProperties.GetSpecialProperties();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = new SelectedItemsPropertiesViewModel();
            var np = e.Parameter as Properties.PropertyNavParam;

            var listedItem = np.navParameter as ListedItem;
            if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
            {
                BaseProperties = new FileProperties(ViewModel, np.tokenSource, Dispatcher, null, listedItem);
            }
            

            //ViewModel.AllDetailsVisibility = ViewModel.BasicDetailsVisibility.Equals(Visibility.Visible)? Visibility.Collapsed : Visibility.Visible;
            //ShowMore.IsChecked = ViewModel.AllDetailsVisibility.Equals(Visibility.Visible);

            base.OnNavigatedTo(e);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var mapUri = new Uri(String.Format(@"bingmaps:?where={0}", ViewModel.Geopoint.Address.FormattedAddress));

            // Launch the Windows Maps app
            var launcherOptions = new Windows.System.LauncherOptions();
            launcherOptions.TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe";
            var success = await Windows.System.Launcher.LaunchUriAsync(mapUri, launcherOptions);
            
        }

        public async Task SaveChanges(ListedItem item)
        {
             await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => (BaseProperties as FileProperties).SyncPropertyChanges());
        }

        private async void ClearPersonalInformation_Click(object sender, RoutedEventArgs e)
        {
            ClearPersonalInformationFlyout.Hide();
            await (BaseProperties as FileProperties).ClearPersonalInformation();
        }
    }
}