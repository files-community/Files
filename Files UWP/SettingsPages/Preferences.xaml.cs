using Windows.Storage;
using Windows.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.IO;
using Files.Filesystem;

namespace Files.SettingsPages
{
    
    public sealed partial class Preferences : Page
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        public Preferences()
        {
            this.InitializeComponent();

            if (localSettings.Values["customLocationsSetting"] != null)
            {
                if (localSettings.Values["customLocationsSetting"].Equals(true))
                {
                    CustomLocationToggle.IsOn = true;
                    DownloadsL.IsEnabled = true;
                    DocumentsL.IsEnabled = true;
                    PictureL.IsEnabled = true;
                    MusicL.IsEnabled = true;
                    VideosL.IsEnabled = true;
                    SaveCustomL.IsEnabled = true;
                }
                else
                {
                    CustomLocationToggle.IsOn = false;
                    DownloadsL.IsEnabled = false;
                    DocumentsL.IsEnabled = false;
                    PictureL.IsEnabled = false;
                    MusicL.IsEnabled = false;
                    VideosL.IsEnabled = false;
                    SaveCustomL.IsEnabled = false;
                }
            }
            else
            {
                CustomLocationToggle.IsOn = false;
                DownloadsL.IsEnabled = false;
                DocumentsL.IsEnabled = false;
                PictureL.IsEnabled = false;
                MusicL.IsEnabled = false;
                VideosL.IsEnabled = false;
                SaveCustomL.IsEnabled = false;
            }
            SuccessMark.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void ToggleSwitch_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if ((sender as ToggleSwitch).IsOn)
            {
                localSettings.Values["customLocationsSetting"] = true;
                DownloadsL.IsEnabled = true;
                DocumentsL.IsEnabled = true;
                PictureL.IsEnabled = true;
                MusicL.IsEnabled = true;
                VideosL.IsEnabled = true;
                SaveCustomL.IsEnabled = true;
            }
            else
            {
                localSettings.Values["customLocationsSetting"] = false;
                DownloadsL.IsEnabled = false;
                DocumentsL.IsEnabled = false;
                PictureL.IsEnabled = false;
                MusicL.IsEnabled = false;
                VideosL.IsEnabled = false;
                SaveCustomL.IsEnabled = false;
            }
        }

        private async void SaveCustomL_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            StorageFolder newLocationSetting;
            if(DownloadsL.Text != null)
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(DownloadsL.Text);
                    localSettings.Values["DownloadsLocation"] = DownloadsL.Text;
                }
                catch (UnauthorizedAccessException)
                {
                    await ItemViewModel<Preferences>.GetCurrentSelectedTabInstance<ProHome>().permissionBox.ShowAsync();
                    return;
                }
                catch (ArgumentException)
                {
                    DownloadsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
                catch (FileNotFoundException)
                {
                    DownloadsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
            }

            if (DocumentsL.Text != null)
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(DocumentsL.Text);
                    localSettings.Values["DocumentsLocation"] = DocumentsL.Text;
                }
                catch (UnauthorizedAccessException)
                {
                    await ItemViewModel<Preferences>.GetCurrentSelectedTabInstance<ProHome>().permissionBox.ShowAsync();
                    return;
                }
                catch (ArgumentException)
                {
                    DocumentsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
                catch (FileNotFoundException)
                {
                    DocumentsL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
            }

            if (PictureL.Text != null)
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(PictureL.Text);
                    localSettings.Values["PicturesLocation"] = PictureL.Text;
                }
                catch (UnauthorizedAccessException)
                {
                    await ItemViewModel<Preferences>.GetCurrentSelectedTabInstance<ProHome>().permissionBox.ShowAsync();
                    return;
                }
                catch (ArgumentException)
                {
                    PictureL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
                catch (FileNotFoundException)
                {
                    PictureL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
            }

            if (MusicL.Text != null)
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(MusicL.Text);
                    localSettings.Values["MusicLocation"] = MusicL.Text;
                }
                catch (UnauthorizedAccessException)
                {
                    await ItemViewModel<Preferences>.GetCurrentSelectedTabInstance<ProHome>().permissionBox.ShowAsync();
                    return;
                }
                catch (ArgumentException)
                {
                    MusicL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
                catch (FileNotFoundException)
                {
                    MusicL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
            }

            if (VideosL.Text != null)
            {
                try
                {
                    newLocationSetting = await StorageFolder.GetFolderFromPathAsync(VideosL.Text);
                    localSettings.Values["VideosLocation"] = VideosL.Text;
                }
                catch (UnauthorizedAccessException)
                {
                    await ItemViewModel<Preferences>.GetCurrentSelectedTabInstance<ProHome>().permissionBox.ShowAsync();
                    return;
                }
                catch (ArgumentException)
                {
                    VideosL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
                catch (FileNotFoundException)
                {
                    VideosL.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                }
            }

            SuccessMark.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
    }
}
