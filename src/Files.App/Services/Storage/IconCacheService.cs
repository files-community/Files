// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using System.IO;

namespace Files.App.Services
{
	internal sealed class IconCacheService : IIconCacheService
	{
		// Dummy path to generate generic icons for folders, executables, and shortcuts.
		private static readonly string _dummyPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "x46696c6573");

		private readonly ConcurrentDictionary<string, byte[]?> _cache = new();
		private readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

		public async Task<byte[]?> GetIconAsync(string? itemPath, string? extension, bool isFolder, uint size)
		{
			var key = $"{(isFolder ? ":folder:" : (extension?.ToLowerInvariant() ?? ":noext:"))}:{size}";

			if (_cache.TryGetValue(key, out var cached))
				return cached;

			// Always use the dummy path so the shell resolves the generic type icon from the
			// extension alone. This works correctly for all path types (local, MTP, FTP, network,
			// cloud, etc.) because the cache is keyed by extension anyway, not by item identity.
			// Folders use a real path so the exact-size extraction succeeds and matches per-item icons.
			var iconPath = isFolder ? Environment.SystemDirectory : (string.IsNullOrEmpty(extension) ? _dummyPath : _dummyPath + extension);

			var icon = await FileThumbnailHelper.GetIconAsync(
				iconPath,
				size,
				isFolder,
				IconOptions.ReturnIconOnly);

			_cache.TryAdd(key, icon);
			return icon;
		}

		public async Task<BitmapImage?> GetIconImageAsync(string? itemPath, string? extension, bool isFolder, uint size)
		{
			// BitmapImage has thread affinity, so only the main window's dispatcher may use this cache
			if (DispatcherQueue.GetForCurrentThread() is null)
				return null;

			var key = $"{(isFolder ? ":folder:" : (extension?.ToLowerInvariant() ?? ":noext:"))}:{size}";
			if (_imageCache.TryGetValue(key, out var cached))
				return cached;

			var data = await GetIconAsync(itemPath, extension, isFolder, size);
			if (data is null)
				return null;

			var image = await data.ToBitmapAsync();
			if (image is not null)
				_imageCache.TryAdd(key, image);

			return image;
		}

		public void Clear()
		{
			_cache.Clear();
			_imageCache.Clear();
		}
	}
}
