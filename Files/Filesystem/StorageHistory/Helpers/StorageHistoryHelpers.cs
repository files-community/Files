using Files.Enums;
using Files.Helpers;
using System;
using System.Threading.Tasks;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryHelpers : IDisposable
    {
        #region Private Members

        private IStorageHistoryOperations storageHistoryOperations;

        #endregion

        #region Constructor

        public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
        {
            this.storageHistoryOperations = storageHistoryOperations;
        }

        #endregion

        #region Undo, Redo

        public async Task<ReturnResult> TryUndo()
        {
            if (CanUndo())
            {
                if (!(await App.SemaphoreSlim.WaitAsync(0)))
                {
                    return ReturnResult.InProgress;
                }

                try
                {
                    App.StorageHistoryIndex--;
                    int index = ArrayHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                    return await storageHistoryOperations.Undo(App.StorageHistory[index]);
                }
                finally
                {
                    App.SemaphoreSlim.Release();
                }
            }

            return ReturnResult.Cancelled;
        }

        public async Task<ReturnResult> TryRedo()
        {
            if (CanRedo())
            {
                if (!(await App.SemaphoreSlim.WaitAsync(0)))
                {
                    return ReturnResult.InProgress;
                }

                try
                {
                    int index = ArrayHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                    App.StorageHistoryIndex++;
                    return await storageHistoryOperations.Redo(App.StorageHistory[index]);
                }
                finally
                {
                    App.SemaphoreSlim.Release();
                }
            }

            return ReturnResult.Cancelled;
        }

        #endregion

        #region Public Helpers

        public static bool CanUndo() =>
            App.StorageHistoryIndex >= 0 && App.StorageHistory.Count > 0;

        public static bool CanRedo() =>
            App.StorageHistoryIndex < App.StorageHistory.Count;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            storageHistoryOperations?.Dispose();

            storageHistoryOperations = null;
        }

        #endregion
    }
}
