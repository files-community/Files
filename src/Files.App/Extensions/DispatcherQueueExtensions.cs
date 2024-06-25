using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;

namespace Files.App.Extensions
{
	// Window.DispatcherQueue seems to be null sometimes.
	// We don't know why, but as a workaround, we invoke the function directly if DispatcherQueue is null.
	public static class DispatcherQueueExtensions
	{
		public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(() =>
			{
				if (dispatcher is not null)
				{
					try
					{
						return dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex)
					{
						if (ex.Message is not "Failed to enqueue the operation")
							throw;
					}
				}

				return function();
			}, App.Logger, typeof(COMException));
		}

		public static Task<T?> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(() =>
			{
				if (dispatcher is not null)
				{
					try
					{
						return dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex)
					{
						if (ex.Message is not "Failed to enqueue the operation")
							throw;
					}
				}

				return function();
			}, App.Logger, typeof(COMException));
		}

		public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(() =>
			{
				if (dispatcher is not null)
				{
					try
					{
						return dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex)
					{
						if (ex.Message is not "Failed to enqueue the operation")
							throw;
					}
				}

				function();
				return Task.CompletedTask;
			}, App.Logger, typeof(COMException));
		}

		public static Task<T?> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(() =>
			{
				if (dispatcher is not null)
				{
					try
					{
						return dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex)
					{
						if (ex.Message is not "Failed to enqueue the operation")
							throw;
					}
				}

				return Task.FromResult(function());
			}, App.Logger, typeof(COMException));
		}

	}
}
