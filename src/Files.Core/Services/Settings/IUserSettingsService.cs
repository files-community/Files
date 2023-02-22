using Files.Core.EventArguments;
using System;

namespace Files.Core.Services.Settings
{
	public interface IUserSettingsService : IBaseSettingsService
	{
		event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

		bool ImportSettings(object import);

		object ExportSettings();

		IPreferencesSettingsService PreferencesSettingsService { get; }

		IFoldersSettingsService FoldersSettingsService { get; }

		IAppearanceSettingsService AppearanceSettingsService { get; }

		IApplicationSettingsService ApplicationSettingsService { get; }

		IPreviewPaneSettingsService PreviewPaneSettingsService { get; }

		ILayoutSettingsService LayoutSettingsService { get; }

		IAppSettingsService AppSettingsService { get; }
	}
}
