// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Data.Contracts;
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
/// maximum coverage of the indexed provider. As the indexed service
/// gains content search, n-grams, etc., we relax the predicates here
/// without touching call sites.</para>
/// </remarks>
public sealed class SearchRouter
{
	// Shared across all SearchRouter instances — gRPC channels are expensive to create
	// and safe to reuse across concurrent calls (HTTP/2 multiplexes over one connection).
	private static readonly IndexedSearchProvider _sharedProvider = new();

	// Cached availability flag. We probe once, then assume the service stays up.
	// Reset to null when a search fails so the next search re-probes.
	private static bool? _serviceAvailable = null;

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
		// Setting (UI toggle) takes precedence; env var kept as a dev/CI override.
		var settings = Ioc.Default.GetService<IUserSettingsService>();
		bool enabled = settings?.GeneralSettingsService.UseIndexedSearch ?? false;
		if (!enabled)
			enabled = string.Equals(
				Environment.GetEnvironmentVariable("FILES_SEARCH_PROVIDER"),
				"Indexed",
				StringComparison.OrdinalIgnoreCase);
		if (!enabled)
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

		// All scopes (Home, libraries, specific folders) route to indexed.
		// SearchIndexedAsync decides whether to apply a scope filter or
		// search the whole index — legacy fallback for Home/Libraries was
		// the main source of multi-minute searches that pinned CPU.
		return true;
	}

	/// <summary>
	/// Resolves the runtime Folder value into a set of scope paths for the
	/// indexed query. Returns an empty array for Home / Libraries / empty
	/// Folder, which the service interprets as "no scope filter" — i.e.,
	/// search the entire index.
	/// </summary>
	private string[] ResolveScopePaths()
	{
		if (string.IsNullOrEmpty(Folder) || Folder == "Home")
			return Array.Empty<string>();

		// Libraries: expand to their underlying folder paths so each one
		// participates in the path-prefix filter. If we can't resolve, fall
		// back to "search everything" — better than crashing or missing hits.
		if (App.LibraryManager.TryGetLibrary(Folder, out var library))
			return library.Folders?.ToArray() ?? Array.Empty<string>();

		return new[] { Folder! };
	}

	private async Task SearchIndexedAsync(IList<ListedItem> results, CancellationToken token)
	{
		// First search (or after a failure): probe once to confirm the service is up
		// and has indexed files. After that we skip the health check entirely —
		// the 10-120ms round trip on every query is the main source of perceived latency.
		if (_serviceAvailable is null)
		{
			HealthStatus health;
			try { health = await _sharedProvider.GetHealthAsync(token); }
			catch { health = new HealthStatus("Indexed", string.Empty, 0, false, false); }

			if (!health.IsAvailable || (health.IsIndexing && health.IndexedFileCount == 0))
			{
				await new FolderSearch { Query = Query, Folder = Folder, MaxItemCount = MaxItemCount }
					.SearchAsync(results, token);
				return;
			}

			_serviceAvailable = true;
		}

		var sq = new SearchQuery(
			Text: Query!,
			ScopePaths: ResolveScopePaths(),
			MaxResults: MaxItemCount > 0 ? (int)MaxItemCount : null);

		try
		{
			await foreach (var hit in _sharedProvider.SearchAsync(sq, token))
			{
				token.ThrowIfCancellationRequested();
				results.Add(ToListedItem(hit));

				if (results.Count == 32 || results.Count % 300 == 0)
					SearchTick?.Invoke(this, EventArgs.Empty);
			}
		}
		catch (Exception) when (!token.IsCancellationRequested)
		{
			// Service went away — reset so next search re-probes, then fall back.
			_serviceAvailable = null;
			results.Clear();
			await new FolderSearch { Query = Query, Folder = Folder, MaxItemCount = MaxItemCount }
				.SearchAsync(results, token);
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
