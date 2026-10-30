// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class PinnedFolderInfo
{
	public string ParsingPath { get; }
	public string Path { get; }
	public string Name { get; }
	public byte[] Pidl { get; }
	public bool IsPinned { get; }

	internal PinnedFolderInfo(string path, string name, byte[] pidl, bool isPinned, string parsingPath)
	{
		Path = path;
		Name = name;
		Pidl = pidl;
		ParsingPath = parsingPath;
		IsPinned = isPinned;
	}
}
