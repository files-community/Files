using System;
using System.Threading.Tasks;
using Files.Backend.Services;
using Microsoft.Toolkit.Uwp;
using Windows.System;

namespace Files.Uwp.ServicesImplementation
{
    internal sealed class ThreadingService : IThreadingService
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public ThreadingService()
        {
            this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public Task ExecuteOnUiThreadAsync(Action action)
        {
            return _dispatcherQueue.EnqueueAsync(action);
        }

        public Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func)
        {
            return _dispatcherQueue.EnqueueAsync<TResult?>(func);
        }
    }
}
