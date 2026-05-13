// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchAbstraction;

/// <summary>
/// Snapshot of a provider's state. Used by the bench harness for warm-up
/// and (later) by the UI / routing layer to decide whether the indexed
/// provider is healthy enough to serve a query or whether to fall back
/// to the legacy provider.
/// </summary>
/// <param name="ProviderName">Echoes <see cref="ISearchProvider.Name"/>.</param>
/// <param name="Version">
/// Provider-defined version string. For the indexed provider this is
/// the search service's assembly version; for the legacy provider it's
/// the Files.App build version.
/// </param>
/// <param name="IndexedFileCount">
/// Files currently in the backing index. <c>0</c> when the provider has
/// no persistent index (e.g. legacy queries Windows Search live).
/// </param>
/// <param name="IsIndexing">
/// True while a background build / re-sync is in progress; queries may
/// return partial results.
/// </param>
/// <param name="IsAvailable">
/// True when the provider can serve queries right now. Distinct from
/// connectivity: a provider may be reachable but still unavailable
/// (e.g. mid-rebuild with no readable index).
/// </param>
public sealed record HealthStatus(
	string ProviderName,
	string Version,
	long IndexedFileCount,
	bool IsIndexing,
	bool IsAvailable);
