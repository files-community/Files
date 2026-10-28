// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Globalization;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides the columns of <see cref="DetailsLayoutPage"/> saved in layout templates.
	/// </summary>
	public static class DetailsLayoutColumnDefaults
	{
		/// <summary>
		/// Gets the visible columns of the specified template in display order.
		/// </summary>
		public static IReadOnlyList<string> GetColumns(LayoutTemplate template)
		{
			var columns = template.Columns is { } savedColumns
				? savedColumns.Where(IsConfigurableColumn).Distinct()
				: GetBuiltInColumns();

			return (string[])[DetailsLayoutColumnIds.Name, .. columns];
		}

		/// <summary>
		/// Gets the columns that the specified template had before the user changed them.
		/// </summary>
		public static IReadOnlyList<string> GetOriginalColumns(LayoutTemplate template)
		{
			return LayoutTemplateIds.IsBuiltIn(template.Id)
				? GetColumns(LayoutTemplateIds.CreateBuiltIn(template.Id))
				: (string[])[DetailsLayoutColumnIds.Name, .. GetBuiltInColumns()];
		}

		public static void SetColumns(LayoutTemplate template, IEnumerable<string> columns)
		{
			template.Columns = [.. columns.Where(IsConfigurableColumn).Distinct()];
		}

		/// <summary>
		/// Applies the columns of the specified template to the specified columns.
		/// </summary>
		public static void Apply(ColumnsViewModel columns, LayoutTemplate template)
		{
			var visibleColumns = GetColumns(template);
			var catalog = ShellPropertyCatalog.GetAsync() is { IsCompletedSuccessfully: true } loadTask ? loadTask.Result : null;

			columns.Columns = [.. DetailsLayoutColumnDefinitions.BuiltIn
				.Where(definition => definition.Id != DetailsLayoutColumnIds.Icon)
				.Select(definition => definition.Id)
				.Concat(visibleColumns.Where(id => !DetailsLayoutColumnDefinitions.IsBuiltIn(id)))
				.Select(id =>
				{
					var width = DetailsLayoutColumnDefinitions.Get(id)?.DefaultWidth ?? (catalog is not null && catalog.TryGetValue(id, out var info) ? info.DefaultWidth : 150);

					return $"{id}|{width.ToString(CultureInfo.InvariantCulture)}|{(visibleColumns.Contains(id) ? 1 : 0)}";
				})];

			columns.ColumnOrder = [.. visibleColumns];
		}

		/// <summary>
		/// Saves the specified columns to the specified template.
		/// </summary>
		/// <remarks>
		/// Columns hidden because they don't apply to the current folder keep their state in the template.
		/// </remarks>
		public static void Save(ColumnsViewModel columns, LayoutTemplate template)
		{
			var previousColumns = GetColumns(template);
			SetColumns(template, columns.OrderedColumns.Ids.Where(id =>
			{
				var column = columns.GetColumn(id);
				return column.IsHidden ? previousColumns.Contains(id) : !column.UserCollapsed;
			}));
		}

		public static string GetDisplayName(string id, IReadOnlyDictionary<string, ShellPropertyColumnInfo>? catalog)
		{
			if (DetailsLayoutColumnDefinitions.IsBuiltIn(id))
				return DetailsLayoutColumnDefinitions.GetHeader(id);

			if (id == "System.Keywords")
				return Strings.FileExplorerTags.GetLocalizedResource();

			return catalog is not null && catalog.TryGetValue(id, out var info) ? info.DisplayName : id;
		}

		private static IEnumerable<string> GetBuiltInColumns()
		{
			return DetailsLayoutColumnDefinitions.BuiltIn
				.Where(definition => definition.IsVisibleByDefault && IsConfigurableColumn(definition.Id))
				.Select(definition => definition.Id);
		}

		private static bool IsConfigurableColumn(string id)
		{
			return !DetailsLayoutColumnOrder.IsFixed(id) && !string.IsNullOrEmpty(id);
		}
	}
}
