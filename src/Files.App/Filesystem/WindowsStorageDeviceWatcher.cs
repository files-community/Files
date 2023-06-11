﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.MMI;
using Files.App.Storage.NativeStorage;
using Files.Backend.Models;
using Files.Sdk.Storage;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;

namespace Files.App.Filesystem
{
	public class WindowsStorageDeviceWatcher : IStorageDeviceWatcher
	{
		public event EventHandler<FileSystemEventArgs> ItemAdded;
		public event EventHandler<FileSystemEventArgs> ItemRemoved;
		public event EventHandler EnumerationCompleted;
		public event EventHandler<FileSystemEventArgs> ItemModified;

		private DeviceWatcher watcher;

		public bool CanBeStarted => watcher.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped or DeviceWatcherStatus.Aborted;

		public WindowsStorageDeviceWatcher()
		{
			watcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
			watcher.Added += Watcher_Added;
			watcher.Removed += Watcher_Removed;
			watcher.EnumerationCompleted += Watcher_EnumerationCompleted;

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
			ItemModified?.Invoke(this, new(WatcherChangeTypes.Changed, e.DeviceId, e.DeviceName));
		}

		private void Win32_OnDeviceRemoved(object? sender, DeviceEventArgs e)
		{
			var item = new NativeFolder(e.DeviceId);
			ItemRemoved?.Invoke(this, new(WatcherChangeTypes.Deleted, e.DeviceId, e.DeviceName));
		}

		private async void Win32_OnDeviceAdded(object? sender, DeviceEventArgs e)
		{
			var driveAdded = new DriveInfo(e.DeviceId);
			var rootAdded = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(e.DeviceId).AsTask());
			if (!rootAdded)
			{
				App.Logger.LogWarning($"{rootAdded.ErrorCode}: Attempting to add the device, {e.DeviceId},"
					+ " failed at the StorageFolder initialization step. This device will be ignored.");
				return;
			}
			
			var type = DriveHelpers.GetDriveType(driveAdded);
			DriveItem driveItem = await DriveItem.CreateFromPropertiesAsync(rootAdded, e.DeviceId, type);

			ItemAdded?.Invoke(this, new(WatcherChangeTypes.Created, driveItem.Path, driveItem.Name));
		}

		private void Watcher_EnumerationCompleted(DeviceWatcher sender, object args)
		{
			EnumerationCompleted?.Invoke(this, EventArgs.Empty);
		}

		private async void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			var item = new NativeFolder(args.Id);
			ItemRemoved?.Invoke(this, new(WatcherChangeTypes.Deleted, item.Path, item.Name));
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
				App.Logger.LogWarning($"{ex.GetType()}: Attempting to add the device, {args.Name},"
					+ $" failed at the StorageFolder initialization step. This device will be ignored. Device ID: {deviceId}");
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

			ItemAdded?.Invoke(this, new(WatcherChangeTypes.Created, driveItem.Path, driveItem.Name));
		}

		public void Start()
		{
			watcher.Start();
		}

		public void Stop()
		{
			if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
			{
				watcher.Stop();
			}

			watcher.Added -= Watcher_Added;
			watcher.Removed -= Watcher_Removed;
			watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;

			DeviceManager.Default.DeviceAdded -= Win32_OnDeviceAdded;
			DeviceManager.Default.DeviceRemoved -= Win32_OnDeviceRemoved;
			DeviceManager.Default.DeviceInserted -= Win32_OnDeviceEjectedOrInserted;
			DeviceManager.Default.DeviceEjected -= Win32_OnDeviceEjectedOrInserted;
		}
	}
}
