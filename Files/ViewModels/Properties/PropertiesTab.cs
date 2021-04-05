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

            if (np.navParameter is LibraryItem library)
            {
                BaseProperties = new LibraryProperties(ViewModel, np.tokenSource, Dispatcher, library, AppInstance);
            }
            else if (np.navParameter is ListedItem item)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    BaseProperties = new FileProperties(ViewModel, np.tokenSource, Dispatcher, ItemMD5HashProgress, item, AppInstance);
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    BaseProperties = new FolderProperties(ViewModel, np.tokenSource, Dispatcher, item, AppInstance);
                }
            }
            else if (np.navParameter is List<ListedItem> items)
            {
                BaseProperties = new CombinedProperties(ViewModel, np.tokenSource, Dispatcher, items, AppInstance);
            }
            else if (np.navParameter is DriveItem drive)
            {
                BaseProperties = new DriveProperties(ViewModel, drive, AppInstance);
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