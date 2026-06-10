// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Search.V1;
using Files.SearchService.Index;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Files.SearchService.Grpc;

internal sealed class SearchGrpcService(FileIndex index)
	: FilesSearch.FilesSearchBase
{
	public override Task<HealthResponse> Health(HealthRequest request, ServerCallContext context) =>
		Task.FromResult(new HealthResponse
		{
			Version = typeof(SearchGrpcService).Assembly.GetName().Version?.ToString() ?? "0.0.0",
			IndexedFileCount = (ulong)index.DocCount,
			Indexing = index.IsIndexing,
		});

	public override async Task Search(
		SearchRequest request,
		IServerStreamWriter<SearchHit> responseStream,
		ServerCallContext context)
	{
		var max = request.MaxResults == 0 ? 10_000 : (int)request.MaxResults;
		var hits = index.Search(request.Query, max, request.ScopePaths);

		foreach (var hit in hits)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			await responseStream.WriteAsync(new SearchHit
			{
				Path = hit.Path,
				Filename = hit.FileName,
				SizeBytes = hit.SizeBytes,
				ModifiedUnixMs = new DateTimeOffset(hit.ModifiedUtc).ToUnixTimeMilliseconds(),
				Score = hit.Score,
			}, context.CancellationToken);
		}
	}
}
