using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;

namespace Files.ViewModels.SettingsViewModels
{
    public class ExperimentalViewModel : ObservableObject
    {
        private bool areFileTagsEnabled = App.AppSettings.AreFileTagsEnabled;

        public bool AreFileTagsEnabled
        {
            get
            {
                return areFileTagsEnabled;
            }
            set
            {
                if (SetProperty(ref areFileTagsEnabled, value))
                {
                    App.AppSettings.AreFileTagsEnabled = value;
                }
            }
        }

        public RelayCommand EditFileTagsCommand => new RelayCommand(() => LaunchFileTagsConfigFile());

        private async void LaunchFileTagsConfigFile()
        {
            await Launcher.LaunchFileAsync(
                await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/filetags.json")));
        }

        public RelayCommand SetAsDefaultExplorerCommand => new RelayCommand(() => SetAsDefaultExplorer());

        private async void SetAsDefaultExplorer()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (_, _) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "SetAsDefaultExplorer" },
                    { "Value", !IsSetAsDefaultFileManager }
                });
            }
            OnPropertyChanged(nameof(IsSetAsOpenFileDialog));
            OnPropertyChanged(nameof(IsSetAsDefaultFileManager));
        }

        public RelayCommand SetAsOpenFileDialogCommand => new RelayCommand(() => SetAsOpenFileDialog());

        private async void SetAsOpenFileDialog()
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (_, _) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "SetAsOpenFileDialog" },
                    { "Value", !IsSetAsOpenFileDialog }
                });
            }
            OnPropertyChanged(nameof(IsSetAsOpenFileDialog));
        }

        public bool IsSetAsDefaultFileManager
        {
            get
            {
                using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\Directory\shell");
                return subkey?.GetValue("") as string == "openinfiles";
            }
        }

        public bool IsSetAsOpenFileDialog
        {
            get
            {
                using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");
                return subkey?.GetValue("") as string == "FilesOpenDialog class";
            }
        }
    }
}