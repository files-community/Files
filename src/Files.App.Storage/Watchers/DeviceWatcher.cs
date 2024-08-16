// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;

namespace Files.App.Storage.Watchers
{
	/// <inheritdoc cref="IDeviceWatcher"/>
	public class DeviceWatcher : IDeviceWatcher
	{
		private Windows.Devices.Enumeration.DeviceWatcher? _winDeviceWatcher;

		private ManagementEventWatcher? _mmiInsertWatcher, _mmiRemoveWatcher, _mmiModifyWatcher;

		/// <inheritdoc/>
		public bool CanBeStarted
			=> _winDeviceWatcher?.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped or DeviceWatcherStatus.Aborted;

		/// <inheritdoc/>
		public event EventHandler<DeviceEventArgs>? DeviceAdded;

		/// <inheritdoc/>
		public event EventHandler<DeviceEventArgs>? DeviceChanged;

		/// <inheritdoc/>
		public event EventHandler<DeviceEventArgs>? DeviceDeleted;

		/// <inheritdoc/>
		public event EventHandler<DeviceEventArgs>? DeviceInserted;

		/// <inheritdoc/>
		public event EventHandler<DeviceEventArgs>? DeviceEjected;

		/// <inheritdoc/>
		public event EventHandler? EnumerationCompleted;

		/// <summary>
		/// Initializes an instance of <see cref="DeviceWatcher"/> class.
		/// </summary>
		public DeviceWatcher()
		{
			StartWatcher();
		}

		/// <inheritdoc/>
		public void StartWatcher()
		{
			_winDeviceWatcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
			_winDeviceWatcher.Added += Watcher_Added;
			_winDeviceWatcher.Removed += Watcher_Removed;
			_winDeviceWatcher.EnumerationCompleted += Watcher_EnumerationCompleted;

			var insertQuery = "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'";
			_mmiInsertWatcher = new(insertQuery);
			_mmiInsertWatcher.EventArrived += new EventArrivedEventHandler(Device_Inserted);
			_mmiInsertWatcher.Start();

			var modifyQuery = "SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk' and TargetInstance.DriveType = 5";
			_mmiModifyWatcher = new(modifyQuery);
			_mmiModifyWatcher.EventArrived += new EventArrivedEventHandler(Device_Modified);
			_mmiModifyWatcher.Start();

			var removeQuery = "SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'";
			_mmiRemoveWatcher = new(removeQuery);
			_mmiRemoveWatcher.EventArrived += new EventArrivedEventHandler(Device_Removed);
			_mmiRemoveWatcher.Start();
		}

		/// <inheritdoc/>
		public void StopWatcher()
		{
			if (_winDeviceWatcher?.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
				_winDeviceWatcher.Stop();

			if (_winDeviceWatcher is not null)
			{
				_winDeviceWatcher.Added -= Watcher_Added;
				_winDeviceWatcher.Removed -= Watcher_Removed;
				_winDeviceWatcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
			}

			_mmiInsertWatcher?.Dispose();
			_mmiRemoveWatcher?.Dispose();
			_mmiModifyWatcher?.Dispose();
			_mmiInsertWatcher = null;
			_mmiRemoveWatcher = null;
			_mmiModifyWatcher = null;
		}

		private void Watcher_Added(Windows.Devices.Enumeration.DeviceWatcher sender, DeviceInformation args)
		{
			DeviceAdded?.Invoke(this, new(args.Name, args.Id));
		}

		private void Watcher_Removed(Windows.Devices.Enumeration.DeviceWatcher sender, DeviceInformationUpdate args)
		{
			DeviceDeleted?.Invoke(this, new(string.Empty, args.Id));
		}

		private void Watcher_EnumerationCompleted(Windows.Devices.Enumeration.DeviceWatcher sender, object args)
		{
			EnumerationCompleted?.Invoke(this, EventArgs.Empty);
		}

		private void Device_Modified(object sender, EventArrivedEventArgs e)
		{
			CimInstance obj = (CimInstance)e.NewEvent.Instance.CimInstanceProperties["TargetInstance"].Value;
			var deviceName = obj.CimInstanceProperties["Name"]?.Value.ToString();
			var deviceId = obj.CimInstanceProperties["DeviceID"]?.Value.ToString();
			var volumeName = obj.CimInstanceProperties["VolumeName"]?.Value.ToString();
			var eventType = volumeName is not null ? DeviceEvent.Inserted : DeviceEvent.Ejected;

			Debug.WriteLine($"Drive modify event: {deviceName}, {deviceId}, {eventType}");

			if (eventType is DeviceEvent.Inserted)
				DeviceInserted?.Invoke(sender, new DeviceEventArgs(deviceName ?? string.Empty, deviceId ?? string.Empty));
			else
				DeviceEjected?.Invoke(sender, new DeviceEventArgs(deviceName ?? string.Empty, deviceId ?? string.Empty));
		}

		private void Device_Removed(object sender, EventArrivedEventArgs e)
		{
			CimInstance obj = (CimInstance)e.NewEvent.Instance.CimInstanceProperties["TargetInstance"].Value;
			var deviceName = (string)obj.CimInstanceProperties["Name"].Value;
			var deviceId = (string)obj.CimInstanceProperties["DeviceID"].Value;

			Debug.WriteLine($"Drive removed event: {deviceName}, {deviceId}");

			DeviceDeleted?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
		}

		private void Device_Inserted(object sender, EventArrivedEventArgs e)
		{
			CimInstance obj = (CimInstance)e.NewEvent.Instance.CimInstanceProperties["TargetInstance"].Value;
			var deviceName = (string)obj.CimInstanceProperties["Name"].Value;
			var deviceId = (string)obj.CimInstanceProperties["DeviceID"].Value;

			Debug.WriteLine($"Drive added event: {deviceName}, {deviceId}");

			DeviceAdded?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			StopWatcher();
		}
	}
}
