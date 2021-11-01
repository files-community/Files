using Files.Models.JsonSettings;
using System.Collections.Generic;
using Windows.Storage;

namespace Files.Services.Implementation
{
    public sealed class BundlesSettingsService : BaseObservableJsonSettingsModel, IBundlesSettingsService
    {
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
            try
            {
                SavedBundles = (Dictionary<string, List<string>>)import;
                
                FlushSettings();

                return true;
            }
            catch
            {
                // TODO: Display the error?
                return false;
            }
        }

        public override object ExportSettings()
        {
            // Return string in Json format
            return jsonSettingsSerializer.SerializeToJson(SavedBundles);
        }
    }
}
