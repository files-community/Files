// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
		public static readonly Guid DataTransferManagerInteropIID =
			new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

		public const int RmRebootReasonNone = 0;
		public const int CCH_RM_MAX_APP_NAME = 255;
		public const int CCH_RM_MAX_SVC_NAME = 63;

		public const int FILE_NOTIFY_CHANGE_FILE_NAME = 1;
		public const int FILE_NOTIFY_CHANGE_DIR_NAME = 2;
		public const int FILE_NOTIFY_CHANGE_ATTRIBUTES = 4;
		public const int FILE_NOTIFY_CHANGE_SIZE = 8;
		public const int FILE_NOTIFY_CHANGE_LAST_WRITE = 16;
		public const int FILE_NOTIFY_CHANGE_LAST_ACCESS = 32;
		public const int FILE_NOTIFY_CHANGE_CREATION = 64;
		public const int FILE_NOTIFY_CHANGE_SECURITY = 256;

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
