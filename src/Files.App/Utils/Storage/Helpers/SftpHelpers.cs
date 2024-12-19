// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using FluentFTP;
using Renci.SshNet;

namespace Files.App.Utils.Storage
{
	public static class SftpHelpers
	{
		public static async Task<bool> EnsureConnectedAsync(this SftpClient ftpClient)
		{
			if (!ftpClient.IsConnected)
			{
				await ftpClient.ConnectAsync(default);
			}

			return true;
		}

		public static bool IsSftpPath(string path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				return path.StartsWith("sftp://", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		public static bool VerifyFtpPath(string path)
		{
			var authority = GetSftpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			return index == -1 || ushort.TryParse(authority.AsSpan(index + 1), out _);
		}

		public static string GetSftpHost(string path)
		{
			var authority = GetSftpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			return index == -1 ? authority : authority[..index];
		}

		public static ushort GetSftpPort(string path)
		{
			var authority = GetSftpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			if (index == -1)
				return 22;

			return ushort.Parse(authority[(index + 1)..]);
		}

		public static string GetSftpAuthority(string path)
		{
			path = path.Replace("\\", "/", StringComparison.Ordinal);
			if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
				return uri.Authority;
			return string.Empty;
		}

		public static string GetSftpPath(string path)
		{
			path = path.Replace("\\", "/", StringComparison.Ordinal);
			var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
			var hostIndex = path.IndexOf('/', schemaIndex);
			return hostIndex == -1 ? "/" : path.Substring(hostIndex);
		}

		public static int GetRootIndex(string path)
		{
			path = path.Replace("\\", "/", StringComparison.Ordinal);
			var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
			return path.IndexOf('/', schemaIndex);
		}
	}
}