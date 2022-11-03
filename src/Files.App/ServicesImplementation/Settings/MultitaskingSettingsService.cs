using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class MultitaskingSettingsService : BaseObservableJsonSettings, IMultitaskingSettingsService
	{
		public MultitaskingSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
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

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(IsDualPaneEnabled):
				case nameof(AlwaysOpenDualPaneInNewTab):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
