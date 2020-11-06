using System.Threading.Tasks;
using Files.Filesystem.FilesystemHistory;

namespace Files.Helpers
{
    public class FilesystemHistoryHelpers
    {
        #region Private Members

        private readonly IShellPage _appInstance;

        private readonly IStorageHistoryOperations _storageHistoryOperations;

        #endregion

        #region Constructor

        public FilesystemHistoryHelpers(IShellPage appInstance, IStorageHistoryOperations storageHistoryOperations)
        {
            this._appInstance = appInstance;
            this._storageHistoryOperations = storageHistoryOperations;
        }

        #endregion

        public async Task Undo()
        {
            if (App.StorageHistoryIndex > 0) // Can Undo
            {
                App.StorageHistoryIndex--;
                int index = OutOfRange.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                await this._storageHistoryOperations.Undo(App.StorageHistory[index]);
            }
        }

        public async Task Redo()
        {
            if (App.StorageHistoryIndex < App.StorageHistory.Count) // Can Redo
            {
                App.StorageHistoryIndex++;
                int index = OutOfRange.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                await this._storageHistoryOperations.Redo(App.StorageHistory[index]);
            }
        }
    }
}
