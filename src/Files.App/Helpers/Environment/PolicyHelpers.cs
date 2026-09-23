// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Win32;

namespace Files.App.Helpers
{
	internal static class PolicyHelpers
	{
		private const string ExplorerPolicyRegistryKey = @"SOFTWARE\Policies\Microsoft\Windows\Explorer";
		private const string FilesPolicyRegistryKey = @"SOFTWARE\Policies\Files Community\Files";

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

		public static bool IsSettingsEnabled { get; } = GetFilesPolicyValue("ShowSettingsButton") is not int value || value != 0;

		public static string? GetImportSettingsFilePath()
		{
			return GetFilesPolicyValue("ImportSettingsFile") is string path && !string.IsNullOrWhiteSpace(path)
				? Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'))
				: null;
		}

		private static object? GetFilesPolicyValue(string name)
		{
			foreach (var hive in (RegistryKey[])[Registry.LocalMachine, Registry.CurrentUser])
			{
				try
				{
					using var policySubkey = hive.OpenSubKey(FilesPolicyRegistryKey);
					if (policySubkey?.GetValue(name) is { } value)
						return value;
				}
				catch
				{
				}
			}

			return null;
		}
	}
}
