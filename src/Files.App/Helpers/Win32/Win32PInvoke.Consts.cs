// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
		public static readonly Guid DataTransferManagerInteropIID =
			new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

		public const int RmRebootReasonNone = 0;
		public const int CCH_RM_MAX_APP_NAME = 255;
		public const int CCH_RM_MAX_SVC_NAME = 63;

	}
}
