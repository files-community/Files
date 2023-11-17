﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services.Settings
{
	public interface IUserSettingsService : IBaseSettingsService
	{
		event EventHandler<SettingChangedEventArgs> OnSettingChangedEvent;

		bool ImportSettings(object import);

		object ExportSettings();

		IGeneralSettingsService GeneralSettingsService { get; }

		IFoldersSettingsService FoldersSettingsService { get; }

		IAppearanceSettingsService AppearanceSettingsService { get; }

		IApplicationSettingsService ApplicationSettingsService { get; }

		IInfoPaneSettingsService InfoPaneSettingsService { get; }

		ILayoutSettingsService LayoutSettingsService { get; }

		IAppSettingsService AppSettingsService { get; }
	}
}
