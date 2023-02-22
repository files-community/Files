using System;
using System.IO;

namespace Files.Core.Helpers
{
	public static class PathHelpers
	{
		public static string FormatName(string path)
		{
			string 
				fileName,
				rootPath = Path.GetPathRoot(path) ?? string.Empty;
			
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

		public static string GetParentDir(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			var index = path.Contains('/') ? path.LastIndexOf("/", StringComparison.Ordinal) : path.LastIndexOf("\\", StringComparison.Ordinal);
			return path.Substring(0, index != -1 ? index : path.Length);
		}

		public static string Combine(string folder, string name)
		{
			if (string.IsNullOrEmpty(folder))
				return name;
			return folder.Contains('/') ? Path.Combine(folder, name).Replace("\\", "/") : Path.Combine(folder, name);
		}
	}
}
