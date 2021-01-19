using Files.Helpers;
using Files.SettingsInterfaces;
using System.Collections.Generic;
using Windows.Storage;

namespace Files.ViewModels
{
    public class BundlesSettingsViewModel : BaseJsonSettingsViewModel, IBundlesSettings
    {
        #region Constructor

        public BundlesSettingsViewModel()
            : base(PathHelpers.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.BundlesSettingsFileName))
        {
        }

        #endregion

        #region IBundlesSettings

        public Dictionary<string, List<string>> SavedBundles
        {
            get => Get<Dictionary<string, List<string>>>(null);
            set => Set(value);
        }

        #endregion

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
            return SavedBundles;
        }

        #endregion
    }
}
