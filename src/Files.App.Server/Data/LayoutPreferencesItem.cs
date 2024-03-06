// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Server.Data.Enums;

namespace Files.App.Server.Data
{
	public sealed class LayoutPreferencesItem
	{
		public ColumnPreferences ColumnsViewModel { get; set; } = new();

		public bool SortDirectoriesAlongsideFiles { get; set; }
		public bool SortFilesFirst { get; set; }
		public bool IsAdaptiveLayoutOverridden { get; set; }

		public FolderLayoutModes LayoutMode { get; set; }

		public SortOption DirectorySortOption { get; set; }
		public SortDirection DirectorySortDirection { get; set; }
		public SortDirection DirectoryGroupDirection { get; set; }

		public GroupOption DirectoryGroupOption { get; set; }
		public GroupByDateUnit DirectoryGroupByDateUnit { get; set; }
	}
}
