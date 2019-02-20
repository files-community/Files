using Windows.Storage;
using Windows.UI.Xaml.Controls;


namespace Files.SettingsPages
{
    
    public sealed partial class Preferences : Page
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        public Preferences()
        {
            this.InitializeComponent();
            if(localSettings.Values["autoRefreshEnabledSetting"] != null)
            {
                if (localSettings.Values["autoRefreshEnabledSetting"].Equals(true))
                {
                    AutoRefreshSwitch.IsOn = true;
                }
                else
                {
                    AutoRefreshSwitch.IsOn = false;
                }
            }
            else
            {
                AutoRefreshSwitch.IsOn = false;
            }

        }

        private void AutoRefreshSwitch_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            
            if((sender as ToggleSwitch).IsOn)
            {
                localSettings.Values["autoRefreshEnabledSetting"] = true;
            }
            else
            {
                localSettings.Values["autoRefreshEnabledSetting"] = false;
            }
        }
    }
}
