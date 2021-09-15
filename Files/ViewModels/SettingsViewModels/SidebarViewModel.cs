using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;

namespace Files.ViewModels.SettingsViewModels
{
    public class SidebarViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public bool ShowFavoritesSection
        {
            get => UserSettingsService.SidebarSettingsService.ShowFavoritesSection;
            set
            {
                if (value != UserSettingsService.SidebarSettingsService.ShowFavoritesSection)
                {
                    UserSettingsService.SidebarSettingsService.ShowFavoritesSection = value;
                    App.SidebarPinnedController.Model.UpdateFavoritesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool PinRecycleBinToSideBar
        {
            get => UserSettingsService.SidebarSettingsService.PinRecycleBinToSideBar;
            set
            {
                if (value != UserSettingsService.SidebarSettingsService.PinRecycleBinToSideBar)
                {
                    UserSettingsService.SidebarSettingsService.PinRecycleBinToSideBar = value;
                    _= App.SidebarPinnedController.Model.ShowHideRecycleBinItemAsync(value);
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowLibrarySection
        {
            get => UserSettingsService.SidebarSettingsService.ShowLibrarySection;
            set
            {
                if (value != UserSettingsService.SidebarSettingsService.ShowLibrarySection)
                {
                    UserSettingsService.SidebarSettingsService.ShowLibrarySection = value;
                    App.LibraryManager.UpdateLibrariesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowDrivesSection
        {
            get => UserSettingsService.SidebarSettingsService.ShowDrivesSection;
            set
            {
                if (value != UserSettingsService.SidebarSettingsService.ShowDrivesSection)
                {
                    UserSettingsService.SidebarSettingsService.ShowDrivesSection = value;
                    App.DrivesManager.UpdateDrivesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowCloudDrivesSection
        {
            get => UserSettingsService.SidebarSettingsService.ShowCloudDrivesSection;
            set
            {
                if (value != UserSettingsService.SidebarSettingsService.ShowCloudDrivesSection)
                {
                    UserSettingsService.SidebarSettingsService.ShowCloudDrivesSection = value;
                    App.CloudDrivesManager.UpdateCloudDrivesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowNetworkDrivesSection
        {
            get => UserSettingsService.SidebarSettingsService.ShowNetworkDrivesSection;
            set
            {
                if (value != UserSettingsService.SidebarSettingsService.ShowNetworkDrivesSection)
                {
                    UserSettingsService.SidebarSettingsService.ShowNetworkDrivesSection = value;
                    App.NetworkDrivesManager.UpdateNetworkDrivesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowWslSection
        {
            get => UserSettingsService.SidebarSettingsService.ShowWslSection;
            set
            {
                if (value != UserSettingsService.SidebarSettingsService.ShowWslSection)
                {
                    UserSettingsService.SidebarSettingsService.ShowWslSection = value;
                    App.WSLDistroManager.UpdateWslSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }
    }
}