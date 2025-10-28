// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed partial class AppSettingsService : BaseObservableJsonSettings, IAppSettingsService
	{
		public AppSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Initialize settings
			RegisterSettingsContext(settingsSharingContext);
		}

		public bool ShowStatusCenterTeachingTip
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowBackgroundRunningNotification
		{
			get => Get(true);
			set => Set(value);
		}

	public bool RestoreTabsOnStartup
	{
		get => Get(false);
		set => Set(value);
	}

	public double StatusCenterWidth
	{
		get => Math.Max(300d, Math.Min(600d, Get(400d)));
		set => Set(Math.Max(300d, Math.Min(600d, value)));
	}

	public double StatusCenterHeight
	{
		get => Math.Max(200d, Math.Min(800d, Get(500d)));
		set => Set(Math.Max(200d, Math.Min(800d, value)));
	}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
