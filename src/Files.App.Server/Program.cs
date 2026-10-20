// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;

namespace Files.App.Server;

class Program
{
	internal static readonly TaskCompletionSource<bool> ExitSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static readonly CancellationTokenSource cancellationTokenSource = new();
	private static readonly FileLoggerProvider logProvider = new(Path.Combine(ApplicationData.Current.LocalFolder.Path, $"debug_server_{(Environment.IsPrivilegedProcess ? "privileged" : "unprivileged")}.log"));
	internal static readonly ILogger Logger = logProvider.CreateLogger(nameof(Program));

	static Type[] GetActivatableTypes() => [typeof(AppService)];

	static async Task Main()
	{
		AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

		RO_REGISTRATION_COOKIE cookie = default;

		_ = PInvoke.RoInitialize(RO_INIT_TYPE.RO_INIT_MULTITHREADED);
		ServerHost.Initialize();

		var types = GetActivatableTypes();
		var classIds = new HSTRING[types.Length];
		var classIdHandles = new WindowsDeleteStringSafeHandle[types.Length];
		try
		{
			for (int i = 0; i < types.Length; i++)
			{
				var name = types[i].FullName!;
				var hr = PInvoke.WindowsCreateString(name, (uint)name.Length, out var classId);
				classIdHandles[i] = classId;
				if (hr.Value != 0)
				{
					Marshal.ThrowExceptionForHR(hr);
				}

				classIds[i] = new HSTRING(classId.DangerousGetHandle());
			}

			unsafe
			{
				delegate* unmanaged[Stdcall]<HSTRING, IActivationFactory_unmanaged**, HRESULT>[] callbacks = new delegate* unmanaged[Stdcall]<HSTRING, IActivationFactory_unmanaged**, HRESULT>[classIds.Length];
				for (int i = 0; i < callbacks.Length; i++)
				{
					callbacks[i] = &Helpers.GetActivationFactory;
				}

				fixed (delegate* unmanaged[Stdcall]<HSTRING, IActivationFactory_unmanaged**, HRESULT>* pCallbacks = callbacks)
				{
					if (PInvoke.RoRegisterActivationFactories(classIds, pCallbacks, out cookie) is HRESULT hr && hr.Value != 0)
					{
						Marshal.ThrowExceptionForHR(hr);
					}
				}
			}
		}
		finally
		{
			foreach (var classId in classIdHandles)
			{
				classId?.Dispose();
			}
		}

		AppDomain.CurrentDomain.ProcessExit += (_, _) => cancellationTokenSource.Cancel();

		try
		{
			await ExitSignal.Task.WaitAsync(cancellationTokenSource.Token);
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
			await ServerHost.ShutdownAsync();
			logProvider.Dispose();
		}
	}

	private static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
	{
		Logger?.LogDebug(e.Exception, "Server exception");
	}
}
