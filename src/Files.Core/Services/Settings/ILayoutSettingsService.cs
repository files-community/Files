// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;

namespace Files.Core.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Item size in the Details View
		/// </summary>
		DetailsViewSizeKind ItemSizeDetailsView { get; set; }

		/// <summary>
		/// Item size in the List View
		/// </summary>
		ListViewSizeKind ItemSizeListView { get; set; }

		/// <summary>
		/// Item size in the Tiles View
		/// </summary>
		TilesViewSizeKind ItemSizeTilesView { get; set; }

		/// <summary>
		/// Item size in the Grid View
		/// </summary>
		GridViewSizeKind ItemSizeGridView { get; set; }

		/// <summary>
		/// Item size in the Columns View
		/// </summary>
		ColumnsViewSizeKind ItemSizeColumnsView { get; set; }
	}
}
