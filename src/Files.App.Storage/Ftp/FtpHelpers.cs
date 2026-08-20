// Copyright (c) Files Community
// Licensed under the MIT License.

using FluentFTP;

namespace Files.App.Storage
{
	internal static class FtpHelpers
	{
		public static string GetFtpPath(string path)
		{
			// FTP paths are raw: URI query, fragment, and escape characters are valid file name characters.
			path = path.Replace('\\', '/');
			var authority = GetFtpAuthority(path);

			return path.Length == authority.Length ? "/" : path.Substring(authority.Length);
		}

		public static Task EnsureConnectedAsync(this AsyncFtpClient ftpClient, CancellationToken cancellationToken = default)
		{
			return ftpClient.IsConnected ? Task.CompletedTask : ftpClient.Connect(cancellationToken);
		}

		public static string GetFtpHost(string path)
			=> new Uri(GetFtpAuthority(path), UriKind.Absolute).DnsSafeHost;

		public static ushort GetFtpPort(string path)
		{
			var uri = new Uri(GetFtpAuthority(path), UriKind.Absolute);
			if (!uri.IsDefaultPort)
				return checked((ushort)uri.Port);

			return uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase) ? (ushort)990 : (ushort)21;
		}

		public static bool IsSameFtpPath(string firstPath, string secondPath)
		{
			return
				string.Equals(GetFtpHost(firstPath), GetFtpHost(secondPath), StringComparison.OrdinalIgnoreCase) &&
				GetFtpPort(firstPath) == GetFtpPort(secondPath) &&
				string.Equals(GetFtpPath(firstPath), GetFtpPath(secondPath), StringComparison.Ordinal);
		}

		public static AsyncFtpClient GetFtpClient(string ftpPath)
		{
			var host = GetFtpHost(ftpPath);
			var port = GetFtpPort(ftpPath);
			var credentials = FtpManager.Credentials.GetValueOrDefault(host) ?? FtpManager.Anonymous;

			return new(host, credentials, port);
		}

		private static string GetFtpAuthority(string path)
		{
			path = path.Replace('\\', '/');
			var schemeIndex = path.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
			if (schemeIndex < 0)
				throw new UriFormatException("The FTP path does not contain a URI scheme.");

			var pathIndex = path.IndexOf('/', schemeIndex + Uri.SchemeDelimiter.Length);
			return pathIndex < 0 ? path : path[..pathIndex];
		}
	}
}
