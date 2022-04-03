﻿using Files.Backend.Services.Settings;
using Files.Uwp.Serialization;
using Files.Uwp.Serialization.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.Uwp.ServicesImplementation.Settings
{
    internal sealed class BundlesSettingsService : BaseObservableJsonSettings, IBundlesSettingsService
    {
        public event EventHandler OnSettingImportedEvent;

        public BundlesSettingsService()
        {
            SettingsSerializer = new DefaultSettingsSerializer();
            JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
            JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);

            Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.BundlesSettingsFileName));
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
                SavedBundles = JsonSettingsSerializer.DeserializeFromJson<Dictionary<string, List<string>>>(importString);
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
            return JsonSettingsSerializer.SerializeToJson(SavedBundles);
        }

        public void ReportToAppCenter()
        {
        }
    }
}
