// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class RecycleBinInfo
{
	public bool Available { get; }
	public long Count { get; }
	public long Size { get; }

	internal RecycleBinInfo(bool available, long count, long size)
	{
		Available = available;
		Count = count;
		Size = size;
	}
}
