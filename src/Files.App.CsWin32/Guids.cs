// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32
{
	public static unsafe partial class PInvoke
	{
		[GuidRVAGen.Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
		public static partial Guid* IID_DesktopWallpaper { get; }

		[GuidRVAGen.Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
		public static partial Guid* CLSID_DesktopWallpaper { get; }
	}
}
