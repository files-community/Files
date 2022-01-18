﻿using Files.Models.JsonSettings;
using Microsoft.AppCenter.Analytics;

namespace Files.Services.Implementation
{
    public class MultitaskingSettingsService : BaseObservableJsonSettingsModel, IMultitaskingSettingsService
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

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(IsVerticalTabFlyoutEnabled):
                case nameof(IsDualPaneEnabled):
                case nameof(AlwaysOpenDualPaneInNewTab):
                    Analytics.TrackEvent($"{e.settingName} {e.newValue}");
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
