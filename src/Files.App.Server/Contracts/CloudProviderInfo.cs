// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Server;

public sealed partial class CloudProviderInfo
{
	public int Id { get; }
	public string Name { get; }
	public string SyncFolder { get; }
	public byte[] IconData { get; }

	internal CloudProviderInfo(ICloudProvider provider)
	{
		Id = (int)provider.ID;
		Name = provider.Name ?? string.Empty;
		SyncFolder = provider.SyncFolder ?? string.Empty;
		IconData = provider.IconData ?? [];
	}
}
