// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for application elevation.
	/// </summary>
	/// <remarks>
	/// If the app is running as admin, some features will be unavailable due to the platform limitation.
	/// </remarks>
	internal static class AppElevationHelpers
	{
		[DllImport("shell32.dll", EntryPoint = "#865", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)] private static extern bool _IsElevationRequired([MarshalAs(UnmanagedType.LPWStr)] string pszPath);

		public static bool IsElevationRequired(string filePath)
		{
			if (string.IsNullOrEmpty(filePath))
				return false;

			return _IsElevationRequired(filePath);
		}

		public static bool IsAppRunAsAdmin()
		{
			using WindowsIdentity identity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(identity).IsInRole(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
		}
	}
}
