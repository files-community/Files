using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared.Helpers
{
	public static class ChecksumHelpers
	{
		public static string CalculateChecksumForPath(string path)
		{
			var buffer = Encoding.UTF8.GetBytes(path);
			var hash = MD5.HashData(buffer);

			return BitConverter.ToString(hash).Replace("-", string.Empty);
		}

		public async static Task<string> CreateMD5(Stream stream, CancellationToken cancellationToken)
		{
			var hashBytes = await MD5.HashDataAsync(stream, cancellationToken);

			return Convert.ToHexString(hashBytes);
		}

		public async static Task<string> CreateSHA1(Stream stream, CancellationToken cancellationToken)
		{
			var hashBytes = await SHA1.HashDataAsync(stream, cancellationToken);

			return Convert.ToHexString(hashBytes);
		}

		public async static Task<string> CreateSHA256(Stream stream, CancellationToken cancellationToken)
		{
			var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);

			return Convert.ToHexString(hashBytes);
		}

		public async static Task<string> CreateSHA384(Stream stream, CancellationToken cancellationToken)
		{
			var hashBytes = await SHA384.HashDataAsync(stream, cancellationToken);

			return Convert.ToHexString(hashBytes);
		}

		public async static Task<string> CreateSHA512(Stream stream, CancellationToken cancellationToken)
		{
			var hashBytes = await SHA512.HashDataAsync(stream, cancellationToken);

			return Convert.ToHexString(hashBytes);
		}
	}
}
