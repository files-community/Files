// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.AppCenter.Analytics;

namespace Files.App.Services.Settings
{
	internal sealed class AppSettingsService : BaseJsonSettings, IAppSettingsService
	{
		public bool ShowStatusCenterTeachingTip
		{
			get => Get(true);
			set => Set(value);
		}

		public bool RestoreTabsOnStartup
		{
			get => Get(false);
			set => Set(value);
		}

		public AppSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Initialize settings
			RegisterSettingsContext(settingsSharingContext);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
