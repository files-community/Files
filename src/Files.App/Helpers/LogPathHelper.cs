// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Files.App.Helpers
{
	public static class LogPathHelper
	{
		public static string GetPathIdentifier(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return "[Empty]";

			try
			{
				using var md5 = MD5.Create();
				var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(path));

				//4 bytes, still low collision 
				var shortHash = BitConverter.ToString(hashBytes, 0, 4).Replace("-", "").ToLowerInvariant();

				var extension = Path.GetExtension(path);

				if (!string.IsNullOrEmpty(extension))
					return $"[hash:{shortHash}{extension}]";
				else
					return $"[hash:{shortHash}]";
			}
			catch
			{
				return "[?]";
			}
		}
	}
}
