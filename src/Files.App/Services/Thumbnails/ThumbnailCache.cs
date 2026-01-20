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

		private const long DefaultCacheSizeMiB = 512;
		private const string CacheFileExtension = ".thumb";

		private const System.IO.FileAttributes CloudPinned = (System.IO.FileAttributes)0x80000;
		private const System.IO.FileAttributes CloudUnpinned = (System.IO.FileAttributes)0x100000;

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

			var input = metadata.CloudStatus != 0
				? $"{path.ToLowerInvariant()}|{size}|{iconType}|{metadata.Modified.Ticks}|{metadata.Size}|{(int)metadata.CloudStatus}"
				: $"{path.ToLowerInvariant()}|{size}|{iconType}|{metadata.Modified.Ticks}|{metadata.Size}";
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
					var cloudStatus = dirInfo.Attributes & (CloudPinned | CloudUnpinned);
					return new FileMetadata
					{
						Modified = dirInfo.LastWriteTimeUtc,
						Size = 0,
						CloudStatus = cloudStatus
					};
				}
				else
				{
					var fileInfo = new FileInfo(path);
					var cloudStatus = fileInfo.Attributes & (CloudPinned | CloudUnpinned);
					return new FileMetadata
					{
						Modified = fileInfo.LastWriteTimeUtc,
						Size = fileInfo.Length,
						CloudStatus = cloudStatus
					};
				}
			}
			catch
			{
				return new FileMetadata
				{
					Modified = DateTime.MinValue,
					Size = 0,
					CloudStatus = 0
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
					_logger.LogInformation("Cache size {Current} MiB exceeds limit {Max} MiB, evicting...",
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
			var cacheSizeMiB = _userSettingsService.GeneralSettingsService.ThumbnailCacheSizeLimit;
			if (cacheSizeMiB <= 0)
				cacheSizeMiB = DefaultCacheSizeMiB;

			return (long)(cacheSizeMiB * 1024 * 1024);
		}

		public async Task<long> GetSizeAsync()
		{
			try
			{
				var cacheFiles = Directory.GetFiles(_cacheDirectory, $"*{CacheFileExtension}");

				long totalSizeOnDisk = 0;
				foreach (var file in cacheFiles)
				{
					var fileSizeOnDisk = Win32Helper.GetFileSizeOnDisk(file);
					totalSizeOnDisk += fileSizeOnDisk ?? 0;
				}

				return totalSizeOnDisk;
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

			long currentSize = 0;
			foreach (var file in files)
			{
				var fileSizeOnDisk = Win32Helper.GetFileSizeOnDisk(file.FullName);
				currentSize += fileSizeOnDisk ?? 0;
			}

			int removedCount = 0;

			foreach (var file in files)
			{
				if (currentSize <= targetSizeBytes)
					break;

				try
				{
					var cacheKey = Path.GetFileNameWithoutExtension(file.Name);
					var fileSizeOnDisk = Win32Helper.GetFileSizeOnDisk(file.FullName);
					currentSize -= fileSizeOnDisk ?? 0;
					file.Delete();
					_memoryIndex.TryRemove(cacheKey, out _);
					removedCount++;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error deleting cache file {Path}", file.FullName);
				}
			}

			_logger.LogInformation("Evicted {Count} cache entries, new size: {Size} MiB",
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
		public System.IO.FileAttributes CloudStatus { get; init; }
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
