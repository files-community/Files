// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Files.App.Helpers
{
	public static class PathNormalization
	{
		public static string GetPathRoot(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			string rootPath = string.Empty;
			try
			{
				var pathAsUri = new Uri(path.Replace("\\", "/", StringComparison.Ordinal));
				rootPath = pathAsUri.GetLeftPart(UriPartial.Authority);
				if (pathAsUri.IsFile && !string.IsNullOrEmpty(rootPath))
					rootPath = new Uri(rootPath).LocalPath;
			}
			catch (UriFormatException)
			{
			}
			if (string.IsNullOrEmpty(rootPath))
				rootPath = Path.GetPathRoot(path) ?? string.Empty;

			return rootPath;
		}

		public static string NormalizePath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return path;

			if (path.StartsWith("\\\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(path))
				return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();

			if (!path.EndsWith(Path.DirectorySeparatorChar))
				path += Path.DirectorySeparatorChar;

			try
			{
				return Path.GetFullPath(new Uri(path).LocalPath)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					.ToUpperInvariant();
			}
			catch (UriFormatException ex)
			{
				App.Logger.LogWarning(ex, path);
				return path;
			}
		}

		public static string? TrimPath(this string path)
		{
			return path?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public static string GetParentDir(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			var index = path.Contains('/', StringComparison.Ordinal) ? path.LastIndexOf("/", StringComparison.Ordinal) : path.LastIndexOf("\\", StringComparison.Ordinal);
			return path.Substring(0, index != -1 ? index : path.Length);
		}

		public static string Combine(string folder, string name)
		{
			if (string.IsNullOrEmpty(folder))
				return name;

			return folder.Contains('/', StringComparison.Ordinal) ? Path.Combine(folder, name).Replace("\\", "/", StringComparison.Ordinal) : Path.Combine(folder, name);
		}
	}
}