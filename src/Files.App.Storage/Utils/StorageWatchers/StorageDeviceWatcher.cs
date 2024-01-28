// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;
using System.IO;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;

namespace Files.App.Storage
{
	public class StorageDeviceWatcher : IDeviceWatcher
	{
		private WMIWatcher? _insertWatcher;
		private WMIWatcher? _removeWatcher;
		private WMIWatcher? _modifyWatcher;
		private DeviceWatcher? _watcher;

		public event EventHandler<DeviceEventArgs>? ItemAdded;
		public event EventHandler<DeviceEventArgs>? ItemDeleted;
		public event EventHandler<DeviceEventArgs>? ItemChanged;
		public event EventHandler<DeviceEventArgs>? ItemInserted;
		public event EventHandler<DeviceEventArgs>? ItemEjected;
		public event EventHandler? EnumerationCompleted;

		public bool CanBeStarted
			=> _watcher?.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped or DeviceWatcherStatus.Aborted;

		public StorageDeviceWatcher()
		{
			StartWatcher();
		}

		/// <inheritdoc/>
		public void StartWatcher()
		{
			_watcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
			_watcher.Added += Watcher_Added;
			_watcher.Removed += Watcher_Removed;
			_watcher.EnumerationCompleted += Watcher_EnumerationCompleted;

			WMIQuery insertQuery = new("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
			_insertWatcher = new WMIWatcher(insertQuery);
			_insertWatcher.EventArrived += new WMIWatcher.WMIEventHandler(WMIWatcher_DeviceChanged);
			_insertWatcher.Start();

			WMIQuery modifyQuery = new("SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk' and TargetInstance.DriveType = 5");
			_modifyWatcher = new WMIWatcher(modifyQuery);
			_modifyWatcher.EventArrived += new WMIWatcher.WMIEventHandler(WMIWatcher_DeviceChanged);
			_modifyWatcher.Start();

			WMIQuery removeQuery = new("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
			_removeWatcher = new WMIWatcher(removeQuery);
			_removeWatcher.EventArrived += new WMIWatcher.WMIEventHandler(WMIWatcher_DeviceChanged);
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

		private void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
		{
			try
			{
				var root = StorageDevice.FromId(args.Id);

				// Check if this drive is associated with a drive letter
				var driveInfo = new DriveInfo(root.Path);

				ItemAdded?.Invoke(this, new(driveInfo.Name, args.Id));
			}
			catch (Exception)
			{
				return;
			}
		}

		private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			try
			{
				var root = StorageDevice.FromId(args.Id);

				// Check if this drive is associated with a drive letter
				var driveInfo = new DriveInfo(root.Path);

				ItemDeleted?.Invoke(this, new(driveInfo.Name, args.Id));
			}
			catch (Exception)
			{
				return;
			}
		}

		private void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
		{
			EnumerationCompleted?.Invoke(this, EventArgs.Empty);
		}

		private void WMIWatcher_DeviceChanged(object sender, CimEventArgs e)
		{
			CimInstance obj = (CimInstance)e.CimSubscriptionResult.Instance.CimInstanceProperties["TargetInstance"].Value;

			var deviceName = obj.CimInstanceProperties["Name"]?.Value as string ?? string.Empty;
			var deviceId = obj.CimInstanceProperties["DeviceID"]?.Value as string ?? string.Empty;
			var volumeName = obj.CimInstanceProperties["VolumeName"]?.Value as string ?? string.Empty;
			var eventType = volumeName is not null ? DeviceEvent.Inserted : DeviceEvent.Ejected;

			Debug.WriteLine($"Drive modify event: {deviceName}, {deviceId}, {eventType}");

			switch (eventType)
			{
				case DeviceEvent.Ejected:
					ItemEjected?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
					break;
				case DeviceEvent.Inserted:
					ItemInserted?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
					break;
				case DeviceEvent.Added:
					ItemAdded?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
					break;
				case DeviceEvent.Removed:
					ItemDeleted?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
					break;
			}
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
