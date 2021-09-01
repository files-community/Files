using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class SidebarViewModel : ObservableObject
    {
        private bool showFavoritesSection = App.AppSettings.ShowFavoritesSection;
        private bool pinRecycleBinToSideBar = App.AppSettings.PinRecycleBinToSideBar;
        private bool showLibrarySection = App.AppSettings.ShowLibrarySection;
        private bool showDrivesSection = App.AppSettings.ShowDrivesSection;
        private bool showCloudDrivesSection = App.AppSettings.ShowCloudDrivesSection;
        private bool showNetworkDrivesSection = App.AppSettings.ShowNetworkDrivesSection;
        private bool showWslSection = App.AppSettings.ShowWslSection;

        public SidebarViewModel()
        {
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
                    App.SidebarPinnedController.Model.UpdateFavoritesSectionVisibility();
                }
            }
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
                    App.LibraryManager.UpdateLibrariesSectionVisibility();
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
                    App.DrivesManager.UpdateDrivesSectionVisibility();
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
                    App.CloudDrivesManager.UpdateCloudDrivesSectionVisibility();
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
                    App.NetworkDrivesManager.UpdateNetworkDrivesSectionVisibility();
                }
            }
        }

        public bool ShowWslSection
        {
            get
            {
                return showWslSection;
            }
            set
            {
                if (SetProperty(ref showWslSection, value))
                {
                    App.AppSettings.ShowWslSection = value;
                    App.WSLDistroManager.UpdateWslSectionVisibility();
                }
            }
        }
    }
}