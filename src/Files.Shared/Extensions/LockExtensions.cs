using Files.Shared.Enums;
using System;
using System.Threading;

namespace Files.Shared.Extensions
{
    /// <summary>Extension methods for the lock() statement </summary>
    public static class LockExtensions
    {
        public static T WithLock<T>(this object lockObject, Func<T> action)
        {
            lock (lockObject)
            {
                return action();
            }
        }
        
        public static void WithLock(this object lockObject, Action action)
        {
            lock (lockObject)
            {
                action();
            }
        }
    }
}