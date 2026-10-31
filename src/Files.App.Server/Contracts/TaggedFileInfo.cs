// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class TaggedFileInfo
{
	public string Path { get; }
	public ulong? FileId { get; }
	public string[] Tags { get; }

	internal TaggedFileInfo(TaggedFile item)
	{
		Path = item.FilePath;
		FileId = item.Frn;
		Tags = item.Tags;
	}
}
