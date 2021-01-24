using Files.DataModels;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.System;

namespace Files.ViewModels.SettingsViewModels
{
    public class PreferencesViewModel : ObservableObject
    {
        private int selectedLanguageIndex = App.AppSettings.DefaultLanguages.IndexOf(App.AppSettings.DefaultLanguage);
        private bool showRestartDialog;
        private Terminal selectedTerminal = App.AppSettings.TerminalController.Model.GetDefaultTerminal();
        private bool pinRecycleBinToSideBar = App.AppSettings.PinRecycleBinToSideBar;
        private bool showConfirmDeleteDialog = App.AppSettings.ShowConfirmDeleteDialog;

        public PreferencesViewModel()
        {
            DefaultLanguages = App.AppSettings.DefaultLanguages;
            Terminals = App.AppSettings.TerminalController.Model.Terminals;
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
                        ShowRestartDialog = true;
                    }
                    else
                    {
                        ShowRestartDialog = false;
                    }
                }
            }
        }

        public bool ShowRestartDialog
        {
            get => showRestartDialog;
            set => SetProperty(ref showRestartDialog, value);
        }

        public List<Terminal> Terminals { get; set; }

        public Terminal SelectedTerminal
        {
            get { return selectedTerminal; }
            set
            {
                if (SetProperty(ref selectedTerminal, value))
                {
                    App.AppSettings.TerminalController.Model.DefaultTerminalName = value.Name;
                    App.AppSettings.TerminalController.SaveModel();
                }
            }
        }

        public RelayCommand EditTerminalApplicationsCommand => new RelayCommand(() => LaunchTerminalsConfigFile());

        public bool PinRecycleBinToSideBar
        {
            get
            {
                return pinRecycleBinToSideBar;
            }
            set
            {
                if (SetProperty(ref pinRecycleBinToSideBar, value))
                {
                    App.AppSettings.PinRecycleBinToSideBar = value;
                }
            }
        }

        public bool ShowConfirmDeleteDialog
        {
            get
            {
                return showConfirmDeleteDialog;
            }
            set
            {
                if (SetProperty(ref showConfirmDeleteDialog, value))
                {
                    App.AppSettings.ShowConfirmDeleteDialog = value;
                }
            }
        }

        private bool enableAdaptivePreviewPane = App.AppSettings.EnableAdaptivePreviewPane;

        public bool EnableAdaptivePreviewPane
        {
            get => enableAdaptivePreviewPane;
            set
            {
                if (SetProperty(ref enableAdaptivePreviewPane, value))
                {
                    App.AppSettings.EnableAdaptivePreviewPane = value;
                }
            }
        }

        private async void LaunchTerminalsConfigFile()
        {
            await Launcher.LaunchFileAsync(
                await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/terminal.json")));
        }
    }
}