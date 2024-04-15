﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;

namespace Files.App.Server;

class Program
{
	internal static readonly AsyncManualResetEvent ExitSignalEvent = new();
	private static readonly CancellationTokenSource cancellationTokenSource = new();
	private static readonly StreamWriter logWriter = new(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug_server.log"), append: true) { AutoFlush = true };

	static async Task Main()
	{
		AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

		nint cookie = 0;

		unsafe
		{
			_ = PInvoke.RoInitialize(RO_INIT_TYPE.RO_INIT_MULTITHREADED);

			var classIds = typeof(Program).Assembly.GetTypes()
				.Where(t => t.IsSealed && t.IsPublic && t.IsClass)
				.Select(t => t.FullName!)
				.Where(name => name.StartsWith("Files.App.Server.", StringComparison.Ordinal))
				.Select(name =>
				{
					if (PInvoke.WindowsCreateString(name, (uint)name.Length, out var classId) is HRESULT hr && hr.Value is not 0)
					{
						Marshal.ThrowExceptionForHR(hr);
					}

					return new HSTRING(classId.DangerousGetHandle());
				})
				.ToArray();

			var callbacks = Enumerable.Repeat((nint)(delegate* unmanaged[Stdcall]<void*, void**, int>)&Helpers.GetActivationFactory, classIds.Length).ToArray();

			if (PInvoke.RoRegisterActivationFactories(classIds, callbacks, out cookie) is HRESULT hr && hr.Value != 0)
			{
				Marshal.ThrowExceptionForHR(hr);
			}

			foreach (var str in classIds)
			{
				_ = PInvoke.WindowsDeleteString(str);
			}
		}

		AppDomain.CurrentDomain.ProcessExit += (_, _) => cancellationTokenSource.Cancel();

		try
		{
			ExitSignalEvent.Reset();
			await ExitSignalEvent.WaitAsync(cancellationTokenSource.Token);
		}
		catch (OperationCanceledException)
		{
			return;
		}
		finally
		{
			if (cookie != 0)
			{
				PInvoke.RoRevokeActivationFactories(cookie);
			}

			PInvoke.RoUninitialize();
		}
	}

	private static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
	{
		logWriter.WriteLine($"{DateTime.Now}|{e.Exception}");
	}
}
