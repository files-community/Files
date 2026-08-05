// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Pools STA <see cref="ThreadWithMessageQueue"/> workers used to query Win32 shell context menus.
	/// Reusing a worker keeps its apartment (and therefore the shell extension handlers loaded into it)
	/// warm between right-clicks, instead of spinning up a fresh thread and cold-loading every handler
	/// on each menu.
	/// </summary>
	internal static class ContextMenuWorkerPool
	{
		// The pool is bounded and workers are retired after a number of uses. This preserves the
		// isolation the old thread-per-menu model gave for free: a shell extension that hangs or
		// corrupts its apartment is confined to a single worker and cannot permanently degrade
		// every future menu.
		private const int MaxIdleWorkers = 3;
		private const int MaxUsesPerWorker = 50;

		private static readonly object _lock = new();
		private static readonly Stack<Worker> _idleWorkers = new();

		internal sealed class Worker(ThreadWithMessageQueue thread)
		{
			public ThreadWithMessageQueue Thread { get; } = thread;

			public int Uses { get; set; }
		}

		/// <summary>
		/// Borrows a warm worker from the pool, or creates one if none are idle.
		/// </summary>
		public static Worker Rent()
		{
			lock (_lock)
			{
				if (_idleWorkers.Count > 0)
					return _idleWorkers.Pop();
			}

			return new Worker(new ThreadWithMessageQueue());
		}

		/// <summary>
		/// Returns a worker to the pool once the caller has finished releasing the COM state it created
		/// (which must happen on the worker's own thread). Over-used or excess workers are disposed.
		/// </summary>
		public static void Return(Worker worker)
		{
			worker.Uses++;

			if (worker.Uses < MaxUsesPerWorker)
			{
				lock (_lock)
				{
					if (_idleWorkers.Count < MaxIdleWorkers)
					{
						_idleWorkers.Push(worker);
						return;
					}
				}
			}

			worker.Thread.Dispose();
		}
	}
}
