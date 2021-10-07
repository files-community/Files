using Files.Models.JsonSettings;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System.IO;
using Windows.Storage;

namespace Files.Services.Implementation
{
    public class UserSettingsService : BaseJsonSettingsModel, IUserSettingsService
    {
        private IFilesAndFoldersSettingsService _FilesAndFoldersSettingsService;
        public IFilesAndFoldersSettingsService FilesAndFoldersSettingsService
        {
            get => GetSettingsService(ref _FilesAndFoldersSettingsService);
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

        private ISidebarSettingsService _SidebarSettingsService;
        public ISidebarSettingsService SidebarSettingsService
        {
            get => GetSettingsService(ref _SidebarSettingsService);
        }

        private IPreferencesSettingsService _PreferencesSettingsService;
        public IPreferencesSettingsService PreferencesSettingsService
        {
            get => GetSettingsService(ref _PreferencesSettingsService);
        }

        private IAppearanceSettingsService _AppearanceSettingsService;
        public IAppearanceSettingsService AppearanceSettingsService
        {
            get => GetSettingsService(ref _AppearanceSettingsService);
        }

        private IStartupSettingsService _StartupSettingsService;
        public IStartupSettingsService StartupSettingsService
        {
            get => GetSettingsService(ref _StartupSettingsService);
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

        private TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService settingsServiceMember)
            where TSettingsService : class
        {
            settingsServiceMember ??= Ioc.Default.GetService<TSettingsService>();
            return settingsServiceMember;
        } 
    }
}
