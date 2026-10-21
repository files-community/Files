// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.System;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Server;

internal static class ServerHost
{
	private static readonly Lock gate = new();
	private readonly record struct ClientWindow(HWND Handle, uint ProcessId);

	private static readonly Dictionary<long, (Process Process, ClientWindow Window)> clients = [];
	private static long nextClient;
	private static bool hadClient;
	private static bool quitting;
	private static int pendingUpdates;
	private static SystemTrayIcon? tray;
	private static DispatcherQueueController controller = null!;
	private static BackgroundSettingsMonitor settings = null!;

	internal static void Initialize()
	{
		controller = DispatcherQueueController.CreateOnDedicatedThread();
		settings = new BackgroundSettingsMonitor();
		settings.Changed += OnSettingsChanged;
		OnSettingsChanged();
	}

	internal static long AddClient(int processId)
	{
		lock (gate)
		{
			if (Program.ExitSignal.Task.IsCompleted)
				throw new InvalidOperationException("The server is shutting down.");

			var id = ++nextClient;
			hadClient = true;
			Process? process = null;
			try
			{
				process = Process.GetProcessById(processId);
				process.Exited += (_, _) => controller.DispatcherQueue.TryEnqueue(() => RemoveClient(id));
				process.EnableRaisingEvents = true;
				clients.Add(id, (process, default));
				return id;
			}
			catch
			{
				process?.Dispose();
				controller.DispatcherQueue.TryEnqueue(ExitIfIdle);
				throw;
			}
		}
	}

	internal static void SetWindow(long id, long handle)
	{
		var window = new HWND((nint)handle);
		uint processId = 0;
		if (!window.IsNull)
			PInvoke.GetWindowThreadProcessId(window, out processId);
		var clientWindow = new ClientWindow(window, processId);
		bool closeWindow;
		lock (gate)
		{
			if (!clients.TryGetValue(id, out var client))
				return;
			if (!window.IsNull && processId != client.Process.Id)
				return;
			clients[id] = (client.Process, clientWindow);
			closeWindow = quitting;
		}
		if (closeWindow && IsClientWindow(clientWindow))
			PInvoke.PostMessage(window, PInvoke.WM_CLOSE, default, default);
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
		Process process;
		lock (gate)
		{
			if (!clients.Remove(id, out var client))
				return;
			process = client.Process;
		}
		process.Dispose();
		ServerEvents.RemoveClient(id);
		if (!Program.ExitSignal.Task.IsCompleted)
		{
			settings.Refresh();
			controller.DispatcherQueue.TryEnqueue(ExitIfIdle);
		}
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
			windows = clients.Values.Select(client => client.Window).ToArray();
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
			windows = clients.Values.Select(client => client.Window).ToArray();
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
