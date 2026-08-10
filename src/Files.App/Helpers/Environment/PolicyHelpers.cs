// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;

namespace Files.App.Helpers
{
	internal static class PolicyHelpers
	{
		private const string ExplorerPolicyRegistryKey = @"SOFTWARE\Policies\Microsoft\Windows\Explorer";

		public static bool IsShellShortcutIconRemotePathEnabled()
		{
			try
			{
				using var policySubkey = Registry.LocalMachine.OpenSubKey(ExplorerPolicyRegistryKey);

				return Convert.ToBoolean(policySubkey?.GetValue("EnableShellShortcutIconRemotePath", false));
			}
			catch
			{
				return false;
			}
		}
	}
}
