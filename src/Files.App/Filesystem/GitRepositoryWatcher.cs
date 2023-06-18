using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using static Files.App.Helpers.NativeDirectoryChangesHelper;

namespace Files.App.Filesystem
{
	public class GitRepositoryWatcher : IWatcher
	{
		private CancellationTokenSource cancellationTokenSource;
		private readonly ILocatableFolder gitDirectory;
		private readonly ConcurrentQueue<uint> gitChangesQueue;
		private readonly AsyncManualResetEvent gitChangedEvent;
		public event EventHandler? GitDirectoryUpdated;
		private Task? gitProcessQueueAction;

		public GitRepositoryWatcher(ILocatableFolder gitDirectory)
		{
			this.gitDirectory = gitDirectory;
			cancellationTokenSource = new CancellationTokenSource();
			gitChangesQueue = new ConcurrentQueue<uint>();
			gitChangedEvent = new AsyncManualResetEvent();
		}

		private void WatchForGitChanges()
		{
			var hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(
				gitDirectory.Path!,
				1,
				1 | 2 | 4,
				nint.Zero,
				3,
				(uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped,
				nint.Zero);

			if (hWatchDir.ToInt64() == -1)
				return;

			gitProcessQueueAction ??= Task.Factory.StartNew(() => ProcessGitChangesQueueAsync(cancellationTokenSource.Token), default,
				TaskCreationOptions.LongRunning, TaskScheduler.Default);

			var gitWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
			{
				var buff = new byte[4096];
				var rand = Guid.NewGuid();
				var notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE | FILE_NOTIFY_CHANGE_CREATION;

				if (true)
					notifyFilters |= FILE_NOTIFY_CHANGE_ATTRIBUTES;

				var overlapped = new OVERLAPPED
				{
					hEvent = CreateEvent(nint.Zero, false, false, null)
				};
				const uint INFINITE = 0xFFFFFFFF;

				while (x.Status != AsyncStatus.Canceled)
				{
					unsafe
					{
						fixed (byte* pBuff = buff)
						{
							ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
							if (x.Status == AsyncStatus.Canceled)
								break;

							ReadDirectoryChangesW(hWatchDir, pBuff,
								4096, true,
								notifyFilters, null,
								ref overlapped, null);

							if (x.Status == AsyncStatus.Canceled)
								break;

							var rc = WaitForSingleObjectEx(overlapped.hEvent, INFINITE, true);

							uint offset = 0;
							ref var notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);
							if (x.Status == AsyncStatus.Canceled)
								break;

							do
							{
								notifyInfo = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[offset]);

								uint action = notifyInfo.Action;

								gitChangesQueue.Enqueue(action);

								offset += notifyInfo.NextEntryOffset;
							}
							while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

							gitChangedEvent.Set();
						}
					}
				}

				CloseHandle(overlapped.hEvent);
				gitChangesQueue.Clear();
			});

			cancellationTokenSource.Token.Register(() =>
			{
				if (gitWatcherAction is not null)
				{
					gitWatcherAction?.Cancel();

					// Prevent duplicate execution of this block
					gitWatcherAction = null;
				}

				CancelIoEx(hWatchDir, nint.Zero);
				CloseHandle(hWatchDir);
			});
		}

		private async Task ProcessGitChangesQueueAsync(CancellationToken cancellationToken)
		{
			const int DELAY = 200;
			var sampler = new IntervalSampler(100);
			int changes = 0;

			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					if (await gitChangedEvent.WaitAsync(DELAY, cancellationToken))
					{
						gitChangedEvent.Reset();
						while (gitChangesQueue.TryDequeue(out var _))
							++changes;

						if (changes != 0 && sampler.CheckNow())
						{
							GitDirectoryUpdated?.Invoke(gitDirectory, EventArgs.Empty!);
							changes = 0;
						}
					}
				}
			}
			catch { }
		}

		public void Start()
		{
			Stop();
			cancellationTokenSource = new CancellationTokenSource();
			WatchForGitChanges();
		}

		public void Stop()
		{
			cancellationTokenSource.Cancel();
		}
	}
}
