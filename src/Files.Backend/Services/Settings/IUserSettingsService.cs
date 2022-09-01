using Files.Shared.EventArguments;
using System;

namespace Files.Backend.Services.Settings
{
    public interface IUserSettingsService : IBaseSettingsService
    {
        event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

        bool ImportSettings(object import);

        object ExportSettings();

        IPreferencesSettingsService PreferencesSettingsService { get; }

        IMultitaskingSettingsService MultitaskingSettingsService { get; }

        IWidgetsSettingsService WidgetsSettingsService { get; }

        IAppearanceSettingsService AppearanceSettingsService { get; }

        IApplicationSettingsService ApplicationSettingsService { get; }

        IPaneSettingsService PaneSettingsService { get; }

        ILayoutSettingsService LayoutSettingsService { get; }
    }
}
