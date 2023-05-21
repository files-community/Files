﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.MMI;
using Files.Backend.Models;
using Files.Sdk.Storage.LocatableStorage;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;

namespace Files.App.Filesystem
{
	/// <summary>
	/// Provides watcher for Windows Storage Device.
	/// </summary>
	public class WindowsStorageDeviceWatcher : IStorageDeviceWatcher
	{
		private readonly DeviceWatcher _watcher;

		public bool CanBeStarted
			=> _watcher.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped or DeviceWatcherStatus.Aborted;

		public event EventHandler<ILocatableFolder> DeviceAdded;

		public event EventHandler<string> DeviceRemoved;

		public event EventHandler EnumerationCompleted;

		public event EventHandler<string> DeviceModified;

		public WindowsStorageDeviceWatcher()
		{
			// Initialize
			_watcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());

			// Start interaction
			_watcher.Added += Watcher_Added;
			_watcher.Removed += Watcher_Removed;
			_watcher.EnumerationCompleted += Watcher_EnumerationCompleted;

			SetupWin32Watcher();
		}

		private void SetupWin32Watcher()
		{
			DeviceManager.Default.DeviceAdded += Win32_OnDeviceAdded;
			DeviceManager.Default.DeviceRemoved += Win32_OnDeviceRemoved;
			DeviceManager.Default.DeviceInserted += Win32_OnDeviceEjectedOrInserted;
			DeviceManager.Default.DeviceEjected += Win32_OnDeviceEjectedOrInserted;
		}

		private void Win32_OnDeviceEjectedOrInserted(object? sender, DeviceEventArgs e)
		{
			DeviceModified?.Invoke(this, e.DeviceId);
		}

		private void Win32_OnDeviceRemoved(object? sender, DeviceEventArgs e)
		{
			DeviceRemoved?.Invoke(this, e.DeviceId);
		}

		private async void Win32_OnDeviceAdded(object? sender, DeviceEventArgs e)
		{
			var driveAdded = new DriveInfo(e.DeviceId);

			var rootAdded = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(e.DeviceId).AsTask());
			if (!rootAdded)
			{
				App.Logger.LogWarning(
					$"{rootAdded.ErrorCode}: Attempting to add the device, {e.DeviceId}," +
					" failed at the StorageFolder initialization step. This device will be ignored.");

				return;
			}
			
			var type = DriveHelpers.GetDriveType(driveAdded);
			DriveItem driveItem = await DriveItem.CreateFromPropertiesAsync(rootAdded, e.DeviceId, type);

			DeviceAdded?.Invoke(this, driveItem);
		}

		private void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
		{
			EnumerationCompleted?.Invoke(this, EventArgs.Empty);
		}

		private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			DeviceRemoved?.Invoke(this, args.Id);
		}

		private async void Watcher_Added(DeviceWatcher sender, DeviceInformation args)
		{
			string deviceId = args.Id;
			StorageFolder root;

			try
			{
				root = StorageDevice.FromId(deviceId);
			}
			catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException)
			{
				App.Logger.LogWarning(
					$"{ex.GetType()}: Attempting to add the device, {args.Name}," +
					$" failed at the StorageFolder initialization step. This device will be ignored. Device ID: {deviceId}");

				return;
			}

			Data.Items.DriveType type;
			try
			{
				// Check if this drive is associated with a drive letter
				var driveAdded = new DriveInfo(root.Path);

				type = DriveHelpers.GetDriveType(driveAdded);
			}
			catch (ArgumentException)
			{
				type = Data.Items.DriveType.Removable;
			}

			var driveItem = await DriveItem.CreateFromPropertiesAsync(root, deviceId, type);

			DeviceAdded?.Invoke(this, driveItem);
		}

		public void Start()
		{
			_watcher.Start();
		}

		public void Stop()
		{
			if (_watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
				_watcher.Stop();

			// Stop interaction
			_watcher.Added -= Watcher_Added;
			_watcher.Removed -= Watcher_Removed;
			_watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
			DeviceManager.Default.DeviceAdded -= Win32_OnDeviceAdded;
			DeviceManager.Default.DeviceRemoved -= Win32_OnDeviceRemoved;
			DeviceManager.Default.DeviceInserted -= Win32_OnDeviceEjectedOrInserted;
			DeviceManager.Default.DeviceEjected -= Win32_OnDeviceEjectedOrInserted;
		}
	}
}
