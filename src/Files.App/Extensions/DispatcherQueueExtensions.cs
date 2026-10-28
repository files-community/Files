using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;

namespace Files.App.Extensions
{
	// Window.DispatcherQueue seems to be null sometimes.
	// We don't know why, but as a workaround, we invoke the function directly if DispatcherQueue is null.
	public static class DispatcherQueueExtensions
	{
		// Always queue so loading cannot start inside a layout or binding callback.
		public static Task EnqueueDeferredAsync(this DispatcherQueue? dispatcher, Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			if (dispatcher?.TryEnqueue(priority, async () =>
			{
				try
				{
					await function();
					completion.TrySetResult();
				}
				catch (Exception ex)
				{
					completion.TrySetException(ex);
				}
			}) is not true)
				completion.TrySetResult();
			return completion.Task;
		}

		public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(async () =>
			{
				if (dispatcher is not null)
				{
					try
					{
						await dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex) when (ex.Message is "Failed to enqueue the operation")
					{
					}
					return;
				}

				await function();
			}, App.Logger, typeof(COMException));
		}

		public static Task<T?> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(async () =>
			{
				if (dispatcher is not null)
				{
					try
					{
						return await dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex) when (ex.Message is "Failed to enqueue the operation")
					{
						return default;
					}
				}

				return await function();
			}, App.Logger, typeof(COMException));
		}

		public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(async () =>
			{
				if (dispatcher is not null)
				{
					try
					{
						await dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex) when (ex.Message is "Failed to enqueue the operation")
					{
					}
					return;
				}

				function();
			}, App.Logger, typeof(COMException));
		}

		public static Task<T?> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
		{
			return SafetyExtensions.IgnoreExceptions(async () =>
			{
				if (dispatcher is not null)
				{
					try
					{
						return await dispatcher.EnqueueAsync(function, priority);
					}
					catch (InvalidOperationException ex) when (ex.Message is "Failed to enqueue the operation")
					{
						return default;
					}
				}

				return function();
			}, App.Logger, typeof(COMException));
		}

	}
}
