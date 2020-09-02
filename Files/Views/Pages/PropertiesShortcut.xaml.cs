using Files.View_Models.Properties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class PropertiesShortcut : Page
    {
        private readonly PropertiesTab PropertiesTab;

        public PropertiesShortcut()
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
    }
}