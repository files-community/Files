// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Security.Principal;

namespace Files.App.Helpers
{
	public static class ElevationHelpers
	{
		public static bool IsElevationRequired(string filePath)
		{
			if (string.IsNullOrEmpty(filePath))
				return false;

			return Win32PInvoke.IsElevationRequired(filePath);
		}

		public static bool IsAppRunAsAdmin()
		{
			using WindowsIdentity identity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(identity).IsInRole(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
		}
	}
}
