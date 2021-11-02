using System;
using Files.EventArguments;
using Files.Models.JsonSettings;

namespace Files.Services
{
    public interface IUserSettingsService
    {
        event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

        ISettingsSharingContext GetSharingContext();

        bool ImportSettings(object import);

        object ExportSettings();

        IPreferencesSettingsService PreferencesSettingsService { get; }

        IMultitaskingSettingsService MultitaskingSettingsService { get; }

        IWidgetsSettingsService WidgetsSettingsService { get; }

        ISidebarSettingsService SidebarSettingsService { get; }

        IAppearanceSettingsService AppearanceSettingsService { get; }

        IStartupSettingsService StartupSettingsService { get; }

        IPreviewPaneSettingsService PreviewPaneSettingsService { get; }

        ILayoutSettingsService LayoutSettingsService { get; }
    }
}
