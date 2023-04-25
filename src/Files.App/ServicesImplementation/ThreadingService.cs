using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.Backend.Services;
using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	internal sealed class ThreadingService : IThreadingService
	{
		private readonly DispatcherQueue _dispatcherQueue;

		public ThreadingService()
		{
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		}

		public Task ExecuteOnUiThreadAsync(Action action)
		{
			return _dispatcherQueue.EnqueueOrInvokeAsync(action);
		}

		public Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func)
		{
			return _dispatcherQueue.EnqueueOrInvokeAsync<TResult?>(func);
		}
	}
}
