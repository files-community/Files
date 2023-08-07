// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
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

		public static string FormatName(string path)
		{
			string fileName;
			string rootPath = Path.GetPathRoot(path) ?? string.Empty;

			if (rootPath == path && path.StartsWith(@"\\"))
			{
				// Network Share path
				fileName = path.Substring(path.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
			}
			else if (rootPath == path)
			{
				// Drive path
				fileName = path;
			}
			else
			{
				// Standard file name
				fileName = Path.GetFileName(path);
			}

			// Check for link file name
			if (FileExtensionHelpers.IsShortcutOrUrlFile(fileName))
				fileName = fileName.Remove(fileName.Length - 4);

			return fileName;
		}

		public static bool IsSpecialFolder(string path)
		{
			foreach (Environment.SpecialFolder specialFolder in Enum.GetValues(typeof(Environment.SpecialFolder)))
			{
				var specialFolderPath = Environment.GetFolderPath(specialFolder);
				if (string.Equals(specialFolderPath, path, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}
	}
}
