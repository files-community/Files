using Files.Helpers;
using Files.SettingsInterfaces;
using System.Collections.Generic;
using Windows.Storage;

namespace Files.ViewModels
{
    public class WidgetsSettingsViewModel : BaseJsonSettingsViewModel, IWidgetsSettings
    {
        #region Constructor

        public WidgetsSettingsViewModel()
            : base(PathHelpers.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.JsonSettingsFileName))
        {
        }

        #endregion

        #region IWidgetsSettings

        public Dictionary<string, List<string>> SavedBundles
        {
            get => Get<Dictionary<string, List<string>>>(null);
            set => Set(value);
        }

        #endregion
    }
}
