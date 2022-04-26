using Files.Shared.Enums;
using System;
using System.Threading.Tasks;

namespace Files.Uwp.Filesystem.FilesystemHistory
{
    public class StorageHistoryHelpers : IDisposable
    {
        private IStorageHistoryOperations operations;

        public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
            => operations = storageHistoryOperations;

        public async Task<ReturnResult> TryUndo()
        {
            if (App.HistoryWrapper.CanUndo())
            {
                if (!await App.SemaphoreSlim.WaitAsync(0))
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
                    App.SemaphoreSlim.Release();
                }
            }

            return ReturnResult.Cancelled;
        }

        public async Task<ReturnResult> TryRedo()
        {
            if (App.HistoryWrapper.CanRedo())
            {
                if (!await App.SemaphoreSlim.WaitAsync(0))
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
                    App.SemaphoreSlim.Release();
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