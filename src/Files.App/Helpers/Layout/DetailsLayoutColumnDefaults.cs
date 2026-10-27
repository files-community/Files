// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Globalization;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides the default columns of <see cref="DetailsLayoutPage"/> for folders without saved preferences.
	/// </summary>
	public static class DetailsLayoutColumnDefaults
	{
		private static ILayoutSettingsService LayoutSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>().LayoutSettingsService;

		/// <summary>
		/// Gets the visible default columns in display order.
		/// </summary>
		public static IReadOnlyList<string> GetColumns()
		{
			var columns = LayoutSettingsService.DetailsLayoutColumns is { } savedColumns
				? savedColumns.Where(IsConfigurableColumn).Distinct()
				: DetailsLayoutColumnDefinitions.All.Where(definition => definition.IsVisibleByDefault && IsConfigurableColumn(definition.Id)).Select(definition => definition.Id);

			return [DetailsLayoutColumnIds.Name, .. columns];
		}

		public static void SetColumns(IEnumerable<string> columns)
		{
			LayoutSettingsService.DetailsLayoutColumns = [.. columns.Where(IsConfigurableColumn).Distinct()];
		}

		/// <summary>
		/// Restores the built-in default columns and widths.
		/// </summary>
		public static void Reset()
		{
			LayoutSettingsService.DetailsLayoutColumns = null;
			LayoutSettingsService.DetailsLayoutColumnWidths = null;
		}

		/// <summary>
		/// Applies the default columns to the specified columns.
		/// </summary>
		public static void Apply(ColumnsViewModel columns)
		{
			var visibleColumns = GetColumns();
			var widths = GetWidths();
			var catalog = ShellPropertyCatalog.GetAsync() is { IsCompletedSuccessfully: true } loadTask ? loadTask.Result : null;

			columns.Columns = [.. DetailsLayoutColumnDefinitions.All
				.Where(definition => definition.Id != DetailsLayoutColumnIds.Icon)
				.Select(definition => definition.Id)
				.Concat(visibleColumns.Where(id => !DetailsLayoutColumnDefinitions.IsBuiltIn(id)))
				.Select(id =>
				{
					var width = widths.TryGetValue(id, out var savedWidth)
						? savedWidth
						: DetailsLayoutColumnDefinitions.Get(id)?.DefaultWidth ?? (catalog is not null && catalog.TryGetValue(id, out var info) ? info.DefaultWidth : 150);

					return $"{id}|{width.ToString(CultureInfo.InvariantCulture)}|{(visibleColumns.Contains(id) ? 1 : 0)}";
				})];

			columns.ColumnOrder = [.. visibleColumns];
		}

		/// <summary>
		/// Saves the specified columns as the default columns.
		/// </summary>
		/// <remarks>
		/// Columns hidden because they don't apply to the current folder keep their previous default state.
		/// </remarks>
		public static void Save(ColumnsViewModel columns)
		{
			var previousColumns = GetColumns();
			SetColumns(columns.OrderedColumns.Ids.Where(id =>
			{
				var column = columns.GetColumn(id);
				return column.IsHidden ? previousColumns.Contains(id) : !column.UserCollapsed;
			}));

			var widths = GetWidths();
			foreach (var id in columns.AllColumnIds)
			{
				if (IsConfigurableColumn(id) || id == DetailsLayoutColumnIds.Name)
					widths[id] = columns.GetColumn(id).UserLengthPixels;
			}

			LayoutSettingsService.DetailsLayoutColumnWidths = [.. widths.Select(pair => $"{pair.Key}|{pair.Value.ToString(CultureInfo.InvariantCulture)}")];
		}

		public static string GetDisplayName(string id, IReadOnlyDictionary<string, ShellPropertyColumnInfo>? catalog)
		{
			if (DetailsLayoutColumnDefinitions.IsBuiltIn(id))
				return DetailsLayoutColumnDefinitions.GetHeader(id);

			return catalog is not null && catalog.TryGetValue(id, out var info) ? info.DisplayName : id;
		}

		private static Dictionary<string, double> GetWidths()
		{
			var widths = new Dictionary<string, double>();
			foreach (var entry in LayoutSettingsService.DetailsLayoutColumnWidths ?? [])
			{
				var separatorIndex = entry.LastIndexOf('|');
				if (separatorIndex > 0 && double.TryParse(entry[(separatorIndex + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var width))
					widths[entry[..separatorIndex]] = width;
			}

			return widths;
		}

		private static bool IsConfigurableColumn(string id)
		{
			return !DetailsLayoutColumnOrder.IsFixed(id) && !string.IsNullOrEmpty(id);
		}
	}
}
