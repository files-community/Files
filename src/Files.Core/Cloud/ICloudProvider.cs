using System;

namespace Files.Core.Cloud
{
	public interface ICloudProvider : IEquatable<ICloudProvider>
	{
		public CloudProviders ID { get; }

		public string Name { get; }
		public string SyncFolder { get; }
		public byte[]? IconData { get; }
	}
}