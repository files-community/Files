using Files.SettingsInterfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using Windows.Storage;

namespace Files.Models.Settings
{
    public class BundlesSettingsModel : BaseJsonSettingsModel, IBundlesSettings
    {
        #region Constructor

        public BundlesSettingsModel()
            : base(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.BundlesSettingsFileName),
                  isCachingEnabled: true)
        {
        }

        #endregion Constructor

        #region IBundlesSettings

        public Dictionary<string, List<string>> SavedBundles
        {
            get => Get<Dictionary<string, List<string>>>(null);
            set => Set(value);
        }

        #endregion IBundlesSettings

        #region Override

        public override void ImportSettings(object import)
        {
            try
            {
                SavedBundles = (Dictionary<string, List<string>>)import;
            }
            catch { }
        }

        public override object ExportSettings()
        {
            // Return string in Json format
            return JsonConvert.SerializeObject(SavedBundles, Formatting.Indented);
        }

        #endregion Override
    }
}