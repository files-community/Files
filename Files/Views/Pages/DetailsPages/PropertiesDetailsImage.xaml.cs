using Files.Filesystem;
using Files.View_Models;
using Files.View_Models.Properties;
using Microsoft.Toolkit.Uwp.UI.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class PropertiesDetailsImage : Page
    {
        public BaseProperties BaseProperties { get; set; }

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        public PropertiesDetailsImage()
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
                BaseProperties = new ImageFileProperties(ViewModel, np.tokenSource, Dispatcher, null, listedItem);
            }
            else if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                BaseProperties = new FolderProperties(ViewModel, np.tokenSource, Dispatcher, listedItem);
            }

            ViewModel.AllDetailsVisibility = ViewModel.BasicDetailsVisibility.Equals(Visibility.Visible)? Visibility.Collapsed : Visibility.Visible;
            ShowMore.IsChecked = ViewModel.AllDetailsVisibility.Equals(Visibility.Visible);

            base.OnNavigatedTo(e);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // Center on New York City
            //var mapUri = new Uri(String.Format(@"bingmaps:?cp={0:g}~{1:g}", Math.Truncate((decimal) ViewModel.Latitude*10000000)/10000000, Math.Truncate((decimal) ViewModel.Longitude * 10000000)/10000000));
            var mapUri = new Uri(String.Format(@"bingmaps:?where={0}", ViewModel.Geopoint.Address.FormattedAddress));

            // Launch the Windows Maps app
            var launcherOptions = new Windows.System.LauncherOptions();
            launcherOptions.TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe";
            var success = await Windows.System.Launcher.LaunchUriAsync(mapUri, launcherOptions);
        }

        private void ShowMore_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.BasicDetailsVisibility = (bool)ShowMore.IsChecked ? Visibility.Collapsed : Visibility.Visible;
            ViewModel.AllDetailsVisibility = (bool)!ShowMore.IsChecked ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}