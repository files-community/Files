using System;
using Files.EventArguments;
using Files.Models.JsonSettings;

namespace Files.Services
{
    public interface IUserSettingsService
    {
        event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

        ISettingsSharingContext GetSharingContext();

        IFilesAndFoldersSettingsService FilesAndFoldersSettingsService { get; }

        IMultitaskingSettingsService MultitaskingSettingsService { get; }

        IWidgetsSettingsService WidgetsSettingsService { get; }

        ISidebarSettingsService SidebarSettingsService { get; }

        IPreferencesSettingsService PreferencesSettingsService { get; }

        IAppearanceSettingsService AppearanceSettingsService { get; }

        IStartupSettingsService StartupSettingsService { get; }

        IPreviewPaneSettingsService PreviewPaneSettingsService { get; }

        ILayoutSettingsService LayoutSettingsService { get; }
    }
}
