using System;
using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class SidebarSettingsService : BaseJsonSettingsModel, ISidebarSettingsService
    {
        public SidebarSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        #region Internal Settings

        public double SidebarWidthPx
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

        public bool PinRecycleBinToSideBar
        {
            get => Get(true);
            set => Set(value);
        }
    }
}
