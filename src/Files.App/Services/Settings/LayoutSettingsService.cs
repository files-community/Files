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
			get => (int)Get((long)Constants.IconHeights.DetailsView.Regular);
			set => Set((long)value);
		}

		public int ItemSizeListView
		{
			get => (int)Get((long)Constants.IconHeights.ListView.Regular);
			set => Set((long)value);
		}

		public int ItemSizeTilesView
		{
			get => (int)Get((long)Constants.IconHeights.TilesView.Regular);
			set => Set((long)value);
		}

		public int ItemSizeGridView
		{
			get => (int)Get((long)Constants.IconHeights.GridView.Medium);
			set => Set((long)value);
		}

		public int ItemSizeColumnsView
		{
			get => (int)Get((long)Constants.IconHeights.ColumnsView.Regular);
			set => Set((long)value);
		}
	}
}
