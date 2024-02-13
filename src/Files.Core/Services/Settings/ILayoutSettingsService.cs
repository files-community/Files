// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Item size in the Details View
		/// </summary>
		int ItemSizeDetailsView { get; set; }

		/// <summary>
		/// Item size in the List View
		/// </summary>
		int ItemSizeListView { get; set; }

		/// <summary>
		/// Item size in the Tiles View
		/// </summary>
		int ItemSizeTilesView { get; set; }

		/// <summary>
		/// Item size in the Grid View
		/// </summary>
		int ItemSizeGridView { get; set; }

		/// <summary>
		/// Item size in the Columns View
		/// </summary>
		int ItemSizeColumnsView { get; set; }
	}
}
