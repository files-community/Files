using Files.Filesystem;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;

namespace Files.View_Models.Properties
{
    class PropertiesTab
    {
        public BaseProperties BaseProperties { get; set; }

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        public void HandlePropertiesLoaded()
        {
            if (BaseProperties != null)
            {
                BaseProperties.GetSpecialProperties();
            }
        }

        public void HandleNavigation(NavigationEventArgs e, CoreDispatcher dispatcher)
        {
            ViewModel = new SelectedItemsPropertiesViewModel();
            var np = e.Parameter as Files.Properties.PropertyNavParam;

            if (np.navParameter is ListedItem)
            {
                var listedItem = np.navParameter as ListedItem;
                if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    BaseProperties = new FileProperties(ViewModel, np.tokenSource, dispatcher, null, listedItem);
                }
                else if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    BaseProperties = new FolderProperties(ViewModel, np.tokenSource, dispatcher, listedItem);
                }
            }
            else if (np.navParameter is List<ListedItem>)
            {
                BaseProperties = new CombinedProperties(ViewModel, np.tokenSource, dispatcher, np.navParameter as List<ListedItem>);
            }
            else if (np.navParameter is DriveItem)
            {
                BaseProperties = new DriveProperties(ViewModel, np.navParameter as DriveItem);
            }
        }
    }
}
