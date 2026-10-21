// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class ShellLocationInfo
{
	public string Path { get; }
	public string Identity { get; }
	public string Name { get; }
	public byte[] Pidl { get; }
	public DateTimeOffset Modified { get; }

	internal ShellLocationInfo(string path, string identity, string name, byte[] pidl, DateTimeOffset modified)
	{
		Path = path;
		Identity = identity;
		Name = name;
		Pidl = pidl;
		Modified = modified;
	}
}
