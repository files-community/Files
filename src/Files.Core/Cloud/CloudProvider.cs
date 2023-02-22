namespace Files.Core.Cloud
{
	public class CloudProvider : ICloudProvider
	{
		public CloudProviders ID { get; }

		public string Name { get; init; } = string.Empty;
		public string SyncFolder { get; init; } = string.Empty;
		public byte[]? IconData { get; init; }

		public CloudProvider(CloudProviders id) => ID = id;

		public override int GetHashCode() => (ID, SyncFolder).GetHashCode();
		public override bool Equals(object? o) => o is ICloudProvider other && Equals(other);
		public bool Equals(ICloudProvider? other) => other is not null && other.ID == ID && other.SyncFolder == SyncFolder;
	}
}