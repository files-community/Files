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

		public DetailsViewSizeKind ItemSizeDetailsView
		{
			get => Get(DetailsViewSizeKind.Small);
			set => Set(value);
		}

		public ListViewSizeKind ItemSizeListView
		{
			get => Get(ListViewSizeKind.Small);
			set => Set(value);
		}

		public TilesViewSizeKind ItemSizeTilesView
		{
			get => Get(TilesViewSizeKind.Small);
			set => Set(value);
		}

		public GridViewSizeKind ItemSizeGridView
		{
			get => Get(GridViewSizeKind.ExtraLarge);
			set => Set(value);
		}

		public ColumnsViewSizeKind ItemSizeColumnsView
		{
			get => Get(ColumnsViewSizeKind.Small);
			set => Set(value);
		}
	}
}
