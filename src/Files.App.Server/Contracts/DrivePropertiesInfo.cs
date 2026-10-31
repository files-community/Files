// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class DrivePropertiesInfo
{
	public bool Available { get; }
	public ulong Capacity { get; }
	public ulong FreeSpace { get; }
	public string FileSystem { get; }

	internal DrivePropertiesInfo(bool available, ulong capacity, ulong freeSpace, string fileSystem)
	{
		Available = available;
		Capacity = capacity;
		FreeSpace = freeSpace;
		FileSystem = fileSystem;
	}
}
