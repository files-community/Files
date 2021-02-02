using Files.SettingsInterfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using Windows.Storage;

namespace Files.ViewModels
{
    public class BundlesSettingsViewModel : BaseJsonSettingsViewModel, IBundlesSettings
    {
        #region Constructor

        public BundlesSettingsViewModel()
            : base(System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.BundlesSettingsFileName))
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