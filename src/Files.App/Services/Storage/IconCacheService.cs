// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO;

namespace Files.App.Services
{
	internal sealed class IconCacheService : IIconCacheService
	{
		// Dummy path to generate generic icons for folders, executables, and shortcuts.
		private static readonly string _dummyPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "x46696c6573");

		private readonly ConcurrentDictionary<string, byte[]?> _cache = new();

		public async Task<byte[]?> GetIconAsync(string itemPath, string? extension, bool isFolder)
		{
			var key = isFolder ? ":folder:" : (extension?.ToLowerInvariant() ?? ":noext:");

			if (_cache.TryGetValue(key, out var cached))
				return cached;

			// Always use the dummy path so the shell resolves the generic type icon from the
			// extension alone. This works correctly for all path types (local, MTP, FTP, network,
			// cloud, etc.) because the cache is keyed by extension anyway, not by item identity.
			var iconPath = isFolder || string.IsNullOrEmpty(extension) ? _dummyPath : _dummyPath + extension;

			var icon = await FileThumbnailHelper.GetIconAsync(
				iconPath,
				Constants.ShellIconSizes.Jumbo,
				isFolder,
				IconOptions.ReturnIconOnly);

			_cache.TryAdd(key, icon);
			return icon;
		}

		public void Clear()
		{
			_cache.Clear();
		}
	}
}
