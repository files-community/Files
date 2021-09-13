using Files.Models.JsonSettings;
using Files.Models.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Services.Implementation
{
    public class UserSettingsService : BaseJsonSettingsModel, IUserSettingsService
    {
        public UserSettingsService()
            : base(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.UserSettingsFileName),
                isCachingEnabled: true)
        {
        }
    }
}
