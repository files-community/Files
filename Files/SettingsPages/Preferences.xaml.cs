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
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;


        public Preferences()
        {
            this.InitializeComponent();

            if (App.AppSettings != null && localSettings.Values["customLocationsSetting"] != null)
            {
                if (localSettings.Values["customLocationsSetting"].Equals(true))
                {
                    CustomLocationToggle.IsOn = true;                
                }
                else
                {
                    aaaa.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                CustomLocationToggle.IsOn = false;
                aaaa.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

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

        private void CustomLocationToggle_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if ((sender as ToggleSwitch).IsOn)
            {
                localSettings.Values["customLocationsSetting"] = true;

                localSettings.Values["DesktopLocation"] = App.AppSettings.DesktopPath;

                localSettings.Values["DownloadsLocation"] = App.AppSettings.DownloadsPath;

                localSettings.Values["DocumentsLocation"] = App.AppSettings.DocumentsPath;

                localSettings.Values["PicturesLocation"] = App.AppSettings.PicturesPath;

                localSettings.Values["MusicLocation"] = App.AppSettings.MusicPath;

                localSettings.Values["VideosLocation"] = App.AppSettings.VideosPath;

                localSettings.Values["OneDriveLocation"] = App.AppSettings.OneDrivePath;

                aaaa.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                localSettings.Values["customLocationsSetting"] = false;

                aaaa.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            }
        }

        private async void EditTerminalApplications_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            LaunchTerminalsConfigFile();
        }

        private async void LaunchTerminalsConfigFile()
        {
            Launcher.LaunchFileAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json")));
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

        private async void btnBrowse_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                var item = ((Button)sender).Tag;
                switch (item)
                {
                    case "1":
                        {
                            localSettings.Values["DesktopLocation"] = folder.Name;
                            App.AppSettings.DesktopPath = folder.Name;
                            break;
                        }
                    case "2":
                        {
                            localSettings.Values["DownloadsLocation"] = folder.Name;
                            App.AppSettings.DownloadsPath = folder.Name;
                            break;
                        }
                    case "3":
                        {
                            localSettings.Values["DocumentsLocation"] = folder.Name;
                            App.AppSettings.DocumentsPath = folder.Name;
                            break;
                        }
                    case "4":
                        {
                            localSettings.Values["PicturesLocation"] = folder.Name;
                            App.AppSettings.PicturesPath = folder.Name;
                            break;
                        }
                    case "5":
                        {
                            localSettings.Values["MusicLocation"] = folder.Name;
                            App.AppSettings.MusicPath = folder.Name;
                            break;
                        }
                    case "6":
                        {
                            localSettings.Values["VideosLocation"] = folder.Name;
                            App.AppSettings.VideosPath = folder.Name;
                            break;
                        }
                    case "7":
                        {
                            localSettings.Values["OneDriveLocation"] = folder.Name;
                            App.AppSettings.OneDrivePath = folder.Name;
                            break;
                        }
                }
            }
        }
    }
}
