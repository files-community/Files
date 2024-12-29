// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Cloud
{
	public interface ICloudProvider : IEquatable<ICloudProvider>
	{
		public CloudProviders ID { get; }

		public string Name { get; }

		public string SyncFolder { get; }

		public byte[]? IconData { get; }
	}
}
