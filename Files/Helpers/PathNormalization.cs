using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
            }
        }
    }
}