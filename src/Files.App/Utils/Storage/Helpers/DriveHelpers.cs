// Copyright (c) Files Community
// Licensed under the MIT License.

using DiscUtils.Udf;
using Files.App.Services.SizeProvider;
using Microsoft.Management.Infrastructure;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Provider;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT;

namespace Files.App.Utils.Storage
{
	public static class DriveHelpers
	{
		private static readonly Guid IID_IStorageProviderStatusUISourceFactory = new Guid("12e46b74-4e5a-58d1-a62f-0376e8ee7dd8");

		public static async void EjectDeviceAsync(string path)
		{
			await ContextMenu.InvokeVerb("eject", path);
		}

		public static string GetVolumeId(string driveName)
		{
			string name = driveName.ToUpperInvariant();
			string query = $"SELECT DeviceID FROM Win32_Volume WHERE DriveLetter = '{name}'";

			using var cimSession = CimSession.Create(null);
			foreach (var item in cimSession.QueryInstances(@"root\cimv2", "WQL", query)) // max 1 result because DriveLetter is unique.
				return (string?)item.CimInstanceProperties["DeviceID"]?.Value ?? string.Empty;

			return string.Empty;
		}

		public static async Task<bool> CheckEmptyDrive(string? drivePath)
		{
			if (string.IsNullOrWhiteSpace(drivePath))
				return false;
			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => drivePath.StartsWith(x.Path, StringComparison.Ordinal));
			if (matchingDrive is null || matchingDrive.Type != Data.Items.DriveType.CDRom || matchingDrive.MaxSpace != ByteSizeLib.ByteSize.FromBytes(0))
				return false;

			var ejectButton = await DialogDisplayHelper.ShowDialogAsync(
				"InsertDiscDialog/Title".GetLocalizedResource(),
				string.Format("InsertDiscDialog/Text".GetLocalizedResource(), matchingDrive.Path),
				"InsertDiscDialog/OpenDriveButton".GetLocalizedResource(),
				"Close".GetLocalizedResource());
			if (ejectButton)
				EjectDeviceAsync(matchingDrive.Path);
			return true;
		}

		public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
		{
			if (!SystemIO.Path.IsPathRooted(devicePath))
				return null;

			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var rootPath = SystemIO.Path.GetPathRoot(devicePath);
			if (devicePath.StartsWith(@"\\?\", StringComparison.Ordinal)) // USB device
			{
				// Check among already discovered drives
				StorageFolder matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
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

		public static async Task<StorageItemThumbnail> GetThumbnailAsync(StorageFolder folder)
			=> (StorageItemThumbnail)await FilesystemTasks.Wrap(()
				=> folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask()
			);

		public static async Task<(bool Success, ulong Capacity, ulong Used)> GetSyncRootQuotaAsync(string path)
		{
			Windows.Storage.StorageFolder folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(path);
			StorageProviderSyncRootInfo? syncRootInfo = null;

			try
			{
				syncRootInfo = StorageProviderSyncRootManager.GetSyncRootInformationForFolder(folder);
			}
			catch
			{
				return (false, 0, 0);
			}

			RegistryKey? key;
			if ((key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\SyncRootManager\\{syncRootInfo.Id}")) is null)
			{
				return (false, 0, 0);
			}

			using (key)
			{
				if (key.GetValue("StorageProviderStatusUISourceFactory") is string statusUIclass)
				{
					StorageProviderStatusUI statusUI;

					unsafe
					{
						if (PInvoke.CoCreateInstance(Guid.Parse(statusUIclass), null, Windows.Win32.System.Com.CLSCTX.CLSCTX_LOCAL_SERVER, IID_IStorageProviderStatusUISourceFactory, out void* statusUISourceFactoryAbi) != 0)
						{
							return (false, 0, 0);
						}

						// CsWinRT wrappers won't work.
						// TODO: look to replace MarshalString with MarshalString.Pinnable?

						nint statusUISourceAbi = 0;
						nint syncRootIdHstring = MarshalString.FromManaged(syncRootInfo.Id);
						nint statusUIAbi = 0;
						ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<IntPtr, IntPtr, IntPtr*, int>)(*(IntPtr*)((nint)(*(IntPtr*)statusUISourceFactoryAbi) + (nint)6 * (nint)sizeof(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>))))((nint)statusUISourceFactoryAbi, syncRootIdHstring, &statusUISourceAbi));
						ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)statusUISourceAbi) + (nint)6 * (nint)sizeof(delegate* unmanaged[Stdcall]<nint, nint*, int>))))(statusUISourceAbi, &statusUIAbi));
						statusUI = StorageProviderStatusUI.FromAbi(statusUIAbi);
						Marshal.Release(statusUISourceAbi);
						Marshal.Release((nint)statusUISourceFactoryAbi);
						MarshalString.DisposeAbi(statusUISourceAbi);
					}
					return (true, statusUI.QuotaUI.QuotaTotalInBytes, statusUI.QuotaUI.QuotaUsedInBytes);
				}
				else
				{
					return (false, 0, 0);
				}
			}
		}
	}
}