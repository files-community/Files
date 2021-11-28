using Files.Extensions;
using Files.Models.JsonSettings;
using System;
using System.Collections.Generic;
using Windows.Storage;

namespace Files.Services.Implementation
{
    public sealed class BundlesSettingsService : BaseObservableJsonSettingsModel, IBundlesSettingsService
    {
        public event EventHandler OnSettingImportedEvent;

        public BundlesSettingsService()
            : base(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.BundlesSettingsFileName),
                  isCachingEnabled: true)
        {
        }

        public Dictionary<string, List<string>> SavedBundles
        {
            get => Get<Dictionary<string, List<string>>>(null);
            set => Set(value);
        }

        public override bool ImportSettings(object import)
        {
            if (import is string importString)
            {
                SavedBundles = jsonSettingsSerializer.DeserializeFromJson<Dictionary<string, List<string>>>(importString);
            }
            else if (import is Dictionary<string, List<string>> importDict)
            {
                SavedBundles = importDict;
            }

            if (SavedBundles != null)
            {
                FlushSettings();
                OnSettingImportedEvent?.Invoke(this, null);
                return true;
            }

            return false;
        }

        public override object ExportSettings()
        {
            // Return string in Json format
            return jsonSettingsSerializer.SerializeToJson(SavedBundles);
        }
    }
}
