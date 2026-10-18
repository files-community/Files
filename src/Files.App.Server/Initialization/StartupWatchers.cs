// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Storage.Watchers;
using Microsoft.Extensions.Logging;
using System.IO.Enumeration;

namespace Files.App.Server;

internal static class StartupWatchers
{
	private static readonly List<FileSystemWatcher> watchers = [];
	private static readonly Lazy<bool> initialized = new(Start);
	private static Files.Core.Storage.Contracts.ITrashWatcher? recycleBin;

	internal static void Initialize() => _ = initialized.Value;

	private static bool Start()
	{
		var recent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent), "AutomaticDestinations");
		Watch(recent, "f01b4d95cf55d32a.automaticDestinations-ms", () =>
		{
			InitializationCache.PinnedFolders.Invalidate();
			JumpListCatalog.Invalidate();
			ServerEvents.Notify("PinnedFolders");
		});
		Watch(recent, "5f7b5f1e01b83767.automaticDestinations-ms", () =>
		{
			LocationCatalog.RecentFiles.Invalidate();
			LocationCatalog.RecentFolders.Invalidate();
			ServerEvents.Notify("RecentItems");
		});
		Watch(ShellLibraryItem.LibrariesPath, "*.library-ms", () =>
		{
			InitializationCache.Libraries.Invalidate();
			ServerEvents.Notify("Libraries");
		});
		Watch(Environment.GetFolderPath(Environment.SpecialFolder.NetworkShortcuts), "*", () =>
		{
			LocationCatalog.NetworkShortcuts.Invalidate();
			ServerEvents.Notify("Network");
		});
		recycleBin = SafetyExtensions.IgnoreExceptions(() => new RecycleBinWatcher(), Program.Logger);
		if (recycleBin is null)
			return true;
		recycleBin.ItemAdded += OnRecycleBinChanged;
		recycleBin.ItemDeleted += OnRecycleBinChanged;
		recycleBin.ItemChanged += OnRecycleBinChanged;
		recycleBin.ItemRenamed += OnRecycleBinChanged;
		recycleBin.RefreshRequested += OnRecycleBinChanged;
		return true;
	}

	private static void OnRecycleBinChanged(object? sender, FileSystemEventArgs e)
	{
		RecycleBinCatalog.Invalidate();
		ServerEvents.Notify("RecycleBin");
	}

	private static void Watch(string directory, string filter, Action changed)
	{
		if (string.IsNullOrEmpty(directory))
			return;
		try
		{
			var existing = directory;
			while (!Directory.Exists(existing))
			{
				existing = Path.GetDirectoryName(existing);
				if (string.IsNullOrEmpty(existing))
					return;
			}
			var watcher = new FileSystemWatcher(existing, existing == directory ? filter : "*")
			{
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
			};
			bool Relevant(string path) => path.Equals(directory, StringComparison.OrdinalIgnoreCase)
				|| (path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
					&& FileSystemName.MatchesSimpleExpression(filter, Path.GetFileName(path), ignoreCase: true));
			void OnChanged(object sender, FileSystemEventArgs e)
			{
				if (Relevant(e.FullPath))
					changed();
			}
			watcher.Changed += OnChanged;
			watcher.Created += OnChanged;
			watcher.Deleted += OnChanged;
			watcher.Renamed += (_, e) =>
			{
				if (Relevant(e.FullPath) || Relevant(e.OldFullPath))
					changed();
			};
			watcher.Error += (_, _) => changed();
			watcher.EnableRaisingEvents = true;
			watchers.Add(watcher);
		}
		catch (Exception ex)
		{
			Program.Logger.LogWarning(ex, "Could not watch a startup catalog.");
		}
	}

	internal static void Dispose()
	{
		foreach (var watcher in watchers)
			watcher.Dispose();
		recycleBin?.Dispose();
	}
}
