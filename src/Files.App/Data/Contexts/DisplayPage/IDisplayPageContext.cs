// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
	public interface IDisplayPageContext : INotifyPropertyChanging, INotifyPropertyChanged
	{
		bool IsLayoutAdaptiveEnabled { get; }
		LayoutTypes LayoutType { get; set; }

		/// <summary>
		/// Gets the layout the active pane is actually displaying. Unlike <see cref="LayoutType"/>,
		/// this never returns <see cref="LayoutTypes.Adaptive"/> — when the adaptive layout is active,
		/// it reports the concrete layout the adaptive logic chose.
		/// </summary>
		LayoutTypes DisplayedLayoutType { get; }

		SortOption SortOption { get; set; }
		SortDirection SortDirection { get; set; }

		GroupOption GroupOption { get; set; }
		SortDirection GroupDirection { get; set; }
		GroupByDateUnit GroupByDateUnit { get; set; }

		bool SortDirectoriesAlongsideFiles { get; set; }
		bool SortFilesFirst { get; set; }
	}
}
