// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Win32;

namespace Files.App.Storage
{
	/// <summary>
	/// Represents a work scheduled to execute on a STA thread.
	/// </summary>
	public partial class STATask
	{
		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <param name="action">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task Run(Action action, ILogger? logger)
		{
			var tcs = new TaskCompletionSource();

			Thread thread =
				new(() =>
				{
					PInvoke.OleInitialize();

					try
					{
						action();
						tcs.SetResult();
					}
					catch (Exception ex)
					{
						tcs.SetResult();
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the function.</typeparam>
		/// <param name="func">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task<T> Run<T>(Func<T> func, ILogger? logger)
		{
			var tcs = new TaskCompletionSource<T>();

			Thread thread =
				new(() =>
				{
					PInvoke.OleInitialize();

					try
					{
						tcs.SetResult(func());
					}
					catch (Exception ex)
					{
						tcs.SetResult(default!);
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <param name="func">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task Run(Func<Task> func, ILogger? logger)
		{
			var tcs = new TaskCompletionSource();

			Thread thread =
				new(async () =>
				{
					PInvoke.OleInitialize();

					try
					{
						await func();
						tcs.SetResult();
					}
					catch (Exception ex)
					{
						tcs.SetResult();
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		/// <summary>
		/// Schedules the specified work to execute in a new background thread initialized with STA state.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the function.</typeparam>
		/// <param name="func">The work to execute in the STA thread.</param>
		/// <param name="logger">A logger to capture any exception that occurs during execution.</param>
		/// <returns>A <see cref="Task"/> that represents the work scheduled to execute in the STA thread.</returns>
		public static Task<T?> Run<T>(Func<Task<T>> func, ILogger? logger)
		{
			var tcs = new TaskCompletionSource<T?>();

			Thread thread =
				new(async () =>
				{
					PInvoke.OleInitialize();

					try
					{
						tcs.SetResult(await func());
					}
					catch (Exception ex)
					{
						tcs.SetResult(default);
						logger?.LogWarning(ex, "An exception was occurred during the execution within STA.");
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				});

			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}
	}
}
