// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchService.Index;

/// <summary>
/// Scores a filename against a query. Simpler and more useful than BM25
/// for filename search — users expect exact and prefix matches to rank first.
///
/// Score tiers:
///   1.0  exact filename match (case-insensitive)
///   0.9  filename starts with query
///   0.8  all query tokens are exact token matches in filename
///   0.6  all query tokens are prefix matches in filename tokens
///   0.4  all query tokens appear anywhere in filename (substring)
/// </summary>
internal static class Scorer
{
	public static float Score(string rawQuery, IList<string> queryTokens, string fileName)
	{
		if (fileName.Equals(rawQuery, StringComparison.OrdinalIgnoreCase))
			return 1.0f;

		if (fileName.StartsWith(rawQuery, StringComparison.OrdinalIgnoreCase))
			return 0.9f;

		var fileTokens = Tokenizer.Tokenize(fileName).ToArray();

		if (AllExact(queryTokens, fileTokens))
			return 0.8f;

		if (AllPrefix(queryTokens, fileTokens))
			return 0.6f;

		if (AllSubstring(queryTokens, fileName))
			return 0.4f;

		return 0.1f;
	}

	private static bool AllExact(IList<string> query, string[] file) =>
		query.All(q => file.Any(f => f.Equals(q, StringComparison.OrdinalIgnoreCase)));

	private static bool AllPrefix(IList<string> query, string[] file) =>
		query.All(q => file.Any(f => f.StartsWith(q, StringComparison.OrdinalIgnoreCase)));

	private static bool AllSubstring(IList<string> query, string fileName) =>
		query.All(q => fileName.Contains(q, StringComparison.OrdinalIgnoreCase));
}
