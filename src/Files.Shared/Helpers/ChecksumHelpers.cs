using System;
using System.Security.Cryptography;
using System.Text;

namespace Files.Shared.Helpers
{
	public static class ChecksumHelpers
	{
		public static string CalculateChecksumForPath(string path)
		{
			var buffer = Encoding.UTF8.GetBytes(path);
			Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
			MD5.HashData(buffer, hash);
			return Convert.ToHexString(hash);
		}
	}
}
