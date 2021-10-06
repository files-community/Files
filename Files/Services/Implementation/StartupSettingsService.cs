using Files.Models.JsonSettings;
using System.Collections.Generic;

namespace Files.Services.Implementation
{
    public class StartupSettingsService : BaseJsonSettingsModel, IStartupSettingsService
    {
        public StartupSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(OpenSpecificPageOnStartup):
                case nameof(ContinueLastSessionOnStartUp):
                case nameof(OpenNewTabOnStartup):
                case nameof(AlwaysOpenNewInstance):
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{nameof(e.settingName)} {e.newValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public bool OpenSpecificPageOnStartup
        {
            get => Get(false);
            set => Set(value);
        }

        public string OpenSpecificPageOnStartupPath
        {
            get => Get(string.Empty);
            set => Set(value);
        }

        public bool ContinueLastSessionOnStartUp
        {
            get => Get(false);
            set => Set(value);
        }

        public bool OpenNewTabOnStartup
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AlwaysOpenNewInstance
        {
            get => Get(false);
            set => Set(value);
        }

        public List<string> TabsOnStartupList
        {
            get => Get<List<string>>(null);
            set => Set(value);
        }

        public List<string> LastSessionTabList
        {
            get => Get<List<string>>(null);
            set => Set(value);
        }
    }
}
