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

		public unsafe static Task RunAsSync(Action action)
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			HANDLE hEventHandle = PInvoke.CreateEvent((SECURITY_ATTRIBUTES*)null, true, false, default);

			var tcs = new TaskCompletionSource();

			Task.Run(() =>
			{
				try
				{
					action();
					tcs.SetResult();
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
				}
				finally
				{
					PInvoke.SetEvent(hEventHandle);
				}
			});

			HANDLE* pEventHandles = stackalloc HANDLE[1];
			pEventHandles[0] = hEventHandle;
			uint dwIndex = 0u;

			PInvoke.CoWaitForMultipleObjects(
				(uint)CWMO_FLAGS.CWMO_DEFAULT,
				PInvoke.INFINITE,
				1u,
				pEventHandles,
				&dwIndex);

			PInvoke.CloseHandle(hEventHandle);

			return tcs.Task;
		}
	}
}
