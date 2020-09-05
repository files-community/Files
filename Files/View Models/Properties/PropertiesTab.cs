using Files.Filesystem;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.View_Models.Properties
{
    public abstract class PropertiesTab : Page
    {
        public BaseProperties BaseProperties { get; set; }

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        protected Microsoft.UI.Xaml.Controls.ProgressBar ItemMD5HashProgress = null;

        protected void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (BaseProperties != null)
            {
                BaseProperties.GetSpecialProperties();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = new SelectedItemsPropertiesViewModel();
            var np = e.Parameter as Files.Properties.PropertyNavParam;

            if (np.navParameter is ListedItem)
            {
                var listedItem = np.navParameter as ListedItem;
                if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    BaseProperties = new FileProperties(ViewModel, np.tokenSource, Dispatcher, ItemMD5HashProgress, listedItem);
                }
                else if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    BaseProperties = new FolderProperties(ViewModel, np.tokenSource, Dispatcher, listedItem);
                }
            }
            else if (np.navParameter is List<ListedItem>)
            {
                BaseProperties = new CombinedProperties(ViewModel, np.tokenSource, Dispatcher, np.navParameter as List<ListedItem>);
            }
            else if (np.navParameter is DriveItem)
            {
                BaseProperties = new DriveProperties(ViewModel, np.navParameter as DriveItem);
            }

            base.OnNavigatedTo(e);
        }
    }
}
