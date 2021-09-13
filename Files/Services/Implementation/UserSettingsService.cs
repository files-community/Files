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

        public double SidebarWidthPx
        {
            get => Get(Math.Min(Math.Max(Get(255d), Constants.UI.MinimumSidebarWidth), 500d));
            set => Set(value);
        }

        public bool IsSidebarOpen
        {
            get => Get(true);
            set => Set(value);
        }

        public double PreviewPaneSizeHorizontalPx
        {
            get => Get(Math.Min(Math.Max(Get(300d), 50d), 600d));
            set => Set(value);
        }

        public double PreviewPaneSizeVerticalPx
        {
            get => Get(Math.Min(Math.Max(Get(250d), 50d), 600d));
            set => Set(value);
        }

        public bool PreviewPaneEnabled
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowPreviewOnly
        {
            get => Get(false);
            set => Set(value);
        }
    }
}
