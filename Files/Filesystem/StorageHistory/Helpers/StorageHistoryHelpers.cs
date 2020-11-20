using Files.Extensions;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryHelpers : IDisposable
    {
        #region Private Members

        private IStorageHistoryOperations storageHistoryOperations;

        private Queue<Func<Task<ReturnResult>>> operationsQueue = new Queue<Func<Task<ReturnResult>>>();

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
                App.StorageHistoryIndex--;
                int index = EnumerableHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                operationsQueue.Enqueue(new Func<Task<ReturnResult>>(() => this.storageHistoryOperations.Undo(App.StorageHistory[index])));
            }
            await operationsQueue.Throttle(1);
        }

        public async Task Redo()
        {
            if (CanRedo())
            {
                int index = EnumerableHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                App.StorageHistoryIndex++;
                operationsQueue.Enqueue(new Func<Task<ReturnResult>>(() => this.storageHistoryOperations.Redo(App.StorageHistory[index])));
            }
            await operationsQueue.Throttle(1);
        }

        #endregion

        #region Public Helpers

        public static bool CanUndo() =>
            App.StorageHistoryIndex > 0;

        public static bool CanRedo() =>
            App.StorageHistoryIndex < App.StorageHistory.Count;

        #endregion

        #region Private Helpers



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
