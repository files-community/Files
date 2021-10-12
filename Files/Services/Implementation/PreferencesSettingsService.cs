using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class PreferencesSettingsService : BaseJsonSettingsModel, IPreferencesSettingsService
    {
        public PreferencesSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowConfirmDeleteDialog):
                case nameof(OpenFoldersInNewTab):
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{nameof(e.settingName)} {e.newValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public bool ShowConfirmDeleteDialog
        {
            get => Get(true);
            set => Set(value);
        }

        public bool OpenFoldersInNewTab
        {
            get => Get(false);
            set => Set(value);
        }
    }
}
