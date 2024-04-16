using Files.Backend.Services.Settings;
using Files.Uwp.Serialization;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;

namespace Files.Uwp.ServicesImplementation.Settings
{
    internal sealed class MultitaskingSettingsService : BaseObservableJsonSettings, IMultitaskingSettingsService
    {
        public MultitaskingSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Register root
            RegisterSettingsContext(settingsSharingContext);
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

        protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.SettingName)
            {
                case nameof(IsVerticalTabFlyoutEnabled):
                case nameof(IsDualPaneEnabled):
                case nameof(AlwaysOpenDualPaneInNewTab):
                    Analytics.TrackEvent($"{e.SettingName} {e.NewValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(IsVerticalTabFlyoutEnabled)}, {IsVerticalTabFlyoutEnabled}");
            Analytics.TrackEvent($"{nameof(IsDualPaneEnabled)}, {IsDualPaneEnabled}");
            Analytics.TrackEvent($"{nameof(AlwaysOpenDualPaneInNewTab)}, {AlwaysOpenDualPaneInNewTab}");
        }
    }
}
