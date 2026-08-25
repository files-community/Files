// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32;

namespace Files.App.Helpers
{
	/// <summary>
	/// Returns memory to the OS after large amounts of items are dropped, once the app has been quiet for a moment.
	/// </summary>
	public static class AppMemoryHelper
	{
		private const int QuietWindowMs = 2000;
		private const int SweepDelayMs = 6000;
		private const int MaxSweepPasses = 4;
		private const long SweepContinueBytes = 64 * 1024 * 1024;

		private static long lastActivityTicks;
		private static int trimRequested;
		private static int workerRunning;

		/// <summary>
		/// Postpones a pending trim; call from hot interaction paths so collections never land mid-gesture.
		/// </summary>
		public static void NotifyActivity()
		{
			Interlocked.Exchange(ref lastActivityTicks, Environment.TickCount64);
		}

		/// <summary>
		/// Requests a full collection once the app has been quiet for a moment; concurrent requests coalesce.
		/// </summary>
		public static void RequestTrim()
		{
			NotifyActivity();
			Interlocked.Exchange(ref trimRequested, 1);
			EnsureWorker();
		}

		private static void EnsureWorker()
		{
			if (Interlocked.CompareExchange(ref workerRunning, 1, 0) != 0)
				return;

			_ = Task.Run(async () =>
			{
				while (Interlocked.Exchange(ref trimRequested, 0) == 1)
				{
					while (Environment.TickCount64 - Interlocked.Read(ref lastActivityTicks) < QuietWindowMs)
						await Task.Delay(500);

					Collect();

					// Finalizer chains release memory over several cycles; keep collecting while a pass still frees a meaningful amount
					for (int pass = 0; pass < MaxSweepPasses; pass++)
					{
						await Task.Delay(SweepDelayMs);
						if (Volatile.Read(ref trimRequested) == 1 ||
							Environment.TickCount64 - Interlocked.Read(ref lastActivityTicks) < QuietWindowMs)
							break;

						var workingSetBefore = Environment.WorkingSet;
						Collect();
						if (workingSetBefore - Environment.WorkingSet < SweepContinueBytes)
							break;
					}

					// Remaining idle pages move to the standby list so the process footprint shrinks immediately
					if (Volatile.Read(ref trimRequested) == 0 &&
						Environment.TickCount64 - Interlocked.Read(ref lastActivityTicks) >= QuietWindowMs)
					{
						using var process = Process.GetCurrentProcess();
						PInvoke.K32EmptyWorkingSet(new Windows.Win32.Foundation.HANDLE(process.Handle));
					}
				}

				Interlocked.Exchange(ref workerRunning, 0);

				// A request that landed between the loop exit and the reset above must not be dropped
				if (Volatile.Read(ref trimRequested) == 1)
					EnsureWorker();
			});
		}

		// Two passes: the first collect queues finalizers whose CCW/RCW releases only free native memory in the second
		private static void Collect()
		{
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
			GC.WaitForPendingFinalizers();
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
		}
	}
}
