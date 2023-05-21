// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.FilesystemHistory
{
	/// <summary>
	/// Provides static helper for storage history.
	/// </summary>
	public class StorageHistoryHelpers : IDisposable
	{
		private IStorageHistoryOperations _operations;

		private readonly static SemaphoreSlim _semaphore = new(1, 1);

		public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
		{
			_operations = storageHistoryOperations;
		}

		public async Task<ReturnResult> TryUndo()
		{
			if (App.HistoryWrapper.CanUndo())
			{
				if (!await _semaphore.WaitAsync(0))

					return ReturnResult.InProgress;

				bool keepHistory = false;

				try
				{
					ReturnResult result = await _operations.Undo(App.HistoryWrapper.GetCurrentHistory());
					keepHistory = result is ReturnResult.Cancelled;

					return result;
				}
				finally
				{
					if (!keepHistory)
						App.HistoryWrapper.DecreaseIndex();

					_semaphore.Release();
				}
			}

			return ReturnResult.Cancelled;
		}

		public async Task<ReturnResult> TryRedo()
		{
			if (App.HistoryWrapper.CanRedo())
			{
				if (!await _semaphore.WaitAsync(0))
					return ReturnResult.InProgress;

				try
				{
					App.HistoryWrapper.IncreaseIndex();

					return await _operations.Redo(App.HistoryWrapper.GetCurrentHistory());
				}
				finally
				{
					_semaphore.Release();
				}
			}

			return ReturnResult.Cancelled;
		}

		public void Dispose()
		{
			_operations?.Dispose();
			_operations = null;
		}
	}
}
