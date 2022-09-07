using Files.Backend.Services.Settings;
using Files.App.Serialization;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;

namespace Files.App.ServicesImplementation.Settings
{
    internal sealed class MultitaskingSettingsService : BaseObservableJsonSettings, IMultitaskingSettingsService
    {
        public MultitaskingSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Register root
            RegisterSettingsContext(settingsSharingContext);
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

        protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(IsDualPaneEnabled)} {IsDualPaneEnabled}");
            Analytics.TrackEvent($"{nameof(AlwaysOpenDualPaneInNewTab)} {AlwaysOpenDualPaneInNewTab}");
        }
    }
}
