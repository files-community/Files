﻿using Files.Shared.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Uwp.Filesystem.FilesystemHistory
{
    public class StorageHistoryHelpers : IDisposable
    {
        private IStorageHistoryOperations operations;

        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
            => operations = storageHistoryOperations;

        public async Task<ReturnResult> TryUndo()
        {
            if (App.HistoryWrapper.CanUndo())
            {
                if (!await semaphore.WaitAsync(0))
                {
                    return ReturnResult.InProgress;
                }
                try
                {
                    return await operations.Undo(App.HistoryWrapper.GetCurrentHistory());
                }
                finally
                {
                    App.HistoryWrapper.DecreaseIndex();
                    semaphore.Release();
                }
            }

            return ReturnResult.Cancelled;
        }

        public async Task<ReturnResult> TryRedo()
        {
            if (App.HistoryWrapper.CanRedo())
            {
                if (!await semaphore.WaitAsync(0))
                {
                    return ReturnResult.InProgress;
                }
                try
                {
                    App.HistoryWrapper.IncreaseIndex();
                    return await operations.Redo(App.HistoryWrapper.GetCurrentHistory());
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