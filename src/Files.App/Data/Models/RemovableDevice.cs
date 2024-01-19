// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Files.App.Helpers.Win32Helper;

namespace Files.App.Data.Models
{
	public class RemovableDevice
	{
		private nint handle;
		private char driveLetter;

		public RemovableDevice(string letter)
		{
			driveLetter = letter[0];

			string filename = @"\\.\" + driveLetter + ":";

			handle = Win32PInvoke.CreateFileFromAppW(filename,
				Win32PInvoke.GENERIC_READ | Win32PInvoke.GENERIC_WRITE,
				Win32PInvoke.FILE_SHARE_READ | Win32PInvoke.FILE_SHARE_WRITE,
				nint.Zero, Win32PInvoke.OPEN_EXISTING, 0, nint.Zero);
		}

		public async Task<bool> EjectAsync()
		{
			bool result = false;

			if (handle.ToInt32() == Win32PInvoke.INVALID_HANDLE_VALUE)
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
				if (Win32PInvoke.DeviceIoControl(handle, Win32PInvoke.FSCTL_LOCK_VOLUME, nint.Zero, 0, nint.Zero, 0, out _, nint.Zero))
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
			return Win32PInvoke.DeviceIoControl(handle, Win32PInvoke.FSCTL_DISMOUNT_VOLUME, nint.Zero, 0, nint.Zero, 0, out _, nint.Zero);
		}

		private bool PreventRemovalOfVolume(bool prevent)
		{
			byte[] buf = new byte[1];
			buf[0] = prevent ? (byte)1 : (byte)0;

			return Win32PInvoke.DeviceIoControl(handle, Win32PInvoke.IOCTL_STORAGE_MEDIA_REMOVAL, buf, 1, nint.Zero, 0, out _, nint.Zero);
		}

		private bool AutoEjectVolume()
		{
			return Win32PInvoke.DeviceIoControl(handle, Win32PInvoke.IOCTL_STORAGE_EJECT_MEDIA, nint.Zero, 0, nint.Zero, 0, out _, nint.Zero);
		}

		private bool CloseVolume()
		{
			return Win32PInvoke.CloseHandle(handle);
		}
	}
}
