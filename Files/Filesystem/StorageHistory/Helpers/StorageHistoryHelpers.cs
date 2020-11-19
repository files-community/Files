using Files.Helpers;
using System;
using System.Threading.Tasks;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryHelpers : IDisposable
    {
        #region Private Members

        private IStorageHistoryOperations _storageHistoryOperations;

        #endregion

        #region Constructor

        public StorageHistoryHelpers(IStorageHistoryOperations storageHistoryOperations)
        {
            this._storageHistoryOperations = storageHistoryOperations;
        }

        #endregion

        #region Undo, Redo

        public async Task<ReturnResult> Undo()
        {
            if (CanUndo())
            {
                App.StorageHistoryIndex--;
                int index = EnumerableHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                return await this._storageHistoryOperations.Undo(App.StorageHistory[index]);
            }
            return ReturnResult.InProgress;
        }

        public async Task<ReturnResult> Redo()
        {
            if (CanRedo())
            {
                int index = EnumerableHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                App.StorageHistoryIndex++;
                return await this._storageHistoryOperations.Redo(App.StorageHistory[index]);
            }
            return ReturnResult.InProgress;
        }

        #endregion

        #region Helpers

        public static bool CanUndo() =>
            App.StorageHistoryIndex > 0;

        public static bool CanRedo() =>
            App.StorageHistoryIndex < App.StorageHistory.Count;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            this._storageHistoryOperations?.Dispose();

            this._storageHistoryOperations = null;
        }

        #endregion
    }
}
