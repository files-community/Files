using Files.Backend.EventArguments;
using Files.Backend.Models.JsonSettings;
using Files.Backend.Services.Settings;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.ServicesImplementation.SettingsServices
{
    public class AppearanceSettingsService : BaseObservableJsonSettingsModel, IAppearanceSettingsService
    {
        public AppearanceSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        #region Internal Settings

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

        #endregion

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

        public override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(MoveOverflowMenuItemsToSubMenu):
                case nameof(ShowFavoritesSection):
                case nameof(ShowLibrarySection):
                case nameof(ShowCloudDrivesSection):
                case nameof(ShowNetworkDrivesSection):
                case nameof(ShowWslSection):
                case nameof(PinRecycleBinToSidebar):
                    Analytics.TrackEvent($"{e.settingName} {e.newValue}");
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
            Analytics.TrackEvent($"{nameof(PinRecycleBinToSidebar)}, {PinRecycleBinToSidebar}");
        }
    }
}
