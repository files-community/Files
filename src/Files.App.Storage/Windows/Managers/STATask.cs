// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using Windows.Win32;

namespace Files.App.Storage
{
	/// <summary>
	/// Represents a work scheduled to execute on a STA thread.
	/// </summary>
	public partial class STATask
	{
		private const int PooledThreadCount = 12;

		private static readonly Channel<Action> _pooledQueue = Channel.CreateUnbounded<Action>();
		private static bool _poolStarted;

		/// <summary>
		/// Schedules the specified work on a shared pool of persistent STA threads, avoiding per-call thread creation.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the function.</typeparam>
		/// <param name="func">The work to execute on an STA pool thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task{T}"/> that represents the scheduled work.</returns>
		public static Task<T> RunPooled<T>(Func<T> func, ILogger? logger)
		{
			if (!Interlocked.CompareExchange(ref _poolStarted, true, false))
			{
				for (int i = 0; i < PooledThreadCount; i++)
				{
					Thread thread =
						new(() =>
						{
							PInvoke.OleInitialize();

							// Consume synchronously: awaiting here would resume on an MTA thread pool thread, losing the STA apartment
							var reader = _pooledQueue.Reader;
							while (reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
								while (reader.TryRead(out var work))
									work();
						});

					thread.IsBackground = true;
					thread.SetApartmentState(ApartmentState.STA);
					thread.Name = $"STATask pool {i}";
					thread.Start();
				}
			}

			var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

			_pooledQueue.Writer.TryWrite(() =>
			{
				try
				{
					tcs.SetResult(func());
				}
				catch (Exception ex)
				{
					tcs.SetResult(default!);
					logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
				}
			});

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <param name="action">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task Run(Action action, ILogger? logger)
		{
			var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			Thread thread =
				new(() =>
				{
					PInvoke.OleInitialize();

					try
					{
						action();
						tcs.SetResult();
					}
					catch (Exception ex)
					{
						tcs.SetResult();
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the function.</typeparam>
		/// <param name="func">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task<T> Run<T>(Func<T> func, ILogger? logger)
		{
			var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

			Thread thread =
				new(() =>
				{
					PInvoke.OleInitialize();

					try
					{
						tcs.SetResult(func());
					}
					catch (Exception ex)
					{
						tcs.SetResult(default!);
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <param name="func">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task Run(Func<Task> func, ILogger? logger)
		{
			var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			Thread thread =
				new(() =>
				{
					PInvoke.OleInitialize();
					using var synchronizationContext = new StaSynchronizationContext();
					var previousContext = SynchronizationContext.Current;
					SynchronizationContext.SetSynchronizationContext(synchronizationContext);

					try
					{
						var task = func();
						synchronizationContext.Run(task);
						task.GetAwaiter().GetResult();
						tcs.SetResult();
					}
					catch (Exception ex)
					{
						tcs.SetResult();
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						SynchronizationContext.SetSynchronizationContext(previousContext);
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the function.</typeparam>
		/// <param name="func">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task<T?> Run<T>(Func<Task<T>> func, ILogger? logger)
		{
			var tcs = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);

			Thread thread =
				new(() =>
				{
					PInvoke.OleInitialize();
					using var synchronizationContext = new StaSynchronizationContext();
					var previousContext = SynchronizationContext.Current;
					SynchronizationContext.SetSynchronizationContext(synchronizationContext);

					try
					{
						var task = func();
						synchronizationContext.Run(task);
						tcs.SetResult(task.GetAwaiter().GetResult());
					}
					catch (Exception ex)
					{
						tcs.SetResult(default);
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						SynchronizationContext.SetSynchronizationContext(previousContext);
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		private sealed class StaSynchronizationContext : SynchronizationContext, IDisposable
		{
			private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
			private readonly Lock _queueLock = new();
			private readonly int _threadId = Environment.CurrentManagedThreadId;
			private bool _acceptingWork = true;

			public override void Post(SendOrPostCallback callback, object? state)
				=> TryPost(callback, state);

			public override void Send(SendOrPostCallback callback, object? state)
			{
				if (Environment.CurrentManagedThreadId == _threadId)
				{
					callback(state);
					return;
				}

				Exception? exception = null;
				using var completed = new ManualResetEventSlim();
				if (!TryPost(
					_ =>
					{
						try
						{
							callback(state);
						}
						catch (Exception ex)
						{
							exception = ex;
						}
						finally
						{
							completed.Set();
						}
					},
					null))
				{
					throw new InvalidOperationException("The STA synchronization context has completed.");
				}
				completed.Wait();

				if (exception is not null)
					ExceptionDispatchInfo.Capture(exception).Throw();
			}

			public void Run(Task task)
			{
				var completionSignal = task.ContinueWith(
					_ => Complete(),
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);

				foreach (var work in _queue.GetConsumingEnumerable())
					work.Callback(work.State);

				completionSignal.GetAwaiter().GetResult();
			}

			private bool TryPost(SendOrPostCallback callback, object? state)
			{
				lock (_queueLock)
				{
					if (!_acceptingWork)
						return false;

					_queue.Add((callback, state));
					return true;
				}
			}

			private void Complete()
			{
				lock (_queueLock)
				{
					if (!_acceptingWork)
						return;

					_acceptingWork = false;
					_queue.CompleteAdding();
				}
			}

			public void Dispose()
			{
				Complete();
				_queue.Dispose();
			}
		}
	}
}
