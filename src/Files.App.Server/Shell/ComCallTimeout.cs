// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;

namespace Files.App.Server;

/// <summary>
/// Performs a COM call with a timeout. If the call does not complete within the specified timeout, it is canceled.
/// </summary>
internal sealed unsafe partial class ComCallTimeout : IDisposable
{
	private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

	private readonly uint threadId = PInvoke.GetCurrentThreadId();
	private readonly Lock gate = new();
	private readonly Timer timer;
	private bool disposed;

	// Must be called on the thread making the COM calls
	internal ComCallTimeout(TimeSpan timeout)
	{
		_ = PInvoke.CoEnableCallCancellation(null);
		timer = new(_ => Cancel(), null, timeout, Interval);
	}

	private void Cancel()
	{
		lock (gate)
		{
			if (!disposed)
				_ = PInvoke.CoCancelCall(threadId, 0);
		}
	}

	public void Dispose()
	{
		lock (gate)
			disposed = true;

		timer.Dispose();
		_ = PInvoke.CoDisableCallCancellation(null);
	}
}
