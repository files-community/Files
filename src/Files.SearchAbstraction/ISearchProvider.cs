// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchAbstraction;

/// <summary>
/// The single seam between the Files UI and any search backend. Every
/// search request — legacy Windows Search, the indexed Rust service, or
/// anything we ship later — flows through this interface.
/// </summary>
/// <remarks>
/// Intentionally minimal: <see cref="SearchAsync"/> streams results so
/// the UI can render the first hit before the backend has finished, and
/// <see cref="GetHealthAsync"/> exists so the bench harness and the UI
/// can both ask "is this provider responsive and how big is its index"
/// without coupling to any one implementation.
/// </remarks>
public interface ISearchProvider
{
	/// <summary>
	/// Stable identifier used in logs, bench output, and provider
	/// selection (e.g. <c>"Legacy"</c>, <c>"Indexed"</c>).
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Streams matching files. Implementations must:
	/// <list type="bullet">
	///   <item>Yield results in score / relevance order when known.</item>
	///   <item>Honor <paramref name="cancellationToken"/> promptly so
	///         the UI can cancel mid-flight when the user keeps typing.</item>
	///   <item>Complete the enumeration cleanly even on transport failure
	///         (throw on entry, not mid-stream, where possible).</item>
	/// </list>
	/// </summary>
	IAsyncEnumerable<SearchResult> SearchAsync(
		SearchQuery query,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Reports backend liveness and basic index stats. Used by the bench
	/// harness for warm-up checks and (eventually) by the UI to decide
	/// whether to fall back to the legacy provider.
	/// </summary>
	Task<HealthStatus> GetHealthAsync(CancellationToken cancellationToken = default);
}
