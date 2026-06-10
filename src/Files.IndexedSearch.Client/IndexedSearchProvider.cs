// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO.Pipes;
using System.Runtime.CompilerServices;
using Files.Search.V1;
using Files.SearchAbstraction;
using Grpc.Core;
using Grpc.Net.Client;

namespace Files.IndexedSearch.Client;

/// <summary>
/// <see cref="ISearchProvider"/> backed by the <c>files-search-service</c>
/// over gRPC on a named pipe (<c>\\.\pipe\files-search</c>).
/// Set <c>FILES_SEARCH_SERVICE_URL</c> to override with a TCP address for
/// dev / integration tests.
/// </summary>
/// <remarks>
/// The channel is constructed lazily and reused for the provider's lifetime —
/// gRPC channels multiplex concurrent calls over a single HTTP/2 connection.
/// Health checks swallow transport errors and return <c>IsAvailable=false</c>
/// so the routing layer can fall back to legacy without try/catch.
/// </remarks>
public sealed class IndexedSearchProvider : ISearchProvider, IDisposable
{
	private static string PipeName =>
		Environment.GetEnvironmentVariable("FILES_SEARCH_PIPE") ?? "files-search";

	private readonly GrpcChannel _channel;
	private readonly FilesSearch.FilesSearchClient _client;

	public IndexedSearchProvider() : this(CreateChannel()) { }

	public IndexedSearchProvider(GrpcChannel channel)
	{
		_channel = channel;
		_client  = new FilesSearch.FilesSearchClient(_channel);
	}

	public string Name => "Indexed";

	public async IAsyncEnumerable<SearchResult> SearchAsync(
		SearchQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var request = new SearchRequest
		{
			Query      = query.Text,
			MaxResults = (uint)Math.Clamp(query.MaxResults ?? 0, 0, uint.MaxValue),
		};
		foreach (var scope in query.ScopePaths)
			request.ScopePaths.Add(scope);

		using var call = _client.Search(request, cancellationToken: cancellationToken);
		await foreach (var hit in call.ResponseStream.ReadAllAsync(cancellationToken))
			yield return ToResult(hit);
	}

	public async Task<HealthStatus> GetHealthAsync(CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(TimeSpan.FromSeconds(3));
		try
		{
			var resp = await _client.HealthAsync(new HealthRequest(), cancellationToken: cts.Token);
			return new HealthStatus(
				ProviderName:     Name,
				Version:          resp.Version,
				IndexedFileCount: (long)resp.IndexedFileCount,
				IsIndexing:       resp.Indexing,
				IsAvailable:      true);
		}
		catch (Exception) when (!cancellationToken.IsCancellationRequested)
		{
			return new HealthStatus(
				ProviderName:     Name,
				Version:          string.Empty,
				IndexedFileCount: 0,
				IsIndexing:       false,
				IsAvailable:      false);
		}
	}

	public void Dispose() => _channel.Dispose();

	// ---- channel factory ---------------------------------------------------

	private static GrpcChannel CreateChannel()
	{
		// Dev / CI override: if a URL is set, use raw TCP (matches the old default).
		var envUrl = Environment.GetEnvironmentVariable("FILES_SEARCH_SERVICE_URL");
		if (envUrl is not null)
			return GrpcChannel.ForAddress(envUrl);

		return CreateNamedPipeChannel();
	}

	private static GrpcChannel CreateNamedPipeChannel()
	{
		var handler = new SocketsHttpHandler
		{
			ConnectCallback = async (_, cancellationToken) =>
			{
				var pipe = new NamedPipeClientStream(
					serverName:  ".",
					pipeName:    PipeName,
					direction:   PipeDirection.InOut,
					options:     PipeOptions.Asynchronous);
				try
				{
					await pipe.ConnectAsync(cancellationToken);
					return pipe;
				}
				catch
				{
					await pipe.DisposeAsync();
					throw;
				}
			},
		};

		// "http://localhost" is a dummy address — the transport is the named
		// pipe above, not a TCP socket. Cleartext HTTP/2 is fine for local IPC.
		return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
		{
			HttpHandler = handler,
		});
	}

	// ---- mapping -----------------------------------------------------------

	private static SearchResult ToResult(SearchHit hit) => new(
		Path:        hit.Path,
		FileName:    hit.Filename,
		// u64 → long: file sizes ≥ 8 EiB don't exist; sign wrap is benign.
		SizeBytes:   unchecked((long)hit.SizeBytes),
		ModifiedUtc: DateTimeOffset.FromUnixTimeMilliseconds(hit.ModifiedUnixMs),
		Score:       hit.Score);
}
