// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for cloud detection.
	/// </summary>
	public sealed class CloudDetector : ICloudDetector
	{
		public async Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync()
		{
			var providers = new List<ICloudProvider>();

			await foreach (var provider in DetectCloudProvidersProgressiveAsync())
				providers.Add(provider);

			return providers
				.OrderBy(provider => provider.ID.ToString())
				.ThenBy(provider => provider.Name)
				.Distinct();
		}

		public async IAsyncEnumerable<ICloudProvider> DetectCloudProvidersProgressiveAsync()
		{
			var groupCount = Server.DriveService.CloudProviderGroupCount;
			var pending = Enumerable.Range(0, groupCount)
				.Select(group => SafetyExtensions.IgnoreExceptions(async () =>
				{
					var providers = await Server.DriveService.GetCloudProvidersAsync(group);
					return providers.Select(provider =>
					{
						var icon = provider.IconData;
						return new CloudProvider((CloudProviders)provider.Id)
						{
							Name = provider.Name,
							SyncFolder = provider.SyncFolder,
							IconData = icon.Length == 0 ? null : icon,
						};
					}).ToArray();
				}, App.Logger))
				.ToList();

			// Yield each detector's results the moment it finishes, so a slow one (for example Google
			// Drive's virtual-drive enumeration) never holds up the rest of the cloud drives.
			while (pending.Count > 0)
			{
				var finished = await Task.WhenAny(pending);
				pending.Remove(finished);

				foreach (var provider in await finished ?? [])
					yield return provider;
			}
		}

	}
}
