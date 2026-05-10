// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Files.SearchAbstraction;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.LegacySearch;

/// <summary>
/// Wraps the Windows Search / AQS query path that upstream's
/// <c>FolderSearch</c> uses, exposed through <see cref="ISearchProvider"/>
/// so the bench harness can A/B it against the indexed provider.
/// </summary>
/// <remarks>
/// Per CLAUDE.md this provider is the frozen reference baseline. The AQS
/// construction and <see cref="QueryOptions"/> shape mirror upstream
/// (`FolderSearch.AQSQuery` / `FolderSearch.ToQueryOptions`); only the
/// UI-coupled bits (ListedItem, thumbnail prefetch, IoC services) are
/// dropped because the abstraction's <see cref="SearchResult"/> doesn't
/// need them. Bug-for-bug parity with upstream is the goal — fixes only
/// land here when they land upstream first.
/// </remarks>
public sealed class LegacySearchProvider : ISearchProvider
{
	private const uint StepSize = 500;

	private static readonly string AssemblyVersion =
		typeof(LegacySearchProvider).Assembly.GetName().Version?.ToString() ?? "0.0";

	public string Name => "Legacy";

	public async IAsyncEnumerable<SearchResult> SearchAsync(
		SearchQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(query.Text))
			yield break;

		var aqs = BuildAqs(query.Text);
		var max = query.MaxResults ?? int.MaxValue;
		var roots = query.ScopePaths.Count > 0
			? query.ScopePaths
			: new[] { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };

		var emitted = 0;
		foreach (var root in roots)
		{
			if (emitted >= max)
				yield break;

			cancellationToken.ThrowIfCancellationRequested();
			var folder = await TryGetFolderAsync(root, cancellationToken);
			if (folder is null)
				continue;

			var options = BuildQueryOptions(aqs);
			var fileQuery = folder.CreateFileQueryWithOptions(options);

			uint index = 0;
			while (true)
			{
				if (emitted >= max)
					yield break;

				cancellationToken.ThrowIfCancellationRequested();
				var step = (uint)Math.Min(StepSize, max - emitted);
				var batch = await fileQuery.GetFilesAsync(index, step).AsTask(cancellationToken);
				if (batch.Count == 0)
					break;

				foreach (var file in batch)
				{
					if (emitted >= max)
						yield break;

					cancellationToken.ThrowIfCancellationRequested();
					var hit = await TryToResultAsync(file, cancellationToken);
					if (hit is not null)
					{
						emitted++;
						yield return hit;
					}
				}
				index += (uint)batch.Count;
			}
		}
	}

	public Task<HealthStatus> GetHealthAsync(CancellationToken cancellationToken = default)
	{
		// Legacy doesn't manage an index — Windows Search is always
		// "available" from this provider's perspective. IndexedFileCount
		// stays 0 because we don't own the indexer's stats.
		var status = new HealthStatus(
			ProviderName: Name,
			Version: AssemblyVersion,
			IndexedFileCount: 0,
			IsIndexing: false,
			IsAvailable: true);
		return Task.FromResult(status);
	}

	private static async Task<StorageFolder?> TryGetFolderAsync(string path, CancellationToken ct)
	{
		try
		{
			return await StorageFolder.GetFolderFromPathAsync(path).AsTask(ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			// Path may be inaccessible (permissions, missing, network),
			// or not a folder; treat as "no results in this scope" to
			// match upstream's swallow-and-continue behavior.
			return null;
		}
	}

	private static async Task<SearchResult?> TryToResultAsync(StorageFile file, CancellationToken ct)
	{
		try
		{
			var props = await file.GetBasicPropertiesAsync().AsTask(ct);
			return new SearchResult(
				Path: file.Path,
				FileName: file.Name,
				SizeBytes: (long)props.Size,
				ModifiedUtc: props.DateModified,
				Score: 1.0f);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			// Stat failures are common during search (file deleted
			// between enumeration and properties read). Skip silently
			// rather than aborting the whole stream.
			return null;
		}
	}

	private static QueryOptions BuildQueryOptions(string aqs)
	{
		var options = new QueryOptions
		{
			FolderDepth = FolderDepth.Deep,
			UserSearchFilter = aqs,
			IndexerOption = IndexerOption.UseIndexerWhenAvailable,
		};
		options.SortOrder.Clear();
		options.SortOrder.Add(new SortEntry
		{
			PropertyName = "System.Search.Rank",
			AscendingOrder = false,
		});
		return options;
	}

	/// <summary>
	/// Mirrors <c>FolderSearch.AQSQuery</c>: '$' prefix means "raw AQS,
	/// strip the prefix"; ':' anywhere means "user knows AQS, pass
	/// through"; otherwise wrap as <c>System.FileName:"foo*"</c> with
	/// the same dot-aware wildcard expansion (<c>foo.docx</c> →
	/// <c>foo*.docx*</c>).
	/// </summary>
	private static string BuildAqs(string text)
	{
		if (text.StartsWith('$'))
			return text[1..];
		if (text.Contains(':'))
			return text;

		string wildcard;
		if (text.Contains('.'))
		{
			var parts = text.Split('.');
			var leading = string.Join('.', parts.SkipLast(1));
			wildcard = $"{leading}*.{parts[^1]}*";
		}
		else
		{
			wildcard = $"{text}*";
		}
		return $"System.FileName:\"{wildcard}\"";
	}
}
