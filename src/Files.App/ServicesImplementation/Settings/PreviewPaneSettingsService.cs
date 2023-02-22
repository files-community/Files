using Files.App.Serialization;
using Files.Core.Services.Settings;
using Files.Core.EventArguments;
using Microsoft.AppCenter.Analytics;
using System;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class PreviewPaneSettingsService : BaseObservableJsonSettings, IPreviewPaneSettingsService
	{
		public bool IsEnabled
		{
			get => Get(false);
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

		public PreviewPaneSettingsService(ISettingsSharingContext settingsSharingContext)
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
