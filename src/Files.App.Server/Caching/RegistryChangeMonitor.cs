// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Security;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace Files.App.Server;

internal sealed partial class RegistryChangeMonitor(RegistryKey root, string path) : IDisposable
{
	private readonly AutoResetEvent changed = new(false);
	private RegistryKey? key;
	private bool watching;

	// Called under the cache lock. The notification stays armed while a snapshot is built.
	internal bool HasChanged()
	{
		if (watching && !changed.WaitOne(0))
			return false;

		var wasWatching = watching;
		key?.Dispose();
		key = null;
		changed.Reset();
		watching = false;
		try
		{
			var ancestor = path;
			while ((key = root.OpenSubKey(ancestor)) is null && ancestor.Length > 0)
			{
				var separator = ancestor.LastIndexOf('\\');
				ancestor = separator < 0 ? string.Empty : ancestor[..separator];
			}

			if (key is not null)
			{
				watching = PInvoke.RegNotifyChangeKeyValue(
					new HKEY(key.Handle.DangerousGetHandle()), string.Equals(ancestor, path, StringComparison.OrdinalIgnoreCase),
					REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME | REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET | REG_NOTIFY_FILTER.REG_NOTIFY_THREAD_AGNOSTIC,
					new HANDLE(changed.SafeWaitHandle.DangerousGetHandle()), true) == WIN32_ERROR.ERROR_SUCCESS;
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
		{
			// Cache expiry remains a fallback if notifications are unavailable.
		}
		return wasWatching || watching;
	}

	public void Dispose()
	{
		key?.Dispose();
		changed.Dispose();
	}
}
