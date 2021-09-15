using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class WidgetsSettingsService : BaseJsonSettingsModel, IWidgetsSettingsService
    {
        public WidgetsSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
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
    }
}
