// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchService.Index;

/// <summary>
/// Thread-safe in-memory inverted index over file names.
///
/// Structure:
///   _docs         — parallel arrays: paths, filenames, sizes, modified times.
///                   Doc IDs are indices into these arrays.
///   _index        — token → sorted int[] of doc IDs (posting list).
///                   Handles whole-word and prefix queries via camelCase/delimiter tokens.
///   _trigramIndex — trigram → sorted int[] of doc IDs.
///                   Handles mid-string substring queries (e.g. "phab" → "ALPHABET.md").
///                   Both are replaced atomically on rebuild; upserts acquire a write lock.
///
/// Query reads snapshot the current index references — no lock needed.
/// Writes (upsert/delete) acquire a write lock and update in place.
/// </summary>
public sealed class FileIndex
{
	// Doc store — indexed by doc ID.
	private volatile DocStore _docs = new();

	// Token inverted index — swapped atomically on rebuild.
	private volatile Dictionary<string, int[]> _index = [];

	// Trigram index for mid-string substring search — swapped atomically on rebuild.
	// Keys are 3-char lowercase substrings of filenames; Ordinal comparison (already lowercased).
	private volatile Dictionary<string, int[]> _trigramIndex = [];

	private readonly ReaderWriterLockSlim _lock = new();

	public long DocCount => _docs.Count;
	public bool IsIndexing { get; internal set; }

	private volatile bool _dirty;
	public bool IsDirty => _dirty;
	internal void MarkClean() => _dirty = false;

	internal List<DocRecord> GetAllRecords()
	{
		_lock.EnterReadLock();
		try { return [.. _docs.EnumerateLive()]; }
		finally { _lock.ExitReadLock(); }
	}

	// ---- Bulk replace (initial build / full rebuild) -----------------------

	internal void ReplaceAll(List<DocRecord> records)
	{
		_lock.EnterWriteLock();
		try
		{
			var store = new DocStore(records.Count);
			var index = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
			var trigrams = new Dictionary<string, List<int>>(StringComparer.Ordinal);

			for (int id = 0; id < records.Count; id++)
			{
				var r = records[id];
				store.Add(r.FullPath, r.FileName, r.SizeBytes, r.ModifiedUtc);

				foreach (var token in Tokenizer.Tokenize(r.FileName))
				{
					if (!index.TryGetValue(token, out var list))
						index[token] = list = [];
					list.Add(id);
				}

				foreach (var tg in Trigrams(r.FileName))
				{
					if (!trigrams.TryGetValue(tg, out var tgList))
						trigrams[tg] = tgList = [];
					tgList.Add(id);
				}
			}

			// Convert to sorted arrays for fast intersection.
			var frozen = new Dictionary<string, int[]>(index.Count, StringComparer.OrdinalIgnoreCase);
			foreach (var (token, list) in index)
			{
				list.Sort();
				frozen[token] = [.. list];
			}

			var frozenTrigrams = new Dictionary<string, int[]>(trigrams.Count, StringComparer.Ordinal);
			foreach (var (tg, list) in trigrams)
			{
				list.Sort();
				frozenTrigrams[tg] = [.. list];
			}

			_docs = store;
			_index = frozen;
			_trigramIndex = frozenTrigrams;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	// ---- Incremental updates (watcher) ------------------------------------

	internal void Upsert(string fullPath, string fileName, ulong sizeBytes, DateTime modifiedUtc)
	{
		_lock.EnterWriteLock();
		try
		{
			// Remove existing doc for this path if present.
			var existing = _docs.FindId(fullPath);
			if (existing >= 0)
				RemoveFromIndex(existing);

			var id = _docs.Add(fullPath, fileName, sizeBytes, modifiedUtc);
			foreach (var token in Tokenizer.Tokenize(fileName))
				InsertPosting(token, id);
			foreach (var tg in Trigrams(fileName))
				InsertTrigramPosting(tg, id);
			_dirty = true;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	internal void Delete(string fullPath)
	{
		_lock.EnterWriteLock();
		try
		{
			var id = _docs.FindId(fullPath);
			if (id >= 0)
			{
				RemoveFromIndex(id);
				_dirty = true;
			}
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	// ---- Query (lock-free snapshot read) ----------------------------------

	internal IReadOnlyList<QueryHit> Search(
		string query, int maxResults, IReadOnlyList<string> scopePaths)
	{
		// Snapshot — no lock needed; all three references are volatile.
		var docs = _docs;
		var index = _index;
		var trigramIndex = _trigramIndex;

		var tokens = Tokenizer.Tokenize(query).ToList();
		if (tokens.Count == 0)
			return [];

		// Token-based AND intersection (whole-word matches).
		var tokenHits = TryTokenIntersect(index, tokens);

		// Trigram-based substring search starts at 3 chars (the trigram width).
		// For 3-char queries the trigram intersection is just one posting list,
		// which used to flood results — but now the two-tier scoring pass keeps
		// the top-N by relevance (exact > startsWith > substring), so the noise
		// sinks to the bottom and only displays if the user scrolls.
		var trigramHits = query.Length >= 3 ? TryTrigramIntersect(trigramIndex, docs, query) : null;

		// Union both candidate sets; early out if both are empty.
		var candidates = Union(tokenHits ?? [], trigramHits ?? []);
		if (candidates.Length == 0)
			return [];

		// Score-then-truncate, but in two passes:
		//
		//   1. Cheap score (no tokenization) for every candidate. Distinguishes
		//      exact / prefix / substring / no-match in O(filename length).
		//   2. Sort by cheap score, take top N, then refine those N with the
		//      full Scorer (which tokenizes for camelCase-aware prefix matching).
		//
		// This avoids the perf cliff for common terms like "json" that match
		// 100k+ candidates — tokenizing every filename in the bulk pass turned
		// 30ms searches into 1+ second searches.
		var scored = new List<QueryHit>(Math.Min(candidates.Length, 32_768));
		foreach (var id in candidates)
		{
			var path = docs.GetPath(id);
			if (path is null) continue;
			if (scopePaths.Count > 0 && !scopePaths.Any(s => path.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
				continue;

			var fileName = docs.GetFileName(id) ?? string.Empty;
			var quick = QuickScore(query, fileName);
			scored.Add(new QueryHit(path, fileName, docs.GetSize(id), docs.GetModified(id), quick));
		}

		scored.Sort(static (a, b) => b.Score.CompareTo(a.Score));
		var top = scored.Count > maxResults ? scored.GetRange(0, maxResults) : scored;

		// Refine top-N with the precise Scorer so camelCase-prefix matches
		// (0.6 tier) sort above plain-substring matches (0.4 tier).
		for (int i = 0; i < top.Count; i++)
		{
			var precise = Scorer.Score(query, tokens, top[i].FileName);
			if (precise != top[i].Score)
				top[i] = top[i] with { Score = precise };
		}
		top.Sort(static (a, b) => b.Score.CompareTo(a.Score));
		return top;
	}

	/// <summary>
	/// O(filename length) tier classifier — no tokenization. Coarse enough
	/// to triage 100k+ candidates fast; precise enough that the top N from
	/// this pass are guaranteed to contain the true top N by full Scorer.
	/// </summary>
	private static float QuickScore(string query, string fileName)
	{
		if (fileName.Equals(query, StringComparison.OrdinalIgnoreCase))
			return 1.0f;
		if (fileName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
			return 0.9f;
		if (fileName.Contains(query, StringComparison.OrdinalIgnoreCase))
			return 0.4f;
		return 0.1f;
	}

	private static int[]? TryTokenIntersect(Dictionary<string, int[]> index, List<string> tokens)
	{
		int[]? hits = null;
		foreach (var token in tokens)
		{
			if (!index.TryGetValue(token, out var posting))
				return null;
			hits = hits is null ? posting : Intersect(hits, posting);
			if (hits.Length == 0)
				return null;
		}
		return hits;
	}

	private static int[]? TryTrigramIntersect(
		Dictionary<string, int[]> trigramIndex, DocStore docs, string query)
	{
		var queryLower = query.ToLowerInvariant();
		int[]? hits = null;
		var seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (var tg in Trigrams(queryLower))
		{
			if (!seen.Add(tg)) continue; // skip duplicate trigrams in query
			if (!trigramIndex.TryGetValue(tg, out var posting))
				return null;
			hits = hits is null ? posting : Intersect(hits, posting);
			if (hits.Length == 0)
				return null;
		}

		if (hits is null)
			return null;

		// Filter false positives: confirm the filename actually contains the query as a substring.
		return Array.FindAll(hits, id =>
			docs.GetPath(id) is not null &&
			(docs.GetFileName(id) ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase));
	}

	// ---- Private helpers --------------------------------------------------

	private void InsertPosting(string token, int docId)
	{
		if (_index.TryGetValue(token, out var existing))
		{
			var idx = Array.BinarySearch(existing, docId);
			if (idx < 0)
			{
				var newArr = new int[existing.Length + 1];
				var insertAt = ~idx;
				existing.AsSpan(0, insertAt).CopyTo(newArr);
				newArr[insertAt] = docId;
				existing.AsSpan(insertAt).CopyTo(newArr.AsSpan(insertAt + 1));
				_index[token] = newArr;
			}
		}
		else
		{
			_index[token] = [docId];
		}
	}

	private void InsertTrigramPosting(string trigram, int docId)
	{
		if (_trigramIndex.TryGetValue(trigram, out var existing))
		{
			var idx = Array.BinarySearch(existing, docId);
			if (idx < 0)
			{
				var newArr = new int[existing.Length + 1];
				var insertAt = ~idx;
				existing.AsSpan(0, insertAt).CopyTo(newArr);
				newArr[insertAt] = docId;
				existing.AsSpan(insertAt).CopyTo(newArr.AsSpan(insertAt + 1));
				_trigramIndex[trigram] = newArr;
			}
		}
		else
		{
			_trigramIndex[trigram] = [docId];
		}
	}

	private void RemoveFromIndex(int docId)
	{
		_docs.MarkDeleted(docId);
		// Posting lists are cleaned lazily on next rebuild to avoid
		// O(n) removal from every posting list on every delete.
	}

	// Yields all 3-char substrings of the lowercased filename.
	private static IEnumerable<string> Trigrams(string fileName)
	{
		var s = fileName.ToLowerInvariant();
		for (int i = 0; i <= s.Length - 3; i++)
			yield return s.Substring(i, 3);
	}

	private static int[] Intersect(int[] a, int[] b)
	{
		var result = new List<int>(Math.Min(a.Length, b.Length));
		int i = 0, j = 0;
		while (i < a.Length && j < b.Length)
		{
			if (a[i] == b[j]) { result.Add(a[i]); i++; j++; }
			else if (a[i] < b[j]) i++;
			else j++;
		}
		return [.. result];
	}

	// Sorted merge of two sorted doc-ID arrays, deduplicating shared IDs.
	private static int[] Union(int[] a, int[] b)
	{
		if (a.Length == 0) return b;
		if (b.Length == 0) return a;
		var result = new List<int>(a.Length + b.Length);
		int i = 0, j = 0;
		while (i < a.Length && j < b.Length)
		{
			if (a[i] == b[j]) { result.Add(a[i]); i++; j++; }
			else if (a[i] < b[j]) { result.Add(a[i]); i++; }
			else { result.Add(b[j]); j++; }
		}
		while (i < a.Length) result.Add(a[i++]);
		while (j < b.Length) result.Add(b[j++]);
		return [.. result];
	}
}

internal readonly record struct DocRecord(
	string FullPath, string FileName, ulong SizeBytes, DateTime ModifiedUtc);

internal readonly record struct QueryHit(
	string Path, string FileName, ulong SizeBytes, DateTime ModifiedUtc, float Score);
