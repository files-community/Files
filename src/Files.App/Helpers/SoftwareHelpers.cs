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

			return
				ContainsName(Registry.CurrentUser.OpenSubKey(registryKey), "Microsoft Visual Studio Code") ||
				ContainsName(Registry.LocalMachine.OpenSubKey(registryKey), "Microsoft Visual Studio Code");
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

		private static bool ContainsName(RegistryKey? key, string find)
		{
			if (key is null)
				return false;

			foreach (var subKey in key.GetSubKeyNames().Select(key.OpenSubKey))
			{
				var displayName = subKey?.GetValue("DisplayName") as string;
				if (!string.IsNullOrWhiteSpace(displayName) && displayName.StartsWith(find))
				{
					key.Close();

					return true;
				}
			}

			key.Close();

			return false;
		}
	}
}
