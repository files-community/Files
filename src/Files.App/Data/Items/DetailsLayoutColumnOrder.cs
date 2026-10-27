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

		public static DetailsLayoutColumnOrder Default { get; } = new([], [], null);

		public IReadOnlyList<string> Ids { get; }

		public ColumnsViewModel? Columns { get; }

		public DetailsLayoutColumnOrder(IEnumerable<string> savedOrder, IReadOnlyList<string> propertyColumnIds, ColumnsViewModel? columns)
		{
			Columns = columns;

			var ids = savedOrder
				.Where(id => !IsFixed(id) && (DetailsLayoutColumnDefinitions.IsBuiltIn(id) || propertyColumnIds.Contains(id)))
				.Distinct()
				.ToList();

			var movableIds = DetailsLayoutColumnIds.All.Where(id => !IsFixed(id)).ToArray();
			for (int i = 0; i < movableIds.Length; i++)
			{
				if (!ids.Contains(movableIds[i]))
					ids.Insert(Math.Min(i, ids.Count), movableIds[i]);
			}

			foreach (var id in propertyColumnIds)
			{
				if (!ids.Contains(id))
					ids.Add(id);
			}

			ids.InsertRange(0, [DetailsLayoutColumnIds.Icon, DetailsLayoutColumnIds.Name]);

			Ids = ids;
			_indices = new(ids.Count);
			for (int i = 0; i < ids.Count; i++)
				_indices[ids[i]] = i;
		}

		/// <summary>
		/// Gets whether the column always keeps its position at the start of the row.
		/// </summary>
		public static bool IsFixed(string? id)
		{
			return id is DetailsLayoutColumnIds.Icon or DetailsLayoutColumnIds.Name;
		}

		public int IndexOf(string? id)
		{
			return id is not null && _indices.TryGetValue(id, out var index) ? index : -1;
		}
	}
}
