using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Files.ViewModels.SettingsViewModels
{
    public class SidebarViewModel : ObservableObject
    {
        private bool pinRecycleBinToSideBar = App.AppSettings.PinRecycleBinToSideBar;
        private bool showLibrarySection = App.AppSettings.ShowLibrarySection;

        public static LibraryManager LibraryManager { get; private set; }

        public SidebarViewModel()
        {
            LibraryManager ??= new LibraryManager();
        }

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

    }
}