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

        #endregion

        #region Constructor

        public StorageHistoryWrapper()
        {
            this.storageHistory = new List<IStorageHistory>();
            this.storageHistoryIndex = 0;
        }

        #endregion

        #region Helpers

        public void AddHistory(IStorageHistory history)
        {
            if (history != null)
            {
                this.storageHistory?.Add(history);

                if (this.storageHistory?.Count > 1)
                {
                    this.storageHistoryIndex++;
                }
            }
        }

        public void RemoveHistory(IStorageHistory history)
        {
            if (history != null)
            {
                this.storageHistory?.Remove(history);
                this.storageHistoryIndex--;
            }
        }

        public void ModifyCurrentHistory(IStorageHistory newHistory)
        {
            this.storageHistory?[this.storageHistoryIndex].Modify(newHistory);
        }

        public IStorageHistory GetCurrentHistory()
        {
            return this.storageHistory?.ElementAt(this.storageHistoryIndex);
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

        #endregion

        #region IDisposable

        public void Dispose()
        {
            storageHistory?.ForEach((item) => item?.Dispose());

            storageHistory?.ForEach((item) => item = null);
            storageHistory = null;
        }

        #endregion
    }
}
