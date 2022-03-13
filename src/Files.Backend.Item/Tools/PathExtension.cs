using System;
using System.IO;

namespace Files.Backend.Item.Tools
{
    internal static class PathExtension
    {
        public static string GetRootPath(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string root = string.Empty;

            try
            {
                Uri uri = new(path.Replace(@"\", "/", StringComparison.Ordinal));
                root = uri.GetLeftPart(UriPartial.Authority);
                if (uri.IsFile && !string.IsNullOrEmpty(root))
                {
                    return new Uri(root).LocalPath;
                }
            }
            catch (UriFormatException)
            {
                return root;
            }

            return string.IsNullOrEmpty(root) ? (Path.GetPathRoot(path) ?? string.Empty) : root;
        }

        public static string GetParentPath(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            var index = path.Contains("/", StringComparison.Ordinal)
                ? path.LastIndexOf("/", StringComparison.Ordinal)
                : path.LastIndexOf(@"\", StringComparison.Ordinal);
            return path[..(index != -1 ? index : path.Length)];
        }

        public static string TrimPath(this string path)
            => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        public static string NormalizePath(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal) || path.IsFtpPath())
            {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
            }

            if (!path.EndsWith(Path.DirectorySeparatorChar))
            {
                path += Path.DirectorySeparatorChar;
            }
            try
            {
                return Path
                    .GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToUpperInvariant();
            }
            catch (UriFormatException)
            {
                return path;
            }
        }

        public static string CombineNameToPath(this string path, string name)
        {
            if (string.IsNullOrEmpty(path))
            {
                return name;
            }
            return path.Contains("/", StringComparison.Ordinal)
                ? Path.Combine(path, name).Replace(@"\", "/", StringComparison.Ordinal)
                : Path.Combine(path, name);
        }
    }
}
