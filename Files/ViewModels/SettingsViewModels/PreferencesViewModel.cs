using Files.DataModels;
using Files.Filesystem;
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
        private bool showRestartControl;
        private Terminal selectedTerminal = App.AppSettings.TerminalController.Model.GetDefaultTerminal();
        private bool pinRecycleBinToSideBar = App.AppSettings.PinRecycleBinToSideBar;
        private bool showConfirmDeleteDialog = App.AppSettings.ShowConfirmDeleteDialog;
        private bool showLibrarySection = App.AppSettings.ShowLibrarySection;
        private bool openFoldersNewTab = App.AppSettings.OpenFoldersNewTab;

        public static LibraryManager LibraryManager { get; private set; }

        public PreferencesViewModel()
        {
            DefaultLanguages = App.AppSettings.DefaultLanguages;
            Terminals = App.AppSettings.TerminalController.Model.Terminals;

            LibraryManager ??= new LibraryManager();
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

        public bool ShowLibrarySection
        {
            get
            {
                return showLibrarySection;
            }
            set
            {
                if (SetProperty(ref showLibrarySection, value))
                {
                    App.AppSettings.ShowLibrarySection = value;
                    
                    LibraryVisibility(App.AppSettings.ShowLibrarySection);
                }
            }
        }

        public async void LibraryVisibility(bool visible)
        {
            if (visible)
            {
                await LibraryManager.EnumerateLibrariesAsync();
            }
            else
            {
                LibraryManager.RemoveLibrariesSideBarSection();
            }
        }

        public bool OpenFoldersNewTab
        {
            get
            {
                return openFoldersNewTab;
            }
            set
            {
                if (SetProperty(ref openFoldersNewTab, value))
                {
                    App.AppSettings.OpenFoldersNewTab = value;
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