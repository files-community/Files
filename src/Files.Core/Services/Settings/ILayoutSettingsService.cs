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
		DetailsViewSizeKind DetailsViewSize { get; set; }

		/// <summary>
		/// Item size in the List View
		/// </summary>
		ListViewSizeKind ListViewSize { get; set; }

		/// <summary>
		/// Item size in the Tiles View
		/// </summary>
		TilesViewSizeKind TilesViewSize { get; set; }

		/// <summary>
		/// Item size in the Grid View
		/// </summary>
		GridViewSizeKind GridViewSize { get; set; }

		/// <summary>
		/// Item size in the Columns View
		/// </summary>
		ColumnsViewSizeKind ColumnsViewSize { get; set; }
	}
}
