// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Serialization;
using Files.Backend.Services.Settings;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
	{
		public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public int DefaultGridViewSize
		{
			get => (int)Get((long)Constants.Browser.GridViewBrowser.GridViewSizeMedium);
			set => Set((long)value);
		}
	}
}
