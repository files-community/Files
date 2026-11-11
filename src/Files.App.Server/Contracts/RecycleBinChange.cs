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
	// Explicit padding keeps HSTRING offsets and array stride consistent on Windows 10.
	public int Reserved;
	public string Directory;
	public string Name;
	public string OldName;
}
