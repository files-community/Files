using System;

namespace Files.Backend.Item.Tools
{
    internal static class FtpPathExtension
    {
        public static bool IsFtpPath(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            return path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("ftpes://", StringComparison.OrdinalIgnoreCase);
        }

        public static string ToFtpPath(this string path)
        {
            path = path.Replace("\\", "/", StringComparison.Ordinal);
            var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
            var hostIndex = path.IndexOf("/", schemaIndex, StringComparison.Ordinal);
            return hostIndex == -1 ? "/" : path[hostIndex..];
        }

        public static bool CheckFtpPath(string ftpPath)
        {
            var authority = GetFtpAuthority(ftpPath);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            return index == -1 || ushort.TryParse(authority[(index + 1)..], out _);
        }

        public static string GetFtpHost(this string ftpPath)
        {
            var authority = GetFtpAuthority(ftpPath);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            return index == -1 ? authority : authority[..index];
        }

        public static ushort GetFtpPort(this string ftpPath)
        {
            var authority = GetFtpAuthority(ftpPath);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            if (index != -1)
            {
                return ushort.Parse(authority[(index + 1)..]);
            }
            return ftpPath.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase)
                ? (ushort)990
                : (ushort)21;
        }

        public static string GetFtpAuthority(this string ftpPath)
        {
            ftpPath = ftpPath.Replace(@"\", "/", StringComparison.Ordinal);
            var schemaIndex = ftpPath.IndexOf("://", StringComparison.Ordinal) + 3;
            var hostIndex = ftpPath.IndexOf("/", schemaIndex, StringComparison.Ordinal);

            return hostIndex == -1
                ? ftpPath[schemaIndex..]
                : ftpPath[schemaIndex..hostIndex];
        }
    }
}
