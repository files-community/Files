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
			handlers.Remove(client);
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
			names = pending.ToArray();
			pending.Clear();
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
