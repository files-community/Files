// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Files.App.Data.Models;
using Files.Shared.Helpers;

namespace Files.App.Services
{
	public sealed class VisitedFoldersService : IVisitedFoldersService
	{
		private static readonly AppJsonSerializerContext _indentedJsonContext = new(new JsonSerializerOptions { WriteIndented = true });

		private readonly ConcurrentDictionary<string, VisitedFolderItem> _cache = new(StringComparer.OrdinalIgnoreCase);
		private readonly SemaphoreSlim _fileLock = new(1, 1);
		private readonly Timer _debounceTimer;
		private bool _isDirty;
		private readonly string _storagePath;

		public VisitedFoldersService()
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var dir = Path.Combine(appData, "Files", "Data");
			Directory.CreateDirectory(dir);
			_storagePath = Path.Combine(dir, "visited_folders.json");

			_debounceTimer = new Timer(async _ => await OnDebounceTimerFired(), null, Timeout.Infinite, Timeout.Infinite);
		}

		public async Task InitializeAsync()
		{
			if (!File.Exists(_storagePath))
			{
				EnsureDefaultFoldersSeeded();
				return;
			}

			await _fileLock.WaitAsync();
			try
			{
				await using var stream = File.OpenRead(_storagePath);
				var items = await JsonSerializer.DeserializeAsync(stream, AppJsonSerializerContext.Default.ListVisitedFolderItem);
				if (items is not null)
				{
					foreach (var item in items)
					{
						if (!string.IsNullOrWhiteSpace(item.FolderPath))
							_cache[item.FolderPath] = item;
					}
				}
			}
			catch
			{
				// Resilient fallback if the storage file is corrupted
			}
			finally
			{
				_fileLock.Release();
			}

			EnsureDefaultFoldersSeeded();
		}

		private void EnsureDefaultFoldersSeeded()
		{
			if (!_cache.IsEmpty)
				return;

			var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string[] defaultPaths =
			[
				Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
				!string.IsNullOrWhiteSpace(userProfile) ? Path.Combine(userProfile, "Downloads") : string.Empty,
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
				Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
				Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
				userProfile
			];

			foreach (var defaultPath in defaultPaths)
			{
				if (!string.IsNullOrWhiteSpace(defaultPath) && Directory.Exists(defaultPath))
					TrackFolder(defaultPath);
			}
		}

		public void TrackFolder(string folderPath, string? folderName = null)
		{
			if (string.IsNullOrWhiteSpace(folderPath) ||
				folderPath.StartsWith("::{", StringComparison.Ordinal) ||
				folderPath.StartsWith("tag:", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(folderPath, "Home", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(folderPath, "Settings", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(folderPath, "ReleaseNotes", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			var root = Path.GetPathRoot(folderPath);
			if (!string.Equals(folderPath, root, StringComparison.OrdinalIgnoreCase))
				folderPath = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			_cache.AddOrUpdate(
				folderPath,
				path =>
				{
					var resolvedName = string.IsNullOrWhiteSpace(folderName) ? Path.GetFileName(path) : folderName;
					if (string.IsNullOrWhiteSpace(resolvedName))
						resolvedName = path;

					return new VisitedFolderItem
					{
						FolderPath = path,
						FolderName = resolvedName,
						VisitCount = 1,
						LastVisitedUtc = DateTime.UtcNow
					};
				},
				(_, existing) =>
				{
					existing.VisitCount++;
					existing.LastVisitedUtc = DateTime.UtcNow;
					if (!string.IsNullOrWhiteSpace(folderName))
						existing.FolderName = folderName;
					return existing;
				});

			_isDirty = true;
			_debounceTimer.Change(5000, Timeout.Infinite); // Persist after 5 seconds of idle
		}

		public IReadOnlyList<VisitedFolderItem> GetTopFolders(int limit = 15)
		{
			EnsureDefaultFoldersSeeded();

			return _cache.Values
				.OrderByDescending(f => f.FrecencyScore)
				.Take(limit)
				.ToList();
		}

		public IReadOnlyList<VisitedFolderItem> Search(string? query, int limit = 15)
		{
			if (string.IsNullOrWhiteSpace(query))
				return GetTopFolders(limit);

			EnsureDefaultFoldersSeeded();
			query = query.Trim();

			return _cache.Values
				.Where(f => f.FolderName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
							f.FolderPath.Contains(query, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(f => f.FolderName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
				.ThenByDescending(f => f.FrecencyScore)
				.Take(limit)
				.ToList();
		}

		public async Task SaveChangesAsync()
		{
			if (!_isDirty)
				return;

			await _fileLock.WaitAsync();
			try
			{
				// Prune deleted folders and limit to 1000 items
				var itemsToSave = _cache.Values
					.Where(f => !Path.IsPathRooted(f.FolderPath) || Directory.Exists(f.FolderPath))
					.OrderByDescending(f => f.FrecencyScore)
					.Take(1000)
					.ToList();

				await using var stream = File.Create(_storagePath);
				await JsonSerializer.SerializeAsync(stream, itemsToSave, _indentedJsonContext.ListVisitedFolderItem);
				_isDirty = false;
			}
			finally
			{
				_fileLock.Release();
			}
		}

		private async Task OnDebounceTimerFired()
		{
			await SaveChangesAsync();
		}
	}
}
