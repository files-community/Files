using Files.DataModels;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
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
            var selectedValue = ((sender as ComboBox).SelectedValue as DefaultLanguageModel).ID;
            if (AppSettings.CurrentLanguage.ID != selectedValue)
            {
                RestartDialog.Show();
            }
            else
            {
                RestartDialog.Dismiss();
            }
        }

        private void ShowRestartNotification()
        {
            ContentDialog restartDialog = new ContentDialog
            {
                Title = ResourceController.GetTranslation("RestartDialogTitle"),
                Content = ResourceController.GetTranslation("RestartDialogText"),
                PrimaryButtonText = ResourceController.GetTranslation("RestartDialogPrimaryButton"),
                CloseButtonText = ResourceController.GetTranslation("RestartDialogCancelButton")
            };

            var toastContent = new ToastContent
            {
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = ResourceController.GetTranslation("RestartDialogTitle")
                            },
                            new AdaptiveText
                            {
                                Text = ResourceController.GetTranslation("RestartDialogText")
                            }
                        }
                    }
                },
                Actions = new ToastActionsCustom
                {
                    Buttons =
                    {
                        new ToastButton(ResourceController.GetTranslation("RestartDialogPrimaryButton"), "restart")
                        {
                            ActivationType = ToastActivationType.Background
                        }
                    }
                }
            };

            // Create the toast notification
            var toastNotif = new ToastNotification(toastContent.GetXml());

            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
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