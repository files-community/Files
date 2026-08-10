// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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

		private static PoolState _idleWorkers = PoolState.Empty;

		private sealed class PoolState(Worker? head, int count)
		{
			internal static readonly PoolState Empty = new(null, 0);

			internal Worker? Head { get; } = head;
			internal int Count { get; } = count;
		}

		internal sealed class Worker(ThreadWithMessageQueue thread)
		{
			public ThreadWithMessageQueue Thread { get; } = thread;

			internal int Uses { get; set; }
			internal Worker? Next { get; set; }
		}

		/// <summary>
		/// Borrows a warm worker from the pool, or creates one if none are idle.
		/// </summary>
		public static Worker Rent()
		{
			while (true)
			{
				PoolState state = Volatile.Read(ref _idleWorkers);
				Worker? worker = state.Head;

				if (worker is null)
				{
					return new Worker(new ThreadWithMessageQueue());
				}

				var newState = new PoolState(worker.Next, state.Count - 1);
				if (ReferenceEquals(Interlocked.CompareExchange(ref _idleWorkers, newState, state), state))
				{
					worker.Next = null;
					return worker;
				}
			}
		}

		/// <summary>
		/// Returns a worker to the pool once the caller has finished releasing the COM state it created
		/// (which must happen on the worker's own thread). Over-used or excess workers are disposed.
		/// </summary>
		public static void Return(Worker worker)
		{
			worker.Uses++;

			if (worker.Uses >= MaxUsesPerWorker)
			{
				worker.Thread.Dispose();
				return;
			}

			while (true)
			{
				PoolState state = Volatile.Read(ref _idleWorkers);

				if (state.Count >= MaxIdleWorkers)
				{
					worker.Thread.Dispose();
					return;
				}

				worker.Next = state.Head;
				var newState = new PoolState(worker, state.Count + 1);

				if (ReferenceEquals(Interlocked.CompareExchange(ref _idleWorkers, newState, state), state))
				{
					return;
				}
			}
		}
	}
}
