using System;
using System.Threading;
using System.Threading.Tasks;
using Files.Shared.Extensions;
using FluentFTP;

namespace Files.Uwp.Storage.FtpStorage
{
    internal static class FtpHelpers
    {
        public static string GetFtpPath(string path)
        {
            path = path.Replace("\\", "/", StringComparison.Ordinal);

            var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
            var hostIndex = path.IndexOf("/", schemaIndex, StringComparison.Ordinal);

            return hostIndex == -1 ? "/" : path.Substring(hostIndex);
        }

        public static async Task<bool> EnsureConnectedAsync(this FtpClient ftpClient, CancellationToken cancellationToken = default)
        {
            if (ftpClient.IsConnected)
                return true;

            try
            {
                await ftpClient.ConnectAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetFtpHost(string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            if (index == -1)
                return authority;

            return authority.Substring(0, index);
        }

        public static ushort GetFtpPort(string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            if (index != -1) 
                return ushort.Parse(authority.Substring(index + 1));

            if (path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase))
                return 990;

            return 21;

        }

        public static string GetFtpAuthority(string path)
        {
            path = path.Replace("\\", "/", StringComparison.Ordinal);
            var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
            var hostIndex = path.IndexOf("/", schemaIndex, StringComparison.Ordinal);

            if (hostIndex == -1)
                hostIndex = path.Length;

            return path.Substring(schemaIndex, hostIndex - schemaIndex);
        }

        public static FtpClient GetFtpClient(string ftpPath)
        {
            var host = FtpHelpers.GetFtpHost(ftpPath);
            var port = FtpHelpers.GetFtpPort(ftpPath);
            var credentials = FtpManager.Credentials.Get(host, FtpManager.Anonymous);

            return new(host, port, credentials);
        }
    }
}
