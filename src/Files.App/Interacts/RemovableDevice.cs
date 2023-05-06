// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.App.Helpers.NativeIoDeviceControlHelper;

namespace Files.App.Interacts
{
	public class RemovableDevice
	{
		private IntPtr handle;
		private char driveLetter;

		public RemovableDevice(string letter)
		{
			driveLetter = letter[0];

			string filename = @"\\.\" + driveLetter + ":";

			handle = CreateFileFromAppW(filename,
				GENERIC_READ | GENERIC_WRITE,
				FILE_SHARE_READ | FILE_SHARE_WRITE,
				IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
		}

		public async Task<bool> EjectAsync()
		{
			bool result = false;

			if (handle.ToInt32() == INVALID_HANDLE_VALUE)
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
				if (DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
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

		private bool DismountVolume()
		{
			return DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
		}

		private bool PreventRemovalOfVolume(bool prevent)
		{
			byte[] buf = new byte[1];
			buf[0] = prevent ? (byte)1 : (byte)0;

			return DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, buf, 1, IntPtr.Zero, 0, out _, IntPtr.Zero);
		}

		private bool AutoEjectVolume()
		{
			return DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
		}

		private bool CloseVolume()
		{
			return CloseHandle(handle);
		}
	}
}
