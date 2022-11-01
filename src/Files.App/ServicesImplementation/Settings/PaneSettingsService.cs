using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.App.ServicesImplementation.Settings
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

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			if (e.SettingName is nameof(ShowPreviewOnly))
			{
				Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
			}

            base.RaiseOnSettingChangedEvent(sender, e);
        }
    }
}
