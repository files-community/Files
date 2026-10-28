// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.Application;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Files.App;

public partial class App
{
	private static readonly Lock serverSessionGate = new();
	private static readonly CancellationTokenSource serverSessionStopping = new();
	private static Task<Server.AppService>? serverSession;
	private static Server.AppService? activeServerSession;
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
					activeServerSession = session;
				}
				ApplyServerWindowHandle(session);
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
				lock (serverSessionGate)
				{
					if (ReferenceEquals(serverProcess, process))
						serverProcess = null;
					if (ReferenceEquals(activeServerSession, session))
						activeServerSession = null;
				}
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
			activeServerSession = null;
			ServerFactoryCache.Clear();
		}
		(sender as Process)?.Dispose();
		_ = SafetyExtensions.IgnoreExceptions(async () =>
		{
			await ServerSession.ConfigureAwait(false);
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
		lock (serverSessionGate)
			serverWindowHandle = handle;
		var session = await ServerSession;
		ApplyServerWindowHandle(session);
	}

	private static void ApplyServerWindowHandle(Server.AppService session)
	{
		while (true)
		{
			long handle;
			lock (serverSessionGate)
			{
				if (!ReferenceEquals(session, activeServerSession))
					return;
				handle = serverWindowHandle;
			}
			session.SetWindowHandle(handle);
			lock (serverSessionGate)
				if (handle == serverWindowHandle)
					return;
		}
	}

	private static void ClearServerWindowHandle()
	{
		Server.AppService? session;
		lock (serverSessionGate)
		{
			serverWindowHandle = 0;
			session = activeServerSession;
		}
		if (session is not null)
			SafetyExtensions.IgnoreExceptions(() => ApplyServerWindowHandle(session), Logger);
	}

	private static void StopServerSession()
	{
		Server.AppService? session;
		Process? process;
		lock (serverSessionGate)
		{
			serverSessionStopping.Cancel();
			session = activeServerSession;
			process = serverProcess;
			serverSession = null;
			serverProcess = null;
			activeServerSession = null;
		}
		process?.Dispose();
		if (session is not null)
			SafetyExtensions.IgnoreExceptions(session.Dispose, Logger);
	}
}
