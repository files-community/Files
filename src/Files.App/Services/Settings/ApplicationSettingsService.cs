// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Serialization;
using Files.Core.Services.Settings;

namespace Files.App.Services.Settings
{
	internal sealed class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
	{
		public bool ClickedToReviewApp
		{
			get => Get(false);
			set => Set(value);
		}
		
		public bool ShowRunningAsAdminPrompt
		{
			get => Get(true);
			set => Set(value);
		}

		public ApplicationSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			RegisterSettingsContext(settingsSharingContext);
		}
	}
}
