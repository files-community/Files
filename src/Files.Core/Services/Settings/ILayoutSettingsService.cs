// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services.Settings
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Default icon size in the Details View
		/// </summary>
		int DefaultIconSizeDetailsView { get; set; }

		/// <summary>
		/// Default icon size in the List View
		/// </summary>
		int DefaultIconSizeListView { get; set; }

		/// <summary>
		/// Default icon size in the Tiles View
		/// </summary>
		int DefaulIconSizeTilesView { get; set; }

		/// <summary>
		/// Default icon size in the Grid View
		/// </summary>
		int DefaulIconSizeGridView { get; set; }

		/// <summary>
		/// Default icon size in the Columns View
		/// </summary>
		int DefaultIconSizeColumnsView { get; set; }
	}
}
