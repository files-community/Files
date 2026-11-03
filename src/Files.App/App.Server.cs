// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.Application;
using System.Runtime.InteropServices;

namespace Files.App;

public partial class App
{
	private static readonly Lock serverSessionGate = new();
	private static readonly CancellationTokenSource serverSessionStopping = new();
	private static Task<Server.AppService>? serverSession;
	private static Process? serverProcess;
	private static long serverWindowHandle;
	private static bool receivedStartupSession;

	internal static event EventHandler<string>? ServerChanged;
	internal static bool CanRestorePreviousSession { get; private set; }
	internal static Task<Server.AppService> ServerSession
	{
		get
		{
			lock (serverSessionGate)
			{
				if (serverSessionStopping.IsCancellationRequested)
					return Task.FromCanceled<Server.AppService>(serverSessionStopping.Token);
				if (serverSession is null || serverSession.IsFaulted || serverSession.IsCanceled)
					serverSession = Task.Run(ConnectServerAsync);
				return serverSession;
			}
		}
	}

	private static async Task<Server.AppService> ConnectServerAsync()
	{
		var deadline = Environment.TickCount64 + 30_000;
		while (true)
		{
			serverSessionStopping.Token.ThrowIfCancellationRequested();
			Server.AppService? session = null;
			Process? process = null;
			try
			{
				session = new Server.AppService(Environment.ProcessId);
				var canRestore = session.CanRestorePreviousSession;
				try
				{
					process = Process.GetProcessById(session.ProcessId);
				}
				catch (ArgumentException)
				{
					throw new COMException("The server exited during registration.", unchecked((int)0x80010108));
				}
				process.Exited += ServerProcessExited;
				process.EnableRaisingEvents = true;
				session.Changed += OnServerChanged;
				lock (serverSessionGate)
				{
					serverSessionStopping.Token.ThrowIfCancellationRequested();
					if (process.HasExited)
						throw new COMException("The server exited during registration.", unchecked((int)0x80010108));
					if (!receivedStartupSession)
					{
						CanRestorePreviousSession = canRestore;
						receivedStartupSession = true;
					}
					serverProcess = process;
				}
				return session;
			}
			catch (Exception ex)
			{
				process?.Dispose();
				if (session is not null)
					SafetyExtensions.IgnoreExceptions(session.Dispose, Logger);
				if (!IsServerDisconnected(ex) || Environment.TickCount64 >= deadline)
					throw;
				ServerFactoryCache.Clear();
				await Task.Delay(100, serverSessionStopping.Token).ConfigureAwait(false);
			}
		}
	}

	private static bool IsServerDisconnected(Exception ex) => ex.HResult is
		unchecked((int)0x800706BA) or unchecked((int)0x800706BE) or unchecked((int)0x800706BF) or
		unchecked((int)0x80010108) or unchecked((int)0x80010007) or unchecked((int)0x80010012) or unchecked((int)0x800401FD);

	private static void OnServerChanged(object? sender, string name) => UiDispatcher?.TryEnqueue(() =>
	{
		if (ServerChanged is not { } handlers)
			return;
		foreach (EventHandler<string> handler in handlers.GetInvocationList())
			SafetyExtensions.IgnoreExceptions(() => handler(null, name), Logger);
	});

	private static void ServerProcessExited(object? sender, EventArgs e)
	{
		lock (serverSessionGate)
		{
			if (!ReferenceEquals(sender, serverProcess) || serverSessionStopping.IsCancellationRequested)
				return;
			serverProcess = null;
			serverSession = null;
			ServerFactoryCache.Clear();
		}
		(sender as Process)?.Dispose();
		_ = SafetyExtensions.IgnoreExceptions(async () =>
		{
			var session = await ServerSession.ConfigureAwait(false);
			serverSessionStopping.Token.ThrowIfCancellationRequested();
			ApplyServerWindowHandle(session);
			_ = SafetyExtensions.IgnoreExceptions(async () => await Server.AppService.InitializeAsync(), Logger);
			foreach (var name in new[]
			{
				ServerChangeNames.Settings("user_settings.json"), ServerChangeNames.Settings("filetags.json"),
				ServerChangeNames.PinnedFolders, ServerChangeNames.RecentItems, ServerChangeNames.Libraries,
				ServerChangeNames.Network, ServerChangeNames.RecycleBin, ServerChangeNames.Drives, ServerChangeNames.Updates,
			})
				OnServerChanged(null, name);
		}, Logger);
	}

	private static async Task SetServerWindowHandleAsync(long handle)
	{
		Volatile.Write(ref serverWindowHandle, handle);
		var session = await ServerSession;
		ApplyServerWindowHandle(session);
	}

	private static void ApplyServerWindowHandle(Server.AppService session)
	{
		long handle;
		do
		{
			handle = Volatile.Read(ref serverWindowHandle);
			session.SetWindowHandle(handle);
		}
		while (handle != Volatile.Read(ref serverWindowHandle));
	}

	private static void ClearServerWindowHandle()
	{
		Volatile.Write(ref serverWindowHandle, 0);
		Task<Server.AppService>? session;
		lock (serverSessionGate)
			session = serverSession;
		if (session?.IsCompletedSuccessfully == true)
			SafetyExtensions.IgnoreExceptions(() => ApplyServerWindowHandle(session.Result), Logger);
	}

	private static void StopServerSession()
	{
		Task<Server.AppService>? session;
		Process? process;
		lock (serverSessionGate)
		{
			serverSessionStopping.Cancel();
			session = serverSession;
			process = serverProcess;
			serverSession = null;
			serverProcess = null;
		}
		process?.Dispose();
		if (session is not null)
			_ = SafetyExtensions.IgnoreExceptions(async () => (await session.ConfigureAwait(false)).Dispose(), Logger);
	}
}
