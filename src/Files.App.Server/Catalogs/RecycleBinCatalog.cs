// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Server;

internal static class RecycleBinCatalog
{
	private static readonly Lock changesGate = new();
	private static readonly Dictionary<long, Queue<RecycleBinChange>> changes = [];
	private static readonly ConcurrentDictionary<string, CachedTask<RecycleBinInfo>> sizes = new(StringComparer.OrdinalIgnoreCase);
	private static readonly CachedTask<bool> hasItems = new(() => Task.Run(() =>
	{
		var sid = WindowsIdentity.GetCurrent().User?.Value;
		if (string.IsNullOrEmpty(sid))
			return false;
		foreach (var drive in System.IO.DriveInfo.GetDrives())
		{
			try
			{
				if (drive.DriveType == System.IO.DriveType.Network || !drive.IsReady)
					continue;
				var path = Path.Combine(drive.Name, "$RECYCLE.BIN", sid);
				if (Directory.Exists(path) && Directory.EnumerateFiles(path, "$I*").Any())
					return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}
		return false;
	}), TimeSpan.FromSeconds(5));

	internal static Task<bool> HasItemsAsync() => hasItems.GetAsync();

	internal static void AddClient(long client)
	{
		lock (changesGate)
			changes[client] = new();
	}

	internal static void RemoveClient(long client)
	{
		lock (changesGate)
			changes.Remove(client);
	}

	internal static RecycleBinChange[] GetChanges(long client)
	{
		lock (changesGate)
		{
			if (!changes.TryGetValue(client, out var queue))
				return [];
			var result = queue.ToArray();
			queue.Clear();
			return result;
		}
	}

	internal static void RecordChange(FileSystemEventArgs args, bool refresh)
	{
		var name = args.Name ?? string.Empty;
		var change = new RecycleBinChange
		{
			Kind = refresh ? RecycleBinChangeKind.Refresh : args.ChangeType switch
			{
				WatcherChangeTypes.Created => RecycleBinChangeKind.Created,
				WatcherChangeTypes.Deleted => RecycleBinChangeKind.Deleted,
				WatcherChangeTypes.Changed => RecycleBinChangeKind.Changed,
				WatcherChangeTypes.Renamed => RecycleBinChangeKind.Renamed,
				_ => RecycleBinChangeKind.Refresh,
			},
			Directory = args.FullPath[..^name.Length],
			Name = name,
			OldName = args is RenamedEventArgs renamed ? renamed.OldName ?? string.Empty : string.Empty,
		};
		lock (changesGate)
		{
			foreach (var queue in changes.Values)
			{
				if (queue.Count >= 512 || change.Kind == RecycleBinChangeKind.Refresh)
				{
					// Match watcher overflow behavior when a client cannot consume changes in time.
					queue.Clear();
					queue.Enqueue(new() { Kind = RecycleBinChangeKind.Refresh, Directory = string.Empty, Name = string.Empty, OldName = string.Empty });
				}
				else if (queue.Count == 0 || queue.Peek().Kind != RecycleBinChangeKind.Refresh)
					queue.Enqueue(change);
			}
		}
		Invalidate();
		ServerEvents.Notify(ServerChangeNames.RecycleBin);
	}

	internal static Task<RecycleBinInfo> QueryAsync(string path) => sizes.GetOrAdd(path,
		key => new CachedTask<RecycleBinInfo>(() => Task.Run(() =>
		{
			SHQUERYRBINFO info = default;
			info.cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>();
			var available = PInvoke.SHQueryRecycleBin(key, ref info) == HRESULT.S_OK;
			return new RecycleBinInfo(available, info.i64NumItems, info.i64Size);
		}), TimeSpan.FromSeconds(5))).GetAsync();

	internal static void Invalidate()
	{
		hasItems.Invalidate();
		foreach (var cache in sizes.Values)
			cache.Invalidate();
	}
}
