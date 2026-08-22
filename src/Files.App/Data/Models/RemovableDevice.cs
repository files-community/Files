// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Data.Models
{
	public sealed class RemovableDevice
	{
		private readonly SafeFileHandle handle;
		private char driveLetter;

		public RemovableDevice(string letter)
		{
			driveLetter = letter[0];

			string filename = @"\\.\" + driveLetter + ":";

			handle = PInvoke.CreateFile(filename,
				(uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
				FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
				null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, 0, null);
		}

		public async Task<bool> EjectAsync()
		{
			bool result = false;

			if (handle.IsInvalid)
			{
				Debug.WriteLine($"Unable to open drive {driveLetter}");
				return false;
			}

			if (await LockVolumeAsync() && DismountVolume())
			{
				PreventRemovalOfVolume(false);
				result = AutoEjectVolume();
			}

			CloseVolume();

			return result;
		}

		private async Task<bool> LockVolumeAsync()
		{
			bool result = false;

			for (int i = 0; i < 5; i++)
			{
				if (LockVolume())
				{
					Debug.WriteLine("Lock successful!");
					result = true;

					break;
				}
				else
				{
					Debug.WriteLine($"Can't lock device, attempt {i + 1}, trying again... ");
				}

				await Task.Delay(500);
			}

			return result;
		}

		private unsafe bool LockVolume()
		{
			return PInvoke.DeviceIoControl(handle, PInvoke.FSCTL_LOCK_VOLUME, [], [], out _, null);
		}

		private unsafe bool DismountVolume()
		{
			return PInvoke.DeviceIoControl(handle, PInvoke.FSCTL_DISMOUNT_VOLUME, [], [], out _, null);
		}

		private unsafe bool PreventRemovalOfVolume(bool prevent)
		{
			byte[] buf = [prevent ? (byte)1 : (byte)0];
			return PInvoke.DeviceIoControl(handle, PInvoke.IOCTL_STORAGE_MEDIA_REMOVAL, buf, [], out _, null);
		}

		private unsafe bool AutoEjectVolume()
		{
			return PInvoke.DeviceIoControl(handle, PInvoke.IOCTL_STORAGE_EJECT_MEDIA, [], [], out _, null);
		}

		private bool CloseVolume()
		{
			handle.Dispose();
			return true;
		}
	}
}
