using Files.DataModels;
using Files.View_Models;
using System;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files.SettingsPages
{
    public sealed partial class Preferences : Page
    {
        private SettingsViewModel AppSettings => App.AppSettings;

        public Preferences()
        {
            InitializeComponent();

            TryGetOneDriveFolder();
        }

        private async void TryGetOneDriveFolder()
        {
            try
            {
                await StorageFolder.GetFolderFromPathAsync(AppSettings.OneDrivePath);
            }
            catch
            {
                AppSettings.PinOneDriveToSideBar = false;
                OneDrivePin.IsEnabled = false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            TerminalApplicationsComboBox.SelectedItem = AppSettings.TerminalController.Model.GetDefaultTerminal();
        }

        private void ComboAppLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AppSettings.CurrentLanguage.ID != AppSettings.DefaultLanguage.ID)
            {
                ShowRestartDialog();
            }
        }

        private async void ShowRestartDialog()
        {
            ContentDialog restartDialog = new ContentDialog
            {
                Title = ResourceController.GetTranslation("RestartDialogTitle"),
                Content = ResourceController.GetTranslation("RestartDialogText"),
                PrimaryButtonText = ResourceController.GetTranslation("RestartDialogPrimaryButton"),
                CloseButtonText = ResourceController.GetTranslation("RestartDialogCancelButton")
            };

            ContentDialogResult result = await restartDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                System.Diagnostics.Debug.WriteLine("Restart app");
                AppRestartFailureReason failureReason = await CoreApplication.RequestRestartAsync("");
                if (failureReason == AppRestartFailureReason.NotInForeground)
                {

                }
            }
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

            var selectedTerminal = (Terminal)comboBox.SelectedItem;

            AppSettings.TerminalController.Model.DefaultTerminalPath = selectedTerminal.Path;

            SaveTerminalSettings();
        }

        private void SaveTerminalSettings()
        {
            App.AppSettings.TerminalController.SaveModel();
        }
    }
}