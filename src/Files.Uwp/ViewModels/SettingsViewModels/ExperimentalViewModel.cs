using Files.Common;
using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;

namespace Files.ViewModels.SettingsViewModels
{
    public class ExperimentalViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public ICommand EditFileTagsCommand { get; }

        public ICommand SetAsDefaultExplorerCommand { get; }

        public ICommand SetAsOpenFileDialogCommand { get; }

        public ExperimentalViewModel()
        {
            IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
            IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();
            IsSetAsDefaultVisible = DetectIsSetAsDefaultVisible();

            EditFileTagsCommand = new AsyncRelayCommand(LaunchFileTagsConfigFile);
            SetAsDefaultExplorerCommand = new AsyncRelayCommand(SetAsDefaultExplorer);
            SetAsOpenFileDialogCommand = new AsyncRelayCommand(SetAsOpenFileDialog);
        }

        public bool AreFileTagsEnabled
        {
            get => UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled)
                {
                    UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowFolderSize
        {
            get => UserSettingsService.PreferencesSettingsService.ShowFolderSize;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ShowFolderSize)
                {
                    UserSettingsService.PreferencesSettingsService.ShowFolderSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LaunchFileTagsConfigFile()
        {
            var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/filetags.json"));
            if (!await Launcher.LaunchFileAsync(configFile))
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    await connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "InvokeVerb" },
                        { "FilePath", configFile.Path },
                        { "Verb", "open" }
                    });
                }
            }
        }

        private async Task SetAsDefaultExplorer()
        {
            if (IsSetAsDefaultFileManager == DetectIsSetAsDefaultFileManager())
            {
                return;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (_, _) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "SetAsDefaultExplorer" },
                    { "Value", IsSetAsDefaultFileManager }
                });
            }
            IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
            if (!IsSetAsDefaultFileManager)
            {
                IsSetAsOpenFileDialog = false;
                await SetAsOpenFileDialog();
            }
        }

        private async Task SetAsOpenFileDialog()
        {
            if (IsSetAsOpenFileDialog == DetectIsSetAsOpenFileDialog())
            {
                return;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (_, _) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "SetAsOpenFileDialog" },
                    { "Value", IsSetAsOpenFileDialog }
                });
            }
            IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();
        }

        private bool DetectIsSetAsDefaultFileManager()
        {
            return ApplicationData.Current.LocalSettings.Values.Get("IsSetAsDefaultFileManager", false);
        }

        private bool DetectIsSetAsOpenFileDialog()
        {
            return ApplicationData.Current.LocalSettings.Values.Get("IsSetAsOpenFileDialog", false);
        }

        private bool DetectIsSetAsDefaultVisible()
        {
            if (Package.Current.Id.FamilyName == "49306atecsolution.FilesUWP_et10x9a9vyk8t" && !IsSetAsDefaultFileManager)
            {
                return false;
            }
            return true;
        }

        private bool isSetAsDefaultFileManager;

        public bool IsSetAsDefaultFileManager
        {
            get => isSetAsDefaultFileManager;
            set => SetProperty(ref isSetAsDefaultFileManager, value);
        }

        private bool isSetAsOpenFileDialog;

        public bool IsSetAsOpenFileDialog
        {
            get => isSetAsOpenFileDialog;
            set => SetProperty(ref isSetAsOpenFileDialog, value);
        }


        private bool isSetAsDefaultVisible;

        public bool IsSetAsDefaultVisible
        {
            get => isSetAsDefaultVisible;
            set => SetProperty(ref isSetAsDefaultVisible, value);
        }
    }
}