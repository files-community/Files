using Files.Extensions;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
                try
                {
                    await App.SemaphoreSlim.WaitAsync(0);

                    App.StorageHistoryIndex--;
                    int index = EnumerableHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                    await this.storageHistoryOperations.Undo(App.StorageHistory[index]);

                    await Task.Delay(10000); // Test
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return;
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
                try
                {
                    await App.SemaphoreSlim.WaitAsync(0);

                    int index = EnumerableHelpers.FitBounds(App.StorageHistoryIndex, App.StorageHistory.Count);
                    App.StorageHistoryIndex++;
                    await this.storageHistoryOperations.Redo(App.StorageHistory[index]);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return;
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
