// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.IndexedSearch.Client;
using Files.SearchAbstraction;
using System.IO;

namespace Files.App.Utils.Storage;

/// <summary>
/// Drop-in replacement for <see cref="FolderSearch"/> that picks
/// between the legacy Windows.Storage.Search path and the new indexed
/// gRPC service based on a runtime opt-in.
/// </summary>
/// <remarks>
/// <para>Default behavior is identical to <see cref="FolderSearch"/> —
/// the indexed path is only taken when <c>FILES_SEARCH_PROVIDER=Indexed</c>
/// is set in the environment AND the query has a shape the indexed
/// provider supports today (no glob, no AQS prefix, no library scope).
/// Per CLAUDE.md the default stays Legacy until the bench gates are met.</para>
///
/// <para>The routing rules below intentionally lean toward "fall back to
/// legacy on anything ambiguous" — the goal is correctness parity, not
/// maximum coverage of the indexed provider. As Tantivy gains content
/// search, n-grams, etc., we relax the predicates here without
/// touching call sites.</para>
/// </remarks>
public sealed class SearchRouter
{
	public string? Query { get; set; }
	public string? Folder { get; set; }
	public uint MaxItemCount { get; set; } = 0;
	public EventHandler? SearchTick;

	public async Task SearchAsync(IList<ListedItem> results, CancellationToken token)
	{
		if (UseIndexed())
		{
			await SearchIndexedAsync(results, token);
			return;
		}

		// Legacy path — delegate verbatim to the upstream implementation.
		// Forwarding SearchTick keeps the existing batched-render UX.
		var legacy = new FolderSearch
		{
			Query = Query,
			Folder = Folder,
			MaxItemCount = MaxItemCount,
		};
		legacy.SearchTick += (_, e) => SearchTick?.Invoke(this, e);
		await legacy.SearchAsync(results, token);
	}

	public async Task<ObservableCollection<ListedItem>> SearchAsync()
	{
		var results = new ObservableCollection<ListedItem>();
		await SearchAsync(results, CancellationToken.None);
		return results;
	}

	private bool UseIndexed()
	{
		if (!string.Equals(
			Environment.GetEnvironmentVariable("FILES_SEARCH_PROVIDER"),
			"Indexed",
			StringComparison.OrdinalIgnoreCase))
			return false;

		if (string.IsNullOrEmpty(Query))
			return false;

		// Glob, AQS prefix, and explicit AQS field syntax all need
		// legacy. Keep this list aligned with what the indexed provider
		// actually understands.
		if (Query.Contains('*') || Query.Contains('?'))
			return false;
		if (Query.StartsWith('$'))
			return false;
		if (Query.Contains(':'))
			return false;

		// Library and Home scopes need fan-out logic the indexed
		// provider doesn't have yet. Real on-disk paths route to indexed.
		if (string.IsNullOrEmpty(Folder) || Folder == "Home")
			return false;
		if (App.LibraryManager.TryGetLibrary(Folder, out _))
			return false;

		return true;
	}

	private async Task SearchIndexedAsync(IList<ListedItem> results, CancellationToken token)
	{
		using var provider = new IndexedSearchProvider();

		// Health probe: if the service isn't running, fall back to
		// legacy rather than failing the search. Users who opt in via
		// env var still get *a* result.
		var health = await provider.GetHealthAsync(token);
		if (!health.IsAvailable)
		{
			await new FolderSearch { Query = Query, Folder = Folder, MaxItemCount = MaxItemCount }
				.SearchAsync(results, token);
			return;
		}

		var sq = new SearchQuery(
			Text: Query!,
			ScopePaths: new[] { Folder! },
			MaxResults: MaxItemCount > 0 ? (int)MaxItemCount : null);

		await foreach (var hit in provider.SearchAsync(sq, token))
		{
			token.ThrowIfCancellationRequested();
			results.Add(ToListedItem(hit));

			// Mirror FolderSearch's batched-render cadence so the UI
			// updates feel the same regardless of provider.
			if (results.Count == 32 || results.Count % 300 == 0)
				SearchTick?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Builds a minimal <see cref="ListedItem"/> directly from indexed
	/// metadata — no per-file <c>StorageFile.GetFileFromPathAsync</c>
	/// round-trip. Thumbnails and extended properties get loaded lazily
	/// by the layout's existing image-loading pipeline, same as for any
	/// other ListedItem.
	/// </summary>
	private static ListedItem ToListedItem(SearchResult hit)
	{
		var ext = hit.FileName.Contains('.', StringComparison.Ordinal)
			? Path.GetExtension(hit.FileName)
			: null;
		var itemType = ext is not null ? ext.Trim('.') + " " : null;

		return new ListedItem(null)
		{
			PrimaryItemAttribute = Windows.Storage.StorageItemTypes.File,
			ItemNameRaw = hit.FileName,
			ItemPath = hit.Path,
			LoadFileIcon = false,
			FileExtension = ext,
			FileSizeBytes = hit.SizeBytes,
			FileSize = ((ulong)hit.SizeBytes).ToSizeString(),
			ItemDateModifiedReal = hit.ModifiedUtc,
			// Indexed schema doesn't carry creation time; surface the
			// modified time so sorting by date doesn't show a 1601-01-01
			// fallback in the UI. Acceptable v0 fidelity loss.
			ItemDateCreatedReal = hit.ModifiedUtc,
			ItemType = itemType,
			NeedsPlaceholderGlyph = false,
			Opacity = 1,
		};
	}
}
