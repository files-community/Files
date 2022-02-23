using Files.Enums;
using Files.EventArguments;
using Files.Models.JsonSettings;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.Services.Implementation
{
    public class PaneSettingsService : BaseObservableJsonSettingsModel, IPaneSettingsService
    {
        public const string ContentKey = "PaneContent";
        public const string HorizontalSizePxKey = "PaneHorizontalSizePx";
        public const string VerticalSizePxKey = "PaneVerticalSizePx";
        public const string MediaVolumeKey = "PaneMediaVolume";
        public const string ShowPreviewOnlyKey = "ShowPreviewOnly";

        public PaneContents Content
        {
            get => Get(PaneContents.None);
            set => Set(value);
        }

        public double HorizontalSizePx
        {
            get => Math.Max(100d, Get(300d));
            set => Set(Math.Max(100d, value));
        }
        public double VerticalSizePx
        {
            get => Math.Max(100d, Get(250d));
            set => Set(Math.Max(100d, value));
        }

        public double MediaVolume
        {
            get => Math.Min(Math.Max(Get(1d), 0d), 1d);
            set => Set(Math.Max(0d, Math.Min(value, 1d)));
        }

        public bool ShowPreviewOnly
        {
            get => Get(false);
            set => Set(value);
        }

        public PaneSettingsService(ISettingsSharingContext settingsSharingContext)
            => RegisterSettingsContext(settingsSharingContext);

        public void ReportToAppCenter()
            => Analytics.TrackEvent($"{nameof(ShowPreviewOnly)}, {ShowPreviewOnly}");

        public override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            if (e.settingName is nameof(ShowPreviewOnly))
            {
                Analytics.TrackEvent($"{e.settingName} {e.newValue}");
            }
            base.RaiseOnSettingChangedEvent(sender, e);
        }

        private void RaiseOnSettingChangedEvent(string propertyName, object newValue)
        {
            string settingName = propertyName switch
            {
                nameof(Content) => ContentKey,
                nameof(HorizontalSizePx) => HorizontalSizePxKey,
                nameof(VerticalSizePxKey) => VerticalSizePxKey,
                nameof(MediaVolume) => MediaVolumeKey,
                nameof(ShowPreviewOnly) => ShowPreviewOnlyKey,
                _ => throw new InvalidOperationException(),
            };
            base.RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(settingName, newValue));
        }
    }
}
