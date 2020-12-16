using Files.View_Models;
using Files.Views;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class Experimental : Page
    {
        public SettingsViewModel AppSettings => MainPage.AppSettings;
        private StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public Experimental()
        {
            this.InitializeComponent();
        }
    }
}