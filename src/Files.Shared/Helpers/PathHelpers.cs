// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;

namespace Files.Shared.Helpers
{
	public static class PathHelpers
	{
		public static string Combine(string folder, string name)
		{
			if (string.IsNullOrEmpty(folder))
				return name;

			return folder.Contains('/') ? Path.Combine(folder, name).Replace('\\', '/') : Path.Combine(folder, name);
		}

		public static bool TryGetFullPath(string commandName, out string fullPath)
		{
			fullPath = string.Empty;
			try
			{
				var p = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						UseShellExecute = false,
						CreateNoWindow = true,
						FileName = "where.exe",
						Arguments = commandName,
						RedirectStandardOutput = true
					}
				};

				p.Start();

				var output = p.StandardOutput.ReadToEnd();
				p.WaitForExit(1000);

				if (p.ExitCode != 0)
					return false;

				// Return the first one with valid executable extension, in case there is a match with no extension
				foreach (var line in output.Split(Environment.NewLine))
				{
					if (FileExtensionHelpers.IsExecutableFile(line))
					{
						fullPath = line;

						return true;
					}
				}

				return false;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool IsInSystemFontsFolder(string path)
		{
			try
			{
				var fullPath = Path.GetFullPath(path);
				var windowsFontsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

				return fullPath.StartsWith(windowsFontsPath, StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}
	}
}
