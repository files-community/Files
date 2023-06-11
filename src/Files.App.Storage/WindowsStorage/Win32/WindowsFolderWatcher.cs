using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Storage.WindowsStorage.Win32.Helpers;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using static Files.App.Storage.WindowsStorage.Win32.Helpers.NativeDirectoryChangesHelper;

namespace Files.App.Storage.WindowsStorage.Win32
{
	public class WindowsFolderWatcher : IFolderWatcher
	{
		public IMutableFolder Folder { get; }
		private CancellationTokenSource watcherTokenSource;
		private readonly AsyncManualResetEvent operationEvent;
		private Task? aProcessQueueAction;
		private readonly ConcurrentQueue<(uint Action, string FileName)> operationQueue;
		private ILocatableFolder folder => (ILocatableFolder)Folder;

		private WindowsStorageService storageService = Ioc.Default.GetRequiredService<WindowsStorageService>();
		private ILogger<WindowsFolderWatcher> logger = Ioc.Default.GetRequiredService<ILogger<WindowsFolderWatcher>>();

		public event NotifyCollectionChangedEventHandler? CollectionChanged;
		public event EventHandler<FileSystemEventArgs> ItemAdded;
		public event EventHandler<FileSystemEventArgs> ItemRemoved;
		public event EventHandler<FileSystemEventArgs> ItemChanged;
		public event EventHandler<RenamedEventArgs> ItemRenamed;

		public WindowsFolderWatcher(IMutableFolder folder)
		{
			Folder = folder;
			operationEvent = new AsyncManualResetEvent();
			operationQueue = new ConcurrentQueue<(uint Action, string FileName)>();
		}

		private void WatchForDirectoryChanges(bool hasSyncStatus = true)
		{
			var hWatchDir = NativeFileOperationsHelper.CreateFileFromApp(folder.Path, 1, 1 | 2 | 4,
				IntPtr.Zero, 3, (uint)NativeFileOperationsHelper.File_Attributes.BackupSemantics | (uint)NativeFileOperationsHelper.File_Attributes.Overlapped, IntPtr.Zero);
			if (hWatchDir.ToInt64() == -1)
				return;

			aProcessQueueAction ??= Task.Factory.StartNew(() => ProcessOperationQueue(watcherTokenSource.Token), default,
				TaskCreationOptions.LongRunning, TaskScheduler.Default);

			var aWatcherAction = Windows.System.Threading.ThreadPool.RunAsync((x) =>
			{
				var buff = new byte[4096];
				var rand = Guid.NewGuid();
				var notifyFilters = FILE_NOTIFY_CHANGE_DIR_NAME | FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_SIZE;

				if (hasSyncStatus)
					notifyFilters |= FILE_NOTIFY_CHANGE_ATTRIBUTES;

				var overlapped = new OVERLAPPED();
				overlapped.hEvent = CreateEvent(IntPtr.Zero, false, false, null);
				const uint INFINITE = 0xFFFFFFFF;

				while (x.Status != AsyncStatus.Canceled)
				{
					unsafe
					{
						fixed (byte* pBuff = buff)
						{
							ref var notifyInformation = ref Unsafe.As<byte, FILE_NOTIFY_INFORMATION>(ref buff[0]);
							if (x.Status != AsyncStatus.Canceled)
							{
								ReadDirectoryChangesW(hWatchDir, pBuff,
								4096, false,
								notifyFilters, null,
								ref overlapped, null);
							}
							else
							{
								break;
							}

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
								string? FileName = null;
								unsafe
								{
									fixed (char* name = notifyInfo.FileName)
									{
										FileName = Path.Combine(folder.Path, new string(name, 0, (int)notifyInfo.FileNameLength / 2));
									}
								}

								uint action = notifyInfo.Action;

								operationQueue.Enqueue((action, FileName));

								offset += notifyInfo.NextEntryOffset;
							}
							while (notifyInfo.NextEntryOffset != 0 && x.Status != AsyncStatus.Canceled);

							operationEvent.Set();
						}
					}
				}

				CloseHandle(overlapped.hEvent);
				operationQueue.Clear();
			});

			watcherTokenSource.Token.Register(() =>
			{
				if (aWatcherAction is not null)
				{
					aWatcherAction?.Cancel();

					// Prevent duplicate execution of this block
					aWatcherAction = null;
				}

				CancelIoEx(hWatchDir, IntPtr.Zero);
				CloseHandle(hWatchDir);
			});
		}

		private async Task ProcessOperationQueue(CancellationToken cancellationToken)
		{
			const uint FILE_ACTION_ADDED = 0x00000001;
			const uint FILE_ACTION_REMOVED = 0x00000002;
			const uint FILE_ACTION_MODIFIED = 0x00000003;
			const uint FILE_ACTION_RENAMED_OLD_NAME = 0x00000004;
			const uint FILE_ACTION_RENAMED_NEW_NAME = 0x00000005;

			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					if (await operationEvent.WaitAsync(200, cancellationToken))
					{
						operationEvent.Reset();

						while (operationQueue.TryDequeue(out var operation))
						{
							if (cancellationToken.IsCancellationRequested)
								break;

							try
							{
								switch (operation.Action)
								{
									case FILE_ACTION_ADDED:
									case FILE_ACTION_RENAMED_NEW_NAME:
										ItemAdded?.Invoke(null, new FileSystemEventArgs(WatcherChangeTypes.Created, folder.Path, operation.FileName));
										break;

									case FILE_ACTION_MODIFIED:
										ItemChanged?.Invoke(null, new FileSystemEventArgs(WatcherChangeTypes.Changed, folder.Path, operation.FileName));
										break;

									case FILE_ACTION_REMOVED:
										ItemRemoved?.Invoke(null, new FileSystemEventArgs(WatcherChangeTypes.Deleted, folder.Path, operation.FileName));
										break;

									case FILE_ACTION_RENAMED_OLD_NAME:
										ItemRenamed?.Invoke(null, new RenamedEventArgs(WatcherChangeTypes.Renamed, folder.Path, operation.FileName, operation.FileName));
										break;
								}
							}
							catch (Exception ex)
							{
								logger.LogWarning(ex, ex.Message);
							}
						}
					}
				}
			}
			catch
			{
				// Prevent disposed cancellation token
			}
		}

		public void Dispose()
		{
			watcherTokenSource.Cancel();
			watcherTokenSource.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}

		public void Start()
		{
			watcherTokenSource = new CancellationTokenSource();
		}

		public void Stop()
		{
			Dispose();
		}
	}
}
