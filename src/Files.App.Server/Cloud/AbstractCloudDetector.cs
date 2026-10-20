// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

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

			try
			{
				await foreach (var provider in GetProviders())
					providers.Add(provider);
			}
			catch (Exception ex)
			{
				Program.Logger.LogWarning(ex, "Cloud discovery was incomplete for {Detector}.", GetType().Name);
			}

			return providers;
		}

		protected abstract IAsyncEnumerable<ICloudProvider> GetProviders();
	}
}
