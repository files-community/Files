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
			var hash = MD5.HashData(buffer);

			return BitConverter.ToString(hash).Replace("-", string.Empty);
		}

		public static string CreateMD5(byte[] fileData)
		{
			var hashBytes = MD5.HashData(fileData);

			return Convert.ToHexString(hashBytes);
		}

		public static string CreateSHA1(byte[] fileData)
		{
			var hashBytes = SHA1.HashData(fileData);

			return Convert.ToHexString(hashBytes);
		}

		public static string CreateSHA256(byte[] fileData)
		{
			var hashBytes = SHA256.HashData(fileData);

			return Convert.ToHexString(hashBytes);
		}

		public static string CreateSHA384(byte[] fileData)
		{
			var hashBytes = SHA384.HashData(fileData);

			return Convert.ToHexString(hashBytes);
		}

		public static string CreateSHA512(byte[] fileData)
		{
			var hashBytes = SHA512.HashData(fileData);

			return Convert.ToHexString(hashBytes);
		}
	}
}
