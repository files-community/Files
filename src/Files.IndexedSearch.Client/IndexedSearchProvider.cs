// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Files.Search.V1;
using Files.SearchAbstraction;
using Grpc.Core;
using Grpc.Net.Client;

namespace Files.IndexedSearch.Client;

/// <summary>
/// <see cref="ISearchProvider"/> backed by the Rust
/// <c>files-search-service</c> over gRPC. Currently TCP on
/// <c>127.0.0.1:50080</c>; will swap to a named pipe channel once the
/// service exposes one. Override the address with
/// <c>FILES_SEARCH_SERVICE_URL</c> for tests / dev.
/// </summary>
/// <remarks>
/// <para>The channel is constructed lazily and reused for the lifetime
/// of the provider — gRPC channels multiplex concurrent calls over a
/// single HTTP/2 connection so there's no benefit to per-call
/// channels, and the connection setup is what we want to amortize.</para>
///
/// <para>Health checks swallow transport errors and return
/// <c>IsAvailable=false</c> so callers (the routing layer, the bench
/// warm-up) can branch without try/catch. Search calls let exceptions
/// propagate — the caller decides whether to fall back to the legacy
/// provider or surface the error.</para>
/// </remarks>
public sealed class IndexedSearchProvider : ISearchProvider, IDisposable
{
	private const string DefaultUrl = "http://127.0.0.1:50080";

	private readonly GrpcChannel _channel;
	private readonly FilesSearch.FilesSearchClient _client;

	public IndexedSearchProvider() : this(ResolveAddress()) { }

	public IndexedSearchProvider(string address)
	{
		_channel = GrpcChannel.ForAddress(address);
		_client = new FilesSearch.FilesSearchClient(_channel);
	}

	public string Name => "Indexed";

	public async IAsyncEnumerable<SearchResult> SearchAsync(
		SearchQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var request = new SearchRequest
		{
			Query = query.Text,
			MaxResults = (uint)Math.Clamp(query.MaxResults ?? 0, 0, uint.MaxValue),
		};
		foreach (var scope in query.ScopePaths)
			request.ScopePaths.Add(scope);

		using var call = _client.Search(request, cancellationToken: cancellationToken);
		await foreach (var hit in call.ResponseStream.ReadAllAsync(cancellationToken))
		{
			yield return ToResult(hit);
		}
	}

	public async Task<HealthStatus> GetHealthAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var resp = await _client.HealthAsync(new HealthRequest(), cancellationToken: cancellationToken);
			return new HealthStatus(
				ProviderName: Name,
				Version: resp.Version,
				IndexedFileCount: (long)resp.IndexedFileCount,
				IsIndexing: resp.Indexing,
				IsAvailable: true);
		}
		catch (RpcException) when (!cancellationToken.IsCancellationRequested)
		{
			// Service is down / unreachable. Surface as "unavailable"
			// rather than throwing so the routing layer can fall back
			// to legacy without a try/catch around every health probe.
			return new HealthStatus(
				ProviderName: Name,
				Version: string.Empty,
				IndexedFileCount: 0,
				IsIndexing: false,
				IsAvailable: false);
		}
	}

	public void Dispose() => _channel.Dispose();

	private static SearchResult ToResult(SearchHit hit) => new(
		Path: hit.Path,
		FileName: hit.Filename,
		// u64 → long: indexed file sizes >= 8 EiB don't exist in
		// practice; if one ever does, the cast wraps and is wrong by
		// a sign. Worth a comment, not a runtime check.
		SizeBytes: unchecked((long)hit.SizeBytes),
		ModifiedUtc: DateTimeOffset.FromUnixTimeMilliseconds(hit.ModifiedUnixMs),
		Score: hit.Score);

	private static string ResolveAddress() =>
		Environment.GetEnvironmentVariable("FILES_SEARCH_SERVICE_URL") ?? DefaultUrl;
}
