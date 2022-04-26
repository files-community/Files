using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.EventArguments;
using Files.Uwp.Serialization;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.Uwp.ServicesImplementation.Settings
{
    internal sealed class PaneSettingsService : BaseObservableJsonSettings, IPaneSettingsService
    {
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
        {
            RegisterSettingsContext(settingsSharingContext);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(ShowPreviewOnly)}, {ShowPreviewOnly}");
        }

        protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            if (e.SettingName is nameof(ShowPreviewOnly))
            {
                Analytics.TrackEvent($"{e.SettingName} {e.NewValue}");
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        private void RaiseOnSettingChangedEvent(string propertyName, object newValue)
        {
            string settingName = propertyName switch
            {
                nameof(Content) => Constants.PaneContent.ContentKey,
                nameof(HorizontalSizePx) => Constants.PaneContent.HorizontalSizePxKey,
                nameof(VerticalSizePx) => Constants.PaneContent.VerticalSizePxKey,
                nameof(MediaVolume) => Constants.PaneContent.MediaVolumeKey,
                nameof(ShowPreviewOnly) => Constants.PaneContent.ShowPreviewOnlyKey,
                _ => throw new InvalidOperationException(),
            };
            base.RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(settingName, newValue));
        }
    }
}
