// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a named set of layout preferences that folders can use.
	/// </summary>
	public sealed partial class LayoutTemplate
	{
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the name chosen by the user, or an empty string to use the built-in name.
		/// </summary>
		public string Name { get; set; } = string.Empty;

		public FolderLayoutModes LayoutMode { get; set; } = FolderLayoutModes.DetailsView;

		public SortOption SortOption { get; set; } = SortOption.Name;

		public string SortColumnId { get; set; } = string.Empty;

		public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

		public bool SortDirectoriesAlongsideFiles { get; set; }

		public bool SortFilesFirst { get; set; }

		public GroupOption GroupOption { get; set; } = GroupOption.None;

		public string GroupColumnId { get; set; } = string.Empty;

		public SortDirection GroupDirection { get; set; } = SortDirection.Ascending;

		public GroupByDateUnit GroupByDateUnit { get; set; } = GroupByDateUnit.Year;

		/// <summary>
		/// Gets or sets the visible columns of <see cref="DetailsLayoutPage"/> in display order, or null for the built-in columns.
		/// </summary>
		public List<string>? Columns { get; set; }

		/// <summary>
		/// Gets or sets the column widths of <see cref="DetailsLayoutPage"/> as "id|width" entries.
		/// </summary>
		public List<string>? ColumnWidths { get; set; }

		[JsonIgnore]
		public bool IsBuiltIn
			=> LayoutTemplateIds.IsBuiltIn(Id);

		[JsonIgnore]
		public string DisplayName
			=> string.IsNullOrEmpty(Name) ? LayoutTemplateIds.GetBuiltInName(Id) : Name;

		public override string ToString()
			=> DisplayName;

		public LayoutTemplate Clone()
		{
			var clone = (LayoutTemplate)MemberwiseClone();
			clone.Columns = Columns is null ? null : [.. Columns];
			clone.ColumnWidths = ColumnWidths is null ? null : [.. ColumnWidths];

			return clone;
		}
	}
}
