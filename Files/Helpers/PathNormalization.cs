using System;
using System.IO;

namespace Files.Helpers
{
    public class PathNormalization
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
                rootPath = new Uri(path).GetLeftPart(UriPartial.Authority);
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
            if (path.StartsWith("\\\\"))
            {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
            }
            else if (path.StartsWith("ftp://"))
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

        public static string GetParentDir(string path)
        {
            var index = path.LastIndexOf("\\");
            return path.Substring(0, index != -1 ? index : path.Length);
        }
    }
}