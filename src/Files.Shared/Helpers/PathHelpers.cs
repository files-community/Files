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
	}
}
