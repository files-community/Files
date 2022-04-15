using Files.Uwp.Helpers;
using Files.Backend.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;

namespace Files.Uwp.ViewModels.SettingsViewModels
{
    public class AppearanceViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
        private AppTheme selectedTheme = App.AppSettings.SelectedTheme;

        public AppearanceViewModel()
        {
            Themes = new List<string>()
            {
                "Default".GetLocalized(),
                "LightTheme".GetLocalized(),
                "DarkTheme".GetLocalized()
            };
        }

        public List<string> Themes { get; set; }
        public List<AppTheme> CustomThemes => App.ExternalResourcesHelper.Themes;

        public int SelectedThemeIndex
        {
            get => selectedThemeIndex;
            set
            {
                if (SetProperty(ref selectedThemeIndex, value))
                {
                    ThemeHelper.RootTheme = (ElementTheme)value;
                    OnPropertyChanged(nameof(SelectedElementTheme));
                }
            }
        }

        public ElementTheme SelectedElementTheme
        {
            get => (ElementTheme)selectedThemeIndex;
        }

        public bool MoveOverflowMenuItemsToSubMenu
        {
            get => UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu)
                {
                    UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu = value;
                    OnPropertyChanged();
                }
            }
        }

        public AppTheme SelectedTheme
        {
            get
            {
                return selectedTheme;
            }
            set
            {
                if (SetProperty(ref selectedTheme, value))
                {
                    if (selectedTheme != null)
                    {
                        // Remove the old resource file and load the new file
                        App.ExternalResourcesHelper.UpdateTheme(App.AppSettings.SelectedTheme, selectedTheme)
                            .ContinueWith(t =>
                            {
                                App.AppSettings.SelectedTheme = selectedTheme;
                                ThemeHelper.UpdateTheme(); // Force the application to use the correct resource file
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                }
            }
        }

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
                    _ = App.SidebarPinnedController.Model.ShowHideRecycleBinItemAsync(value);
                    OnPropertyChanged();
                }
            }
        }

        public bool UseCompactStyles
        {
            get => UserSettingsService.AppearanceSettingsService.UseCompactStyles;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.UseCompactStyles)
                {
                    UserSettingsService.AppearanceSettingsService.UseCompactStyles = value;

                    //Apply the correct styles
                    ThemeHelper.SetCompactStyles(value, updateTheme: true);

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

        public bool AreFileTagsEnabled => UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled;

        public bool ShowFileTagsSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowFileTagsSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowFileTagsSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowFileTagsSection = value;
                    App.FileTagsManager.UpdateFileTagsSectionVisibility();
                    OnPropertyChanged();
                }
            }
        }

        public async Task OpenThemesFolder()
        {
            await CoreApplication.MainView.Dispatcher.YieldAsync();
            await NavigationHelpers.OpenPathInNewTab(App.ExternalResourcesHelper.ImportedThemesFolder.Path);
        }
    }
}