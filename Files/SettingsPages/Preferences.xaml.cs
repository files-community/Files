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

                    DesktopL.IsEnabled = true;
                    DesktopL.Text = localSettings.Values["DesktopLocation"].ToString();

                    DownloadsL.IsEnabled = true;
                    DownloadsL.Text = localSettings.Values["DownloadsLocation"].ToString();

                    DocumentsL.IsEnabled = true;
                    DocumentsL.Text = localSettings.Values["DocumentsLocation"].ToString();

                    PictureL.IsEnabled = true;
                    PictureL.Text = localSettings.Values["PicturesLocation"].ToString();

                    MusicL.IsEnabled = true;
                    MusicL.Text = localSettings.Values["MusicLocation"].ToString();

                    VideosL.IsEnabled = true;
                    VideosL.Text = localSettings.Values["VideosLocation"].ToString();

                    OneDriveL.IsEnabled = true;
                    OneDriveL.Text = localSettings.Values["OneDriveLocation"].ToString();

                    SaveCustomL.IsEnabled = true;
                    aaaa.Visibility = Windows.UI.Xaml.Visibility.Visible;

                }
                else
                {
                    CustomLocationToggle.IsOn = false;
                    DesktopL.IsEnabled = false;
                    DownloadsL.IsEnabled = false;
                    DocumentsL.IsEnabled = false;
                    PictureL.IsEnabled = false;
                    MusicL.IsEnabled = false;
                    VideosL.IsEnabled = false;
                    SaveCustomL.IsEnabled = false;
                    OneDriveL.IsEnabled = false;
                    aaaa.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                }
            }
            else
            {
                CustomLocationToggle.IsOn = false;
                DesktopL.IsEnabled = false;
                DownloadsL.IsEnabled = false;
                DocumentsL.IsEnabled = false;
                PictureL.IsEnabled = false;
                MusicL.IsEnabled = false;
                VideosL.IsEnabled = false;
                SaveCustomL.IsEnabled = false;
                OneDriveL.IsEnabled = false;
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
            if (localSettings.Values["terminal_id"] != null) terminalId = (int) localSettings.Values["terminal_id"];

            TerminalApplicationsComboBox.SelectedItem = App.AppSettings.Terminals.Single(p => p.Id == terminalId);
        }

        private void CustomLocationToggle_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if ((sender as ToggleSwitch).IsOn)
            {
                localSettings.Values["customLocationsSetting"] = true;

                DesktopL.IsEnabled = true;
                localSettings.Values["DesktopLocation"] = App.AppSettings.DesktopPath;

                DownloadsL.IsEnabled = true;
                localSettings.Values["DownloadsLocation"] = App.AppSettings.DownloadsPath;

                DocumentsL.IsEnabled = true;
                localSettings.Values["DocumentsLocation"] = App.AppSettings.DocumentsPath;

                PictureL.IsEnabled = true;
                localSettings.Values["PicturesLocation"] = App.AppSettings.PicturesPath;

                MusicL.IsEnabled = true;
                localSettings.Values["MusicLocation"] = App.AppSettings.MusicPath;

                VideosL.IsEnabled = true;
                localSettings.Values["VideosLocation"] = App.AppSettings.VideosPath;

                OneDriveL.IsEnabled = true;
                localSettings.Values["OneDriveLocation"] = App.AppSettings.OneDrivePath;

                DesktopL.Text = localSettings.Values["DesktopLocation"].ToString();
                DownloadsL.Text = localSettings.Values["DownloadsLocation"].ToString();
                DocumentsL.Text = localSettings.Values["DocumentsLocation"].ToString();
                PictureL.Text = localSettings.Values["PicturesLocation"].ToString();
                MusicL.Text = localSettings.Values["MusicLocation"].ToString();
                VideosL.Text = localSettings.Values["VideosLocation"].ToString();
                OneDriveL.Text = localSettings.Values["OneDriveLocation"].ToString();
                aaaa.Visibility = Windows.UI.Xaml.Visibility.Visible;

                SaveCustomL.IsEnabled = true;
            }
            else
            {
                localSettings.Values["customLocationsSetting"] = false;
                DesktopL.IsEnabled = false;
                DownloadsL.IsEnabled = false;
                DocumentsL.IsEnabled = false;
                PictureL.IsEnabled = false;
                MusicL.IsEnabled = false;
                VideosL.IsEnabled = false;
                OneDriveL.IsEnabled = false;
                SaveCustomL.IsEnabled = false;
                aaaa.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            }
        }

        private async void SaveCustomL_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            StorageFolder newLocationSetting;
            bool isFlawless = true;

            if (!string.IsNullOrEmpty(DesktopL.Text))
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(DesktopL.Text);
                    localSettings.Values["DesktopLocation"] = DesktopL.Text;
                    DesktopL.BorderBrush = new SolidColorBrush(Colors.Black);
                }
                catch (ArgumentException)
                {
                    DesktopL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
                catch (FileNotFoundException)
                {
                    DesktopL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
            }

            if (!string.IsNullOrEmpty(DownloadsL.Text))
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(DownloadsL.Text);
                    localSettings.Values["DownloadsLocation"] = DownloadsL.Text;
                    DownloadsL.BorderBrush = new SolidColorBrush(Colors.Black);

                }
                catch (ArgumentException)
                {
                    DownloadsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
                catch (FileNotFoundException)
                {
                    DownloadsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
            }

            if (!string.IsNullOrEmpty(DocumentsL.Text))
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(DocumentsL.Text);
                    localSettings.Values["DocumentsLocation"] = DocumentsL.Text;
                    DocumentsL.BorderBrush = new SolidColorBrush(Colors.Black);
                }
                catch (ArgumentException)
                {
                    DocumentsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
                catch (FileNotFoundException)
                {
                    DocumentsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
            }

            if (!string.IsNullOrEmpty(PictureL.Text))
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(PictureL.Text);
                    localSettings.Values["PicturesLocation"] = PictureL.Text;
                    PictureL.BorderBrush = new SolidColorBrush(Colors.Black);
                }
                catch (ArgumentException)
                {
                    PictureL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
                catch (FileNotFoundException)
                {
                    PictureL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
            }

            if (!string.IsNullOrEmpty(MusicL.Text))
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(MusicL.Text);
                    localSettings.Values["MusicLocation"] = MusicL.Text;
                    MusicL.BorderBrush = new SolidColorBrush(Colors.Black);
                }
                catch (ArgumentException)
                {
                    MusicL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
                catch (FileNotFoundException)
                {
                    MusicL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
            }

            if (!string.IsNullOrEmpty(VideosL.Text))
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(VideosL.Text);
                    localSettings.Values["VideosLocation"] = VideosL.Text;
                    VideosL.BorderBrush = new SolidColorBrush(Colors.Black);
                }
                catch (ArgumentException)
                {
                    VideosL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
                catch (FileNotFoundException)
                {
                    VideosL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
            }

            if (!string.IsNullOrEmpty(OneDriveL.Text))
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(OneDriveL.Text);
                    localSettings.Values["OneDriveLocation"] = OneDriveL.Text;
                    OneDriveL.BorderBrush = new SolidColorBrush(Colors.Black);
                }
                catch (ArgumentException)
                {
                    OneDriveL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
                catch (FileNotFoundException)
                {
                    OneDriveL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    isFlawless = false;
                }
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
    }
}
