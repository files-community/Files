// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Files.App.Services
{
	internal sealed class IconCacheService : IIconCacheService
	{
		private readonly ConcurrentDictionary<string, byte[]?> _cache = new();
		private readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

		public async Task<byte[]?> GetIconAsync(string? itemPath, string? extension, bool isFolder, uint size)
		{
			var key = $"{(isFolder ? ":folder:" : (extension?.ToLowerInvariant() ?? ":noext:"))}:{size}:{App.AppModel.AppWindowDPI}";

			if (_cache.TryGetValue(key, out var cached))
				return cached;

			var scaledSize = size * App.AppModel.AppWindowDPI;
			var pixels = new[] { 16, 24, 32, 48, 64, 96, 128, 256 }.FirstOrDefault(value => value >= scaledSize, 256);
			var buffer = await Server.AppService.GetGenericIconAsync(extension ?? string.Empty, isFolder, pixels);
			var icon = buffer.Length == 0 ? null : buffer.ToArray();

			_cache.TryAdd(key, icon);
			return icon;
		}

		public async Task<BitmapImage?> GetIconImageAsync(string? itemPath, string? extension, bool isFolder, uint size)
		{
			// BitmapImage has thread affinity, so only the main window's dispatcher may use this cache
			if (DispatcherQueue.GetForCurrentThread() is null)
				return null;

			var key = $"{(isFolder ? ":folder:" : (extension?.ToLowerInvariant() ?? ":noext:"))}:{size}:{App.AppModel.AppWindowDPI}";
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
