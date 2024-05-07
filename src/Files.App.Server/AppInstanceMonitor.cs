// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Files.App.Server;

public sealed class AppInstanceMonitor
{
	private static int processCount = 0;
	internal static ConcurrentDictionary<int, ConcurrentBag<IDisposable>> AppInstanceResources = new();

	public static void StartMonitor(int processId)
	{
		var process = Process.GetProcessById(processId);
		Interlocked.Increment(ref processCount);
		process.EnableRaisingEvents = true;
		process.Exited += Process_Exited;
	}

	private static void Process_Exited(object? sender, EventArgs e)
	{
		if (sender is Process { Id: var processId } process)
		{
			process.Dispose();

			if (AppInstanceResources.TryRemove(processId, out var instances))
			{
				foreach (var instance in instances)
				{
					instance.Dispose();
				}
			}

			if (Interlocked.Decrement(ref processCount) == 0)
			{
				Program.ExitSignalEvent.Set();
			}
		}
	}
}
