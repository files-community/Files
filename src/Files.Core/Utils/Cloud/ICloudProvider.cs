﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Utils.Cloud
{
	public interface ICloudProvider : IEquatable<ICloudProvider>
	{
		public CloudProviders ID { get; }

		public string Name { get; }

		public string SyncFolder { get; }

		public byte[]? IconData { get; }
	}
}
