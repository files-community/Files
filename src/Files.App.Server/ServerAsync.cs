// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

namespace Files.App.Server;

internal static class ServerAsync
{
	internal static IAsyncAction Run(Func<CancellationToken, Task> taskProvider)
		=> new ServerAsyncAction(AsyncInfo.Run(taskProvider));

	internal static IAsyncOperation<T> Run<T>(Func<CancellationToken, Task<T>> taskProvider)
		=> new ServerAsyncOperation<T>(AsyncInfo.Run(taskProvider));

	// A client can exit while its operation is pending. Only discard failures to deliver its callback.
	internal static bool IsDisconnected(Exception exception) => exception.HResult is
		unchecked((int)0x800706BA) or // RPC_S_SERVER_UNAVAILABLE
		unchecked((int)0x800706BE) or // RPC_S_CALL_FAILED
		unchecked((int)0x800706BF) or // RPC_S_CALL_FAILED_DNE
		unchecked((int)0x80010108) or // RPC_E_DISCONNECTED
		unchecked((int)0x80010007) or // RPC_E_SERVER_DIED
		unchecked((int)0x80010012) or // RPC_E_SERVER_DIED_DNE
		unchecked((int)0x800401FD);   // CO_E_OBJNOTCONNECTED
}

internal sealed partial class ServerAsyncAction(IAsyncAction operation) : IAsyncAction
{
	public uint Id => operation.Id;
	public AsyncStatus Status => operation.Status;
	public Exception ErrorCode => operation.ErrorCode;
	public void Cancel() => operation.Cancel();
	public void Close() => operation.Close();
	public void GetResults() => operation.GetResults();

	public AsyncActionCompletedHandler Completed
	{
		get => (operation.Completed?.Target as Completion)?.Handler!;
		set => operation.Completed = value is null ? null! : new Completion(this, value).Invoke;
	}

	private sealed class Completion(ServerAsyncAction owner, AsyncActionCompletedHandler handler)
	{
		internal AsyncActionCompletedHandler Handler { get; } = handler;

		internal void Invoke(IAsyncAction sender, AsyncStatus status)
		{
			try
			{
				Handler(owner, status);
			}
			catch (Exception ex) when (ServerAsync.IsDisconnected(ex))
			{
			}
		}
	}
}

internal sealed partial class ServerAsyncOperation<T>(IAsyncOperation<T> operation) : IAsyncOperation<T>
{
	public uint Id => operation.Id;
	public AsyncStatus Status => operation.Status;
	public Exception ErrorCode => operation.ErrorCode;
	public void Cancel() => operation.Cancel();
	public void Close() => operation.Close();
	public T GetResults() => operation.GetResults();

	public AsyncOperationCompletedHandler<T> Completed
	{
		get => (operation.Completed?.Target as Completion)?.Handler!;
		set => operation.Completed = value is null ? null! : new Completion(this, value).Invoke;
	}

	private sealed class Completion(ServerAsyncOperation<T> owner, AsyncOperationCompletedHandler<T> handler)
	{
		internal AsyncOperationCompletedHandler<T> Handler { get; } = handler;

		internal void Invoke(IAsyncOperation<T> sender, AsyncStatus status)
		{
			try
			{
				Handler(owner, status);
			}
			catch (Exception ex) when (ServerAsync.IsDisconnected(ex))
			{
			}
		}
	}
}
