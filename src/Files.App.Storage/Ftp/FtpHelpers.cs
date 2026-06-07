// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Extensions;
using FluentFTP;
using System.Net;

namespace Files.App.Storage
{
	internal static class FtpHelpers
	{
		public static string GetFtpPath(string path)
		{
			path = path.Replace('\', '/');

			var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
			var hostIndex = path.IndexOf('/', schemaIndex);

			return hostIndex == -1 ? "/" : path.Substring(hostIndex);
		}

		public static Task EnsureConnectedAsync(this AsyncFtpClient ftpClient, CancellationToken cancellationToken = default)
		{
			return ftpClient.IsConnected ? Task.CompletedTask : ftpClient.Connect(cancellationToken);
		}

		public static string GetFtpHost(string path)
		{
			var authority = GetFtpAuthority(path);
			var atIndex = authority.IndexOf('@', StringComparison.Ordinal);
			if (atIndex != -1)
			{
				var hostPart = authority.Substring(atIndex + 1);
				var colonIndex = hostPart.IndexOf(':', StringComparison.Ordinal);
				return colonIndex == -1 ? hostPart : hostPart.Substring(0, colonIndex);
			}

			var index = authority.IndexOf(':', StringComparison.Ordinal);
			return index == -1 ? authority : authority.Substring(0, index);
		}

		public static ushort GetFtpPort(string path)
		{
			var authority = GetFtpAuthority(path);
			var atIndex = authority.IndexOf('@', StringComparison.Ordinal);
			var hostPart = atIndex != -1 ? authority.Substring(atIndex + 1) : authority;
			var index = hostPart.IndexOf(':', StringComparison.Ordinal);

			if (index != -1)
				return ushort.Parse(hostPart.Substring(index + 1));

			return path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) ? (ushort)990 : (ushort)21;
		}

		public static string GetFtpAuthority(string path)
		{
			path = path.Replace('\', '/');
			var schemaIndex = path.IndexOf("://", StringComparison.Ordinal) + 3;
			var hostIndex = path.IndexOf('/', schemaIndex);

			if (hostIndex == -1)
				hostIndex = path.Length;

			return path.Substring(schemaIndex, hostIndex - schemaIndex);
		}
		public static NetworkCredential? GetFtpCredentials(string path)
		{
			var authority = GetFtpAuthority(path);
			var atIndex = authority.IndexOf('@', StringComparison.Ordinal);

			if (atIndex == -1)
				return null; // No credentials in URL

			var credentialsPart = authority.Substring(0, atIndex);
			var colonIndex = credentialsPart.IndexOf(':', StringComparison.Ordinal);

			if (colonIndex == -1)
			{
				// Only username, no password
				return new NetworkCredential(Uri.UnescapeDataString(credentialsPart), "");
			}

			var username = Uri.UnescapeDataString(credentialsPart.Substring(0, colonIndex));
			var password = Uri.UnescapeDataString(credentialsPart.Substring(colonIndex + 1));

			return new NetworkCredential(username, password);
		}

		public static AsyncFtpClient GetFtpClient(string ftpPath)
		{
			var host = GetFtpHost(ftpPath);
			var port = GetFtpPort(ftpPath);
			var credentials = GetFtpCredentials(ftpPath) ?? FtpManager.Credentials.Get(host, FtpManager.Anonymous);

			return new(host, credentials, port);
		}
	}
}
