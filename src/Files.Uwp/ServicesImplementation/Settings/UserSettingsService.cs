using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.Shared.Extensions;
using Files.Uwp.Serialization;
using Files.Uwp.Serialization.Implementation;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.Uwp.ServicesImplementation.Settings
{
    internal sealed class UserSettingsService : BaseJsonSettings, IUserSettingsService
    {
        private IPreferencesSettingsService _PreferencesSettingsService;
        public IPreferencesSettingsService PreferencesSettingsService
        {
            get => GetSettingsService(ref _PreferencesSettingsService);
        }

        private IMultitaskingSettingsService _MultitaskingSettingsService;
        public IMultitaskingSettingsService MultitaskingSettingsService
        {
            get => GetSettingsService(ref _MultitaskingSettingsService);
        }

        private IWidgetsSettingsService _WidgetsSettingsService;
        public IWidgetsSettingsService WidgetsSettingsService
        {
            get => GetSettingsService(ref _WidgetsSettingsService);
        }

        private IAppearanceSettingsService _AppearanceSettingsService;
        public IAppearanceSettingsService AppearanceSettingsService
        {
            get => GetSettingsService(ref _AppearanceSettingsService);
        }

        private IPaneSettingsService _PaneSettingsService;
        public IPaneSettingsService PaneSettingsService
        {
            get => GetSettingsService(ref _PaneSettingsService);
        }

        private ILayoutSettingsService _LayoutSettingsService;
        public ILayoutSettingsService LayoutSettingsService
        {
            get => GetSettingsService(ref _LayoutSettingsService);
        }

        private IApplicationSettingsService _ApplicationSettingsService;
        public IApplicationSettingsService ApplicationSettingsService
        {
            get => GetSettingsService(ref _ApplicationSettingsService);
        }

        public UserSettingsService()
        {
            SettingsSerializer = new DefaultSettingsSerializer();
            JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
            JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

            Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.UserSettingsFileName));
        }

        public override object ExportSettings()
        {
            var export = (Dictionary<string, object>)base.ExportSettings();

            // Remove session settings
            export.Remove(nameof(PreferencesSettingsService.LastSessionTabList));

            return JsonSettingsSerializer.SerializeToJson(export);
        }

        public override bool ImportSettings(object import)
        {
            Dictionary<string, object> settingsImport = null;
            if (import is string importString)
            {
                settingsImport = JsonSettingsSerializer.DeserializeFromJson<Dictionary<string, object>>(importString);
            }
            else if (import is Dictionary<string, object> importDict)
            {
                settingsImport = importDict;
            }

            if (!settingsImport.IsEmpty() && base.ImportSettings(settingsImport))
            {
                foreach (var item in settingsImport)
                {
                    RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(item.Key, item.Value));
                }

                return true;
            }

            return false;
        }

        private TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService settingsServiceMember)
            where TSettingsService : class, IBaseSettingsService
        {
            settingsServiceMember ??= Ioc.Default.GetService<TSettingsService>();
            return settingsServiceMember;
        }

        public void ReportToAppCenter()
        {
            PreferencesSettingsService?.ReportToAppCenter();
            MultitaskingSettingsService?.ReportToAppCenter();
            WidgetsSettingsService?.ReportToAppCenter();
            AppearanceSettingsService?.ReportToAppCenter();
            PreferencesSettingsService?.ReportToAppCenter();
            LayoutSettingsService?.ReportToAppCenter();
            PaneSettingsService?.ReportToAppCenter();
        }
    }
}
