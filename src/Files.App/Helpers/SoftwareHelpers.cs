// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Win32;

namespace Files.App.Helpers
{
	internal static class SoftwareHelpers
	{
		public static bool IsVSCodeInstalled()
		{
			string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

			var key = Registry.CurrentUser.OpenSubKey(registryKey);
			if (key is null)
				return false;

			string? displayName;

			foreach (var subKey in key.GetSubKeyNames().Select(key.OpenSubKey))
			{
				displayName = subKey?.GetValue("DisplayName") as string;
				if (!string.IsNullOrWhiteSpace(displayName) && displayName.StartsWith("Microsoft Visual Studio Code"))
				{
					key.Close();

					return true;
				}
			}

			key.Close();

			return false;
		}

		public static bool IsVSInstalled()
		{
			string registryKey = @"SOFTWARE\Microsoft\VisualStudio";

			var key = Registry.LocalMachine.OpenSubKey(registryKey);
			if (key is null)
				return false;

			key.Close();

			return true;
		}
	}
}
