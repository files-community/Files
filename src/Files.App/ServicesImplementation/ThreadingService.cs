using System;
using System.Threading.Tasks;
using Files.Backend.Services;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

namespace Files.App.ServicesImplementation
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
