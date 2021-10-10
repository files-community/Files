using System;
using System.IO;

namespace Files.Helpers
{
    public static class PathNormalization
    {
        public static string GetPathRoot(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }
            string rootPath = "";
            try
            {
                var pathAsUri = new Uri(path.Replace("\\", "/"));
                rootPath = pathAsUri.GetLeftPart(UriPartial.Authority);
                if (pathAsUri.IsFile && !string.IsNullOrEmpty(rootPath))
                {
                    rootPath = new Uri(rootPath).LocalPath;
                }
            }
            catch (UriFormatException)
            {
            }
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.GetPathRoot(path);
            }
            return rootPath;
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            if (path.StartsWith("\\\\") || path.StartsWith("//") || FtpHelpers.IsFtpPath(path))
            {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
            }
            else
            {
                if (!path.EndsWith(Path.DirectorySeparatorChar))
                {
                    path += Path.DirectorySeparatorChar;
                }

                try
                {
                    return Path.GetFullPath(new Uri(path).LocalPath)
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .ToUpperInvariant();
                }
                catch (UriFormatException ex)
                {
                    App.Logger.Warn(ex, path);
                    return path;
                }
            }
        }

        public static string TrimPath(this string path)
        {
            return path?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string GetParentDir(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            var index = path.Contains("/") ? path.LastIndexOf("/") : path.LastIndexOf("\\");
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