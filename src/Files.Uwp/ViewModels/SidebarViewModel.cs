using Files.DataModels.NavigationControlItems;
using Files.Filesystem;
using Files.Helpers;
using Files.Services;
using Files.UserControls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace Files.ViewModels
{
    public class SidebarViewModel : ObservableObject, IDisposable
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();
        public ICommand EmptyRecycleBinCommand { get; private set; }

        private IPaneHolder paneHolder;

        public IPaneHolder PaneHolder
        {
            get => paneHolder;
            set => SetProperty(ref paneHolder, value);
        }

        public IFilesystemHelpers FilesystemHelpers => PaneHolder?.FilesystemHelpers;

        public static readonly GridLength CompactSidebarWidth = SidebarControl.GetSidebarCompactSize();

        private NavigationViewDisplayMode sidebarDisplayMode;

        public NavigationViewDisplayMode SidebarDisplayMode
        {
            get => sidebarDisplayMode;
            set
            {
                if (SetProperty(ref sidebarDisplayMode, value))
                {
                    OnPropertyChanged(nameof(IsSidebarCompactSize));
                    UpdateTabControlMargin();
                }
            }
        }

        public bool IsSidebarCompactSize => SidebarDisplayMode == NavigationViewDisplayMode.Compact || SidebarDisplayMode == NavigationViewDisplayMode.Minimal;

        public void NotifyInstanceRelatedPropertiesChanged(string arg)
        {
            UpdateSidebarSelectedItemFromArgs(arg);

            OnPropertyChanged(nameof(SidebarSelectedItem));
        }

        public void UpdateSidebarSelectedItemFromArgs(string arg)
        {
            var value = arg;

            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = UserControls.SidebarControl.SideBarItems
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .Concat(UserControls.SidebarControl.SideBarItems.Where(x => (x as LocationItem)?.ChildItems != null).SelectMany(x => (x as LocationItem).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
                .ToList();

            if (string.IsNullOrEmpty(value))
            {
                //SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home".GetLocalized()));
                return;
            }

            item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                if (value == "Home".GetLocalized())
                {
                    item = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home".GetLocalized()));
                }
            }

            if (SidebarSelectedItem != item)
            {
                SidebarSelectedItem = item;
            }
        }

        public bool IsSidebarOpen
        {
            get => UserSettingsService.AppearanceSettingsService.IsSidebarOpen;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.IsSidebarOpen)
                {
                    UserSettingsService.AppearanceSettingsService.IsSidebarOpen = value;
                    OnPropertyChanged();
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
                }
            }
        }

        public bool ShowFileTagsSection
        {
            get => UserSettingsService.AppearanceSettingsService.ShowFileTagsSection;
            set
            {
                if (value != UserSettingsService.AppearanceSettingsService.ShowFileTagsSection)
                {
                    UserSettingsService.AppearanceSettingsService.ShowFileTagsSection = value;
                    App.FileTagsManager.UpdateFileTagsSectionVisibility();
                }
            }
        }

        private INavigationControlItem selectedSidebarItem;

        public INavigationControlItem SidebarSelectedItem
        {
            get => selectedSidebarItem;
            set => SetProperty(ref selectedSidebarItem, value);
        }

        public SidebarViewModel()
        {
            EmptyRecycleBinCommand = new RelayCommand<RoutedEventArgs>(EmptyRecycleBin);
            UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
        }

        public async void EmptyRecycleBin(RoutedEventArgs e)
        {
            await RecycleBinHelpers.S_EmptyRecycleBin();
        }

        private void UserSettingsService_OnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(UserSettingsService.AppearanceSettingsService.IsSidebarOpen):
                    if (UserSettingsService.AppearanceSettingsService.IsSidebarOpen != IsSidebarOpen)
                    {
                        OnPropertyChanged(nameof(IsSidebarOpen));
                    }
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowFavoritesSection):
                    OnPropertyChanged(nameof(ShowFavoritesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowLibrarySection):
                    OnPropertyChanged(nameof(ShowLibrarySection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection):
                    OnPropertyChanged(nameof(ShowCloudDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowDrivesSection):
                    OnPropertyChanged(nameof(ShowDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection):
                    OnPropertyChanged(nameof(ShowNetworkDrivesSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowWslSection):
                    OnPropertyChanged(nameof(ShowWslSection));
                    break;
                case nameof(UserSettingsService.AppearanceSettingsService.ShowFileTagsSection):
                    OnPropertyChanged(nameof(ShowFileTagsSection));
                    break;
            }
        }

        public void Dispose()
        {
            UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
        }

        public void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            SidebarDisplayMode = args.DisplayMode;
        }

        public void UpdateTabControlMargin()
        {
            TabControlMargin = SidebarDisplayMode switch
            {
                // This prevents the pane toggle button from overlapping the tab control in minimal mode
                NavigationViewDisplayMode.Minimal => new GridLength(44, GridUnitType.Pixel),
                _ => new GridLength(0, GridUnitType.Pixel),
            };
        }

        private GridLength tabControlMargin;

        public GridLength TabControlMargin
        {
            get => tabControlMargin;
            set => SetProperty(ref tabControlMargin, value);
        }
    }
}