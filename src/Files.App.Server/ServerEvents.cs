// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Files.App.Server;

internal static class ServerEvents
{
	private static readonly Lock gate = new();
	private static readonly HashSet<string> pending = [];
	private static readonly Timer timer = new(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
	private static readonly Dictionary<long, EventHandler<string>?> handlers = [];
	private static readonly Dictionary<long, int> pinnedFolderUpdates = [];
	private static bool flushScheduled;

	internal static void Add(long client, EventHandler<string> handler)
	{
		lock (gate)
		{
			handlers.TryGetValue(client, out var current);
			handlers[client] = current + handler;
		}
	}

	internal static void Remove(long client, EventHandler<string> handler)
	{
		lock (gate)
		{
			if (handlers.TryGetValue(client, out var current))
				handlers[client] = current - handler;
		}
	}

	internal static void RemoveClient(long client)
	{
		lock (gate)
		{
			handlers.Remove(client);
			if (pinnedFolderUpdates.Remove(client))
			{
				pending.Add(ServerChangeNames.PinnedFolders);
				ScheduleFlush();
			}
		}
	}

	internal static void BeginPinnedFoldersUpdate(long client)
	{
		lock (gate)
		{
			pinnedFolderUpdates.TryGetValue(client, out var count);
			pinnedFolderUpdates[client] = count + 1;
		}
	}

	internal static void EndPinnedFoldersUpdate(long client)
	{
		lock (gate)
		{
			if (!pinnedFolderUpdates.TryGetValue(client, out var count))
				return;
			if (count > 1)
				pinnedFolderUpdates[client] = count - 1;
			else
				pinnedFolderUpdates.Remove(client);
			pending.Add(ServerChangeNames.PinnedFolders);
			ScheduleFlush();
		}
	}

	internal static void Notify(string name)
	{
		lock (gate)
		{
			pending.Add(name);
			ScheduleFlush();
		}
	}

	private static void ScheduleFlush()
	{
		if (flushScheduled)
			return;
		flushScheduled = true;
		timer.Change(150, Timeout.Infinite);
	}

	private static void Flush()
	{
		string[] names;
		lock (gate)
		{
			flushScheduled = false;
			names = pending.Where(name => name != ServerChangeNames.PinnedFolders || pinnedFolderUpdates.Count == 0).ToArray();
			pending.ExceptWith(names);
		}
		foreach (var name in names)
			Publish(name);
	}

	private static void Publish(string name)
	{
		KeyValuePair<long, EventHandler<string>?>[] listeners;
		lock (gate)
			listeners = handlers.ToArray();

		// Never call a client while holding a cache or watcher lock.
		_ = Task.Run(() =>
		{
			foreach (var listener in listeners)
			{
				lock (gate)
				{
					if (name == ServerChangeNames.PinnedFolders && pinnedFolderUpdates.Count > 0)
					{
						pending.Add(name);
						return;
					}
				}
				try
				{
					listener.Value?.Invoke(null, name);
				}
				catch (Exception ex)
				{
					Program.Logger.LogDebug(ex, "Could not notify a Files instance.");
				}
			}
		});
	}
}
