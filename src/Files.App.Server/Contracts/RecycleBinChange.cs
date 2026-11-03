// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public enum RecycleBinChangeKind
{
	Refresh,
	Created,
	Deleted,
	Changed,
	Renamed,
}

public struct RecycleBinChange
{
	public RecycleBinChangeKind Kind;
	public string Directory;
	public string Name;
	public string OldName;
}
