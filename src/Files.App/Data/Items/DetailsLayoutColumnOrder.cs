// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents the display order of the columns shown in <see cref="DetailsLayoutPage"/>.
	/// </summary>
	public sealed partial class DetailsLayoutColumnOrder
	{
		private readonly Dictionary<string, int> _indices;

		public static DetailsLayoutColumnOrder Default { get; } = new([], null);

		public IReadOnlyList<string> Ids { get; }

		public ColumnsViewModel? Columns { get; }

		public DetailsLayoutColumnOrder(IEnumerable<string> savedOrder, ColumnsViewModel? columns)
		{
			Columns = columns;

			var ids = savedOrder
				.Where(id => !IsPinned(id) && DetailsLayoutColumnDefinitions.IsBuiltIn(id))
				.Distinct()
				.ToList();

			var movableIds = DetailsLayoutColumnIds.BuiltIn.Where(id => !IsPinned(id)).ToArray();
			for (int i = 0; i < movableIds.Length; i++)
			{
				if (!ids.Contains(movableIds[i]))
					ids.Insert(Math.Min(i, ids.Count), movableIds[i]);
			}

			ids.Insert(0, DetailsLayoutColumnIds.Icon);

			Ids = ids;
			_indices = new(ids.Count);
			for (int i = 0; i < ids.Count; i++)
				_indices[ids[i]] = i;
		}

		/// <summary>
		/// Gets whether the column is always shown.
		/// </summary>
		public static bool IsRequired(string? id)
		{
			return id is DetailsLayoutColumnIds.Icon or DetailsLayoutColumnIds.Name;
		}

		/// <summary>
		/// Gets whether the column always keeps its position at the start of the row.
		/// </summary>
		public static bool IsPinned(string? id)
		{
			return id is DetailsLayoutColumnIds.Icon;
		}

		public int IndexOf(string? id)
		{
			return id is not null && _indices.TryGetValue(id, out var index) ? index : -1;
		}

		/// <summary>
		/// Gets the first shown column after the icon, whose cells are indented and use the primary text style.
		/// </summary>
		public string? GetPrimaryId()
		{
			foreach (var id in Ids)
			{
				if (!IsPinned(id) && Columns?.FindColumn(id)?.Visibility == Microsoft.UI.Xaml.Visibility.Visible)
					return id;
			}

			return null;
		}
	}
}
