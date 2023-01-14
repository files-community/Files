using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.Services.SizeProvider
{
	public class DrivesSizeProvider : ISizeProvider
	{
		private readonly ConcurrentDictionary<string, ISizeProvider> providers = new();

		public event EventHandler<SizeChangedEventArgs>? SizeChanged;

		public async Task CleanAsync()
		{
			var currentDrives = DriveInfo.GetDrives().Select(x => x.Name).ToArray();
			var oldDriveNames = providers.Keys.Except(currentDrives).ToArray();

			foreach (var oldDriveName in oldDriveNames)
			{
				providers.TryRemove(oldDriveName, out var _);
			}

			foreach (var provider in providers.Values)
			{
				await provider.CleanAsync();
			}
		}

		public async Task ClearAsync()
		{
			foreach (var provider in providers)
			{
				await provider.Value.ClearAsync();
			}
			providers.Clear();
		}

		public Task UpdateAsync(string path, CancellationToken cancellationToken)
		{
			string driveName = GetDriveName(path);
			if (!providers.ContainsKey(driveName))
			{
				CreateProvider(driveName);
			}
			var provider = providers[driveName];
			return provider.UpdateAsync(path, cancellationToken);
		}

		public bool TryGetSize(string path, out ulong size)
		{
			string driveName = GetDriveName(path);
			if (!providers.ContainsKey(driveName))
			{
				size = 0;
				return false;
			}
			var provider = providers[driveName];
			return provider.TryGetSize(path, out size);
		}

		private static string GetDriveName(string path) => Directory.GetDirectoryRoot(path);

		private void CreateProvider(string driveName)
		{
			var provider = new CachedSizeProvider();
			provider.SizeChanged += Provider_SizeChanged;
			providers.TryAdd(driveName, provider);
		}

		private void Provider_SizeChanged(object? sender, SizeChangedEventArgs e)
			=> SizeChanged?.Invoke(this, e);

		public void Dispose()
		{
			foreach (var provider in providers.Values)
			{
				provider.SizeChanged -= Provider_SizeChanged;
				provider.Dispose();
			}
		}
	}
}
