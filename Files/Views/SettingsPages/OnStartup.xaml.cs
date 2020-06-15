using Files.View_Models;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace Files.SettingsPages
{
    public sealed partial class OnStartup : Page
    {
        public SettingsViewModel AppSettings => App.AppSettings;
        public OnStartup()
        {
            InitializeComponent();
        }

        private async void btnOpenASpecificPageOnStartupPath_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                // Application now has read/write access to the picked file
                App.AppSettings.OpenASpecificPageOnStartupPath = folder.Path;
            }
        }
    }
}