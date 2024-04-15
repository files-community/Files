// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Win32;
using System.IO;

namespace Files.App.Helpers
{
	internal static class SoftwareHelpers
	{
		private const string UninstallRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
		private const string VsRegistryKey = @"SOFTWARE\Microsoft\VisualStudio";
		
		private const string VsCodeName = "Microsoft Visual Studio Code";


		public static bool IsVSCodeInstalled()
		{
			return
				ContainsName(Registry.CurrentUser.OpenSubKey(UninstallRegistryKey), VsCodeName) ||
				ContainsName(Registry.LocalMachine.OpenSubKey(UninstallRegistryKey), VsCodeName);
		}

		public static bool IsVSInstalled()
		{
			var key = Registry.LocalMachine.OpenSubKey(VsRegistryKey);
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
