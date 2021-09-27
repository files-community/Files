using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
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

        public IRelayCommand EditFileTagsCommand => new AsyncRelayCommand(() => LaunchFileTagsConfigFile());

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
    }
}