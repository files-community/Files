using System;
using System.IO;

namespace Files.Shared.Helpers
{
	public static class PathHelpers
	{
		public static string GetParentDir(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			var index = path.Contains("/") ? path.LastIndexOf("/", StringComparison.Ordinal) : path.LastIndexOf("\\", StringComparison.Ordinal);
			return path.Substring(0, index != -1 ? index : path.Length);
		}

		public static string Combine(string folder, string name)
		{
			if (string.IsNullOrEmpty(folder))
			{
				return name;
			}
			return folder.Contains("/") ? Path.Combine(folder, name).Replace("\\", "/") : Path.Combine(folder, name);
		}
	}
}
