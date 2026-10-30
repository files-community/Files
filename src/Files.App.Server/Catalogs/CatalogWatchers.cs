// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage;
using Microsoft.Extensions.Logging;
using System.Security.Principal;

namespace Files.App.Server;

internal static class CatalogWatchers
{
	private static List<DirectoryChangeMonitor> watchers = [];
	private static readonly Lazy<bool> initialized = new(Start);
	private static readonly Lock recycleBinGate = new();
	private static List<DirectoryChangeMonitor> recycleBins = [];
	private static long recycleBinVersion;
	private static bool stopped;

	internal static void Initialize() => _ = initialized.Value;

	private static bool Start()
	{
		var recentFolder = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
		var recent = Path.Combine(recentFolder, "AutomaticDestinations");
		Watch(recentFolder, "*.lnk", LocationCatalog.InvalidateRecentItems);
		Watch(recent, "f01b4d95cf55d32a.automaticDestinations-ms", () =>
		{
			InitializationCache.PinnedFolders.Invalidate();
			JumpListCatalog.Invalidate();
			ServerEvents.Notify(ServerChangeNames.PinnedFolders);
			LocationCatalog.InvalidateRecentItems();
		});
		Watch(recent, "5f7b5f1e01b83767.automaticDestinations-ms", LocationCatalog.InvalidateRecentItems);
		Watch(ShellLibraryItem.LibrariesPath, "*.library-ms", () =>
		{
			InitializationCache.Libraries.Invalidate();
			ServerEvents.Notify(ServerChangeNames.Libraries);
		});
		Watch(Environment.GetFolderPath(Environment.SpecialFolder.NetworkShortcuts), "*", () =>
		{
			LocationCatalog.NetworkShortcuts.Invalidate();
			ServerEvents.Notify(ServerChangeNames.Network);
		});
		RefreshRecycleBinWatcher();
		return true;
	}

	internal static void RefreshRecycleBinWatcher()
	{
		long version;
		lock (recycleBinGate)
		{
			if (stopped)
				return;
			version = ++recycleBinVersion;
		}
		var replacements = new List<DirectoryChangeMonitor>();
		try
		{
			using var identity = WindowsIdentity.GetCurrent();
			var sid = identity.User?.Value;
			if (!string.IsNullOrEmpty(sid))
			{
				foreach (var drive in System.IO.DriveInfo.GetDrives())
				{
					if (drive.DriveType == System.IO.DriveType.Network)
						continue;
					var watcher = SafetyExtensions.IgnoreExceptions(() => new DirectoryChangeMonitor(
						Path.Combine(drive.Name, "$RECYCLE.BIN", sid), "*", false, OnRecycleBinChanged, watchDeviceRemoval: true), Program.Logger);
					if (watcher is not null)
						replacements.Add(watcher);
				}
			}
		}
		catch
		{
			foreach (var watcher in replacements)
				watcher.Dispose();
			throw;
		}
		List<DirectoryChangeMonitor> previous;
		bool installed;
		lock (recycleBinGate)
		{
			installed = !stopped && version == recycleBinVersion;
			previous = installed ? recycleBins : replacements;
			if (installed)
				recycleBins = replacements;
		}
		foreach (var watcher in previous)
			watcher.Dispose();
		if (installed)
			RecycleBinCatalog.RecordChange(new FileSystemEventArgs(WatcherChangeTypes.All, string.Empty, string.Empty), refresh: true);
	}

	private static void OnRecycleBinChanged(FileSystemEventArgs e)
	{
		if (e.ChangeType == WatcherChangeTypes.All)
			RecycleBinCatalog.RecordChange(e, refresh: true);
		else if (e.ChangeType is WatcherChangeTypes.Created or WatcherChangeTypes.Deleted && !string.IsNullOrEmpty(e.Name))
		{
			if (e.Name.StartsWith("$I", StringComparison.Ordinal))
			{
				// Availability depends on metadata files, but they aren't items to display in the view.
				RecycleBinCatalog.Invalidate();
				ServerEvents.Notify(ServerChangeNames.RecycleBin);
			}
			else
				RecycleBinCatalog.RecordChange(e, refresh: false);
		}
	}

	private static void Watch(string directory, string filter, Action changed)
	{
		if (string.IsNullOrEmpty(directory))
			return;
		try
		{
			var watcher = new DirectoryChangeMonitor(directory, filter, true, _ => changed());
			lock (recycleBinGate)
			{
				if (!stopped)
				{
					watchers.Add(watcher);
					return;
				}
			}
			watcher.Dispose();
		}
		catch (Exception ex)
		{
			Program.Logger.LogWarning(ex, "Could not watch a startup catalog.");
		}
	}

	internal static void Dispose()
	{
		List<DirectoryChangeMonitor> previous;
		List<DirectoryChangeMonitor> previousBins;
		lock (recycleBinGate)
		{
			stopped = true;
			recycleBinVersion++;
			previous = watchers;
			previousBins = recycleBins;
			watchers = [];
			recycleBins = [];
		}
		foreach (var watcher in previous.Concat(previousBins))
			watcher.Dispose();
	}
}
