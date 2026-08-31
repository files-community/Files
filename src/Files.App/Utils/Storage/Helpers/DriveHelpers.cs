// Copyright (c) Files Community
// Licensed under the MIT License.

using DiscUtils.Udf;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Files.App.Utils.Storage
{
	public static class DriveHelpers
	{
		public static async void EjectDeviceAsync(string path)
		{
			var driveRoot = path.EndsWith('\\') ? path : path + '\\';

			ReleaseDriveHandles(driveRoot);

			// Give the released handles a moment to close before the shell issues the removal query
			await Task.Delay(300);

			await ContextMenu.InvokeVerb("eject", path);

			// If the volume is still mounted the eject was vetoed; re-arm the Recycle Bin watcher
			await Task.Delay(2000);
			if (SystemIO.Directory.Exists(driveRoot))
				Ioc.Default.GetRequiredService<IStorageTrashBinService>().Watcher.StartWatcher(driveRoot);
		}

		/// <summary>
		/// Releases the handles Files itself holds on the drive (directory change watchers, sidebar
		/// subtree watchers, the Recycle Bin watcher) so they can't veto the device removal.
		/// </summary>
		private static void ReleaseDriveHandles(string driveRoot)
		{
			// Navigate every pane showing the drive to Home and close its directory watcher
			var multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();
			foreach (var tab in multitaskingContext.Control?.GetAllTabInstances() ?? [])
			{
				if (tab is not ShellPanesPage panesPage)
					continue;

				foreach (var pane in panesPage.GetPanes())
				{
					var panePath = pane.ShellViewModel?.CurrentFolder?.ItemPath;
					if (panePath is not null && (panePath + '\\').StartsWith(driveRoot, StringComparison.OrdinalIgnoreCase))
					{
						pane.ShellViewModel?.CloseWatcher();
						pane.NavigateHome();
					}
				}
			}

			// Stop sidebar subtree watchers rooted on the drive
			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
			if (drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => string.Equals(x.Path?.TrimEnd('\\'), driveRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) is { } driveItem)
				driveItem.StopWatchingSubfoldersAndDescendants();

			// Drop the Recycle Bin watcher for this drive
			Ioc.Default.GetRequiredService<IStorageTrashBinService>().Watcher.StopWatcher(driveRoot);
		}

		public static async Task<bool> CheckEmptyDrive(string? drivePath)
		{
			if (string.IsNullOrWhiteSpace(drivePath))
				return false;
			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => drivePath.StartsWith(x.Path!, StringComparison.Ordinal));
			if (matchingDrive is null || matchingDrive.Type != Data.Items.DriveType.CDRom || matchingDrive.MaxSpace != ByteSizeLib.ByteSize.FromBytes(0))
				return false;

			var ejectButton = await DialogDisplayHelper.ShowDialogAsync(
				Strings.InsertDiscDialogTitle.GetLocalizedResource(),
				string.Format(Strings.InsertDiscDialogText.GetLocalizedResource(), matchingDrive.Path),
				Strings.InsertDiscDialog_OpenDriveButton.GetLocalizedResource(),
				Strings.Close.GetLocalizedResource());
			if (ejectButton)
				EjectDeviceAsync(matchingDrive.Path!);
			return true;
		}

		public static async Task<StorageFolderWithPath?> GetRootFromPathAsync(string? devicePath)
		{
			if (!SystemIO.Path.IsPathRooted(devicePath))
				return null;

			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var rootPath = SystemIO.Path.GetPathRoot(devicePath);
			if (rootPath is null)
				return null;

			if (devicePath.StartsWith(@"\\?\", StringComparison.Ordinal)) // USB device
			{
				// Check among already discovered drives
				StorageFolder? matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
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
			else if (
						  (devicePath.StartsWith(@"\\", StringComparison.Ordinal) ||
							 GetDriveType(new SystemIO.DriveInfo(devicePath)) is DriveType.Network) &&
						  !devicePath.StartsWith(@"\\SHELL\", StringComparison.Ordinal)
					)
			{
				int lastSepIndex = rootPath.LastIndexOf('\\');
				rootPath = lastSepIndex > 1 ? rootPath.Substring(0, lastSepIndex) : rootPath; // Remove share name
				return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
			}
			// It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
			return null;
		}

		public static bool IsMtpPath(string path)
		{
			return path.StartsWith(@"\\?\", StringComparison.Ordinal);
		}

		public static bool IsNetworkPath(string path)
		{
			if (IsMtpPath(path))
				return false;

			try
			{
				return path.StartsWith(@"\\", StringComparison.Ordinal) ||
					GetDriveType(new SystemIO.DriveInfo(path)) is Data.Items.DriveType.Network;
			}
			catch
			{
				return false;
			}
		}

		public static Data.Items.DriveType GetDriveType(System.IO.DriveInfo drive)
		{
			if (drive.DriveType is System.IO.DriveType.Unknown)
			{
				string path = PathNormalization.NormalizePath(drive.Name);

				if (path is "A:" or "B:")
					return Data.Items.DriveType.FloppyDisk;
			}

			return drive.DriveType switch
			{
				SystemIO.DriveType.CDRom => Data.Items.DriveType.CDRom,
				SystemIO.DriveType.Fixed => Data.Items.DriveType.Fixed,
				SystemIO.DriveType.Network => Data.Items.DriveType.Network,
				SystemIO.DriveType.NoRootDirectory => Data.Items.DriveType.NoRootDirectory,
				SystemIO.DriveType.Ram => Data.Items.DriveType.Ram,
				SystemIO.DriveType.Removable => Data.Items.DriveType.Removable,
				_ => Data.Items.DriveType.Unknown,
			};
		}

		public static unsafe string GetExtendedDriveLabel(SystemIO.DriveInfo drive)
		{
			return SafetyExtensions.IgnoreExceptions(() =>
			{
				if (drive.DriveType is not SystemIO.DriveType.CDRom || drive.DriveFormat is not "UDF")
					return drive.VolumeLabel;

				return SafetyExtensions.IgnoreExceptions(() =>
				{
					string dosDevicePath = "";

					fixed (char* cDeviceName = drive.Name)
					{
						var cch = PInvoke.QueryDosDevice(cDeviceName, null, 0u);

						fixed (char* cTargetPath = new char[cch])
						{
							PWSTR pszTargetPath = new(cTargetPath);
							PInvoke.QueryDosDevice(cDeviceName, pszTargetPath, 0u);
							dosDevicePath = pszTargetPath.ToString();
						}
					}

					if (string.IsNullOrEmpty(dosDevicePath))
						return drive.VolumeLabel;

					using var driveStream = new SystemIO.FileStream(
						dosDevicePath.Replace(@"\Device\", @"\\.\"),
						SystemIO.FileMode.Open,
						SystemIO.FileAccess.Read);

					using var udf = new UdfReader(driveStream);

					return udf.VolumeLabel;
				}) ?? drive.VolumeLabel;

			}) ?? "";
		}

		public static async Task<StorageItemThumbnail?> GetThumbnailAsync(StorageFolder folder)
			=> await FilesystemTasks.Wrap(()
				=> folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask()
			);
	}
}
