// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI;
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
			return _dispatcherQueue.EnqueueAsync(action);
		}

		public Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func)
		{
			return _dispatcherQueue.EnqueueAsync<TResult?>(func);
		}
	}
}
