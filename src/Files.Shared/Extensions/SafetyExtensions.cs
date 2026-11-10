// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Files.Shared.Extensions
{
	public static class SafetyExtensions
	{
		public static bool IgnoreExceptions(Action action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				action();
				return true;
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return false;
				}
				else
					throw;
			}
		}

		public static async Task<bool> IgnoreExceptions(Func<Task> action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				await action();

				return true;
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return false;
				}
				else
					throw;
			}
		}

		public static T? IgnoreExceptions<T>(Func<T> action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				return action();
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return default;
				}
				else
					throw;
			}
		}

		public static async Task<T?> IgnoreExceptions<T>(Func<Task<T>> action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				return await action();
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return default;
				}
				else
					throw;
			}
		}

		public static async Task<TOut> Wrap<TOut>(Func<Task<TOut>> inputTask, Func<Func<Task<TOut>>, Exception, Task<TOut>> onFailed)
		{
			try
			{
				return await inputTask();
			}
			catch (Exception ex)
			{
				return await onFailed(inputTask, ex);
			}
		}

		public static async Task WrapAsync(Func<Task> inputTask, Func<Func<Task>, Exception, Task> onFailed)
		{
			try
			{
				await inputTask();
			}
			catch (Exception ex)
			{
				await onFailed(inputTask, ex);
			}
		}

		/// <summary>
		/// Awaits a background callback. Recoverable exceptions are logged; fatal exceptions propagate.
		/// </summary>
		public static async Task LogExceptionsAsync(Func<Task> action, ILogger? logger, string message)
		{
			try
			{
				await action();
			}
			catch (OperationCanceledException)
			{
				// Cancellation is a normal end of background work.
			}
			catch (Exception ex) when (IsNonFatal(ex))
			{
				try
				{
					logger?.LogError(ex, "{Message}", message);
				}
				catch (Exception logEx) when (IsNonFatal(logEx))
				{
					// Logging must not become an unhandled exception on an async void callback.
				}
			}
		}

		private static bool IsNonFatal(Exception ex)
		{
			if (ex is AggregateException aggregate)
			{
				foreach (var inner in aggregate.InnerExceptions)
				{
					if (!IsNonFatal(inner))
						return false;
				}

				return true;
			}

			return ex is not OutOfMemoryException
				and not AccessViolationException
				and not SEHException
				and not InsufficientExecutionStackException;
		}
	}
}
