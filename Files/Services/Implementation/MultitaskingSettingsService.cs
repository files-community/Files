using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class MultitaskingSettingsService : BaseJsonSettingsModel, IMultitaskingSettingsService
    {
        public MultitaskingSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public bool IsVerticalTabFlyoutEnabled
        {
            get => Get(true);
            set => Set(value);
        }

        public bool IsDualPaneEnabled
        {
            get => Get(false);
            set => Set(value);
        }

        public bool AlwaysOpenDualPaneInNewTab
        {
            get => Get(false);
            set => Set(value);
        }
    }
}
