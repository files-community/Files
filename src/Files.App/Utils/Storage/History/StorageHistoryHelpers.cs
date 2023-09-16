// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage
{
	public class StorageHistoryHelpers : IDisposable
	{
		private static readonly StorageHistoryWrapper _storageHistoryWrapper = Ioc.Default.GetRequiredService<StorageHistoryWrapper>();

		private IStorageHistoryOperations operations;

		private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
			=> operations = storageHistoryOperations;

		public async Task<ReturnResult> TryUndo()
		{
			if (_storageHistoryWrapper.CanUndo())
			{
				if (!await semaphore.WaitAsync(0))
				{
					return ReturnResult.InProgress;
				}
				bool keepHistory = false;
				try
				{
					ReturnResult result = await operations.Undo(_storageHistoryWrapper.GetCurrentHistory());
					keepHistory = result is ReturnResult.Cancelled;
					return result;
				}
				finally
				{
					if (!keepHistory)
						_storageHistoryWrapper.DecreaseIndex();

					semaphore.Release();
				}
			}

			return ReturnResult.Cancelled;
		}

		public async Task<ReturnResult> TryRedo()
		{
			if (_storageHistoryWrapper.CanRedo())
			{
				if (!await semaphore.WaitAsync(0))
				{
					return ReturnResult.InProgress;
				}
				try
				{
					_storageHistoryWrapper.IncreaseIndex();
					return await operations.Redo(_storageHistoryWrapper.GetCurrentHistory());
				}
				finally
				{
					semaphore.Release();
				}
			}

			return ReturnResult.Cancelled;
		}

		public void Dispose()
		{
			operations?.Dispose();
			operations = null;
		}
	}
}