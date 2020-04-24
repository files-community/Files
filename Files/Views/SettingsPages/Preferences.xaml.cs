using Windows.Storage;
using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.IO;
using Files.Filesystem;
using Newtonsoft.Json;
using Files.DataModels;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI.Xaml.Navigation;
using System.Linq;

namespace Files.SettingsPages
{
    public sealed partial class Preferences : Page
    {
        private StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public Preferences()
        {
            this.InitializeComponent();

            try
            {
                StorageFolder.GetFolderFromPathAsync(App.AppSettings.OneDrivePath);
            }
            catch (Exception)
            {
                App.AppSettings.PinOneDriveToSideBar = false;
                OneDrivePin.IsEnabled = false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var terminalId = 1;
            if (localSettings.Values["terminal_id"] != null) terminalId = (int)localSettings.Values["terminal_id"];

            TerminalApplicationsComboBox.SelectedItem = App.AppSettings.Terminals.Single(p => p.Id == terminalId);
        }

        private void EditTerminalApplications_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            LaunchTerminalsConfigFile();
        }

        private async void LaunchTerminalsConfigFile()
        {
            await Launcher.LaunchFileAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json")));
        }

        private void TerminalApp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;

            var selectedTerminal = (TerminalModel)comboBox.SelectedItem;

            localSettings.Values["terminal_id"] = selectedTerminal.Id;
        }

        private void OneDrivePin_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            OneDrivePin.IsEnabled = false;
            App.AppSettings.PinOneDriveToSideBar = OneDrivePin.IsOn;
            OneDrivePin.IsEnabled = true;
        }
    }
}