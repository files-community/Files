using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.Uwp.Serialization;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.Uwp.ServicesImplementation.Settings
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

        public bool PinRecycleBinToSidebar
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
                case nameof(PinRecycleBinToSidebar):
                case nameof(UseCompactStyles):
                    Analytics.TrackEvent($"{e.SettingName} {e.NewValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(MoveOverflowMenuItemsToSubMenu)}, {MoveOverflowMenuItemsToSubMenu}");
            Analytics.TrackEvent($"{nameof(ShowFavoritesSection)}, {ShowFavoritesSection}");
            Analytics.TrackEvent($"{nameof(ShowLibrarySection)}, {ShowLibrarySection}");
            Analytics.TrackEvent($"{nameof(ShowCloudDrivesSection)}, {ShowCloudDrivesSection}");
            Analytics.TrackEvent($"{nameof(ShowNetworkDrivesSection)}, {ShowNetworkDrivesSection}");
            Analytics.TrackEvent($"{nameof(ShowWslSection)}, {ShowWslSection}");
            Analytics.TrackEvent($"{nameof(ShowFileTagsSection)}, {ShowFileTagsSection}");
            Analytics.TrackEvent($"{nameof(PinRecycleBinToSidebar)}, {PinRecycleBinToSidebar}");
            Analytics.TrackEvent($"{nameof(UseCompactStyles)}, {UseCompactStyles}");
        }
    }
}
