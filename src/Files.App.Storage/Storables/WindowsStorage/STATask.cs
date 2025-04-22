// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;

namespace Files.App.Storage
{
	/// <summary>
	/// Represents an asynchronous operation on STA.
	/// </summary>
	public partial class STATask
	{
		public static Task Run(Action action)
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
						tcs.SetException(ex);
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				})
				{
					IsBackground = true,
					Priority = ThreadPriority.Normal
				};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		public static Task<T> Run<T>(Func<T> func)
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
						tcs.SetException(ex);
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				})
				{
					IsBackground = true,
					Priority = ThreadPriority.Normal
				};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		public static Task Run(Func<Task> func)
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
						tcs.SetException(ex);
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				})
				{
					IsBackground = true,
					Priority = ThreadPriority.Normal
				};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}

		public static Task<T?> Run<T>(Func<Task<T>> func)
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
						tcs.SetException(ex);
					}
					finally
					{
						PInvoke.OleUninitialize();
					}
				})
				{
					IsBackground = true,
					Priority = ThreadPriority.Normal
				};

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			return tcs.Task;
		}
	}
}
