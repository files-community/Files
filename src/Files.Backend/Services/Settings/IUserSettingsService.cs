using System;
using Files.Backend.EventArguments;
using Files.Backend.Models.JsonSettings;

namespace Files.Backend.Services.Settings
{
    public interface IUserSettingsService : IBaseSettingsService
    {
        event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

        ISettingsSharingContext GetSharingContext();

        bool ImportSettings(object import);

        object ExportSettings();

        IPreferencesSettingsService PreferencesSettingsService { get; }

        IMultitaskingSettingsService MultitaskingSettingsService { get; }

        IWidgetsSettingsService WidgetsSettingsService { get; }

        IAppearanceSettingsService AppearanceSettingsService { get; }

        IPaneSettingsService PaneSettingsService { get; }

        ILayoutSettingsService LayoutSettingsService { get; }
    }
}
