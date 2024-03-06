namespace Files.App.Server.Data
{
	public sealed class LayoutPreferencesItem
	{
		public ColumnPreferences ColumnsViewModel { get; set; } = new();

		public bool SortDirectoriesAlongsideFiles { get; set; }
		public bool SortFilesFirst { get; set; }
		public bool IsAdaptiveLayoutOverridden { get; set; }
		public int GridViewSize { get; set; }

		public byte LayoutMode { get; set; }

		public byte DirectorySortOption { get; set; }
		public byte DirectorySortDirection { get; set; }
		public byte DirectoryGroupDirection { get; set; }

		public byte DirectoryGroupOption { get; set; }
		public byte DirectoryGroupByDateUnit { get; set; }
	}
}
