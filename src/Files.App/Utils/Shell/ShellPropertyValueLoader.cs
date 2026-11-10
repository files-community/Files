// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.Concurrent;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Loads the Windows property values of items, such as for the property columns of <see cref="DetailsLayoutPage"/>.
	/// </summary>
	public static class ShellPropertyValueLoader
	{
		private const int BatchSize = 64;

		private static readonly ConcurrentDictionary<ListedItem, Task> _loads = new();

		/// <summary>
		/// Loads the specified properties of the item that it doesn't have yet.
		/// </summary>
		public static async Task LoadAsync(ListedItem item, IReadOnlyList<string> propertyIds)
		{
			if (propertyIds.Count == 0 || item.ItemPath is not { Length: > 0 } path)
				return;

			// The second pass reads properties another caller's load left out
			for (var pass = 0; pass < 2; pass++)
			{
				var missing = propertyIds.Where(id => item.ShellPropertyValues?.ContainsKey(id) is not true).ToArray();
				if (missing.Length == 0)
					return;

				var load = _loads.GetOrAdd(item, _ => ReadAsync(item, path, missing));
				try
				{
					await load;
				}
				finally
				{
					_loads.TryRemove(new(item, load));
				}
			}
		}

		/// <summary>
		/// Loads the specified properties of many items in batches, such as for sorting a folder.
		/// </summary>
		public static async Task LoadAsync(IReadOnlyList<ListedItem> items, IReadOnlyList<string> propertyIds, CancellationToken token)
		{
			if (propertyIds.Count == 0)
				return;

			var loadedByOthers = new List<ListedItem>();
			var batches = new List<Task>();
			foreach (var batch in items.Where(item => item.ItemPath is { Length: > 0 } && propertyIds.Any(id => item.ShellPropertyValues?.ContainsKey(id) is not true)).Chunk(BatchSize))
			{
				var completion = new TaskCompletionSource();
				var owned = new List<ListedItem>(batch.Length);
				foreach (var item in batch)
					(_loads.TryAdd(item, completion.Task) ? owned : loadedByOthers).Add(item);

				batches.Add(ReadBatchAsync(owned, propertyIds, completion, token));
			}

			await Task.WhenAll(batches);

			// Reads properties the other callers' loads left out
			if (!token.IsCancellationRequested && loadedByOthers.Count > 0)
				await Task.WhenAll(loadedByOthers.Select(item => LoadAsync(item, propertyIds)));
		}

		private static async Task ReadBatchAsync(List<ListedItem> items, IReadOnlyList<string> propertyIds, TaskCompletionSource completion, CancellationToken token)
		{
			try
			{
				var values = await ShellPropertyReader.ReadAsync(items.Select(item => item.ItemPath!).ToArray(), propertyIds, token);
				for (var i = 0; i < items.Count; i++)
				{
					if (values[i] is { } itemValues)
						SetValues(items[i], itemValues);
				}
			}
			finally
			{
				completion.SetResult();
				foreach (var item in items)
					_loads.TryRemove(new(item, completion.Task));
			}
		}

		/// <summary>
		/// Reads the properties the item already has again, such as after the file changed.
		/// </summary>
		public static async Task ReloadAsync(ListedItem item)
		{
			if (item.ShellPropertyValues is not { Count: > 0 } current || item.ItemPath is not { Length: > 0 } path)
				return;

			item.ShellPropertyValues = await ShellPropertyReader.ReadAsync(path, current.Keys.ToArray());
		}

		private static async Task ReadAsync(ListedItem item, string path, string[] propertyIds)
		{
			SetValues(item, await ShellPropertyReader.ReadAsync(path, propertyIds));
		}

		private static void SetValues(ListedItem item, Dictionary<string, ShellPropertyValue> values)
		{
			if (item.ShellPropertyValues is { } current)
			{
				foreach (var (id, value) in current)
					values.TryAdd(id, value);
			}

			item.ShellPropertyValues = values;
		}
	}
}
