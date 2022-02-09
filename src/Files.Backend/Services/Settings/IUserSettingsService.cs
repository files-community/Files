﻿using System;

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

        IPreviewPaneSettingsService PreviewPaneSettingsService { get; }

        ILayoutSettingsService LayoutSettingsService { get; }
    }
}
