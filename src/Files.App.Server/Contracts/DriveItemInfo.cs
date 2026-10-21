// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace Files.App.Server;

public sealed partial class DriveItemInfo
{
	public string Id { get; }
	public StorageFolder Root { get; }
	public string Name { get; }
	public string Label { get; }
	public DriveType Kind { get; }
	public byte[] Icon { get; }

	internal DriveItemInfo(string id, StorageFolder root, string name, string label, DriveType kind, byte[] icon)
	{
		Id = id;
		Root = root;
		Name = name;
		Label = label;
		Kind = kind;
		Icon = icon;
	}
}
