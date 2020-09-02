using Files.Filesystem;
using Files.View_Models.Properties;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class PropertiesGeneral : Page
    {
        private readonly PropertiesTab PropertiesTab;

        public PropertiesGeneral()
        {
            this.InitializeComponent();
            PropertiesTab = new PropertiesTab();
        }

        private void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            PropertiesTab.HandlePropertiesLoaded();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            PropertiesTab.HandleNavigation(e, Dispatcher);
            base.OnNavigatedTo(e);
        }

        public async Task SaveChanges(ListedItem item)
        {
            if (PropertiesTab.ViewModel.OriginalItemName != null)
            {
                await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => App.CurrentInstance.InteractionOperations.RenameFileItem(item,
                      PropertiesTab.ViewModel.OriginalItemName,
                      PropertiesTab.ViewModel.ItemName));
            }
        }
    }
}