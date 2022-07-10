using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Uwp.Serialization;

namespace Files.Uwp.ServicesImplementation.Settings
{
    internal sealed class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
    {
        public bool WasPromptedToReview
        {
            get => Get(false);
            set => Set(value);
        }

        public void ReportToAppCenter()
        {
           
        }

        public ApplicationSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            RegisterSettingsContext(settingsSharingContext);
        }
    }
}
