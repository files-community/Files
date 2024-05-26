// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Storage.FtpStorage;
using Files.Shared.Extensions;
using Renci.SshNet;

namespace Files.App.Storage.SftpStorage
{
	internal static class SftpHelpers
	{
		public static string GetSftpPath(string path) => FtpHelpers.GetFtpPath(path);

		public static Task EnsureConnectedAsync(this SftpClient sftpClient, CancellationToken cancellationToken = default)
			=> sftpClient.IsConnected ? Task.CompletedTask : sftpClient.ConnectAsync(cancellationToken);

		public static string GetSftpAuthority(string path) => FtpHelpers.GetFtpAuthority(path);

		public static string GetSftpHost(string path)
		{
			var authority = GetSftpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			return index == -1 ? authority : authority[..index];
		}

		public static int GetSftpPort(string path)
		{
			var authority = GetSftpAuthority(path);
			var index = authority.IndexOf(':', StringComparison.Ordinal);

			if (index == -1)
				return 22;

			return ushort.Parse(authority[(index + 1)..]);
		}

		public static SftpClient GetSftpClient(string ftpPath)
		{
			var host = GetSftpHost(ftpPath);
			var port = GetSftpPort(ftpPath);
			var credentials = SftpManager.Credentials.Get(host, SftpManager.EmptyCredentials);

			return new(host, port, credentials?.UserName, credentials?.Password);
		}
	}
}
