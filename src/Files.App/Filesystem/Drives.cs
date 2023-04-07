using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Helpers.MMI;
using Files.Backend.Services.SizeProvider;
using Files.Shared;
using Files.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.Storage.FileProperties;
using DriveType = Files.App.DataModels.NavigationControlItems.DriveType;
using IO = System.IO;

namespace Files.App.Filesystem
{
	public class DrivesManager : ObservableObject
	{
		private readonly ILogger logger = Ioc.Default.GetRequiredService<ILogger<App>>();
		private readonly ISizeProvider folderSizeProvider = Ioc.Default.GetService<ISizeProvider>();

		private bool isDriveEnumInProgress;
		private DeviceWatcher watcher;

		public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private readonly List<DriveItem> drives = new();
		public IReadOnlyList<DriveItem> Drives
		{
			get
			{
				lock (drives)
				{
					return drives.ToList().AsReadOnly();
				}
			}
		}

		private bool showUserConsentOnInit = false;
		public bool ShowUserConsentOnInit
		{
			get => showUserConsentOnInit;
			set => SetProperty(ref showUserConsentOnInit, value);
		}

		public DrivesManager() => SetupWatcher();

		public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
		{
			if (!Path.IsPathRooted(devicePath))
				return null;

			var rootPath = Path.GetPathRoot(devicePath);
			if (devicePath.StartsWith(@"\\?\", StringComparison.Ordinal)) // USB device
			{
				// Check among already discovered drives
				StorageFolder matchingDrive = App.DrivesManager.Drives.FirstOrDefault(x =>
					Helpers.PathNormalization.NormalizePath(x.Path) == Helpers.PathNormalization.NormalizePath(rootPath))?.Root;
				if (matchingDrive is null)
				{
					// Check on all removable drives
					var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
					string normalizedRootPath = Helpers.PathNormalization.NormalizePath(rootPath).Replace(@"\\?\", string.Empty, StringComparison.Ordinal);
					foreach (var item in remDevices)
					{
						try
						{
							var root = StorageDevice.FromId(item.Id);
							if (normalizedRootPath == root.Name.ToUpperInvariant())
							{
								matchingDrive = root;
								break;
							}
						}
						catch (Exception)
						{
							// Ignore this..
						}
					}
				}
				if (matchingDrive is not null)
				{
					return new StorageFolderWithPath(matchingDrive, rootPath);
				}
			}
			// Network share
			else if (devicePath.StartsWith(@"\\", StringComparison.Ordinal) &&
				!devicePath.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
			{
				int lastSepIndex = rootPath.LastIndexOf(@"\", StringComparison.Ordinal);
				rootPath = lastSepIndex > 1 ? rootPath.Substring(0, lastSepIndex) : rootPath; // Remove share name
				return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
			}
			// It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
			return null;
		}

		public async Task UpdateDrivesAsync()
		{
			isDriveEnumInProgress = true;

			if (await GetDrivesAsync())
			{
				if (!Drives.Any(d => d.Type is not DriveType.Removable && d.Path is @"C:\"))
				{
					// Show consent dialog if the exception is UnauthorizedAccessException
					// and the C: drive could not be accessed
					ShowUserConsentOnInit = true;
				}
			}
			StartWatcher();

			isDriveEnumInProgress = false;
		}

		public void ResumeDeviceWatcher()
		{
			if (!isDriveEnumInProgress)
			{
				StartWatcher();
			}
		}

		public async Task HandleWin32DriveEvent(DeviceEvent eventType, string deviceId)
		{
			switch (eventType)
			{
				case DeviceEvent.Added:

					break;
				case DeviceEvent.Removed:

					break;
				case DeviceEvent.Inserted:
				case DeviceEvent.Ejected:

					break;
			}
		}

		public void Dispose()
		{
			DeviceManager.Default.DeviceAdded -= OnDeviceAdded;
			DeviceManager.Default.DeviceRemoved -= OnDeviceRemoved;
			DeviceManager.Default.DeviceInserted -= OnDeviceEjectedOrInserted;
			DeviceManager.Default.DeviceEjected -= OnDeviceEjectedOrInserted;

			if (watcher?.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
			{
				watcher.Stop();
			}
		}

		private void SetupWatcher()
		{
			watcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
			watcher.Added += Watcher_Added;
			watcher.Removed += Watcher_Removed;
			watcher.EnumerationCompleted += Watcher_EnumerationCompleted;

			DeviceManager.Default.DeviceAdded += OnDeviceAdded;
			DeviceManager.Default.DeviceRemoved += OnDeviceRemoved;
			DeviceManager.Default.DeviceInserted += OnDeviceEjectedOrInserted;
			DeviceManager.Default.DeviceEjected += OnDeviceEjectedOrInserted;
		}

		private void OnDeviceRemoved(object? sender, DeviceEventArgs e)
		{
			lock (drives)
			{
				drives.RemoveAll(x => x.DeviceID == e.DeviceId);
			}

			DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Watcher_EnumerationCompleted();
		}

		private async void OnDeviceEjectedOrInserted(object? sender, DeviceEventArgs e)
		{
			var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(e.DeviceId).AsTask());
			DriveItem matchingDriveEjected = Drives.FirstOrDefault(x => x.DeviceID == e.DeviceId);
			if (rootModified && matchingDriveEjected is not null)
			{
				_ = App.Window.DispatcherQueue.EnqueueAsync(() =>
				{
					matchingDriveEjected.Root = rootModified.Result;
					matchingDriveEjected.Text = rootModified.Result.DisplayName;
					return matchingDriveEjected.UpdatePropertiesAsync();
				});
			}
		}

		private async void OnDeviceAdded(object? sender, Helpers.MMI.DeviceEventArgs e)
		{
			var driveAdded = new DriveInfo(e.DeviceId);
			var rootAdded = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(e.DeviceId).AsTask());
			if (!rootAdded)
			{
				logger.LogWarning($"{rootAdded.ErrorCode}: Attempting to add the device, {e.DeviceId},"
					+ " failed at the StorageFolder initialization step. This device will be ignored.");
				return;
			}

			DriveItem driveItem;
			using (var thumbnail = await GetThumbnailAsync(rootAdded.Result))
			{
				var type = GetDriveType(driveAdded);
				driveItem = await DriveItem.CreateFromPropertiesAsync(rootAdded, e.DeviceId, type, thumbnail);
			}

			lock (drives)
			{
				// If drive already in list, skip.
				var matchingDrive = drives.FirstOrDefault(x => x.DeviceID == e.DeviceId ||
					string.IsNullOrEmpty(rootAdded.Result.Path)
						? x.Path.Contains(rootAdded.Result.Name, StringComparison.OrdinalIgnoreCase)
						: x.Path == rootAdded.Result.Path
				);
				if (matchingDrive is not null)
				{
					// Update device id to match drive letter
					matchingDrive.DeviceID = e.DeviceId;
					return;
				}
				logger.LogInformation($"Drive added from fulltrust process: {driveItem.Path}, {driveItem.Type}");
				drives.Add(driveItem);
			}

			DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, driveItem));
			Watcher_EnumerationCompleted();
		}

		private void StartWatcher()
		{
			if (watcher.Status is DeviceWatcherStatus.Created or DeviceWatcherStatus.Stopped or DeviceWatcherStatus.Aborted)
			{
				watcher.Start();
			}
			else
			{
				Watcher_EnumerationCompleted();
			}
		}

		private async void Watcher_Added(DeviceWatcher _, DeviceInformation info)
		{
			string deviceId = info.Id;
			StorageFolder root = null;
			try
			{
				root = StorageDevice.FromId(deviceId);
			}
			catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException)
			{
				logger.LogWarning($"{ex.GetType()}: Attempting to add the device, {info.Name},"
					+ $" failed at the StorageFolder initialization step. This device will be ignored. Device ID: {deviceId}");
				return;
			}

			DriveType type;
			try
			{
				// Check if this drive is associated with a drive letter
				var driveAdded = new DriveInfo(root.Path);
				type = GetDriveType(driveAdded);
			}
			catch (ArgumentException)
			{
				type = DriveType.Removable;
			}

			using var thumbnail = await GetThumbnailAsync(root);
			var driveItem = await DriveItem.CreateFromPropertiesAsync(root, deviceId, type, thumbnail);

			lock (drives)
			{
				// If drive already in list, skip.
				if (drives.Any(x => x.DeviceID == deviceId ||
					string.IsNullOrEmpty(root.Path) ? x.Path.Contains(root.Name, StringComparison.OrdinalIgnoreCase) : x.Path == root.Path))
				{
					return;
				}

				logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");
				drives.Add(driveItem);
			}

			DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, driveItem));

			// Update the collection on the ui-thread.
			Watcher_EnumerationCompleted();
		}

		private void Watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			logger.LogInformation($"Drive removed: {args.Id}");
			lock (drives)
			{
				drives.RemoveAll(x => x.DeviceID == args.Id);
			}

			DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			// Update the collection on the ui-thread.
			Watcher_EnumerationCompleted();
		}

		private async void Watcher_EnumerationCompleted(DeviceWatcher sender = null, object args = null)
		{
			Debug.WriteLine("DeviceWatcher_EnumerationCompleted");
			await folderSizeProvider.CleanAsync();
		}

		private async Task<bool> GetDrivesAsync()
		{
			// Flag set if any drive throws UnauthorizedAccessException
			bool unauthorizedAccessDetected = false;

			var list = DriveInfo.GetDrives();

			foreach (var drive in list)
			{
				var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
				if (res.ErrorCode is FileSystemStatusCode.Unauthorized)
				{
					unauthorizedAccessDetected = true;
					logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}
				else if (!res)
				{
					logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}

				using var thumbnail = await GetThumbnailAsync(res.Result);
				var type = GetDriveType(drive);
				var driveItem = await DriveItem.CreateFromPropertiesAsync(res.Result, drive.Name.TrimEnd('\\'), type, thumbnail);

				lock (drives)
				{
					// If drive already in list, skip.
					if (drives.Any(x => x.Path == drive.Name))
					{
						continue;
					}

					logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");
					drives.Add(driveItem);
				}

				DataChanged?.Invoke(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, driveItem));
			}

			return unauthorizedAccessDetected;
		}

		private static DriveType GetDriveType(DriveInfo drive)
		{
			if (drive.DriveType is IO.DriveType.Unknown)
			{
				string path = Helpers.PathNormalization.NormalizePath(drive.Name);

				if (path is "A:" or "B:")
					return DriveType.FloppyDisk;
			}

			return drive.DriveType switch
			{
				IO.DriveType.CDRom => DriveType.CDRom,
				IO.DriveType.Fixed => DriveType.Fixed,
				IO.DriveType.Network => DriveType.Network,
				IO.DriveType.NoRootDirectory => DriveType.NoRootDirectory,
				IO.DriveType.Ram => DriveType.Ram,
				IO.DriveType.Removable => DriveType.Removable,
				_ => DriveType.Unknown,
			};
		}

		private static async Task<StorageItemThumbnail> GetThumbnailAsync(StorageFolder folder)
			=> (StorageItemThumbnail)await FilesystemTasks.Wrap(()
				=> folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask()
			   );
	}
}