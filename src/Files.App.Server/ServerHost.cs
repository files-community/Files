// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.System;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Server;

internal static class ServerHost
{
	private static readonly Lock gate = new();
	private readonly record struct ClientWindow(HWND Handle, uint ProcessId);

	private static readonly Dictionary<long, ClientWindow> clients = [];
	private static long nextClient;
	private static bool hadClient;
	private static bool quitting;
	private static int pendingUpdates;
	private static SystemTrayIcon? tray;
	private static DispatcherQueueController controller = null!;
	private static ServerSettings settings = null!;

	internal static void Initialize()
	{
		controller = DispatcherQueueController.CreateOnDedicatedThread();
		settings = new ServerSettings();
		settings.Changed += OnSettingsChanged;
		OnSettingsChanged();
	}

	internal static long AddClient()
	{
		lock (gate)
		{
			var id = ++nextClient;
			hadClient = true;
			clients.Add(id, default);
			return id;
		}
	}

	internal static void SetWindow(long id, long handle)
	{
		var window = new HWND((nint)handle);
		uint processId = 0;
		if (!window.IsNull)
			PInvoke.GetWindowThreadProcessId(window, out processId);
		lock (gate)
		{
			if (clients.ContainsKey(id))
				clients[id] = new(window, processId);
		}
	}

	internal static void BeginUpdate()
	{
		lock (gate)
			pendingUpdates++;
	}

	internal static void EndUpdate()
	{
		lock (gate)
			pendingUpdates = Math.Max(0, pendingUpdates - 1);
		controller.DispatcherQueue.TryEnqueue(ExitIfIdle);
	}

	internal static void RemoveClient(long id)
	{
		lock (gate)
			clients.Remove(id);
		if (!Program.ExitSignal.Task.IsCompleted && settings.Refresh())
			controller.DispatcherQueue.TryEnqueue(ExitIfIdle);
	}

	private static void ExitIfIdle()
	{
		lock (gate)
		{
			if (clients.Count == 0 && (quitting || (hadClient && (!settings.KeepRunning || pendingUpdates > 0))))
				Program.ExitSignal.TrySetResult(true);
		}
	}

	private static void OnSettingsChanged()
	{
		if (!Program.ExitSignal.Task.IsCompleted)
		{
			controller.DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					if (settings.ShowTrayIcon && !quitting)
						(tray ??= new SystemTrayIcon()).Show();
					else
						tray?.Hide();
				}
				catch (Exception ex)
				{
					Program.Logger.LogWarning(ex, "Could not update the server tray icon.");
				}
				ExitIfIdle();
			});
		}
	}

	internal static bool ActivateWindow()
	{
		ClientWindow[] windows;
		lock (gate)
			windows = clients.Values.ToArray();
		foreach (var window in windows)
		{
			if (!IsClientWindow(window))
				continue;
			if (PInvoke.IsIconic(window.Handle))
				PInvoke.ShowWindow(window.Handle, SHOW_WINDOW_CMD.SW_RESTORE);
			PInvoke.SetForegroundWindow(window.Handle);
			return true;
		}
		return false;
	}

	private static bool IsClientWindow(ClientWindow window)
		=> !window.Handle.IsNull && window.ProcessId != 0 &&
			PInvoke.GetWindowThreadProcessId(window.Handle, out var processId) != 0 && processId == window.ProcessId;

	internal static void RequestQuit()
	{
		ClientWindow[] windows;
		lock (gate)
		{
			quitting = true;
			windows = clients.Values.ToArray();
		}
		tray?.Hide();
		foreach (var window in windows)
		{
			if (IsClientWindow(window))
				PInvoke.PostMessage(window.Handle, PInvoke.WM_CLOSE, default, default);
		}
		ExitIfIdle();
	}

	private static Task InvokeAsync(Action action)
	{
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!controller.DispatcherQueue.TryEnqueue(() =>
		{
			try
			{
				action();
				completion.SetResult();
			}
			catch (Exception ex)
			{
				completion.SetException(ex);
			}
		}))
			completion.SetException(new InvalidOperationException("The server dispatcher is shutting down."));
		return completion.Task;
	}

	internal static async Task ShutdownAsync()
	{
		settings.Dispose();
		await InvokeAsync(() =>
		{
			tray?.Dispose();
			tray = null;
		});
		await controller.ShutdownQueueAsync();
	}
}
