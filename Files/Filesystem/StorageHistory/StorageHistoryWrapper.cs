using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistoryWrapper : IDisposable
    {
        #region Private Members

        private List<IStorageHistory> storageHistory;

        private int storageHistoryIndex;

        #endregion Private Members

        #region Constructor

        public StorageHistoryWrapper()
        {
            this.storageHistory = new List<IStorageHistory>();
            this.storageHistoryIndex = -1;
        }

        #endregion Constructor

        #region Helpers

        public void AddHistory(IStorageHistory history)
        {
            if (history != null)
            {
                this.storageHistoryIndex++;
                this.storageHistory.Insert(this.storageHistoryIndex, history);
                // If a history item is added also remove all the redo operations after it
                for (var idx = this.storageHistory.Count - 1; idx > this.storageHistoryIndex; idx--)
                {
                    this.storageHistory.RemoveAt(idx);
                }
            }
        }

        public void RemoveHistory(IStorageHistory history, bool decreaseIndex)
        {
            if (history != null)
            {
                // If a history item is invalid also remove all the redo operations after it
                for (var idx = this.storageHistory.Count - 1; idx > this.storageHistoryIndex; idx--)
                {
                    this.storageHistory.RemoveAt(idx);
                }
                if (decreaseIndex)
                {
                    this.storageHistoryIndex--;
                }
                this.storageHistory.Remove(history);
            }
        }

        public void ModifyCurrentHistory(IStorageHistory newHistory)
        {
            this.storageHistory[this.storageHistoryIndex].Modify(newHistory);
        }

        public IStorageHistory GetCurrentHistory()
        {
            return this.storageHistory.ElementAt(this.storageHistoryIndex);
        }

        public void IncreaseIndex()
        {
            this.storageHistoryIndex++;
        }

        public void DecreaseIndex()
        {
            this.storageHistoryIndex--;
        }

        public bool CanUndo() =>
            this.storageHistoryIndex >= 0 && this.storageHistory.Count > 0;

        public bool CanRedo() =>
            (this.storageHistoryIndex + 1) < this.storageHistory.Count;

        #endregion Helpers

        #region IDisposable

        public void Dispose()
        {
            storageHistory?.ForEach((item) => item?.Dispose());
            storageHistory?.ForEach((item) => item = null);
            storageHistory = null;
        }

        #endregion IDisposable
    }
}