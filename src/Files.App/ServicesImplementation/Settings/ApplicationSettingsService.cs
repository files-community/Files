using Files.App.Serialization;
using Files.Core.Services.Settings;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
	{
		public bool ClickedToReviewApp
		{
			get => Get(false);
			set => Set(value);
		}

		public ApplicationSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			RegisterSettingsContext(settingsSharingContext);
		}
	}
}
