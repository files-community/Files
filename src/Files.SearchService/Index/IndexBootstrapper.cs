// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Usn;

namespace Files.SearchService.Index;

/// <summary>
/// Handles initial index construction and reconcile-on-restart.
/// On first run: full enumeration via USN journal (or fallback walk).
/// On restart with existing persisted index: load from disk, then
/// stat-diff to catch changes that happened while the service was offline.
/// </summary>
internal static class IndexBootstrapper
{
	public static async Task BootstrapAsync(
		FileIndex index,
		string root,
		string indexDir,
		CancellationToken cancellation)
	{
		Directory.CreateDirectory(indexDir);
		var persistPath = Path.Combine(indexDir, "index.bin");

		index.IsIndexing = true;
		try
		{
			if (File.Exists(persistPath))
			{
				await LoadAndReconcileAsync(index, root, persistPath, cancellation);
			}
			else
			{
				await BuildFreshAsync(index, root, persistPath, cancellation);
			}
		}
		finally
		{
			index.IsIndexing = false;
		}
	}

	private static async Task BuildFreshAsync(
		FileIndex index, string root, string persistPath, CancellationToken cancellation)
	{
		var reader = new UsnJournalReader(root);
		var records = new List<DocRecord>();
		const int LiveBatchSize = 50_000;

		await Task.Run(() =>
		{
			foreach (var entry in reader.Enumerate(cancellation))
			{
				records.Add(new DocRecord(entry.FullPath, entry.FileName, entry.SizeBytes, entry.ModifiedUtc));

				// Publish a snapshot every LiveBatchSize records so searches can
				// return partial results before the walk finishes.
				if (records.Count % LiveBatchSize == 0)
					index.ReplaceAll(new List<DocRecord>(records));
			}
		}, cancellation);

		index.ReplaceAll(records);
		await IndexPersistence.SaveAsync(persistPath, records, cancellation);
	}

	private static async Task LoadAndReconcileAsync(
		FileIndex index, string root, string persistPath, CancellationToken cancellation)
	{
		// Load persisted records first so the service can answer queries
		// while the reconcile walk runs.
		var persisted = await IndexPersistence.LoadAsync(persistPath, cancellation);
		index.ReplaceAll(persisted);

		// Walk the filesystem and diff against the loaded index.
		var reader = new UsnJournalReader(root);
		var fsMap = new Dictionary<string, (ulong Size, DateTime Modified)>(StringComparer.OrdinalIgnoreCase);

		await Task.Run(() =>
		{
			foreach (var entry in reader.Enumerate(cancellation))
				fsMap[entry.FullPath] = (entry.SizeBytes, entry.ModifiedUtc);
		}, cancellation);

		var persistedMap = persisted.ToDictionary(r => r.FullPath, StringComparer.OrdinalIgnoreCase);

		// Upsert new or modified files.
		foreach (var (path, (size, modified)) in fsMap)
		{
			if (!persistedMap.TryGetValue(path, out var rec) || rec.ModifiedUtc != modified)
				index.Upsert(path, Path.GetFileName(path), size, modified);
		}

		// Delete files that no longer exist on disk.
		foreach (var path in persistedMap.Keys)
		{
			if (!fsMap.ContainsKey(path))
				index.Delete(path);
		}

		// Re-persist the reconciled state.
		var all = new List<DocRecord>(fsMap.Count);
		foreach (var (path, (size, modified)) in fsMap)
			all.Add(new DocRecord(path, Path.GetFileName(path), size, modified));
		await IndexPersistence.SaveAsync(persistPath, all, cancellation);
	}
}
