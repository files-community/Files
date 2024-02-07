// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.Settings
{
	internal sealed class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
	{
		public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public int DefaultIconSizeDetailsView
		{
			get => (int)Get((long)Constants.DefaultIconSizes.Large);
			set => Set((long)value);
		}

		public int DefaultIconSizeListView
		{
			get => (int)Get((long)Constants.DefaultIconSizes.Large);
			set => Set((long)value);
		}

		public int DefaulIconSizeTilesView
		{
			get => (int)Get((long)Constants.Browser.GridViewBrowser.GridViewSizeMedium);
			set => Set((long)value);
		}

		public int DefaulIconSizeGridView
		{
			get => (int)Get((long)Constants.Browser.GridViewBrowser.GridViewSizeMedium);
			set => Set((long)value);
		}

		public int DefaultIconSizeColumnsView
		{
			get => (int)Get((long)Constants.DefaultIconSizes.Large);
			set => Set((long)value);
		}
	}
}
