using FluentFTP;
using System;
using System.Threading.Tasks;

namespace Files.Backend.Filesystem.Storage
{
    public static class FtpExtensions
    {
        public static async Task<bool> EnsureConnectedAsync(this FtpClient ftpClient)
        {
            if (!ftpClient.IsConnected)
            {
                try
                {
                    await ftpClient.ConnectAsync();
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

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

        public static bool VerifyFtpPath(this string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(':');

            return index is -1 || ushort.TryParse(authority.Substring(index + 1), out _);
        }

        public static string GetFtpPath(this string path)
        {
            path = path.Replace('\\', '/');
            var schemaIndex = path.IndexOf("://") + 3;
            var hostIndex = path.IndexOf('/', schemaIndex);
            return hostIndex is -1 ? "/" : path.Substring(hostIndex);
        }

        public static string GetFtpHost(this string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(':');

            if (index is -1)
            {
                return authority;
            }
            return authority.Substring(0, index);
        }

        public static ushort GetFtpPort(this string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(':');

            if (index is -1)
            {
                return path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) ? (ushort)990 : (ushort)21;
            }

            return ushort.Parse(authority.Substring(index + 1));
        }

        public static string GetFtpAuthority(this string path)
        {
            path = path.Replace('\\', '/');
            var schemaIndex = path.IndexOf("://") + 3;
            var hostIndex = path.IndexOf("/", schemaIndex);

            if (hostIndex is -1)
            {
                hostIndex = path.Length;
            }

            return path.Substring(schemaIndex, hostIndex - schemaIndex);
        }
    }
}
