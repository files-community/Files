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
            get => UserSettingsService.AppearanceSettingsService.ShowFavoritesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowFavoritesSection = value;
                    App.SidebarPinnedController.Model.UpdateFavoritesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool PinRecycleBinToSideBar
        {
            get => UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar)
                {
                    UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar = value;
                    _= App.SidebarPinnedController.Model.ShowHideRecycleBinItemAsync(value);
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowLibrarySection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowLibrarySection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowLibrarySection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowLibrarySection = value;
                    App.LibraryManager.UpdateLibrariesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowDrivesSection = value;
                    App.DrivesManager.UpdateDrivesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowCloudDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection = value;
                    App.CloudDrivesManager.UpdateCloudDrivesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowNetworkDrivesSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection = value;
                    App.NetworkDrivesManager.UpdateNetworkDrivesSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowWslSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowWslSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowWslSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowWslSection = value;
                    App.WSLDistroManager.UpdateWslSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }
    }
}