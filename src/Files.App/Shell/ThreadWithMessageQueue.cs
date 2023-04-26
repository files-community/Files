using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Files.App.Shell
{
	[SupportedOSPlatform("Windows")]
	public class ThreadWithMessageQueue : Disposable
	{
		private readonly Channel<Internal> channel;

		private readonly Thread thread;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				channel.Writer.TryComplete();
				thread.Join();
			}
		}

		public async Task<V> PostMethod<V>(Func<object> payload)
		{
			var message = new Internal(payload);

			if (await channel.Writer.WaitToWriteAsync())
				channel.Writer.TryWrite(message);

			return (V)await message.tcs.Task;
		}

		public async Task<object> PostMethod(Action payload)
		{
			var message = new Internal(payload);

			if (await channel.Writer.WaitToWriteAsync())
				channel.Writer.TryWrite(message);

			return await message.tcs.Task;
		}

		public ThreadWithMessageQueue()
		{
			channel = Channel.CreateUnbounded<Internal>(new UnboundedChannelOptions { SingleReader = true });

			thread = new Thread(new ThreadStart(async () =>
			{
				while (await channel.Reader.WaitToReadAsync())
				{
					while (channel.Reader.TryRead(out var message))
					{
						var res = message.payload();
						message.tcs.SetResult(res);
					}
				}
			}));

			// Do not prevent app from closing
			thread.IsBackground = true;

			thread.Start();
		}

		private class Internal
		{
			public Func<object?> payload;

			public TaskCompletionSource<object> tcs;

			public Internal(Action payload)
			{
				this.payload = () => { payload(); return default; };
				tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			public Internal(Func<object?> payload)
			{
				this.payload = payload;
				tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}
	}
}
