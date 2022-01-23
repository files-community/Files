using Files.Enums;
using System;
using System.Threading.Tasks;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryHelpers : IDisposable
    {
        #region Private Members

        private IStorageHistoryOperations storageHistoryOperations;

        #endregion Private Members

        #region Constructor

        public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
        {
            this.storageHistoryOperations = storageHistoryOperations;
        }

        #endregion Constructor

        #region Undo, Redo

        public async Task<ReturnResult> TryUndo()
        {
            if (App.HistoryWrapper.CanUndo())
            {
                if (!(await App.SemaphoreSlim.WaitAsync(0)))
                {
                    return ReturnResult.InProgress;
                }

                try
                {
                    return await storageHistoryOperations.Undo(App.HistoryWrapper.GetCurrentHistory());
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
                if (!(await App.SemaphoreSlim.WaitAsync(0)))
                {
                    return ReturnResult.InProgress;
                }

                try
                {
                    App.HistoryWrapper.IncreaseIndex();
                    return await storageHistoryOperations.Redo(App.HistoryWrapper.GetCurrentHistory());
                }
                finally
                {
                    App.SemaphoreSlim.Release();
                }
            }

            return ReturnResult.Cancelled;
        }

        #endregion Undo, Redo

        #region IDisposable

        public void Dispose()
        {
            storageHistoryOperations?.Dispose();

            storageHistoryOperations = null;
        }

        #endregion IDisposable
    }
}