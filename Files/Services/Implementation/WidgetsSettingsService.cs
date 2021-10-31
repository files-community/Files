using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class WidgetsSettingsService : BaseObservableJsonSettingsModel, IWidgetsSettingsService
    {
        public WidgetsSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowFoldersWidget):
                case nameof(ShowRecentFilesWidget):
                case nameof(ShowDrivesWidget):
                case nameof(ShowBundlesWidget):
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{nameof(e.settingName)} {e.newValue}");
                    break;
            }
            base.RaiseOnSettingChangedEvent(sender, e);
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
