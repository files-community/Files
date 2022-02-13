using Files.Backend.EventArguments;
using Files.Backend.Models.JsonSettings;
using Files.Backend.Services.Settings;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.Services.Implementation
{
    public class PreviewPaneSettingsService : BaseObservableJsonSettingsModel, IPreviewPaneSettingsService
    {
        public PreviewPaneSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public double PreviewPaneMediaVolume
        {
            get => Math.Min(Math.Max(Get(1.0d), 0.0d), 1.0d);
            set => Set(value);
        }

        public double PreviewPaneSizeHorizontalPx
        {
            get => Get(Math.Min(Math.Max(Get(300d), 50d), 600d));
            set => Set(value);
        }

        public double PreviewPaneSizeVerticalPx
        {
            get => Get(Math.Min(Math.Max(Get(250d), 50d), 600d));
            set => Set(value);
        }

        public bool PreviewPaneEnabled
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowPreviewOnly
        {
            get => Get(false);
            set => Set(value);
        }

        public override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowPreviewOnly):
                    Analytics.TrackEvent($"{e.settingName} {e.newValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(ShowPreviewOnly)}, {ShowPreviewOnly}");
        }
    }
}
