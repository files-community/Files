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

        protected override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(MoveOverflowMenuItemsToSubMenu):
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{nameof(e.settingName)} {e.newValue}");
                    break;
            }
            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public bool MoveOverflowMenuItemsToSubMenu
        {
            get => Get(true);
            set => Set(value);
        }
    }
}
