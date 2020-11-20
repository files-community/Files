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

        public async Task Undo()
        {
            if (CanUndo())
            {
                if (!(await App.SemaphoreSlim.WaitAsync(0)))
                    return;

                try
                {
                    App.StorageHistoryIndex--;
                    int index = ArrayHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                    await this.storageHistoryOperations.Undo(App.StorageHistory[index]);
                }
                finally
                {
                    App.SemaphoreSlim.Release();
                }
            }
        }

        public async Task Redo()
        {
            if (CanRedo())
            {
                if (!(await App.SemaphoreSlim.WaitAsync(0)))
                    return;

                try
                {
                    int index = ArrayHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                    App.StorageHistoryIndex++;
                    await this.storageHistoryOperations.Redo(App.StorageHistory[index]);
                }
                finally
                {
                    App.SemaphoreSlim.Release();
                }
            }
        }

        #endregion

        #region Public Helpers

        public static bool CanUndo() =>
            App.StorageHistoryIndex > 0;

        public static bool CanRedo() =>
            App.StorageHistoryIndex < App.StorageHistory.Count;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            this.storageHistoryOperations?.Dispose();

            this.storageHistoryOperations = null;
        }

        #endregion
    }
}
