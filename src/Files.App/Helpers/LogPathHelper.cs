// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides helper methods for formatting paths in logs.
	/// </summary>
	public static class LogPathHelper
	{
		public static string GetFileName(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return "[Empty]";

			try
			{
				return Path.GetFileName(path) ?? "?";
			}
			catch
			{
				return "?";
			}
		}

		public static string GetDirectoryName(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return "[Empty]";

			try
			{
				// Trim trailing separators to ensure we get the last directory name
				var trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				return Path.GetFileName(trimmedPath) ?? "?";
			}
			catch
			{
				return "?";
			}
		}
	}
}
