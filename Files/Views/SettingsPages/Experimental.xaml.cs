using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Experimental : Page
    {
        private StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public Experimental()
        {
            this.InitializeComponent();
        }
    }
}