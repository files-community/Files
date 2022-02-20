using Files.Models.JsonSettings;
using Microsoft.AppCenter.Analytics;

namespace Files.Services.Implementation
{
    public class WidgetsSettingsService : BaseObservableJsonSettingsModel, IWidgetsSettingsService
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

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowFoldersWidget):
                case nameof(ShowRecentFilesWidget):
                case nameof(ShowDrivesWidget):
                case nameof(ShowBundlesWidget):
                    Analytics.TrackEvent($"{e.settingName} {e.newValue}");
                    break;
            }
            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(ShowFoldersWidget)}, {ShowFoldersWidget}");
            Analytics.TrackEvent($"{nameof(ShowRecentFilesWidget)}, {ShowRecentFilesWidget}");
            Analytics.TrackEvent($"{nameof(ShowDrivesWidget)}, {ShowDrivesWidget}");
            Analytics.TrackEvent($"{nameof(ShowBundlesWidget)}, {ShowBundlesWidget}");
        }
    }
}
