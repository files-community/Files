using Files.Shared.Enums;
using System;
using System.Threading;

namespace Files.Shared
{
    /// <summary>Wrapper for the ReaderWriterLockSlim which allows callers to dispose the object to remove the lock </summary>
    /// https://itneverworksfirsttime.wordpress.com/2011/06/29/an-idisposable-locking-implementation/
    public class DisposableLockWrapper : IDisposable
    {
        /// <summary>The lock object being wrapped</summary>
        private readonly ReaderWriterLockSlim readerWriterLock;

        /// <summary>The lock type</summary>
        private readonly LockType lockType;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposableLockWrapper"/> class.
        /// </summary>
        /// <param name="readerWriterLock">The reader writer lock.</param>
        /// <param name="lockType">Type of the lock.</param>
        public DisposableLockWrapper(ReaderWriterLockSlim readerWriterLock, LockType lockType)
        {
            this.readerWriterLock = readerWriterLock;
            this.lockType = lockType;

            switch (this.lockType)
            {
                case LockType.Read:
                    this.readerWriterLock.EnterReadLock();
                    break;

                case LockType.UpgradeableRead:
                    this.readerWriterLock.EnterUpgradeableReadLock();
                    break;

                case LockType.Write:
                    this.readerWriterLock.EnterWriteLock();
                    break;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed objects
                switch (this.lockType)
                {
                    case LockType.Read:
                        this.readerWriterLock.ExitReadLock();
                        break;

                    case LockType.UpgradeableRead:
                        this.readerWriterLock.ExitUpgradeableReadLock();
                        break;

                    case LockType.Write:
                        this.readerWriterLock.ExitWriteLock();
                        break;
                }
            }
        }
    }
}