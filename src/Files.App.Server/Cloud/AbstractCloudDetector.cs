// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;

namespace Files.App.Server.Cloud
{
	internal abstract class AbstractCloudDetector
	{
		static AbstractCloudDetector() => SQLitePCL.Batteries_V2.Init();

		protected static SqliteConnection CreateReadOnlyConnection(string path)
			=> new(new SqliteConnectionStringBuilder
			{
				DataSource = path,
				Mode = SqliteOpenMode.ReadOnly,
				Pooling = false,
			}.ToString());

		public async Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync()
		{
			var providers = new List<ICloudProvider>();

			await foreach (var provider in GetProviders())
				providers.Add(provider);

			return providers;
		}

		protected abstract IAsyncEnumerable<ICloudProvider> GetProviders();
	}
}
