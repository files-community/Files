using NLog;
using System;
using System.IO;

namespace Files.Helpers
{
    public class PathNormalization
    {
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
                    LogManager.GetCurrentClassLogger().Error(ex, path);
                    throw;
                }
            }
        }
    }
}