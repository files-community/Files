// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;

namespace Files.App.Services.Thumbnails
{
	public sealed class ThumbnailService : IThumbnailService
	{
		private readonly SemaphoreSlim loadThumbnailSemaphore;
		public Task ClearCacheAsync() => _cache.ClearAsync();
		public Task<long> GetCacheSizeAsync() => _cache.GetSizeAsync();
		public Task EvictCacheAsync(long targetSizeBytes) => _cache.EvictToSizeAsync(targetSizeBytes);

		private static readonly HashSet<string> _perFileIconExtensions = new(StringComparer.OrdinalIgnoreCase)
		{
			".exe", ".lnk", ".ico", ".url", ".scr"
		};

		private readonly IThumbnailCache _cache;
		private readonly IThumbnailGenerator _defaultGenerator;
		private readonly ILogger _logger;
		private readonly IUserSettingsService _userSettingsService;

		public ThumbnailService(
			IThumbnailCache cache,
			IThumbnailGenerator defaultGenerator,
			IUserSettingsService userSettingsService,
			ILogger<ThumbnailService> logger)
		{
			_cache = cache;
			_defaultGenerator = defaultGenerator;
			_userSettingsService = userSettingsService;
			_logger = logger;
			loadThumbnailSemaphore = new SemaphoreSlim(1, 1);
		}

		public async Task<byte[]?> GetThumbnailAsync(
			string path,
			int size,
			bool isFolder,
			IconOptions options,
			CancellationToken ct)
		{
			try
			{
				if (options.HasFlag(IconOptions.ReturnIconOnly) && !isFolder)
				{
					var extension = Path.GetExtension(path);
					if (!string.IsNullOrEmpty(extension) && !_perFileIconExtensions.Contains(extension))
					{
						var cachedIcon = _cache.GetIcon(extension, size);
						if (cachedIcon is not null)
							return cachedIcon;
					}
				}

				if (!_userSettingsService.GeneralSettingsService.EnableThumbnailCache)
				{
					var thumbnailNonCached = await GenerateWithOptionalSemaphoreAsync(path, size, isFolder, options, ct);
					ct.ThrowIfCancellationRequested();

					if (thumbnailNonCached is not null && options.HasFlag(IconOptions.ReturnIconOnly) && !isFolder)
					{
						var ext = Path.GetExtension(path);
						if (!string.IsNullOrEmpty(ext) && !_perFileIconExtensions.Contains(ext))
							_cache.SetIcon(ext, size, thumbnailNonCached);
					}

					return thumbnailNonCached;
				}

				var cached = await _cache.GetAsync(path, size, options, ct);
				if (cached is not null)
					return cached.Data;

				if (!options.HasFlag(IconOptions.ReturnIconOnly))
				{
					var probe = await _defaultGenerator.GenerateAsync(path, size, isFolder, options | IconOptions.ReturnOnlyIfCached, ct);
					if (probe is not null)
					{
						ct.ThrowIfCancellationRequested();
						await _cache.SetAsync(path, size, options, probe, ct);
						return probe;
					}
				}

				var thumbnail = await GenerateWithOptionalSemaphoreAsync(path, size, isFolder, options, ct);
				ct.ThrowIfCancellationRequested();

				if (thumbnail is not null)
				{
					ct.ThrowIfCancellationRequested();
					if (options.HasFlag(IconOptions.ReturnIconOnly) && !isFolder)
					{
						// Icons go to in-memory cache only, not disk
						var ext = Path.GetExtension(path);
						if (!string.IsNullOrEmpty(ext) && !_perFileIconExtensions.Contains(ext))
							_cache.SetIcon(ext, size, thumbnail);
					}
					else if (!options.HasFlag(IconOptions.ReturnIconOnly) && !isFolder)
						await _cache.SetAsync(path, size, options, thumbnail, ct);
				}

				return thumbnail;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get thumbnail for {Path}", path);
				return null;
			}
		}

		private async Task<byte[]?> GenerateWithOptionalSemaphoreAsync(
			string path, int size, bool isFolder, IconOptions options, CancellationToken ct)
		{
			bool useSemaphore = options.HasFlag(IconOptions.ReturnThumbnailOnly);
			if (useSemaphore)
				await loadThumbnailSemaphore.WaitAsync(ct);
			try
			{
				return await _defaultGenerator.GenerateAsync(path, size, isFolder, options, ct);
			}
			finally
			{
				if (useSemaphore)
					loadThumbnailSemaphore.Release();
			}
		}

	}
}
