// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Data.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage;

namespace Files.App.Services.Thumbnails
{
	public sealed class ThumbnailCache : IThumbnailCache
	{
		private readonly string _cacheDirectory;
		private readonly ConcurrentDictionary<string, CacheEntry> _memoryIndex;
		private readonly SemaphoreSlim _evictionLock;
		private readonly ILogger _logger;
		private readonly IUserSettingsService _userSettingsService;

		private const long DefaultCacheSizeMB = 512;
		private const string CacheFileExtension = ".thumb";

		public ThumbnailCache(IUserSettingsService userSettingsService, ILogger<ThumbnailCache> logger)
		{
			_logger = logger;
			_userSettingsService = userSettingsService;
			_cacheDirectory = Path.Combine(
				ApplicationData.Current.LocalFolder.Path,
				"thumbnail_cache");

			Directory.CreateDirectory(_cacheDirectory);

			_memoryIndex = new ConcurrentDictionary<string, CacheEntry>();
			_evictionLock = new SemaphoreSlim(1, 1);

			_ = LoadCacheIndexAsync();
		}

		public async Task<byte[]?> GetAsync(string path, int size, IconOptions options, CancellationToken ct)
		{
			try
			{
				if (!File.Exists(path) && !Directory.Exists(path))
					return null;

				var metadata = GetFileMetadata(path);
				var cacheKey = GenerateCacheKey(path, size, options, metadata);

				if (_memoryIndex.TryGetValue(cacheKey, out var entry))
				{
					if (File.Exists(entry.CachePath))
					{
						File.SetLastAccessTime(entry.CachePath, DateTime.UtcNow);
						return await File.ReadAllBytesAsync(entry.CachePath, ct);
					}
					else
					{
						_memoryIndex.TryRemove(cacheKey, out _);
					}
				}

				return null;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error reading cache for {Path}", path);
				return null;
			}
		}

		public async Task SetAsync(string path, int size, IconOptions options, byte[] thumbnail, CancellationToken ct)
		{
			try
			{
				if (IsPathInCacheDirectory(path))
					return;

				var metadata = GetFileMetadata(path);
				var cacheKey = GenerateCacheKey(path, size, options, metadata);
				var cachePath = Path.Combine(_cacheDirectory, $"{cacheKey}{CacheFileExtension}");

				await File.WriteAllBytesAsync(cachePath, thumbnail, ct);

				_memoryIndex[cacheKey] = new CacheEntry
				{
					Path = path,
					Size = size,
					CachePath = cachePath,
					CreatedAt = DateTime.UtcNow,
					FileModified = metadata.Modified,
					FileSize = metadata.Size
				};

				_logger.LogDebug("Cached thumbnail for {Path} ({Bytes} bytes)", path, thumbnail.Length);

				_ = TryEvictIfNeededAsync();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error writing cache for {Path}", path);
			}
		}

		private bool IsPathInCacheDirectory(string path)
		{
			try
			{
				var normalizedPath = Path.GetFullPath(path);
				return normalizedPath.StartsWith(_cacheDirectory, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private string GenerateCacheKey(string path, int size, IconOptions options, FileMetadata metadata)
		{
			var iconType = options.HasFlag(IconOptions.ReturnIconOnly) ? "icon" : "thumb";

			// Unique key for hashing
			var input = $"{path.ToLowerInvariant()}|{size}|{iconType}|{metadata.Modified.Ticks}|{metadata.Size}";
			var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
			return Convert.ToHexString(hash);
		}

		private FileMetadata GetFileMetadata(string path)
		{
			try
			{
				if (Directory.Exists(path))
				{
					var dirInfo = new DirectoryInfo(path);
					return new FileMetadata
					{
						Modified = dirInfo.LastWriteTimeUtc,
						Size = 0
					};
				}
				else
				{
					var fileInfo = new FileInfo(path);
					return new FileMetadata
					{
						Modified = fileInfo.LastWriteTimeUtc,
						Size = fileInfo.Length
					};
				}
			}
			catch
			{
				return new FileMetadata
				{
					Modified = DateTime.MinValue,
					Size = 0
				};
			}
		}

		private async Task LoadCacheIndexAsync()
		{
			try
			{
				var cacheDir = new DirectoryInfo(_cacheDirectory);
				if (!cacheDir.Exists)
					return;

				var files = cacheDir.GetFiles($"*{CacheFileExtension}");
				_logger.LogInformation("Loading cache index: {Count} entries", files.Length);

				foreach (var file in files)
				{
					var cacheKey = Path.GetFileNameWithoutExtension(file.Name);
					_memoryIndex.TryAdd(cacheKey, new CacheEntry
					{
						CachePath = file.FullName,
						CreatedAt = file.CreationTimeUtc
					});
				}

				_logger.LogInformation("Cache index loaded: {Count} entries", _memoryIndex.Count);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error loading cache index");
			}
		}

		private async Task TryEvictIfNeededAsync()
		{
			if (!_evictionLock.Wait(0))
				return;

			try
			{
				var currentSize = await GetSizeAsync();
				var maxCacheSizeBytes = GetMaxCacheSizeBytes();

				if (currentSize > maxCacheSizeBytes)
				{
					_logger.LogInformation("Cache size {Current} MB exceeds limit {Max} MB, evicting...",
						currentSize / 1024 / 1024, maxCacheSizeBytes / 1024 / 1024);

					await EvictToSizeCoreAsync(maxCacheSizeBytes * 3 / 4);
				}
			}
			finally
			{
				_evictionLock.Release();
			}
		}

		private long GetMaxCacheSizeBytes()
		{
			var cacheSizeMB = _userSettingsService.GeneralSettingsService.ThumbnailCacheSizeLimit;
			if (cacheSizeMB <= 0)
				cacheSizeMB = DefaultCacheSizeMB;

			return (long)(cacheSizeMB * 1024 * 1024);
		}

		public async Task<long> GetSizeAsync()
		{
			try
			{
				var cacheDir = new DirectoryInfo(_cacheDirectory);
				if (!cacheDir.Exists)
					return 0;

				return cacheDir.GetFiles($"*{CacheFileExtension}").Sum(f => f.Length);
			}
			catch
			{
				return 0;
			}
		}		
		
		public async Task EvictToSizeAsync(long targetSizeBytes)
		{
			await _evictionLock.WaitAsync();

			try
			{
				await EvictToSizeCoreAsync(targetSizeBytes);
			}
			finally
			{
				_evictionLock.Release();
			}
		}


		private async Task EvictToSizeCoreAsync(long targetSizeBytes)
		{
			var cacheDir = new DirectoryInfo(_cacheDirectory);
			if (!cacheDir.Exists)
				return;

			var files = cacheDir.GetFiles($"*{CacheFileExtension}")
				.OrderBy(f => f.LastAccessTime)
				.ToList();

			long currentSize = files.Sum(f => f.Length);
			int removedCount = 0;

			foreach (var file in files)
			{
				if (currentSize <= targetSizeBytes)
					break;

				try
				{
					var cacheKey = Path.GetFileNameWithoutExtension(file.Name);
					currentSize -= file.Length;
					file.Delete();
					_memoryIndex.TryRemove(cacheKey, out _);
					removedCount++;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error deleting cache file {Path}", file.FullName);
				}
			}

			_logger.LogInformation("Evicted {Count} cache entries, new size: {Size} MB",
				removedCount, currentSize / 1024 / 1024);
		}

		public async Task ClearAsync()
		{
			await _evictionLock.WaitAsync();

			try
			{
				var cacheDir = new DirectoryInfo(_cacheDirectory);
				if (!cacheDir.Exists)
					return;

				foreach (var file in cacheDir.GetFiles($"*{CacheFileExtension}"))
				{
					file.Delete();
				}

				_memoryIndex.Clear();
				_logger.LogInformation("Cache cleared");
			}
			finally
			{
				_evictionLock.Release();
			}
		}
	}

	internal record FileMetadata
	{
		public DateTime Modified { get; init; }
		public long Size { get; init; }
	}

	internal record CacheEntry
	{
		public string Path { get; init; } = string.Empty;
		public int Size { get; init; }
		public string CachePath { get; init; } = string.Empty;
		public DateTime CreatedAt { get; init; }
		public DateTime FileModified { get; init; }
		public long FileSize { get; init; }
	}
}
