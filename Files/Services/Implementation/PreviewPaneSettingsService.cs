using System;
using Files.EventArguments;
using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class PreviewPaneSettingsService : BaseObservableJsonSettingsModel, IPreviewPaneSettingsService
    {
        public PreviewPaneSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowPreviewOnly):
                    //case nameof(DisplayedTimeStyle):
                    //case nameof(ThemeHelper.RootTheme):
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{nameof(e.settingName)} {e.newValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
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
    }
}
