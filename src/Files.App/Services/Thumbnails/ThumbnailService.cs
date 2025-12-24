// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;

namespace Files.App.Services.Thumbnails
{
	public sealed class ThumbnailService : IThumbnailService
	{
		private readonly IThumbnailCache _cache;
		private readonly Dictionary<string, IThumbnailGenerator> _customGenerators;
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
			_customGenerators = new Dictionary<string, IThumbnailGenerator>();
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
				if (!_userSettingsService.GeneralSettingsService.EnableThumbnailCache)
				{
					var generator = SelectGenerator(path, isFolder);
					return await generator.GenerateAsync(path, size, isFolder, options, ct);
				}

				var cached = await _cache.GetAsync(path, size, options, ct);
				if (cached is not null)
					return cached;

				var selectedGenerator = SelectGenerator(path, isFolder);
				var thumbnail = await selectedGenerator.GenerateAsync(path, size, isFolder, options, ct);

				if (thumbnail is not null)
					await _cache.SetAsync(path, size, options, thumbnail, ct);

				return thumbnail;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to get thumbnail for {Path}", path);
				return null;
			}
		}

		public void RegisterGenerator(IThumbnailGenerator generator)
		{
			foreach (var type in generator.SupportedTypes)
			{
				_customGenerators[type.ToLowerInvariant()] = generator;
				_logger.LogInformation("Registered custom generator for {Type}", type);
			}
		}

		private IThumbnailGenerator SelectGenerator(string path, bool isFolder)
		{
			if (isFolder)
			{
				if (_customGenerators.TryGetValue("folder", out var folderGen))
					return folderGen;
			}
			else
			{
				var extension = Path.GetExtension(path).ToLowerInvariant();
				if (_customGenerators.TryGetValue(extension, out var customGen))
					return customGen;
			}

			return _defaultGenerator;
		}

		public Task ClearCacheAsync() => _cache.ClearAsync();
		public Task<long> GetCacheSizeAsync() => _cache.GetSizeAsync();
		public Task EvictCacheAsync(long targetSizeBytes) => _cache.EvictToSizeAsync(targetSizeBytes);
	}
}
