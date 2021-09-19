using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class AppearanceSettingsService : BaseJsonSettingsModel, IAppearanceSettingsService
    {
        public AppearanceSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public bool MoveOverflowMenuItemsToSubMenu
        {
            get => Get(true);
            set => Set(value);
        }
    }
}
