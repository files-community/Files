using Files.Models.JsonSettings;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System;
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
