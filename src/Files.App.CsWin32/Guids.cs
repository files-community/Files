// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Windows.Win32
{
	// CLSIDs
	public static unsafe partial class PInvoke
	{
		[GuidRVAGen.Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")]
		public static partial Guid* CLSID_DesktopWallpaper { get; }
	}

	// IIDs
	public static unsafe partial class PInvoke
	{
		[GuidRVAGen.Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
		public static partial Guid* IID_IDesktopWallpaper { get; }
	}

	// Misc
	public static unsafe partial class PInvoke
	{
		[GuidRVAGen.Guid("94f60519-2850-4924-aa5a-d15e84868039")]
		public static partial Guid* BHID_EnumItems { get; }
	}
}
