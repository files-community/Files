// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Files.App.Utils.Shell
{
	public sealed partial class ThreadWithMessageQueue : Disposable
	{
		private readonly BlockingCollection<IInternal> messageQueue;

		private readonly Thread thread;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				messageQueue.CompleteAdding();
				thread.Join();
				messageQueue.Dispose();
			}
		}

		public Task<V> PostMethod<V>(Func<V> payload)
		{
			var message = new Internal<V>(payload);
			messageQueue.TryAdd(message);

			return message.Task;
		}

		public Task PostMethod(Action payload)
		{
			var message = new ActionInternal(payload);
			messageQueue.TryAdd(message);

			return message.Task;
		}

		public ThreadWithMessageQueue()
		{
			messageQueue = new BlockingCollection<IInternal>(new ConcurrentQueue<IInternal>());

			thread = new Thread(new ThreadStart(() =>
			{
				foreach (var message in messageQueue.GetConsumingEnumerable())
					message.Invoke();
			}));

			thread.SetApartmentState(ApartmentState.STA);

			// Do not prevent app from closing
			thread.IsBackground = true;

			thread.Start();
		}

		private interface IInternal
		{
			void Invoke();
		}

		private sealed class Internal<T> : IInternal
		{
			private readonly Func<T> payload;
			private readonly TaskCompletionSource<T> taskCompletionSource =
				new(TaskCreationOptions.RunContinuationsAsynchronously);

			public Task<T> Task => taskCompletionSource.Task;

			public Internal(Func<T> payload)
			{
				this.payload = payload;
			}

			public void Invoke()
			{
				taskCompletionSource.SetResult(payload());
			}
		}

		private sealed class ActionInternal : IInternal
		{
			private readonly Action payload;
			private readonly TaskCompletionSource taskCompletionSource =
				new(TaskCreationOptions.RunContinuationsAsynchronously);

			public Task Task => taskCompletionSource.Task;

			public ActionInternal(Action payload)
			{
				this.payload = payload;
			}

			public void Invoke()
			{
				payload();
				taskCompletionSource.SetResult();
			}
		}
	}
}
