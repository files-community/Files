﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Files.App.Helpers
{
	public static class ElevationHelpers
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
