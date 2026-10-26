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

		public static DetailsLayoutColumnOrder Default { get; } = new([]);

		public IReadOnlyList<string> Ids { get; }

		public DetailsLayoutColumnOrder(IEnumerable<string> savedOrder)
		{
			var ids = savedOrder
				.Where(id => id != DetailsLayoutColumnIds.Icon && DetailsLayoutColumnIds.All.Contains(id))
				.Distinct()
				.ToList();

			for (int i = 1; i < DetailsLayoutColumnIds.All.Count; i++)
			{
				if (!ids.Contains(DetailsLayoutColumnIds.All[i]))
					ids.Insert(Math.Min(i - 1, ids.Count), DetailsLayoutColumnIds.All[i]);
			}

			ids.Insert(0, DetailsLayoutColumnIds.Icon);

			Ids = ids;
			_indices = new(ids.Count);
			for (int i = 0; i < ids.Count; i++)
				_indices[ids[i]] = i;
		}

		public int IndexOf(string? id)
		{
			return id is not null && _indices.TryGetValue(id, out var index) ? index : -1;
		}
	}
}
