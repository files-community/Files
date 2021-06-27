using Files.Filesystem;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;

namespace Files.ViewModels.SettingsViewModels
{
    public class SidebarViewModel : ObservableObject
    {
        private bool pinRecycleBinToSideBar = App.AppSettings.PinRecycleBinToSideBar;
        private bool showLibrarySection = App.AppSettings.ShowLibrarySection;
        private bool showFavoritesSection = App.AppSettings.ShowFavoritesSection;
        private bool showDrivesSection = App.AppSettings.ShowDrivesSection;
        private bool showCloudDrivesSection = App.AppSettings.ShowCloudDrivesSection;
        private bool showNetworkDrivesSection = App.AppSettings.ShowNetworkDrivesSection;

        public static LibraryManager LibraryManager { get; private set; }
        public static FavoritesManager FavoritesManager { get; private set; }
        public static CloudDrivesManager CloudDrivesManager { get; private set; }
        public static DrivesManager DrivesManager { get; private set; }
        public static NetworkDrivesManager NetworkDrivesManager { get; private set; }

        public SidebarViewModel()
        {
            LibraryManager ??= new LibraryManager();
            FavoritesManager ??= new FavoritesManager();
            CloudDrivesManager ??= new CloudDrivesManager();
            DrivesManager ??= new DrivesManager();
            NetworkDrivesManager ??= new NetworkDrivesManager();
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
                    LibraryManager.UpdateLibrariesSectionVisibility();
                }
            }
        }

        public bool ShowFavoritesSection
        {
            get
            {
                return showFavoritesSection;
            }
            set
            {
                if (SetProperty(ref showFavoritesSection, value))
                {
                    App.AppSettings.ShowFavoritesSection = value;
                    FavoritesManager.UpdateFavoritesSectionVisibility();
                }
            }
        }

        public bool ShowDrivesSection
        {
            get
            {
                return showDrivesSection;
            }
            set
            {
                if (SetProperty(ref showDrivesSection, value))
                {
                    App.AppSettings.ShowDrivesSection = value;
                    DrivesManager.UpdateDrivesSectionVisibility();
                }
            }
        }

        public bool ShowCloudDrivesSection
        {
            get
            {
                return showCloudDrivesSection;
            }
            set
            {
                if (SetProperty(ref showCloudDrivesSection, value))
                {
                    App.AppSettings.ShowCloudDrivesSection = value;
                    CloudDrivesManager.UpdateCloudDrivesSectionVisibility();
                }
            }
        }

        public bool ShowNetworkDrivesSection
        {
            get
            {
                return showNetworkDrivesSection;
            }
            set
            {
                if (SetProperty(ref showNetworkDrivesSection, value))
                {
                    App.AppSettings.ShowNetworkDrivesSection = value;
                    NetworkDrivesManager.UpdateNetworkDrivesSectionVisibility();
                }
            }
        }
    }
}