// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Serialization;
using Files.Backend.Services.Settings;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
	{
		public bool ClickedToReviewApp
		{
			get => Get(false);
			set => Set(value);
		}

		public ApplicationSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			RegisterSettingsContext(settingsSharingContext);
		}
	}
}
