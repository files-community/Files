// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Server;

internal sealed partial class DeviceRemovalMonitor : IDisposable
{
	private GCHandle context;
	private HCMNOTIFICATION notification;

	internal unsafe DeviceRemovalMonitor(string root, Action<bool> changed)
	{
		using var handle = PInvoke.CreateFile(root, 0,
			FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE | FILE_SHARE_MODE.FILE_SHARE_DELETE,
			null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS, null);
		if (handle.IsInvalid)
			throw new Win32Exception(Marshal.GetLastPInvokeError());

		CM_NOTIFY_FILTER filter = new()
		{
			cbSize = (uint)sizeof(CM_NOTIFY_FILTER),
			FilterType = CM_NOTIFY_FILTER_TYPE.CM_NOTIFY_FILTER_TYPE_DEVICEHANDLE,
		};
		filter.u.DeviceHandle.hTarget = new HANDLE(handle.DangerousGetHandle());
		context = GCHandle.Alloc(changed);
		HCMNOTIFICATION registration = default;
		var result = PInvoke.CM_Register_Notification(&filter, (void*)GCHandle.ToIntPtr(context), &OnNotification, &registration);
		GC.KeepAlive(handle);
		if (result != CONFIGRET.CR_SUCCESS)
		{
			context.Free();
			throw new InvalidOperationException($"Could not register device removal notifications: {result}.");
		}
		notification = registration;
		// The registration remains valid after its target handle is closed.
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	private static unsafe uint OnNotification(HCMNOTIFICATION notification, void* context, CM_NOTIFY_ACTION action, CM_NOTIFY_EVENT_DATA* data, uint size)
	{
		try
		{
			var changed = (Action<bool>)GCHandle.FromIntPtr((nint)context).Target!;
			if (action is CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEQUERYREMOVE or
				CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEREMOVEPENDING or CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEREMOVECOMPLETE)
				changed(true);
			else if (action == CM_NOTIFY_ACTION.CM_NOTIFY_ACTION_DEVICEQUERYREMOVEFAILED)
				changed(false);
		}
		catch (Exception ex)
		{
			Program.Logger.LogWarning(ex, "Could not update directory watches for device removal.");
		}
		return 0;
	}

	public void Dispose()
	{
		if (!context.IsAllocated)
			return;

		// Wait for callbacks before freeing their context; never unregister from a callback.
		var result = PInvoke.CM_Unregister_Notification(notification);
		if (result != CONFIGRET.CR_SUCCESS)
		{
			Program.Logger.LogWarning("Could not unregister device removal notifications: {Result}.", result);
			return;
		}
		context.Free();
	}
}
