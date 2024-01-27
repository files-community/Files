// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using FluentFTP;
using System.Net;

namespace Files.App.Storage
{
	/// <summary>
	/// Provides static helper for FTP storage.
	/// </summary>
	public static class FtpStorageHelper
	{
		public static readonly Dictionary<string, NetworkCredential> Credentials = new();

		public static readonly NetworkCredential Anonymous = new("anonymous", "anonymous");

		public static bool IsFtpPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			return
				path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
				path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) ||
				path.StartsWith("ftpes://", StringComparison.OrdinalIgnoreCase);
		}

		public static bool VerifyFtpPath(string path)
		{
			var authority = GetFtpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			return index == -1 || ushort.TryParse(authority.Substring(index + 1), out _);
		}

		public static string GetFtpPath(string path)
		{
			path = path.Replace("\\", "/", StringComparison.Ordinal);

			var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
			var hostIndex = path.IndexOf("/", schemaIndex, StringComparison.Ordinal);

			return hostIndex == -1 ? "/" : path.Substring(hostIndex);
		}

		public static string GetFtpHost(string path)
		{
			var authority = GetFtpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			return index == -1 ? authority : authority.Substring(0, index);
		}

		public static ushort GetFtpPort(string path)
		{
			var authority = GetFtpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			if (index != -1)
				return ushort.Parse(authority.Substring(index + 1));

			return path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) ? (ushort)990 : (ushort)21;
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

		public static AsyncFtpClient GetFtpClient(string ftpPath)
		{
			var host = GetFtpHost(ftpPath);
			var port = GetFtpPort(ftpPath);
			var credentials = Credentials.Get(host, Anonymous);

			return new(host, credentials, port);
		}

		public static async Task<bool> EnsureConnectedAsync(this AsyncFtpClient ftpClient, CancellationToken cancellationToken = default)
		{
			try
			{
				if (!ftpClient.IsConnected)
					await ftpClient.Connect(cancellationToken);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
