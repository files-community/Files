// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Serialization;
using Files.Core.Services.Settings;

namespace Files.App.Services.Settings
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
