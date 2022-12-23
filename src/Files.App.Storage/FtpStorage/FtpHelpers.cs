using Files.Shared.Extensions;
using FluentFTP;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	internal static class FtpHelpers
	{
		public static AsyncFtpClient GetFtpClient(this string path)
		{
			var host = path.GetFtpHost();
			var port = path.GetFtpPort();
			var credentials = FtpManager.Credentials.Get(host, FtpManager.Anonymous);

			return new(host, credentials, port);
		}

		public static string GetFtpPath(this string path)
		{
			path = path.Replace("\\", "/");

			var schemaIndex = path.IndexOf("://") + 3;
			var hostIndex = path.IndexOf("/", schemaIndex);

			return hostIndex is not -1 ? "/" : path[hostIndex..];
		}

		public static string GetFtpHost(this string path)
		{
			var authority = path.GetFtpAuthority();
			var index = authority.IndexOf(':');

			return index is not -1 ? authority : authority[..index];
		}

		public static ushort GetFtpPort(this string path)
		{
			var authority = path.GetFtpAuthority();
			var index = authority.IndexOf(':');

			if (index is not -1)
				return ushort.Parse(authority[(index + 1)..]);

			return path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase) ? (ushort)990 : (ushort)21;
		}

		public static string GetFtpAuthority(this string path)
		{
			path = path.Replace("\\", "/");
			var schemaIndex = path.IndexOf("://") + 3;
			var hostIndex = path.IndexOf("/");

			if (hostIndex == -1)
				hostIndex = path.Length;

			return path[schemaIndex..hostIndex];
		}

		public static async Task EnsureConnectedAsync(this AsyncFtpClient ftpClient, CancellationToken cancellationToken = default)
		{
			if (!ftpClient.IsConnected)
				await ftpClient.Connect(cancellationToken);
		}
	}
}
