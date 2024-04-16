﻿using FluentFTP;
using System;
using System.Threading.Tasks;

namespace Files.Uwp.Helpers
{
    public static class FtpHelpers
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

        public static bool IsFtpPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("ftpes://", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public static bool VerifyFtpPath(string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            if (index == -1)
            {
                return true;
            }

            return ushort.TryParse(authority.Substring(index + 1), out _);
        }

        public static string GetFtpHost(string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            if (index == -1)
            {
                return authority;
            }

            return authority.Substring(0, index);
        }

        public static ushort GetFtpPort(string path)
        {
            var authority = GetFtpAuthority(path);
            var index = authority.IndexOf(":", StringComparison.Ordinal);

            if (index == -1)
            {
                if (path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase))
                {
                    return 990;
                }

                return 21;
            }

            return ushort.Parse(authority.Substring(index + 1));
        }

        public static string GetFtpAuthority(string path)
        {
            path = path.Replace("\\", "/", StringComparison.Ordinal);
            var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
            var hostIndex = path.IndexOf("/", schemaIndex, StringComparison.Ordinal);

            if (hostIndex == -1)
            {
                hostIndex = path.Length;
            }

            return path.Substring(schemaIndex, hostIndex - schemaIndex);
        }

        public static string GetFtpPath(string path)
        {
            path = path.Replace("\\", "/", StringComparison.Ordinal);
            var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
            var hostIndex = path.IndexOf("/", schemaIndex, StringComparison.Ordinal);
            return hostIndex == -1 ? "/" : path.Substring(hostIndex);
        }
    }
}