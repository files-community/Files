// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Win32;
using System.IO;

namespace Files.App.Helpers
{
	internal static class SoftwareHelpers
	{
		public static bool IsVSCodeInstalled()
		{
			string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
			string vsCodeName = "Microsoft Visual Studio Code";

			return
				ContainsName(Registry.CurrentUser.OpenSubKey(registryKey), vsCodeName) ||
				ContainsName(Registry.LocalMachine.OpenSubKey(registryKey), vsCodeName);
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

		public static bool IsPythonInstalled()
		{
			try
			{
				ProcessStartInfo psi = new ProcessStartInfo();
				psi.FileName = "python";
				psi.Arguments = "--version";
				psi.RedirectStandardOutput = true;
				psi.UseShellExecute = false;
				psi.CreateNoWindow = true;

				using (Process process = Process.Start(psi))
				{
					using (StreamReader reader = process.StandardOutput)
					{
						string result = reader.ReadToEnd();
						return result.Contains("Python");
					}
				}
			}
			catch
			{
				return false;
			}
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
