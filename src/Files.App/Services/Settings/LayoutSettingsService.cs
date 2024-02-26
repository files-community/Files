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

		public DetailsViewSizeKind DetailsViewSize
		{
			get => Get(DetailsViewSizeKind.Small);
			set => Set(value);
		}

		public ListViewSizeKind ListViewSize
		{
			get => Get(ListViewSizeKind.Small);
			set => Set(value);
		}

		public TilesViewSizeKind TilesViewSize
		{
			get => Get(TilesViewSizeKind.Small);
			set => Set(value);
		}

		public GridViewSizeKind GridViewSize
		{
			get => Get(GridViewSizeKind.Large);
			set => Set(value);
		}

		public ColumnsViewSizeKind ColumnsViewSize
		{
			get => Get(ColumnsViewSizeKind.Small);
			set => Set(value);
		}
	}
}
