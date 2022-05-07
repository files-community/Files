using Files.Shared.Enums;
using System;
using System.Threading;

namespace Files.Shared.Extensions
{
    /// <summary>Extension methods for the ReaderWriterLockSlim object </summary>
    public static class ReaderWriterLockSlimExtension
    {
        /// <summary>
        /// Obtains a Read lock on the ReaderWriterLockSlim object
        /// </summary>
        /// <param name="readerWriterLock">The reader writer lock.</param>
        /// <returns>An IDisposable object that will release the lock on disposal</returns>
        public static IDisposable ObtainReadLock(this ReaderWriterLockSlim readerWriterLock)
        {
            return new DisposableLockWrapper(readerWriterLock, LockType.Read);
        }

        /// <summary>
        /// Obtains an Upgradeable Read lock on the ReaderWriterLockSlim object
        /// </summary>
        /// <param name="readerWriterLock">The reader writer lock.</param>
        /// <returns>An IDisposable object that will release the lock on disposal</returns>
        public static IDisposable ObtainUpgradeableReadLock(this ReaderWriterLockSlim readerWriterLock)
        {
            return new DisposableLockWrapper(readerWriterLock, LockType.UpgradeableRead);
        }

        /// <summary>
        /// Obtains a Write Lock on the ReaderWriterLockSlim object
        /// </summary>
        /// <param name="readerWriterLock">The reader writer lock.</param>
        /// <returns>An IDisposable object that will release the lock on disposal</returns>
        public static IDisposable ObtainWriteLock(this ReaderWriterLockSlim readerWriterLock)
        {
            return new DisposableLockWrapper(readerWriterLock, LockType.Write);
        }

        public static T WithReadLock<T>(this ReaderWriterLockSlim readerWriterLock, Func<T> action)
        {
            using (var _ = readerWriterLock.ObtainReadLock())
            {
                return action();
            }
        }
        public static T WithWriteLock<T>(this ReaderWriterLockSlim readerWriterLock, Func<T> action)
        {
            using (var _ = readerWriterLock.ObtainWriteLock())
            {
                return action();
            }
        }
        public static void WithReadLock(this ReaderWriterLockSlim readerWriterLock, Action action)
        {
            using (var _ = readerWriterLock.ObtainReadLock())
            {
                action();
            }
        }
        public static void WithWriteLock(this ReaderWriterLockSlim readerWriterLock, Action action)
        {
            using (var _ = readerWriterLock.ObtainWriteLock())
            {
                action();
            }
        }
    }
}