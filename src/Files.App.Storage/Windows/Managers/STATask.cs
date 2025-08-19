// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.Security;

namespace Files.App.Storage
{
	/// <summary>
	/// Represents a synchronous/asynchronous operation on STA.
	/// </summary>
	public partial class STATask
	{
		public static Task Run(Action action, ILogger? logger = null)
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
						tcs.SetException(ex);
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

		public static Task<T> Run<T>(Func<T> func, ILogger? logger = null)
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
						tcs.SetException(ex);
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

		public static Task Run(Func<Task> func, ILogger? logger = null)
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
						tcs.SetException(ex);
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

		public static Task<T?> Run<T>(Func<Task<T>> func, ILogger? logger = null)
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
						tcs.SetException(ex);
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
