using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.App.Serialization;
using Microsoft.AppCenter.Analytics;

namespace Files.App.ServicesImplementation.Settings
{
    internal sealed class WidgetsSettingsService : BaseObservableJsonSettings, IWidgetsSettingsService
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
