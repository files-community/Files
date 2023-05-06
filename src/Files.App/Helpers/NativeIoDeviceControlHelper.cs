// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.App.Helpers
{
	public class NativeIoDeviceControlHelper
	{
		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr CreateFileFromAppW(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr SecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("api-ms-win-core-io-l1-1-0.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool DeviceIoControl(
			IntPtr hDevice,
			uint dwIoControlCode,
			IntPtr lpInBuffer,
			uint nInBufferSize,
			IntPtr lpOutBuffer,
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped);

		[DllImport("api-ms-win-core-io-l1-1-0.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool DeviceIoControl(
			IntPtr hDevice,
			uint dwIoControlCode,
			byte[] lpInBuffer,
			uint nInBufferSize,
			IntPtr lpOutBuffer,
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped);

		[DllImport("api-ms-win-core-handle-l1-1-0.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);

		public const int INVALID_HANDLE_VALUE = -1;
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;
		public const int FILE_SHARE_READ = 0x00000001;
		public const int FILE_SHARE_WRITE = 0x00000002;
		public const int OPEN_EXISTING = 3;
		public const int FSCTL_LOCK_VOLUME = 0x00090018;
		public const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
		public const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
		public const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;
	}
}
