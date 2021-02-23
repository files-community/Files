using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class SidebarViewModel : ObservableObject
    {
        private bool showSidebarFavoritesExpanded = App.AppSettings.IsSidebarFavoritesExpanded;
        private bool showSidebarDrivesExpanded = App.AppSettings.IsSidebarDrivesExpanded;
        private bool showSidebarCloudDrivesExpanded = App.AppSettings.IsSidebarCloudDrivesExpanded;
        private bool showSidebarNetworkExpanded = App.AppSettings.IsSidebarNetworkExpanded;

        public bool ShowSidebarFavoritesExpanded
        {
            get
            {
                return showSidebarFavoritesExpanded;
            }
            set
            {
                if (SetProperty(ref showSidebarFavoritesExpanded, value))
                {
                    App.AppSettings.IsSidebarFavoritesExpanded = value;
                }
            }
        }

        public bool ShowSidebarDrivesExpanded
        {
            get
            {
                return showSidebarDrivesExpanded;
            }
            set
            {
                if (SetProperty(ref showSidebarDrivesExpanded, value))
                {
                    App.AppSettings.IsSidebarDrivesExpanded = value;
                }
            }
        }

        public bool ShowSidebarCloudDrivesExpanded
        {
            get
            {
                return showSidebarCloudDrivesExpanded;
            }
            set
            {
                if (SetProperty(ref showSidebarCloudDrivesExpanded, value))
                {
                    App.AppSettings.IsSidebarCloudDrivesExpanded = value;
                }
            }
        }

        public bool ShowSidebarNetworkExpanded
        {
            get
            {
                return showSidebarNetworkExpanded;
            }
            set
            {
                if (SetProperty(ref showSidebarNetworkExpanded, value))
                {
                    App.AppSettings.IsSidebarNetworkExpanded = value;
                }
            }
        }
    }
}