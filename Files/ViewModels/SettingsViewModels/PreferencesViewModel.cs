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
        private bool showAllContextMenuItems = App.AppSettings.ShowAllContextMenuItems;
        private bool showCopyLocationOption = App.AppSettings.ShowCopyLocationOption;

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
                selectedLanguageIndex = value;
                OnPropertyChanged(nameof(SelectedLanguageIndex));

                App.AppSettings.DefaultLanguage = DefaultLanguages[SelectedLanguageIndex];

                if (App.AppSettings.CurrentLanguage.ID != DefaultLanguages[SelectedLanguageIndex].ID)
                {
                    ShowRestartDialog = true;
                }
                else
                {
                    ShowRestartDialog = false;
                }
            }
        }

        public bool ShowRestartDialog
        {
            get { return showRestartDialog; }
            set
            {
                if (showRestartDialog != value)
                {
                    showRestartDialog = value;
                    OnPropertyChanged(nameof(ShowRestartDialog));
                }
            }
        }

        public List<Terminal> Terminals { get; set; }

        public Terminal SelectedTerminal
        {
            get { return selectedTerminal; }
            set
            {
                if (selectedTerminal != value)
                {
                    selectedTerminal = value;
                    OnPropertyChanged(nameof(SelectedTerminal));

                    App.AppSettings.TerminalController.Model.DefaultTerminalPath = selectedTerminal.Path;
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
                if (pinRecycleBinToSideBar != value)
                {
                    pinRecycleBinToSideBar = value;
                    App.AppSettings.PinRecycleBinToSideBar = pinRecycleBinToSideBar;
                    OnPropertyChanged(nameof(PinRecycleBinToSideBar));
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
                if (showConfirmDeleteDialog != value)
                {
                    showConfirmDeleteDialog = value;
                    App.AppSettings.ShowConfirmDeleteDialog = showConfirmDeleteDialog;
                    OnPropertyChanged(nameof(ShowConfirmDeleteDialog));
                }
            }
        }

        public bool ShowAllContextMenuItems
        {
            get
            {
                return showAllContextMenuItems;
            }
            set
            {
                if (showAllContextMenuItems != value)
                {
                    showAllContextMenuItems = value;
                    App.AppSettings.ShowAllContextMenuItems = showAllContextMenuItems;
                    OnPropertyChanged(nameof(ShowAllContextMenuItems));
                }
            }
        }

        public bool ShowCopyLocationOption
        {
            get
            {
                return showCopyLocationOption;
            }
            set
            {
                if (showCopyLocationOption != value)
                {
                    showCopyLocationOption = value;
                    App.AppSettings.ShowCopyLocationOption = showCopyLocationOption;
                    OnPropertyChanged(nameof(ShowCopyLocationOption));
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
