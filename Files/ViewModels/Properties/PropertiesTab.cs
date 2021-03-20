using Files.Filesystem;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.ViewModels.Properties
{
    public abstract class PropertiesTab : Page
    {
        public IShellPage AppInstance = null;

        public BaseProperties BaseProperties { get; set; }

        public SelectedItemsPropertiesViewModel ViewModel { get; set; }

        protected Microsoft.UI.Xaml.Controls.ProgressBar ItemMD5HashProgress = null;

        protected virtual void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            if (BaseProperties != null)
            {
                BaseProperties.GetSpecialProperties();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var np = e.Parameter as Views.Properties.PropertyNavParam;

            AppInstance = np.AppInstanceArgument;
            ViewModel = new SelectedItemsPropertiesViewModel(AppInstance.SlimContentPage);

            if (np.navParameter is ListedItem)
            {
                var listedItem = np.navParameter as ListedItem;
                if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    BaseProperties = new FileProperties(ViewModel, np.tokenSource, Dispatcher, ItemMD5HashProgress, listedItem, AppInstance);
                }
                else if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    BaseProperties = new FolderProperties(ViewModel, np.tokenSource, Dispatcher, listedItem, AppInstance);
                }
            }
            else if (np.navParameter is List<ListedItem>)
            {
                BaseProperties = new CombinedProperties(ViewModel, np.tokenSource, Dispatcher, np.navParameter as List<ListedItem>, AppInstance);
            }
            else if (np.navParameter is DriveItem)
            {
                BaseProperties = new DriveProperties(ViewModel, np.navParameter as DriveItem, AppInstance);
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (BaseProperties != null && BaseProperties.TokenSource != null)
            {
                BaseProperties.TokenSource.Cancel();
            }

            base.OnNavigatedFrom(e);
        }
    }
}