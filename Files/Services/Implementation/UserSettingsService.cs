using Files.Extensions;
using Files.Models.JsonSettings;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.Services.Implementation
{
    public class UserSettingsService : BaseJsonSettingsModel, IUserSettingsService
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

        private IPreviewPaneSettingsService _PreviewPaneSettingsService;
        public IPreviewPaneSettingsService PreviewPaneSettingsService
        {
            get => GetSettingsService(ref _PreviewPaneSettingsService);
        }

        private ILayoutSettingsService _LayoutSettingsService;
        public ILayoutSettingsService LayoutSettingsService
        {
            get => GetSettingsService(ref _LayoutSettingsService);
        }

        public UserSettingsService()
            : base(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.UserSettingsFileName),
                isCachingEnabled: true)
        {
        }

        public override object ExportSettings()
        {
            var export = (Dictionary<string, object>)base.ExportSettings();

            // Remove session settings
            export.Remove(nameof(PreferencesSettingsService.LastSessionTabList));

            return jsonSettingsSerializer.SerializeToJson(export);
        }

        public override bool ImportSettings(object import)
        {
            var settingsImport = jsonSettingsSerializer.DeserializeFromJson<Dictionary<string, object>>((string)import);

            if (!settingsImport.IsEmpty())
            {
                var diff = JsonSettingsDatabase.TakeDifferent(settingsImport);

                if (base.ImportSettings(settingsImport))
                {
                    foreach (var item in diff)
                    {
                        RaiseOnSettingChangedEvent(this, new EventArguments.SettingChangedEventArgs(item.Key, item.Value));
                    }

                    return true;
                }
            }

            return false;
        }

        private TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService settingsServiceMember)
            where TSettingsService : class
        {
            settingsServiceMember ??= Ioc.Default.GetService<TSettingsService>();
            return settingsServiceMember;
        } 
    }
}
