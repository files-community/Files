// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.SearchAbstraction;

/// <summary>
/// One matched file. The abstraction stays narrow on purpose — anything
/// the UI needs beyond these fields (icon, tags, etc.) is fetched lazily
/// from <see cref="Path"/> at render time, so the provider doesn't pay
/// for fields the caller may not use.
/// </summary>
/// <param name="Path">Absolute filesystem path. Acts as the result identity.</param>
/// <param name="FileName">File name without directory.</param>
/// <param name="SizeBytes">Reported file size, in bytes.</param>
/// <param name="ModifiedUtc">
/// Last-modified time, UTC. <see cref="DateTimeOffset.MinValue"/> when
/// the provider couldn't read it (e.g. stale index entry, denied stat).
/// </param>
/// <param name="Score">
/// Provider-defined relevance score; higher = more relevant. Not
/// comparable across providers.
/// </param>
public sealed record SearchResult(
	string Path,
	string FileName,
	long SizeBytes,
	DateTimeOffset ModifiedUtc,
	float Score);
