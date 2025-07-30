// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed partial class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
	{
		public bool HasClickedReviewPrompt
		{
			get => Get(false);
			set => Set(value);
		}

		public bool HasClickedSponsorPrompt
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowRunningAsAdminPrompt
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowDataStreamsAreHiddenPrompt
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
