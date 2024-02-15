// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Default icon size in the Details View
		/// </summary>
		int DefaultIconHeightDetailsView { get; set; }

		/// <summary>
		/// Default icon size in the List View
		/// </summary>
		int DefaultIconHeightListView { get; set; }

		/// <summary>
		/// Default icon size in the Tiles View
		/// </summary>
		int DefaulIconHeightTilesView { get; set; }

		/// <summary>
		/// Default icon size in the Grid View
		/// </summary>
		int DefaulIconHeightGridView { get; set; }

		/// <summary>
		/// Default icon size in the Columns View
		/// </summary>
		int DefaultIconHeightColumnsView { get; set; }
	}
}
