// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;

namespace Files.App.Utils.Storage
{
	/// <inheritdoc cref="IStorageCacheService"/>
	internal sealed class StorageCacheService : IStorageCacheService
	{
		private readonly ConcurrentDictionary<string, string> cachedDictionary = new();

		private readonly IUserSettingsService _userSettingsService;

		public StorageCacheService(IUserSettingsService userSettingsService)
		{
			_userSettingsService = userSettingsService;
		}

		/// <inheritdoc/>
		public ValueTask<string> GetDisplayName(string path, CancellationToken cancellationToken)
		{
			if (_userSettingsService.AppearanceSettingsService.ShowFileExtensionsOnlyWhileEditing)
			{
				return cachedDictionary.TryGetValue(path, out var displayName)
					? ValueTask.FromResult(displayName)
					: ValueTask.FromResult(SystemIO.Path.GetFileName(path));
			}
			else
			{
				return cachedDictionary.TryGetValue(path, out var displayName)
					? ValueTask.FromResult(SystemIO.Path.GetFileNameWithoutExtension(displayName))
					: ValueTask.FromResult(SystemIO.Path.GetFileNameWithoutExtension(path));
			}
		}

		/// <inheritdoc/>
		public ValueTask AddDisplayName(string path, string? displayName)
		{
			if (string.IsNullOrEmpty(displayName))
			{
				cachedDictionary.TryRemove(path, out _);
				return ValueTask.CompletedTask;
			}

			cachedDictionary[path] = displayName;

			return ValueTask.CompletedTask;
		}
	}
}
