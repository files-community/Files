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

		public int ItemSizeDetailsView
		{
			get => Get((int)LayoutDetailsViewIconHeightKind.Regular);
			set => Set(value);
		}

		public int ItemSizeListView
		{
			get => Get((int)LayoutListViewIconHeightKind.Regular);
			set => Set(value);
		}

		public int ItemSizeTilesView
		{
			get => Get((int)LayoutTilesViewIconHeightKind.Regular);
			set => Set(value);
		}

		public int ItemSizeGridView
		{
			get => Get((int)LayoutGridViewIconHeightKind.Medium);
			set => Set(value);
		}

		public int ItemSizeColumnsView
		{
			get => Get((int)LayoutColumnsViewIconHeightKind.Regular);
			set => Set(value);
		}
	}
}
