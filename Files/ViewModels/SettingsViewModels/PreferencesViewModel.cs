using Files.DataModels;
using Files.Enums;
using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;

namespace Files.ViewModels.SettingsViewModels
{
    public class PreferencesViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private int selectedLanguageIndex = App.AppSettings.DefaultLanguages.IndexOf(App.AppSettings.DefaultLanguage);
        private bool showRestartControl;
        private Terminal selectedTerminal = App.TerminalController.Model.GetDefaultTerminal();
        private int selectedDateFormatIndex = (int)Enum.Parse(typeof(TimeStyle), App.AppSettings.DisplayedTimeStyle.ToString());

        public ICommand EditTerminalApplicationsCommand { get; }

        public PreferencesViewModel()
        {
            DefaultLanguages = App.AppSettings.DefaultLanguages;
            Terminals = App.TerminalController.Model.Terminals;
            DateFormats = new List<string>
            {
                "ApplicationTimeStye".GetLocalized(),
                "SystemTimeStye".GetLocalized()
            };

            EditTerminalApplicationsCommand = new AsyncRelayCommand(LaunchTerminalsConfigFile);
        }

        public List<string> DateFormats { get; set; }

        public int SelectedDateFormatIndex
        {
            get
            {
                return selectedDateFormatIndex;
            }
            set
            {
                if (SetProperty(ref selectedDateFormatIndex, value))
                {
                    App.AppSettings.DisplayedTimeStyle = (TimeStyle)value;
                }
            }
        }

        public ObservableCollection<DefaultLanguageModel> DefaultLanguages { get; set; }

        public int SelectedLanguageIndex
        {
            get { return selectedLanguageIndex; }
            set
            {
                if (SetProperty(ref selectedLanguageIndex, value))
                {
                    App.AppSettings.DefaultLanguage = DefaultLanguages[value];

                    if (App.AppSettings.CurrentLanguage.ID != DefaultLanguages[value].ID)
                    {
                        ShowRestartControl = true;
                    }
                    else
                    {
                        ShowRestartControl = false;
                    }
                }
            }
        }

        public bool ShowRestartControl
        {
            get => showRestartControl;
            set => SetProperty(ref showRestartControl, value);
        }

        public List<Terminal> Terminals { get; set; }

        public Terminal SelectedTerminal
        {
            get { return selectedTerminal; }
            set
            {
                if (SetProperty(ref selectedTerminal, value))
                {
                    App.TerminalController.Model.DefaultTerminalName = value.Name;
                    App.TerminalController.SaveModel();
                }
            }
        }

        public bool ShowConfirmDeleteDialog
        {
            get => UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog)
                {
                    UserSettingsService.PreferencesSettingsService.ShowConfirmDeleteDialog = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OpenFoldersNewTab
        {
            get => UserSettingsService.PreferencesSettingsService.OpenFoldersInNewTab;
            set
            {
                if (value != UserSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                {
                    UserSettingsService.PreferencesSettingsService.OpenFoldersInNewTab = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LaunchTerminalsConfigFile()
        {
            var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json"));
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