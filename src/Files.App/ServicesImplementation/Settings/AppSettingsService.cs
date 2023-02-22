using Files.App.Serialization;
using Files.Core.Services.Settings;
using Files.Core.EventArguments;
using Microsoft.AppCenter.Analytics;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class AppSettingsService : BaseObservableJsonSettings, IAppSettingsService
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

		public bool RestoreTabsOnStartup
		{
			get => Get(false);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(ShowStatusCenterTeachingTip):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
				case nameof(RestoreTabsOnStartup):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
