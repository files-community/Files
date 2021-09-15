using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;

namespace Files.ViewModels.SettingsViewModels
{
    public class ExperimentalViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public bool AreFileTagsEnabled
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand EditFileTagsCommand { get; private set; }

        public ExperimentalViewModel()
        {
            EditFileTagsCommand = new AsyncRelayCommand(LaunchFileTagsConfigFile);
        }

        private async Task LaunchFileTagsConfigFile()
        {
            await Launcher.LaunchFileAsync(
                await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/filetags.json")));
        }
    }
}