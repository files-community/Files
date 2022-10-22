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
       
        IFoldersSettingsService FoldersSettingsService { get; }

        IMultitaskingSettingsService MultitaskingSettingsService { get; }

        IAppearanceSettingsService AppearanceSettingsService { get; }

        IApplicationSettingsService ApplicationSettingsService { get; }

        IPaneSettingsService PaneSettingsService { get; }

        ILayoutSettingsService LayoutSettingsService { get; }

		IAppSettingsService AppSettingsService { get; }
    }
}
