using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.App.ServicesImplementation.Settings
{
    internal sealed class AppearanceSettingsService : BaseObservableJsonSettings, IAppearanceSettingsService
    {
        public AppearanceSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Register root
            RegisterSettingsContext(settingsSharingContext);
        }

        public double SidebarWidth
        {
            get => Get(Math.Min(Math.Max(Get(255d), Constants.UI.MinimumSidebarWidth), 500d));
            set => Set(value);
        }

        public bool IsSidebarOpen
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowFavoritesSection
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowLibrarySection
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowDrivesSection
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowCloudDrivesSection
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowNetworkDrivesSection
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowWslSection
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowFileTagsSection
        {
            get => Get(true);
            set => Set(value);
        }

        public bool MoveOverflowMenuItemsToSubMenu
        {
            get => Get(true);
            set => Set(value);
        }

        public bool UseCompactStyles
        {
            get => Get(false);
            set => Set(value);
        }
        public bool ShowFoldersWidget
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowRecentFilesWidget
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowDrivesWidget
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowBundlesWidget
        {
            get => Get(false);
            set => Set(value);
        }

        public bool FoldersWidgetExpanded
        {
            get => Get(true);
            set => Set(value);
        }

        public bool RecentFilesWidgetExpanded
        {
            get => Get(true);
            set => Set(value);
        }

        public bool DrivesWidgetExpanded
        {
            get => Get(true);
            set => Set(value);
        }

        public bool BundlesWidgetExpanded
        {
            get => Get(true);
            set => Set(value);
        }

        protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.SettingName)
            {
                case nameof(MoveOverflowMenuItemsToSubMenu):
                case nameof(ShowFavoritesSection):
                case nameof(ShowLibrarySection):
                case nameof(ShowCloudDrivesSection):
                case nameof(ShowNetworkDrivesSection):
                case nameof(ShowWslSection):
                case nameof(ShowFileTagsSection):
                case nameof(UseCompactStyles):
                case nameof(ShowFoldersWidget):
                case nameof(ShowRecentFilesWidget):
                case nameof(ShowDrivesWidget):
                case nameof(ShowBundlesWidget):
                    Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }
    }
}
