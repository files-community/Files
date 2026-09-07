// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics;

namespace Files.App.Server;

public sealed class AppInstanceMonitor
{
	private static int processCount = 0;

	public static void StartMonitor(int processId)
	{
		var process = Process.GetProcessById(processId);
		Interlocked.Increment(ref processCount);
		process.EnableRaisingEvents = true;
		process.Exited += Process_Exited;
	}

	private static void Process_Exited(object? sender, EventArgs e)
	{
		if (sender is Process process)
		{
			process.Dispose();

			if (Interlocked.Decrement(ref processCount) == 0)
			{
				Program.ExitSignal.TrySetResult(true);
			}
		}
	}
}
