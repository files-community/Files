// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class PinnedFolderInfo
{
	public string ParsingPath { get; }
	public string Tooltip { get; }
	public string Path { get; }
	public string Name { get; }
	public byte[] Pidl { get; }
	public bool IsPinned { get; }

	internal PinnedFolderInfo(string path, string name, byte[] pidl, bool isPinned, string parsingPath, string tooltip)
	{
		Path = path;
		Name = name;
		Pidl = pidl;
		ParsingPath = parsingPath;
		Tooltip = tooltip;
		IsPinned = isPinned;
	}
}
