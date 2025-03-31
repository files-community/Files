// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Storage
{
	public static partial class WindowsStorableHelpers
	{
		public static async Task<bool> TrySetShortcutIconOnPowerShellAsElevatedAsync(this IWindowsStorable storable, IWindowsStorable iconFile, int index)
		{
			string psScript = 
				$@"$FilePath = '{storable}'
					$IconFile = '{iconFile}'
					$IconIndex = '{index}'

					$Shell = New-Object -ComObject WScript.Shell
					$Shortcut = $Shell.CreateShortcut($FilePath)
					$Shortcut.IconLocation = ""$IconFile, $IconIndex""
					$Shortcut.Save()";

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "PowerShell.exe",
					Arguments = $"-NoProfile -EncodedCommand {Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(psScript))}",
					Verb = "RunAs",
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					UseShellExecute = true
				},
			};

			process.Start();
			await process.WaitForExitAsync();

			return true;
		}
	}
}
