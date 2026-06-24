// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchService.Index;

/// <summary>
/// Parallel-array document store. Doc IDs are stable indices.
/// Deleted docs are marked with a null path and excluded from results.
/// Compacted on full rebuild.
/// </summary>
internal sealed class DocStore
{
	private readonly List<string?> _paths;
	private readonly List<string?> _fileNames;
	private readonly List<ulong> _sizes;
	private readonly List<DateTime> _modified;
	private readonly Dictionary<string, int> _pathToId;

	internal DocStore(int capacity = 0)
	{
		_paths = new(capacity);
		_fileNames = new(capacity);
		_sizes = new(capacity);
		_modified = new(capacity);
		_pathToId = new(capacity, StringComparer.OrdinalIgnoreCase);
	}

	internal long Count => _paths.Count(p => p is not null);

	internal int Add(string fullPath, string fileName, ulong sizeBytes, DateTime modifiedUtc)
	{
		var id = _paths.Count;
		_paths.Add(fullPath);
		_fileNames.Add(fileName);
		_sizes.Add(sizeBytes);
		_modified.Add(modifiedUtc);
		_pathToId[fullPath] = id;
		return id;
	}

	internal int FindId(string fullPath) =>
		_pathToId.TryGetValue(fullPath, out var id) ? id : -1;

	internal void MarkDeleted(int id)
	{
		if (id < 0 || id >= _paths.Count) return;
		var path = _paths[id];
		if (path is not null)
			_pathToId.Remove(path);
		_paths[id] = null;
		_fileNames[id] = null;
	}

	internal string? GetPath(int id) =>
		id >= 0 && id < _paths.Count ? _paths[id] : null;

	internal string? GetFileName(int id) =>
		id >= 0 && id < _fileNames.Count ? _fileNames[id] : null;

	internal ulong GetSize(int id) =>
		id >= 0 && id < _sizes.Count ? _sizes[id] : 0;

	internal DateTime GetModified(int id) =>
		id >= 0 && id < _modified.Count ? _modified[id] : default;

	internal IEnumerable<DocRecord> EnumerateLive()
	{
		for (int i = 0; i < _paths.Count; i++)
		{
			var path = _paths[i];
			if (path is null) continue;
			yield return new DocRecord(path, _fileNames[i]!, _sizes[i], _modified[i]);
		}
	}
}
