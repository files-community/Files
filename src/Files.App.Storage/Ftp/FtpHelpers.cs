// Copyright (c) Files Community
// Licensed under the MIT License.

using FluentFTP;

namespace Files.App.Storage
{
	internal static class FtpHelpers
	{
		public static string GetFtpPath(string path)
			=> new Uri(path.Replace('\\', '/'), UriKind.Absolute).LocalPath;

		public static Task EnsureConnectedAsync(this AsyncFtpClient ftpClient, CancellationToken cancellationToken = default)
		{
			return ftpClient.IsConnected ? Task.CompletedTask : ftpClient.Connect(cancellationToken);
		}

		public static string GetFtpHost(string path)
			=> new Uri(path.Replace('\\', '/'), UriKind.Absolute).DnsSafeHost;

		public static ushort GetFtpPort(string path)
		{
			var uri = new Uri(path.Replace('\\', '/'), UriKind.Absolute);
			if (!uri.IsDefaultPort)
				return checked((ushort)uri.Port);

			return uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase) ? (ushort)990 : (ushort)21;
		}

		public static AsyncFtpClient GetFtpClient(string ftpPath)
		{
			var host = GetFtpHost(ftpPath);
			var port = GetFtpPort(ftpPath);
			var credentials = FtpManager.Credentials.Get(host, FtpManager.Anonymous);

			return new(host, credentials, port);
		}
	}
}
