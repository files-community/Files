// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public class AsyncManualResetEvent
	{
		private volatile TaskCompletionSource<bool> _taskCompletionSource = new();

		public async Task WaitAsync(CancellationToken cancellationToken = default)
		{
			var tcs = _taskCompletionSource;
			var cancelTcs = new TaskCompletionSource<bool>();

			cancellationToken.Register(
				s => ((TaskCompletionSource<bool>)s!).TrySetCanceled(), cancelTcs);

			await await Task.WhenAny(tcs.Task, cancelTcs.Task);
		}

		public async Task<bool> WaitAsync(int milliseconds, CancellationToken cancellationToken = default)
		{
			var tcs = _taskCompletionSource;
			var cancelTcs = new TaskCompletionSource<bool>();

			cancellationToken.Register(
				s => ((TaskCompletionSource<bool>)s!).TrySetCanceled(), cancelTcs);

			return await await Task.WhenAny(tcs.Task, cancelTcs.Task, Delay(milliseconds));
		}

		public void Set()
		{
			var tcs = _taskCompletionSource;
			Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true),
				tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
			tcs.Task.Wait();
		}

		public void Reset()
		{
			var newTcs = new TaskCompletionSource<bool>();
			while (true)
			{
				var tcs = _taskCompletionSource;
				if (!tcs.Task.IsCompleted ||
					Interlocked.CompareExchange(ref _taskCompletionSource, newTcs, tcs) == tcs)
					return;
			}
		}

		private static async Task<bool> Delay(int milliseconds)
		{
			await Task.Delay(milliseconds);
			return false;
		}
	}
}
