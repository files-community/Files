using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class SidebarViewModel : ObservableObject
    {
        private bool showSidebarFavoritesExpanded = App.AppSettings.ShowSidebarFavoritesExpanded;
        private bool showSidebarDrivesExpanded = App.AppSettings.ShowSidebarDrivesExpanded;
        private bool showSidebarCloudDrivesExpanded = App.AppSettings.ShowSidebarCloudDrivesExpanded;
        private bool showSidebarNetworkExpanded = App.AppSettings.ShowSidebarNetworkExpanded;

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
                    App.AppSettings.ShowSidebarFavoritesExpanded = value;
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
                    App.AppSettings.ShowSidebarDrivesExpanded = value;
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
                    App.AppSettings.ShowSidebarCloudDrivesExpanded = value;
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
                    App.AppSettings.ShowSidebarNetworkExpanded = value;
                }
            }
        }
    }
}