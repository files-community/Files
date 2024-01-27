// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;
using System.Collections.Specialized;

namespace Files.App.Storage.Watchers
{
	public class DeviceWatcher : IWatcher, IDeviceWatcher
	{
		private WMIWatcher? _insertWatcher;
		private WMIWatcher? _removeWatcher;
		private WMIWatcher? _modifyWatcher;

		public event EventHandler<DeviceEventArgs>? ItemAdded;
		public event EventHandler<DeviceEventArgs>? ItemDeleted;
		public event EventHandler<DeviceEventArgs>? ItemChanged;
		public event EventHandler<DeviceEventArgs>? ItemRenamed;
		public event EventHandler<DeviceEventArgs>? ItemInserted;
		public event EventHandler<DeviceEventArgs>? ItemEjected;
		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		public DeviceWatcher()
		{
			StartWatcher();
		}

		/// <inheritdoc/>
		public void StartWatcher()
		{
			WMIQuery insertQuery = new("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
			_insertWatcher = new WMIWatcher(insertQuery);
			_insertWatcher.EventArrived += new WMIWatcher.WMIEventHandler(WMI_DeviceInserted);
			_insertWatcher.Start();

			WMIQuery modifyQuery = new("SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk' and TargetInstance.DriveType = 5");
			_modifyWatcher = new WMIWatcher(modifyQuery);
			_modifyWatcher.EventArrived += new WMIWatcher.WMIEventHandler(WMI_DeviceModified);
			_modifyWatcher.Start();

			WMIQuery removeQuery = new("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
			_removeWatcher = new WMIWatcher(removeQuery);
			_removeWatcher.EventArrived += new WMIWatcher.WMIEventHandler(WMI_DeviceRemoved);
			_removeWatcher.Start();
		}

		/// <inheritdoc/>
		public void StopsWatcher()
		{
			_insertWatcher?.Dispose();
			_removeWatcher?.Dispose();
			_modifyWatcher?.Dispose();
			_insertWatcher = null;
			_removeWatcher = null;
			_modifyWatcher = null;
		}

		private void WMI_DeviceModified(object sender, WMIEventArgs e)
		{
			CimInstance obj = (CimInstance)e.CimSubscriptionResult.Instance.CimInstanceProperties["TargetInstance"].Value;

			var deviceName = obj.CimInstanceProperties["Name"]?.Value as string ?? string.Empty;
			var deviceId = obj.CimInstanceProperties["DeviceID"]?.Value as string ?? string.Empty;
			var volumeName = obj.CimInstanceProperties["VolumeName"]?.Value as string ?? string.Empty;
			var eventType = volumeName is not null ? DeviceEvent.Inserted : DeviceEvent.Ejected;

			Debug.WriteLine($"Drive modify event: {deviceName}, {deviceId}, {eventType}");

			if (eventType == DeviceEvent.Inserted)
				ItemInserted?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
			else
				ItemEjected?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
		}

		private void WMI_DeviceRemoved(object sender, WMIEventArgs e)
		{
			CimInstance obj = (CimInstance)e.CimSubscriptionResult.Instance.CimInstanceProperties["TargetInstance"].Value;

			var deviceName = (string)obj.CimInstanceProperties["Name"].Value;
			var deviceId = (string)obj.CimInstanceProperties["DeviceID"].Value;

			Debug.WriteLine($"Drive removed event: {deviceName}, {deviceId}");

			ItemDeleted?.Invoke(sender, new(deviceName, deviceId));
		}

		private void WMI_DeviceInserted(object sender, WMIEventArgs e)
		{
			CimInstance obj = (CimInstance)e.CimSubscriptionResult.Instance.CimInstanceProperties["TargetInstance"].Value;

			var deviceName = (string)obj.CimInstanceProperties["Name"].Value;
			var deviceId = (string)obj.CimInstanceProperties["DeviceID"].Value;

			Debug.WriteLine($"Drive added event: {deviceName}, {deviceId}");

			ItemAdded?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
		}

		/// <inheritdoc/>
		public ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			StopsWatcher();
		}
	}
}
