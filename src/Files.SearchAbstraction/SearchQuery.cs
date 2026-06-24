// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchAbstraction;

/// <summary>
/// Immutable description of a single search request.
/// </summary>
/// <param name="Text">
/// Raw user query. Tokenization, glob expansion, and AQS detection are
/// the provider's responsibility — the abstraction does not parse.
/// </param>
/// <param name="ScopePaths">
/// Roots that constrain results. Empty means "wherever the provider
/// indexes by default". Each path is an absolute filesystem path; matches
/// are by path-prefix (i.e. include subdirectories).
/// </param>
/// <param name="MaxResults">
/// Cap on results yielded. <c>null</c> means no caller cap; providers
/// may still impose their own ceiling for safety.
/// </param>
public sealed record SearchQuery(
	string Text,
	IReadOnlyList<string> ScopePaths,
	int? MaxResults = null)
{
	public static SearchQuery ForText(string text) =>
		new(text, Array.Empty<string>());
}
